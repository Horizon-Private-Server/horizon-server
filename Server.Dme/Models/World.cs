using DotNetty.Common.Internal.Logging;
using Microsoft.Extensions.Logging;
using RT.Common;
using RT.Models;
using Server.Dme.PluginArgs;
using Server.Plugins.Interface;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Dme.Models
{
    public class World : IDisposable
    {
        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<World>();

        public const int MAX_WORLDS = 256;
        public const int MAX_CLIENTS_PER_WORLD = 10;

         #region Id Management

        private static ConcurrentDictionary<int, World> _idToWorld = new ConcurrentDictionary<int, World>();
        private ConcurrentDictionary<int, bool> _pIdIsUsed = new ConcurrentDictionary<int, bool>();

        private void RegisterWorld()
        {
            int i = 0;
            while (i < MAX_WORLDS && _idToWorld.ContainsKey(i))
                ++i;

            // 
            if (i == MAX_WORLDS)
                throw new InvalidOperationException("Max worlds reached!");

            // 
            WorldId = i;
            _idToWorld.TryAdd(i, this);
            Logger.Info($"Registered world with id {i}");
        }

        private void FreeWorld()
        {
            _idToWorld.TryRemove(WorldId, out _);
            Logger.Info($"Unregistered world with id {WorldId}");
        }

        private bool TryRegisterNewClientIndex(out int index)
        {
            for (index = 0; index < _pIdIsUsed.Count; ++index)
            {
                if (_pIdIsUsed.TryGetValue(index, out var isUsed) && !isUsed)
                {
                    _pIdIsUsed[index] = true;
                    return true;
                }
            }

            return false;
        }

        public void UnregisterClientIndex(int index)
        {
            _pIdIsUsed[index] = false;
        }

        #endregion

        public int WorldId { get; protected set; } = -1;

        public int MaxPlayers { get; protected set; } = 0;

        public bool SelfDestructFlag { get; protected set; } = false;

        public bool ForceDestruct { get; protected set; } = false;

        public bool Timedout => !WorldTimeUtc.HasValue && (Server.Common.Utils.GetHighPrecisionUtcTime() - WorldCreatedTimeUtc).TotalSeconds > Program.Settings.GameTimeoutSeconds;

        public bool Destroy => (Timedout || SelfDestructFlag) && Clients.Count == 0;
        public bool Destroyed { get; protected set; } = false;

        public DateTime WorldCreatedTimeUtc { get; protected set; } = Server.Common.Utils.GetHighPrecisionUtcTime();
        public DateTime? WorldTimeUtc { get; protected set; } = null;

        public ConcurrentDictionary<int, ClientObject> Clients = new ConcurrentDictionary<int, ClientObject>();

        public MediusManager Manager { get; } = null;
        
        public World(MediusManager manager, int maxPlayers)
        {
            Manager = manager;

            // populate collection of used player ids
            for (int i = 0; i < MAX_CLIENTS_PER_WORLD; ++i)
                _pIdIsUsed.TryAdd(i, false);

            RegisterWorld();
            this.MaxPlayers = maxPlayers;
        }

        public void Dispose()
        {
            FreeWorld();
            Destroyed = true;
        }

        public async Task Stop()
        {
            // Stop all clients
            await Task.WhenAll(Clients.Select(x => x.Value.Stop()));

            Dispose();
        }

        public async Task Tick()
        {
            // Process clients
            for (int i = 0; i < MAX_CLIENTS_PER_WORLD; ++i)
            {
                if (Clients.TryGetValue(i, out var client))
                {
                    if (client.Destroy || ForceDestruct || Destroyed)
                    {
                        OnPlayerLeft(client);
                        Manager.RemoveClient(client);
                        _ = client.Stop();
                        Clients.TryRemove(i, out _);
                    }
                    else if (client.IsAggTime)
                    {
                        client.Tick();
                    }
                }
            }

            // Remove
            if (Destroy)
            {
                if (!Destroyed)
                {
                    Logger.Info($"{this} destroyed.");
                    await Stop();
                }

                Manager.RemoveWorld(this);
            }
        }

        #region Send

        public void BroadcastTcp(ClientObject source, byte[] Payload)
        {
            var msg = new RT_MSG_CLIENT_APP_SINGLE()
            {
                TargetOrSource = (short)source.DmeId,
                Payload = Payload
            };

            foreach (var client in Clients)
            {
                if (client.Value == source || !client.Value.IsAuthenticated || !client.Value.IsConnected || !client.Value.RecvFlag.HasFlag(RT_RECV_FLAG.RECV_BROADCAST))
                    continue;

                client.Value.EnqueueTcp(msg);
            }
        }

        public void BroadcastUdp(ClientObject source, byte[] Payload)
        {
            var msg = new RT_MSG_CLIENT_APP_SINGLE()
            {
                TargetOrSource = (short)source.DmeId,
                Payload = Payload
            };

            foreach (var client in Clients)
            {
                if (client.Value == source || !client.Value.IsAuthenticated || !client.Value.IsConnected || !client.Value.RecvFlag.HasFlag(RT_RECV_FLAG.RECV_BROADCAST))
                    continue;

                client.Value.EnqueueUdp(msg);
            }
        }

        public void SendTcpAppList(ClientObject source, IEnumerable<int> targetDmeIds, byte[] Payload)
        {
            foreach (var targetId in targetDmeIds)
            {
                if (Clients.TryGetValue(targetId, out var client))
                {
                    if (client == null || !client.IsAuthenticated || !client.IsConnected || !client.RecvFlag.HasFlag(RT_RECV_FLAG.RECV_LIST))
                        continue;

                    client.EnqueueTcp(new RT_MSG_CLIENT_APP_SINGLE()
                    {
                        TargetOrSource = (short)source.DmeId,
                        Payload = Payload
                    });
                }
            }
        }

        public void SendUdpAppList(ClientObject source, IEnumerable<int> targetDmeIds, byte[] Payload)
        {
            foreach (var targetId in targetDmeIds)
            {
                if (Clients.TryGetValue(targetId, out var client))
                {
                    if (client == null || !client.IsAuthenticated || !client.IsConnected || !client.RecvFlag.HasFlag(RT_RECV_FLAG.RECV_LIST))
                        continue;

                    client.EnqueueUdp(new RT_MSG_CLIENT_APP_SINGLE()
                    {
                        TargetOrSource = (short)source.DmeId,
                        Payload = Payload
                    });
                }
            }
        }

        public void SendTcpAppSingle(ClientObject source, short targetDmeId, byte[] Payload)
        {
            var target = Clients.FirstOrDefault(x => x.Value.DmeId == targetDmeId).Value;

            if (target != null && target.IsAuthenticated && target.IsConnected && target.RecvFlag.HasFlag(RT_RECV_FLAG.RECV_SINGLE))
            {
                target.EnqueueTcp(new RT_MSG_CLIENT_APP_SINGLE()
                {
                    TargetOrSource = (short)source.DmeId,
                    Payload = Payload
                });
            }
        }

        public void SendUdpAppSingle(ClientObject source, short targetDmeId, byte[] Payload)
        {
            var target = Clients.FirstOrDefault(x => x.Value.DmeId == targetDmeId).Value;

            if (target != null && target.IsAuthenticated && target.IsConnected && target.RecvFlag.HasFlag(RT_RECV_FLAG.RECV_SINGLE))
            {
                target.EnqueueUdp(new RT_MSG_CLIENT_APP_SINGLE()
                {
                    TargetOrSource = (short)source.DmeId,
                    Payload = Payload
                });
            }
        }

        #endregion

        #region Message Handlers

        public void OnEndGameRequest(MediusServerEndGameRequest request)
        {
            SelfDestructFlag = true;
            ForceDestruct = request.BrutalFlag;
        }

        public void OnPlayerJoined(ClientObject player)
        {
            // Plugin
            Program.Plugins.OnEvent(PluginEvent.DME_PLAYER_ON_JOINED, new OnPlayerArgs()
            {
                Player = player,
                Game = this
            });

            // Tell other clients
            foreach (var client in Clients)
            {
                if (client.Value == player || !client.Value.RecvFlag.HasFlag(RT_RECV_FLAG.RECV_NOTIFICATION))
                    continue;

                client.Value.EnqueueTcp(new RT_MSG_SERVER_CONNECT_NOTIFY()
                {
                    PlayerIndex = (short)player.DmeId,
                    ScertId = (short)player.ScertId,
                    IP = player.RemoteUdpEndpoint?.Address
                });
            }

            // Tell server
            Manager.Enqueue(new MediusServerConnectNotification()
            {
                MediusWorldUID = (uint)WorldId,
                PlayerSessionKey = player.SessionKey,
                ConnectEventType = MGCL_EVENT_TYPE.MGCL_EVENT_CLIENT_CONNECT
            });
        }

        public void OnPlayerLeft(ClientObject player)
        {
            // Plugin
            Program.Plugins.OnEvent(PluginEvent.DME_PLAYER_ON_LEFT, new OnPlayerArgs()
            {
                Player = player,
                Game = this
            });

            // Tell other clients
            foreach (var client in Clients)
            {
                if (client.Value == player || !client.Value.RecvFlag.HasFlag(RT_RECV_FLAG.RECV_NOTIFICATION))
                    continue;

                client.Value.EnqueueTcp(new RT_MSG_SERVER_DISCONNECT_NOTIFY()
                {
                    PlayerIndex = (short)player.DmeId,
                    ScertId = (short)player.ScertId,
                    IP = player.RemoteUdpEndpoint?.Address
                });
            }

            // Tell server
            Manager.Enqueue(new MediusServerConnectNotification()
            {
                MediusWorldUID = (uint)WorldId,
                PlayerSessionKey = player.SessionKey,
                ConnectEventType = MGCL_EVENT_TYPE.MGCL_EVENT_CLIENT_DISCONNECT
            });
        }

        public MediusServerJoinGameResponse OnJoinGameRequest(MediusServerJoinGameRequest request)
        {
            ClientObject newClient;

            // If world is full then fail
            if (Clients.Count >= MAX_CLIENTS_PER_WORLD)
            {
                Logger.Warn($"Player attempted to join world {this} but there is no room!");
                return new MediusServerJoinGameResponse()
                {
                    MessageID = request.MessageID,
                    Confirmation = MGCL_ERROR_CODE.MGCL_UNSUCCESSFUL
                };
            }

            // Client already added
            if (TryRegisterNewClientIndex(out var newClientIndex))
            {
                if (!Clients.TryAdd(newClientIndex, newClient = new ClientObject(request.ConnectInfo.SessionKey, this, newClientIndex)))
                {
                    UnregisterClientIndex(newClientIndex);
                    return new MediusServerJoinGameResponse()
                    {
                        MessageID = request.MessageID,
                        Confirmation = MGCL_ERROR_CODE.MGCL_UNSUCCESSFUL
                    };
                }
                else
                {
                    newClient.OnDestroyed += (client) => { UnregisterClientIndex(client.DmeId); };
                }
            }
            else
            {
                Logger.Warn($"Player attempted to join world {this} but unable to add player!");
                return new MediusServerJoinGameResponse()
                {
                    MessageID = request.MessageID,
                    Confirmation = MGCL_ERROR_CODE.MGCL_UNSUCCESSFUL
                };
            }

            // Add client to manager
            Manager.AddClient(newClient);

            return new MediusServerJoinGameResponse()
            {
                MessageID = request.MessageID,
                DmeClientIndex = newClient.DmeId,
                AccessKey = newClient.Token,
                Confirmation = MGCL_ERROR_CODE.MGCL_SUCCESS
            };
        }

        #endregion

        public override string ToString()
        {
            return $"WorldId:{WorldId}, ClientCount:{Clients.Count}";
        }

    }
}
