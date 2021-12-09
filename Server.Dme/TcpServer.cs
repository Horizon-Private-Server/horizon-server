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
using DotNetty.Handlers.Timeout;

namespace Server.Dme
{
    public class TcpServer
    {
        public static Random RNG = new Random();

        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<TcpServer>();

        public bool IsRunning => _boundChannel != null && _boundChannel.Active;

        public int Port => Program.Settings.TCPPort;

        protected IEventLoopGroup _bossGroup = null;
        protected IEventLoopGroup _workerGroup = null;
        protected IChannel _boundChannel = null;
        protected ScertServerHandler _scertHandler = null;
        private ushort _clientCounter = 0;

        protected internal class ChannelData
        {
            public int ApplicationId { get; set; } = 0;
            public bool Ignore { get; set; } = false;
            public ClientObject ClientObject { get; set; } = null;
            public ConcurrentQueue<BaseScertMessage> RecvQueue { get; } = new ConcurrentQueue<BaseScertMessage>();
            public ConcurrentQueue<BaseScertMessage> SendQueue { get; } = new ConcurrentQueue<BaseScertMessage>();
            public DateTime TimeConnected { get; set; } = Utils.GetHighPrecisionUtcTime();


            /// <summary>
            /// Timesout client if they authenticated after a given number of seconds.
            /// </summary>
            public bool ShouldDestroy => ClientObject == null && (Utils.GetHighPrecisionUtcTime() - TimeConnected).TotalSeconds > Program.Settings.ClientTimeoutSeconds;
        }

        protected ConcurrentQueue<IChannel> _forceDisconnectQueue = new ConcurrentQueue<IChannel>();
        protected ConcurrentDictionary<string, ChannelData> _channelDatas = new ConcurrentDictionary<string, ChannelData>();
        protected ConcurrentDictionary<uint, ClientObject> _scertIdToClient = new ConcurrentDictionary<uint, ClientObject>();

        /// <summary>
        /// Start the Dme Tcp Server.
        /// </summary>
        public virtual async void Start()
        {
            //
            _bossGroup = new MultithreadEventLoopGroup(1);
            _workerGroup = new MultithreadEventLoopGroup();
            _scertHandler = new ScertServerHandler();

            // Add client on connect
            _scertHandler.OnChannelActive += (channel) =>
            {
                string key = channel.Id.AsLongText();
                _channelDatas.TryAdd(key, new ChannelData());
            };

            // Remove client on disconnect
            _scertHandler.OnChannelInactive += (channel) =>
            {
                string key = channel.Id.AsLongText();
                if (_channelDatas.TryRemove(key, out var data))
                {
                    if (data.ClientObject != null)
                    {
                        data.ClientObject.OnTcpDisconnected();
                        _scertIdToClient.TryRemove(data.ClientObject.ScertId, out _);
                    }
                }
            };

            // Queue all incoming messages
            _scertHandler.OnChannelMessage += (channel, message) =>
            {
                string key = channel.Id.AsLongText();
                if (_channelDatas.TryGetValue(key, out var data))
                {
                    if (!data.Ignore && (data.ClientObject == null || !data.ClientObject.IsDestroyed))
                    {
                        data.RecvQueue.Enqueue(message);

                        if (message is RT_MSG_SERVER_ECHO serverEcho)
                            data.ClientObject?.OnRecvServerEcho(serverEcho);
                    }
                }

                // Log if id is set
                if (message.CanLog())
                    Logger.Info($"TCP RECV {data?.ClientObject},{channel}: {message}");
            };

            var bootstrap = new ServerBootstrap();
            bootstrap
                .Group(_bossGroup, _workerGroup)
                .Channel<TcpServerSocketChannel>()
                .Option(ChannelOption.SoBacklog, 100)
                .Handler(new LoggingHandler(LogLevel.INFO))
                .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
                {
                    IChannelPipeline pipeline = channel.Pipeline;

                    pipeline.AddLast(new WriteTimeoutHandler(Program.Settings.ClientTimeoutSeconds));
                    pipeline.AddLast(new ScertEncoder());
                    pipeline.AddLast(new ScertIEnumerableEncoder());
                    pipeline.AddLast(new ScertTcpFrameDecoder(DotNetty.Buffers.ByteOrder.LittleEndian, 1024, 1, 2, 0, 0, false));
                    pipeline.AddLast(new ScertDecoder());
                    pipeline.AddLast(new ScertMultiAppDecoder());
                    pipeline.AddLast(_scertHandler);
                }));

