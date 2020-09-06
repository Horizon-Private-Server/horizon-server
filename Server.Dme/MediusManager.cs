using DotNetty.Codecs;
using DotNetty.Codecs.Json;
using DotNetty.Common.Internal.Logging;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using RT.Common;
using RT.Cryptography;
using RT.Models;
using Server.Pipeline.Tcp;
using Server.Common;
using Server.Dme.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Server.Dme
{
    public class MediusManager
    {
        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<MediusManager>();

        public bool IsConnected => _mpsChannel != null && _mpsChannel.Active && _mpsState > 0;
        public DateTime? TimeLostConnection { get; set; } = null;

        private enum MPSConnectionState
        {
            FAILED = -1,
            NO_CONNECTION,
            CONNECTED,
            HELLO,
            HANDSHAKE,
            CONNECT_TCP,
            SET_ATTRIBUTES,
            AUTHENTICATED
        }

        private ConcurrentDictionary<string, ClientObject> _accessTokenToClient = new ConcurrentDictionary<string, ClientObject>();
        private ConcurrentDictionary<string, ClientObject> _sessionKeyToClient = new ConcurrentDictionary<string, ClientObject>();

        private PS2_RSA _serverKey => Program.GlobalAuthKey;
        private PS2_RSA _clientKey => Program.Settings.MPS.Key;
        private PS2_RC4 _sessionCipher = null;

        private DateTime _utcConnectionState;
        private MPSConnectionState _mpsState = MPSConnectionState.NO_CONNECTION;

        private IEventLoopGroup _group = null;
        private IChannel _mpsChannel = null;
        private Bootstrap _bootstrap = null;
        private ScertServerHandler _scertHandler = null;

        private List<World> _worlds = new List<World>();
        private ConcurrentQueue<World> _removeWorldQueue = new ConcurrentQueue<World>();

        private ConcurrentQueue<BaseScertMessage> _mpsRecvQueue { get; } = new ConcurrentQueue<BaseScertMessage>();
        private ConcurrentQueue<BaseScertMessage> _mpsSendQueue { get; } = new ConcurrentQueue<BaseScertMessage>();

        #region Clients

        public ClientObject GetClientByAccessToken(string accessToken)
        {
            if (_accessTokenToClient.TryGetValue(accessToken, out var result))
                return result;

            return null;
        }
        public ClientObject GetClientBySessionKey(string sessionKey)
        {
            if (_sessionKeyToClient.TryGetValue(sessionKey, out var result))
                return result;

            return null;
        }


        public void AddClient(ClientObject client)
        {
            if (client.Destroy)
                throw new InvalidOperationException($"Attempting to add {client} to MediusManager but client is ready to be destroyed.");

            if (_accessTokenToClient.TryAdd(client.Token, client))
            {
                if (!_sessionKeyToClient.TryAdd(client.SessionKey, client))
                {
                    _accessTokenToClient.TryRemove(client.Token, out _);
                }
            }
        }

        public void RemoveClient(ClientObject client)
        {
            if (client == null)
                return;

            _sessionKeyToClient.TryRemove(client.SessionKey, out _);
            _accessTokenToClient.TryRemove(client.Token, out _);
        }

        #endregion


        #region MPS Client

        public async Task Start()
        {
            _group = new MultithreadEventLoopGroup();
            _scertHandler = new ScertServerHandler();

            TimeLostConnection = null;

            // Add client on connect
            _scertHandler.OnChannelActive += (channel) =>
            {

            };

            // Remove client on disconnect
            _scertHandler.OnChannelInactive += async (channel) =>
            {
                Logger.Error($"Lost connection to MPS");
                TimeLostConnection = DateTime.UtcNow;
                await Stop();
            };

            // Queue all incoming messages
            _scertHandler.OnChannelMessage += (channel, message) =>
            {
                // Add to queue
                _mpsRecvQueue.Enqueue(message);

                // Log if id is set
                if (message.CanLog())
                    Logger.Info($"MPS RECV {channel}: {message}");
            };

            _bootstrap = new Bootstrap();
            _bootstrap
                .Group(_group)
                .Channel<TcpSocketChannel>()
                .Option(ChannelOption.TcpNodelay, true)
                .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                {
                    IChannelPipeline pipeline = channel.Pipeline;

                    pipeline.AddLast(new ScertEncoder());
                    pipeline.AddLast(new ScertIEnumerableEncoder());
                    pipeline.AddLast(new ScertTcpFrameDecoder(DotNetty.Buffers.ByteOrder.LittleEndian, Constants.MEDIUS_MESSAGE_MAXLEN, 1, 2, 0, 0, false));
                    pipeline.AddLast(new ScertDecoder(_sessionCipher, _serverKey));
                    pipeline.AddLast(_scertHandler);
                }));

            await ConnectMPS();
        }

        public async Task Stop()
        {
            await Task.WhenAll(_worlds.Select(x => x.Stop()));
            await _group.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));

            // 
            _worlds.Clear();
            _removeWorldQueue.Clear();
            _mpsRecvQueue.Clear();
            _mpsSendQueue.Clear();
        }

        public async Task TickUdp()
        {
            if (_mpsChannel == null)
                return;

            await Task.WhenAll(_worlds.Select(x => x.TickUdp()));
        }

        public async Task Tick()
        {
            if (_mpsChannel == null)
                return;

            // 
            List<BaseScertMessage> responses = new List<BaseScertMessage>();

            //
            if (_mpsState == MPSConnectionState.FAILED || 
                (_mpsState != MPSConnectionState.AUTHENTICATED && (DateTime.UtcNow - _utcConnectionState).TotalSeconds > 30))
                throw new Exception("Failed to authenticate with the MPS server.");

            try
            {
                // Process all messages in queue
                while (_mpsRecvQueue.TryDequeue(out var message))
                {
                    try
                    {
                        await ProcessMessage(message, _mpsChannel);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                    }
                }

                // Process each world
                await Task.WhenAll(_worlds.Select(x => x.Tick()));

                // Handle world removals
                while (_removeWorldQueue.TryDequeue(out var world))
                    _worlds.Remove(world);

                // Send if writeable
                if (_mpsChannel.IsWritable)
                {
                    // Add send queue to responses
                    while (_mpsSendQueue.TryDequeue(out var message))
                        responses.Add(message);


                    //
                    if (responses.Count > 0)
                        await _mpsChannel.WriteAndFlushAsync(responses);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);

            }
        }

        private async Task ConnectMPS()
        {
            _utcConnectionState = DateTime.UtcNow;
            _mpsState = MPSConnectionState.NO_CONNECTION;

            try
            {
                _mpsChannel = await _bootstrap.ConnectAsync(new IPEndPoint(Utils.GetIp(Program.Settings.MPS.Ip), Program.Settings.MPS.Port));
            }
            catch (Exception)
            {
                Logger.Error($"Failed to connect to MPS");
                TimeLostConnection = DateTime.UtcNow;
                return;
            }

            if (!_mpsChannel.Active)
                return;

            _mpsState = MPSConnectionState.CONNECTED;

            // Send hello
            await _mpsChannel.WriteAndFlushAsync(new RT_MSG_CLIENT_HELLO()
            {
                Parameters = new ushort[]
                {
                    2,
                    0x6e,
                    0x6d,
                    1,
                    1
                }
            });

            _mpsState = MPSConnectionState.HELLO;
        }

        private async Task ProcessMessage(BaseScertMessage message, IChannel serverChannel)
        {
            // 
            switch (message)
            {
                // Authentication
                case RT_MSG_SERVER_HELLO serverHello:
                    {
                        if (_mpsState != MPSConnectionState.HELLO)
                            throw new Exception($"Unexpected RT_MSG_SERVER_HELLO from server. {serverHello}");

                        // Send public key
                        Enqueue(new RT_MSG_CLIENT_CRYPTKEY_PUBLIC()
                        {
                            Key = _clientKey.N.ToByteArrayUnsigned().Reverse().ToArray()
                        });

                        _mpsState = MPSConnectionState.HANDSHAKE;
                        break;
                    }
                case RT_MSG_SERVER_CRYPTKEY_PEER serverCryptKeyPeer:
                    {
                        if (_mpsState != MPSConnectionState.HANDSHAKE)
                            throw new Exception($"Unexpected RT_MSG_SERVER_CRYPTKEY_PEER from server. {serverCryptKeyPeer}");

                        await _mpsChannel.WriteAndFlushAsync(new RT_MSG_CLIENT_CONNECT_TCP()
                        {
                            AppId = Program.Settings.ApplicationId
                        });

                        _mpsState = MPSConnectionState.CONNECT_TCP;
                        break;
                    }
                case RT_MSG_SERVER_CONNECT_ACCEPT_TCP serverConnectAcceptTcp:
                    {
                        if (_mpsState != MPSConnectionState.CONNECT_TCP)
                            throw new Exception($"Unexpected RT_MSG_SERVER_CONNECT_ACCEPT_TCP from server. {serverConnectAcceptTcp}");

                        // Send attributes
                        await _mpsChannel.WriteAndFlushAsync(new RT_MSG_CLIENT_APP_TOSERVER()
                        {
                            Message = new MediusServerSetAttributesRequest()
                            {
                                MessageID = new MessageId(),
                                ListenServerAddress = new NetAddress()
                                {
                                    Address = Program.SERVER_IP.ToString(),
                                    Port = (uint)Program.TcpServer.Port
                                }
                            }
                        });

                        _mpsState = MPSConnectionState.SET_ATTRIBUTES;
                        break;
                    }

                // 
                case RT_MSG_SERVER_ECHO serverEcho:
                    {
                        Enqueue(serverEcho);
                        break;
                    }
                case RT_MSG_CLIENT_ECHO clientEcho:
                    {
                        Enqueue(new RT_MSG_CLIENT_ECHO() { Value = clientEcho.Value });
                        break;
                    }
                case RT_MSG_SERVER_APP serverApp:
                    {
                        ProcessMediusMessage(serverApp.Message, serverChannel);
                        break;
                    }

                case RT_MSG_SERVER_FORCED_DISCONNECT serverForcedDisconnect:
                case RT_MSG_CLIENT_DISCONNECT_WITH_REASON clientDisconnectWithReason:
                    {
                        await serverChannel.CloseAsync();
                        _mpsState = MPSConnectionState.NO_CONNECTION;
                        break;
                    }
                default:
                    {
                        Logger.Warn($"UNHANDLED MESSAGE: {message}");

                        break;
                    }
            }

            return;
        }

        private void ProcessMediusMessage(BaseMediusMessage message, IChannel clientChannel)
        {
            if (message == null)
                return;

            switch (message)
            {
                // 
                case MediusServerSetAttributesResponse setAttributesResponse:
                    {
                        if (_mpsState != MPSConnectionState.SET_ATTRIBUTES)
                            throw new Exception($"Unexpected MediusServerSetAttributesResponse from server. {setAttributesResponse}");

                        if (setAttributesResponse.Confirmation == MGCL_ERROR_CODE.MGCL_SUCCESS)
                            _mpsState = MPSConnectionState.AUTHENTICATED;
                        else
                            _mpsState = MPSConnectionState.FAILED;
                        break;
                    }

                //
                case MediusServerCreateGameWithAttributesRequest createGameWithAttributesRequest:
                    {
                        World world = new World(createGameWithAttributesRequest.MaxClients);
                        _worlds.Add(world);

                        Enqueue(new MediusServerCreateGameWithAttributesResponse()
                        {
                            MessageID = createGameWithAttributesRequest.MessageID,
                            Confirmation = MGCL_ERROR_CODE.MGCL_SUCCESS,
                            WorldID = world.WorldId
                        });
                        break;
                    }
                case MediusServerJoinGameRequest joinGameRequest:
                    {
                        var world = _worlds.FirstOrDefault(x => x.WorldId == joinGameRequest.ConnectInfo.WorldID);
                        if (world == null)
                        {
                            Enqueue(new MediusServerJoinGameResponse()
                            {
                                MessageID = joinGameRequest.MessageID,
                                Confirmation = MGCL_ERROR_CODE.MGCL_INVALID_ARG,
                            });
                        }
                        else
                        {
                            Enqueue(world.OnJoinGameRequest(joinGameRequest));
                        }
                        break;
                    }
                case MediusServerEndGameRequest endGameRequest:
                    {
                        _worlds.FirstOrDefault(x => x.WorldId == endGameRequest.WorldID)?.OnEndGameRequest(endGameRequest);

                        break;
                    }
                default:
                    {
                        Logger.Warn($"UNHANDLED MESSAGE: {message}");

                        break;
                    }
            }
        }

        #region Queue

        public void Enqueue(BaseScertMessage message)
        {
            _mpsSendQueue.Enqueue(message);
        }

        public void Enqueue(IEnumerable<BaseScertMessage> messages)
        {
            foreach (var message in messages)
                _mpsSendQueue.Enqueue(message);
        }

        public void Enqueue(BaseMediusMessage message)
        {
            _mpsSendQueue.Enqueue(new RT_MSG_CLIENT_APP_TOSERVER() { Message = message });
        }

        #endregion


        #endregion

        #region World Manager

        public void RemoveWorld(World world)
        {
            if (world != null)
                _removeWorldQueue.Enqueue(world);
        }

        #endregion

    }
}
