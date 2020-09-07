using DotNetty.Common.Internal.Logging;
using DotNetty.Common.Utilities;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Microsoft.VisualBasic.FileIO;
using RT.Common;
using RT.Cryptography;
using RT.Models;
using Server.Pipeline.Tcp;
using Server.Pipeline.Udp;
using Server.Dme.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Server.Dme.PluginArgs;

namespace Server.Dme
{
    public class UdpServer
    {
        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<UdpServer>();


        public int Port { get; protected set; } = -1;
        public PS2_RSA AuthKey => Program.GlobalAuthKey;
        protected PS2_RC4 _sessionCipher = null;

        protected IEventLoopGroup _workerGroup = null;
        protected IChannel _boundChannel = null;
        protected ScertDatagramHandler _scertHandler = null;

        protected ClientObject ClientObject { get; set; } = null;
        protected EndPoint AuthenticatedEndPoint { get; set; } = null;

        private ConcurrentQueue<ScertDatagramPacket> _recvQueue = new ConcurrentQueue<ScertDatagramPacket>();
        private ConcurrentQueue<ScertDatagramPacket> _sendQueue = new ConcurrentQueue<ScertDatagramPacket>();

        private BaseScertMessage _lastMessage { get; set; } = null;

        #region Port Management

        private static ConcurrentDictionary<int, UdpServer> _portToServer = new ConcurrentDictionary<int, UdpServer>();
        private void RegisterPort()
        {
            int i = Program.Settings.UDPPort;
            while (_portToServer.ContainsKey(i))
                ++i;

            if (_portToServer.TryAdd(i, this))
                Port = i;
        }

        private void FreePort()
        {
            if (Port < 0)
                return;

            _portToServer.TryRemove(Port, out _);
        }

        #endregion

        public UdpServer(ClientObject clientObject)
        {
            this.ClientObject = clientObject;
            RegisterPort();
        }

        /// <summary>
        /// Start the Dme Udp Client Server.
        /// </summary>
        public virtual async void Start()
        {
            //
            _workerGroup = new MultithreadEventLoopGroup();

            Func<RT_MSG_TYPE, CipherContext, ICipher> getCipher = (id, context) =>
            {
                switch (context)
                {
                    case CipherContext.RC_CLIENT_SESSION: return _sessionCipher;
                    case CipherContext.RSA_AUTH: return AuthKey;
                    default: return null;
                }
            };
            _scertHandler = new ScertDatagramHandler();

            // Queue all incoming messages
            _scertHandler.OnChannelMessage += (channel, message) =>
            {
                var pluginArgs = new OnUdpMsg()
                {
                    Player = this.ClientObject,
                    Packet = message
                };

                // Plugin
                Program.Plugins.OnEvent(Plugins.PluginEvent.DME_GAME_ON_RECV_UDP, pluginArgs);

                if (!pluginArgs.Ignore)
                    _recvQueue.Enqueue(message);
            };

            var bootstrap = new Bootstrap();
            bootstrap
                .Group(_workerGroup)
                .Channel<SocketDatagramChannel>()
                .Handler(new LoggingHandler(LogLevel.INFO))
                .Handler(new ActionChannelInitializer<IChannel>(channel =>
                {
                    IChannelPipeline pipeline = channel.Pipeline;

                    pipeline.AddLast(new ScertDatagramEncoder(Constants.MEDIUS_MESSAGE_MAXLEN));
                    pipeline.AddLast(new ScertDatagramIEnumerableEncoder(Constants.MEDIUS_MESSAGE_MAXLEN));
                    pipeline.AddLast(new ScertDatagramDecoder());
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
                        _workerGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)));

                FreePort();
            }
        }

        #region Message Processing

