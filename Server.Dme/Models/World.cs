using DotNetty.Common.Internal.Logging;
using Microsoft.Extensions.Logging;
using RT.Common;
using RT.Models;
using Server.Dme.PluginArgs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        private int GetFreeClientIndex()
        {
            bool[] usedIds = new bool[MAX_CLIENTS_PER_WORLD];

            foreach (var client in Clients)
                usedIds[client.Value.DmeId] = true;

            for (int i = 0; i < usedIds.Length; ++i)
                if (!usedIds[i])
                    return i;

            throw new Exception($"Game is full");
        }

        #endregion

        public int WorldId { get; protected set; } = -1;

        public int MaxPlayers { get; protected set; } = 0;

        public bool SelfDestructFlag { get; protected set; } = false;

        public bool ForceDestruct { get; protected set; } = false;

        public bool Timedout => !WorldTimeUtc.HasValue && (DateTime.UtcNow - WorldCreatedTimeUtc).TotalSeconds > Program.Settings.GameTimeoutSeconds;

        public bool Destroy => (Timedout || SelfDestructFlag) && Clients.Count == 0;
        public bool Destroyed { get; protected set; } = false;

        public DateTime WorldCreatedTimeUtc { get; protected set; } = DateTime.UtcNow;
        public DateTime? WorldTimeUtc { get; protected set; } = null;

        public ConcurrentDictionary<int, ClientObject> Clients = new ConcurrentDictionary<int, ClientObject>();


        public World(int maxPlayers)
        {
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

        public async Task TickUdp()
        {
            // Process clients
            await Task.WhenAll(Clients.Select(x => x.Value.Udp?.Tick()));
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
                        Program.Manager.RemoveClient(client);
                        await client.Stop();
                        Clients.TryRemove(i, out _);
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

                Program.Manager.RemoveWorld(this);
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
                if (client.Value == source)
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
                if (client.Value == source)
                    continue;

                client.Value.EnqueueUdp(msg);
            }
        }

        public void SendTcpAppSingle(ClientObject source, short targetDmeId, byte[] Payload)
        {
            var target = Clients.FirstOrDefault(x => x.Value.DmeId == targetDmeId).Value;

            if (target != null)
            {
                target.EnqueueTcp(new RT_MSG_CLIENT_APP_SINGLE()
                {
                    TargetOrSource = (short)source.DmeId,
                    Payload = Payload
                });
            }
            else
            {
                
            }
        }

        public void SendUdpAppSingle(ClientObject source, short targetDmeId, byte[] Payload)
        {
            var target = Clients.FirstOrDefault(x => x.Value.DmeId == targetDmeId).Value;

            if (target != null)
            {
                target.EnqueueUdp(new RT_MSG_CLIENT_APP_SINGLE()
                {
                    TargetOrSource = (short)source.DmeId,
                    Payload = Payload
                });
            }
            else
            {

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
            Program.Plugins.OnEvent(Plugins.PluginEvent.DME_PLAYER_ON_JOINED, new OnPlayerArgs()
            {
                Player = player,
                Game = this
            });

            // Tell other clients
            foreach (var client in Clients)
            {
                if (client.Value == player)
                    continue;

                client.Value.EnqueueTcp(new RT_MSG_SERVER_CONNECT_NOTIFY()
                {
                    PlayerIndex = (short)player.DmeId,
                    ScertId = (short)player.ScertId,
                    IP = player.RemoteUdpEndpoint?.Address
                });
            }

            // Tell server
            Program.Manager.Enqueue(new MediusServerConnectNotification()
            {
                MediusWorldUID = (uint)WorldId,
                PlayerSessionKey = player.SessionKey,
                ConnectEventType = MGCL_EVENT_TYPE.MGCL_EVENT_CLIENT_CONNECT
            });
        }

        public void OnPlayerLeft(ClientObject player)
        {
            // Plugin
            Program.Plugins.OnEvent(Plugins.PluginEvent.DME_PLAYER_ON_LEFT, new OnPlayerArgs()
            {
                Player = player,
                Game = this
            });

            // Tell other clients
            foreach (var client in Clients)
            {
                if (client.Value == player)
                    continue;

                client.Value.EnqueueTcp(new RT_MSG_SERVER_DISCONNECT_NOTIFY()
                {
                    PlayerIndex = (short)player.DmeId,
                    ScertId = (short)player.ScertId,
                    IP = player.RemoteUdpEndpoint?.Address
                });
            }

            // Tell server
            Program.Manager.Enqueue(new MediusServerConnectNotification()
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
                return new MediusServerJoinGameResponse()
                {
                    MessageID = request.MessageID,
                    Confirmation = MGCL_ERROR_CODE.MGCL_UNSUCCESSFUL
                };
            }

            // Client already added
            var newClientIndex = GetFreeClientIndex();
            if (!Clients.TryAdd(newClientIndex, newClient = new ClientObject(request.ConnectInfo.SessionKey, this, newClientIndex)))
            {
                return new MediusServerJoinGameResponse()
                {
                    MessageID = request.MessageID,
                    Confirmation = MGCL_ERROR_CODE.MGCL_UNSUCCESSFUL
                };
            }

            // Add client to manager
            Program.Manager.AddClient(newClient);

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