            _boundChannel = await bootstrap.BindAsync(Port);
        }

        /// <summary>
        /// Stop the server.
        /// </summary>
        public virtual async Task Stop()
        {
            try
            {
                await _boundChannel.CloseAsync();
            }
            finally
            {
                await Task.WhenAll(
                        _bossGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)),
                        _workerGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)));
            }
        }

        /// <summary>
        /// Process messages.
        /// </summary>
        public async Task Tick()
        {
            if (_scertHandler == null || _scertHandler.Group == null)
                return;

            await Task.WhenAll(_scertHandler.Group.Select(c => Tick(c)));

            // Disconnect and remove timedout unauthenticated channels
            while (_forceDisconnectQueue.TryDequeue(out var channel))
            {
                // Send disconnect message
                _ = ForceDisconnectClient(channel);

                // Remove
                _channelDatas.TryRemove(channel.Id.AsLongText(), out var d);
                Logger.Warn($"REMOVING CHANNEL {channel},{d},{d?.ClientObject}");

                // close after 5 seconds
                _ = Task.Run(async () =>
                {
                    await Task.Delay(5000);
                    try
                    {
                        await channel?.CloseAsync();
                    }
                    catch (Exception) { }
                });
            }
        }

        private async Task Tick(IChannel clientChannel)
        {
            if (clientChannel == null)
                return;

            // 
            List<BaseScertMessage> responses = new List<BaseScertMessage>();
            string key = clientChannel.Id.AsLongText();

            try
            {
                // 
                if (_channelDatas.TryGetValue(key, out var data))
                {
                    // Destroy
                    if (data.ShouldDestroy)
                    {
                        _forceDisconnectQueue.Enqueue(clientChannel);
                        return;
                    }

                    // Disconnect on destroy
                    if (data.ClientObject != null && data.ClientObject.IsDestroyed)
                    {
                        data.Ignore = true;
                        return;
                    }

                    // Process all messages in queue
                    while (data.RecvQueue.TryDequeue(out var message))
                    {
                        try
                        {
                            await ProcessMessage(message, clientChannel, data);
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e);
                            _ = ForceDisconnectClient(clientChannel);
                        }
                    }

                    // Send if writeable
                    if (clientChannel.IsWritable)
                    {
                        // Add send queue to responses
                        while (data.SendQueue.TryDequeue(out var message))
                            responses.Add(message);

                        if (data.ClientObject != null)
                        {
                            // Echo
                            if ((Utils.GetHighPrecisionUtcTime() - data.ClientObject.UtcLastServerEchoSent).TotalSeconds > Program.Settings.ServerEchoInterval)
                            {
                                responses.Add(new RT_MSG_SERVER_ECHO());
                                data.ClientObject.UtcLastServerEchoSent = Utils.GetHighPrecisionUtcTime();
                            }

                            // Add client object's send queue to responses
                            // But only if not in a world
                            if (data.ClientObject.DmeWorld == null || data.ClientObject.DmeWorld.Destroyed)
                                while (data.ClientObject.TcpSendMessageQueue.TryDequeue(out var message))
                                    responses.Add(message);
                        }

                        //
                        if (responses.Count > 0)
                            _ = clientChannel.WriteAndFlushAsync(responses);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        #region Message Processing

        protected async Task ProcessMessage(BaseScertMessage message, IChannel clientChannel, ChannelData data)
        {
            // Get ScertClient data
            var scertClient = clientChannel.GetAttribute(Server.Pipeline.Constants.SCERT_CLIENT).Get();
            scertClient.CipherService.EnableEncryption = Program.Settings.EncryptMessages;

            // 
            switch (message)
            {
                case RT_MSG_CLIENT_HELLO clientHello:
                    {
                        // initialize default key
                        scertClient.CipherService.SetCipher(CipherContext.RSA_AUTH, scertClient.GetDefaultRSAKey(Program.Settings.DefaultKey));

                        // send hello
                        Queue(new RT_MSG_SERVER_HELLO() { RsaPublicKey = Program.Settings.EncryptMessages ? Program.Settings.DefaultKey.N : Org.BouncyCastle.Math.BigInteger.Zero }, clientChannel);
                        break;
                    }
                case RT_MSG_CLIENT_CRYPTKEY_PUBLIC clientCryptKeyPublic:
                    {
                        // generate new client session key
                        scertClient.CipherService.GenerateCipher(CipherContext.RSA_AUTH, clientCryptKeyPublic.Key.Reverse().ToArray());
                        scertClient.CipherService.GenerateCipher(CipherContext.RC_CLIENT_SESSION);

                        Queue(new RT_MSG_SERVER_CRYPTKEY_PEER() { Key = scertClient.CipherService.GetPublicKey(CipherContext.RC_CLIENT_SESSION) }, clientChannel);
                        break;
                    }
                case RT_MSG_CLIENT_CONNECT_TCP_AUX_UDP clientConnectTcpAuxUdp:
                    {
                        data.ApplicationId = clientConnectTcpAuxUdp.AppId;
                        data.ClientObject = Program.Manager.GetClientByAccessToken(clientConnectTcpAuxUdp.AccessToken);
                        if (data.ClientObject == null || data.ClientObject.DmeWorld == null || data.ClientObject.DmeWorld.WorldId != clientConnectTcpAuxUdp.ARG1)
                            throw new Exception($"Client connected with invalid world id!");

                        data.ClientObject.ApplicationId = clientConnectTcpAuxUdp.AppId;
                        data.ClientObject.OnTcpConnected(clientChannel);
                        data.ClientObject.ScertId = GenerateNewScertClientId();
                        if (!_scertIdToClient.TryAdd(data.ClientObject.ScertId, data.ClientObject))
                            throw new Exception($"Duplicate scert client id");
                        Queue(new RT_MSG_SERVER_CONNECT_REQUIRE() { Contents = Utils.FromString("0648024802") }, clientChannel);
                        break;
                    }
                case RT_MSG_CLIENT_CONNECT_TCP clientConnectTcp:
                    {
                        data.ApplicationId = clientConnectTcp.AppId;
                        break;
                    }
                case RT_MSG_CLIENT_CONNECT_READY_REQUIRE clientConnectReadyRequire:
                    {
                        if (!scertClient.IsPS3Client)
                        {
                            Queue(new RT_MSG_SERVER_CRYPTKEY_GAME() { Key = scertClient.CipherService.GetPublicKey(CipherContext.RC_CLIENT_SESSION) }, clientChannel);
                        }
                        Queue(new RT_MSG_SERVER_CONNECT_ACCEPT_TCP()
                        {
                            UNK_00 = (ushort)data.ClientObject.DmeId,
                            UNK_02 = data.ClientObject.ScertId,
                            UNK_06 = (ushort)data.ClientObject.DmeWorld.Clients.Count,
                            IP = (clientChannel.RemoteAddress as IPEndPoint)?.Address
                        }, clientChannel);
                        break;
                    }
                case RT_MSG_CLIENT_CONNECT_READY_TCP clientConnectReadyTcp:
                    {
                        // Update recv flag
                        data.ClientObject.RecvFlag = clientConnectReadyTcp.RecvFlag;

                        Queue(new RT_MSG_SERVER_STARTUP_INFO_NOTIFY()
                        {
                            GameHostType = (byte)MGCL_GAME_HOST_TYPE.MGCLGameHostClientServerAuxUDP,
                            Timestamp = (uint)(Utils.GetHighPrecisionUtcTime() - data.ClientObject.DmeWorld.WorldCreatedTimeUtc).TotalMilliseconds
                        }, clientChannel);
                        Queue(new RT_MSG_SERVER_INFO_AUX_UDP()
                        {
                            Ip = Program.SERVER_IP,
                            Port = (ushort)data.ClientObject.UdpPort
                        }, clientChannel);
                        break;
                    }
                case RT_MSG_CLIENT_CONNECT_READY_AUX_UDP connectReadyAuxUdp:
                    {
                        data.ClientObject?.OnConnectionCompleted();
                        data.ClientObject?.DmeWorld.OnPlayerJoined(data.ClientObject);

                        Queue(new RT_MSG_SERVER_CONNECT_COMPLETE()
                        {
                            ARG1 = (ushort)data.ClientObject.DmeWorld.Clients.Count
                        }, clientChannel);

                        Queue(new RT_MSG_SERVER_APP()
                        {
                            Message = new DMEServerVersion()
                            {
                                Version = "2.10.0009"
                            }
                        }, clientChannel);

                        break;
                    }
                case RT_MSG_SERVER_ECHO serverEchoReply:
                    {

                        break;
                    }
                case RT_MSG_CLIENT_ECHO clientEcho:
                    {
                        Queue(new RT_MSG_CLIENT_ECHO() { Value = clientEcho.Value }, clientChannel);
                        break;
                    }
                case RT_MSG_CLIENT_SET_RECV_FLAG setRecvFlag:
                    {
                        data.ClientObject.RecvFlag = setRecvFlag.Flag;
                        break;
                    }
                case RT_MSG_CLIENT_SET_AGG_TIME setAggTime:
                    {
                        data.ClientObject.AggTimeMs = setAggTime.AggTime;
                        break;
                    }
                case RT_MSG_CLIENT_TIMEBASE_QUERY timebaseQuery:
                    {
                        Queue(new RT_MSG_SERVER_TIMEBASE_QUERY_NOTIFY()
                        {
                            ClientTime = timebaseQuery.Timestamp,
                            ServerTime = (uint)(Utils.GetHighPrecisionUtcTime() - data.ClientObject.DmeWorld.WorldCreatedTimeUtc).TotalMilliseconds
                        }, clientChannel);
                        break;
                    }
                case RT_MSG_CLIENT_TOKEN_MESSAGE tokenMessage:
                    {

                        break;
                    }
                case RT_MSG_CLIENT_APP_BROADCAST clientAppBroadcast:
                    {
                        data.ClientObject?.DmeWorld?.BroadcastTcp(data.ClientObject, clientAppBroadcast.Payload);
                        break;
                    }
                case RT_MSG_CLIENT_APP_LIST clientAppList:
                    {
                        data.ClientObject?.DmeWorld?.SendTcpAppList(data.ClientObject, clientAppList.Targets, clientAppList.Payload);
                        break;
                    }
                case RT_MSG_CLIENT_APP_SINGLE clientAppSingle:
                    {
                        data.ClientObject.DmeWorld?.SendTcpAppSingle(data.ClientObject, clientAppSingle.TargetOrSource, clientAppSingle.Payload);
                        break;
                    }
                case RT_MSG_CLIENT_APP_TOSERVER clientAppToServer:
                    {
                        ProcessMediusMessage(clientAppToServer.Message, clientChannel, data);
                        break;
                    }

                case RT_MSG_CLIENT_DISCONNECT_WITH_REASON clientDisconnectWithReason:
                    {
                        _ = clientChannel.CloseAsync();
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

        protected virtual void ProcessMediusMessage(BaseMediusMessage message, IChannel clientChannel, ChannelData data)
        {
            if (message == null)
                return;
        }

        #endregion

        #region Channel

        /// <summary>
        /// Closes the client channel.
        /// </summary>
        protected async Task ForceDisconnectClient(IChannel channel)
        {
            try
            {
                // send force disconnect message
                await channel.WriteAndFlushAsync(new RT_MSG_SERVER_FORCED_DISCONNECT()
                {
                    Reason = SERVER_FORCE_DISCONNECT_REASON.SERVER_FORCED_DISCONNECT_ERROR
                });

                // close channel
                await channel.CloseAsync();
            }
            catch (Exception e)
            {
                // Silence exception since the client probably just closed the socket before we could write to it
            }
            finally
            {

            }
        }

        #endregion

        #region Queue

        public void Queue(BaseScertMessage message, params IChannel[] clientChannels)
        {
            Queue(message, (IEnumerable<IChannel>)clientChannels);
        }

        public void Queue(BaseScertMessage message, IEnumerable<IChannel> clientChannels)
        {
            foreach (var clientChannel in clientChannels)
                if (clientChannel != null)
                    if (_channelDatas.TryGetValue(clientChannel.Id.AsLongText(), out var data))
                        data.SendQueue.Enqueue(message);
        }

        public void Queue(IEnumerable<BaseScertMessage> messages, params IChannel[] clientChannels)
        {
            Queue(messages, (IEnumerable<IChannel>)clientChannels);
        }

        public void Queue(IEnumerable<BaseScertMessage> messages, IEnumerable<IChannel> clientChannels)
        {
            foreach (var clientChannel in clientChannels)
                if (clientChannel != null)
                    if (_channelDatas.TryGetValue(clientChannel.Id.AsLongText(), out var data))
                        foreach (var message in messages)
                            data.SendQueue.Enqueue(message);
        }

        #endregion

        public ClientObject GetClientByScertId(ushort scertId)
        {
            if (_scertIdToClient.TryGetValue(scertId, out var result))
                return result;

            return null;
        }

        protected ushort GenerateNewScertClientId()
        {
            return _clientCounter++;
        }
    }
}