        protected void ProcessMessage(ScertDatagramPacket packet)
        {
            var message = packet.Message;

            // 
            switch (message)
            {
                case RT_MSG_CLIENT_CONNECT_AUX_UDP connectAuxUdp:
                    {
                        var clientObject = Program.TcpServer.GetClientByScertId(connectAuxUdp.ScertId);
                        if (clientObject != ClientObject)
                            break;

                        // 
                        AuthenticatedEndPoint = packet.Source;

                        ClientObject.RemoteUdpEndpoint = AuthenticatedEndPoint as IPEndPoint;
                        ClientObject.OnUdpConnected();
                        SendTo(new RT_MSG_SERVER_CONNECT_ACCEPT_AUX_UDP()
                        {
                            PlayerId = (ushort)ClientObject.DmeId,
                            ScertId = ClientObject.ScertId,
                            PlayerCount = (ushort)ClientObject.DmeWorld.Clients.Count,
                            EndPoint = ClientObject.RemoteUdpEndpoint
                        }, packet.Source);
                        break;
                    }
                case RT_MSG_SERVER_ECHO serverEchoReply:
                    {

                        break;
                    }
                case RT_MSG_CLIENT_ECHO clientEcho:
                    {
                        SendTo(new RT_MSG_CLIENT_ECHO() { Value = clientEcho.Value }, packet.Source);
                        break;
                    }
                case RT_MSG_CLIENT_APP_BROADCAST clientAppBroadcast:
                    {
                        if (AuthenticatedEndPoint == null || !AuthenticatedEndPoint.Equals(packet.Source))
                            break;

                        /*
                        // Don't send dupes
                        if (_lastMessage != null && _lastMessage is RT_MSG_CLIENT_APP_BROADCAST lastBroadcast && lastBroadcast.Equals(clientAppBroadcast))
                            break;

                        // Speedhack, reduce pad message in DL
                        if (clientAppBroadcast.Payload.Length == 0x22 && clientAppBroadcast.Payload[0] == 2 && clientAppBroadcast.Payload[1] == 9)
                        {
                            if (_lastMessage != null && _lastMessage is RT_MSG_CLIENT_APP_BROADCAST lb && lb.Payload.Length == 0x22)
                            {
                                int i;
                                for (i = 0; i < clientAppBroadcast.Payload.Length; ++i)
                                {
                                    if (i >= 0x14 && i <= 0x17)
                                        continue;

                                    if (clientAppBroadcast.Payload[i] != lb.Payload[i])
                                        goto dododood;
                                }

                                break;
                            }
                        }

                        
                        dododood:;
                        */

                        ClientObject.DmeWorld?.BroadcastUdp(ClientObject, clientAppBroadcast.Payload);
                        break;
                    }
                case RT_MSG_CLIENT_APP_LIST clientAppList:
                    {
                        if (AuthenticatedEndPoint == null || !AuthenticatedEndPoint.Equals(packet.Source))
                            break;

                        var world = ClientObject.DmeWorld;
                        if (world != null && clientAppList.Targets != null)
                        {
                            foreach (var target in clientAppList.Targets)
                            {
                                world.SendUdpAppSingle(ClientObject, (short)target, clientAppList.Payload);
                            }
                        }
                        break;
                    }
                case RT_MSG_CLIENT_APP_SINGLE clientAppSingle:
                    {
                        if (AuthenticatedEndPoint == null || !AuthenticatedEndPoint.Equals(packet.Source))
                            break;

                        ClientObject.DmeWorld?.SendUdpAppSingle(ClientObject, clientAppSingle.TargetOrSource, clientAppSingle.Payload);
                        break;
                    }
                case RT_MSG_CLIENT_APP_TOSERVER clientAppToServer:
                    {
                        if (AuthenticatedEndPoint == null || !AuthenticatedEndPoint.Equals(packet.Source))
                            break;

                        ProcessMediusMessage(clientAppToServer.Message);
                        break;
                    }

                case RT_MSG_CLIENT_DISCONNECT_WITH_REASON clientDisconnectWithReason:
                    {
                        
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

        protected virtual void ProcessMediusMessage(BaseMediusMessage message)
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
                await channel.CloseAsync();
            }
        }

        #endregion

        #region Send

        private void SendTo(BaseScertMessage message, EndPoint target)
        {
            if (target == null)
                return;

            _sendQueue.Enqueue(new ScertDatagramPacket(message, target));
        }

        public void Send(BaseScertMessage message)
        {
            if (AuthenticatedEndPoint == null)
                return;

            _sendQueue.Enqueue(new ScertDatagramPacket(message, AuthenticatedEndPoint));
        }

        public void Send(IEnumerable<BaseScertMessage> messages)
        {
            if (AuthenticatedEndPoint == null)
                return;

            foreach (var message in messages)
                _sendQueue.Enqueue(new ScertDatagramPacket(message, AuthenticatedEndPoint));
        }

        #endregion

        #region Tick

        public async Task Tick()
        {
            if (_boundChannel == null || !_boundChannel.Active)
                return;

            // 
            List<ScertDatagramPacket> responses = new List<ScertDatagramPacket>();

            try
            {
                // Process all messages in queue
                while (_recvQueue.TryDequeue(out var message))
                {
                    try
                    {
                        ProcessMessage(message);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                    }
                }

                // Send if writeable
                if (_boundChannel.IsWritable)
                {
                    // Add send queue to responses
                    while (_sendQueue.TryDequeue(out var message))
                        responses.Add(message);

                    //
                    if (responses.Count > 0)
                    {
                        if (responses.Count > 2)
                        {

                        }
                        await _boundChannel.WriteAndFlushAsync(responses);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        #endregion

    }
}
