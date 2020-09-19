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
    public class TcpServer
    {
        public static Random RNG = new Random();

        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<TcpServer>();

        public bool IsRunning => _boundChannel != null && _boundChannel.Active;

        public int Port => Program.Settings.TCPPort;
        public PS2_RSA AuthKey => Program.GlobalAuthKey;

        protected IEventLoopGroup _bossGroup = null;
        protected IEventLoopGroup _workerGroup = null;
        protected IChannel _boundChannel = null;
        protected ScertServerHandler _scertHandler = null;
        private ushort _clientCounter = 0;

        protected internal class ChannelData
        {
            public int ApplicationId { get; set; } = 0;
            public ClientObject ClientObject { get; set; } = null;
            public ConcurrentQueue<BaseScertMessage> RecvQueue { get; } = new ConcurrentQueue<BaseScertMessage>();
            public ConcurrentQueue<BaseScertMessage> SendQueue { get; } = new ConcurrentQueue<BaseScertMessage>();
        }

        protected ConcurrentDictionary<string, ChannelData> _channelDatas = new ConcurrentDictionary<string, ChannelData>();
        protected ConcurrentDictionary<ushort, ClientObject> _scertIdToClient = new ConcurrentDictionary<ushort, ClientObject>();

        protected PS2_RC4 _sessionCipher = null;

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
                    if (data.ClientObject == null || !data.ClientObject.IsDestroyed)
                    {
                        data.RecvQueue.Enqueue(message);
                        data.ClientObject?.OnEcho(true, DateTime.UtcNow);
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

                    pipeline.AddLast(new ScertEncoder());
                    pipeline.AddLast(new ScertIEnumerableEncoder());
                    pipeline.AddLast(new ScertTcpFrameDecoder(DotNetty.Buffers.ByteOrder.LittleEndian, 1024, 1, 2, 0, 0, false));
                    pipeline.AddLast(new ScertDecoder(_sessionCipher, AuthKey));
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
                    // Disconnect on destroy
                    if (data.ClientObject != null && data.ClientObject.IsDestroyed)
                    {
                        await DisconnectClient(clientChannel);
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
                            // Add client object's send queue to responses
                            while (data.ClientObject.TcpSendMessageQueue.TryDequeue(out var message))
                                responses.Add(message);

                            // Echo
                            if ((DateTime.UtcNow - data.ClientObject.LastTcpMessageUtc).TotalSeconds > Program.Settings.ServerEchoInterval)
                                Echo(ref responses);
                        }

                        //
                        if (responses.Count > 0)
                            await clientChannel.WriteAndFlushAsync(responses);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        /// <summary>
        /// Adds a server echo message to the collection of messages.
        /// </summary>
        /// <param name="responses"></param>
        protected void Echo(ref List<BaseScertMessage> responses)
        {
            responses.Add(new RT_MSG_SERVER_ECHO() { });
        }

        #region Message Processing

        protected async Task ProcessMessage(BaseScertMessage message, IChannel clientChannel, ChannelData data)
        {
            // 
            switch (message)
            {
                case RT_MSG_CLIENT_HELLO clientHello:
                    {
                        Queue(new RT_MSG_SERVER_HELLO() { ARG2 = 0 }, clientChannel);
                        break;
                    }
                case RT_MSG_CLIENT_CRYPTKEY_PUBLIC clientCryptKeyPublic:
                    {
                        Queue(new RT_MSG_SERVER_CRYPTKEY_PEER() { Key = Utils.FromString(Program.KEY) }, clientChannel);
                        break;
                    }
                case RT_MSG_CLIENT_CONNECT_TCP_AUX_UDP clientConnectTcpAuxUdp:
                    {
                        data.ApplicationId = clientConnectTcpAuxUdp.AppId;
                        data.ClientObject = Program.Manager.GetClientByAccessToken(clientConnectTcpAuxUdp.AccessToken);
                        if (data.ClientObject.DmeWorld == null || data.ClientObject.DmeWorld.WorldId != clientConnectTcpAuxUdp.ARG1)
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
                        // Queue(new RT_MSG_SERVER_CRYPTKEY_GAME() { Key = Utils.FromString(Program.KEY) }, clientChannel);
                        Queue(new RT_MSG_SERVER_CONNECT_ACCEPT_TCP()
                        {
                            UNK_00 = (ushort)data.ClientObject.DmeId,
                            UNK_02 = data.ClientObject.ScertId,
                            UNK_04 = 0,
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
                            Timestamp = (uint)(DateTime.UtcNow - data.ClientObject.DmeWorld.WorldCreatedTimeUtc).TotalMilliseconds
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

                        data.ClientObject?.DmeWorld.OnPlayerJoined(data.ClientObject);
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
                        data.ClientObject?.DmeWorld?.OnSetAggTime(setAggTime);
                        break;
                    }
                case RT_MSG_CLIENT_TIMEBASE_QUERY timebaseQuery:
                    {
                        Queue(new RT_MSG_SERVER_TIMEBASE_QUERY_NOTIFY()
                        {
                            ClientTime = timebaseQuery.Timestamp,
                            ServerTime = (uint)(DateTime.UtcNow - data.ClientObject.DmeWorld.WorldCreatedTimeUtc).TotalMilliseconds
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
                        await DisconnectClient(clientChannel);
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
        protected async Task DisconnectClient(IChannel channel)
        {
            try
            {
                //await channel.WriteAndFlushAsync(new RT_MSG_SERVER_FORCED_DISCONNECT());
            }
            catch (Exception)
            {
                // Silence exception since the client probably just closed the socket before we could write to it
            }
            finally
            {
                // await channel.CloseAsync();
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
