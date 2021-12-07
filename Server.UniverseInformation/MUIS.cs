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
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Handlers.Timeout;

namespace Server.UnivereInformation
{
    public class MUIS
    {
        public static Random RNG = new Random();

        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<MUIS>();

        private int _port = 0;
        public int Port => _port;

        protected IEventLoopGroup _bossGroup = null;
        protected IEventLoopGroup _workerGroup = null;
        protected IChannel _boundChannel = null;
        protected ScertServerHandler _scertHandler = null;
        private uint _clientCounter = 0;

        protected internal class ChannelData
        {
            public int ApplicationId { get; set; } = 0;
            public ConcurrentQueue<BaseScertMessage> RecvQueue { get; } = new ConcurrentQueue<BaseScertMessage>();
            public ConcurrentQueue<BaseScertMessage> SendQueue { get; } = new ConcurrentQueue<BaseScertMessage>();
        }

        protected ConcurrentDictionary<string, ChannelData> _channelDatas = new ConcurrentDictionary<string, ChannelData>();

        public MUIS(int port)
        {
            this._port = port;
        }

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
                _channelDatas.TryRemove(key, out var data);
            };

            // Queue all incoming messages
            _scertHandler.OnChannelMessage += (channel, message) =>
            {
                string key = channel.Id.AsLongText();
                if (_channelDatas.TryGetValue(key, out var data))
                {
                    data.RecvQueue.Enqueue(message);
                }

                // Log if id is set
                if (message.CanLog())
                    Logger.Info($"TCP RECV {channel}: {message}");
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

                    pipeline.AddLast(new WriteTimeoutHandler(15));
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

                        if (scertClient.MediusVersion < 112)
                        {   //PS2
                            Queue(new RT_MSG_SERVER_HELLO(), clientChannel);
                        }
                        else
                        {
                            string s = "71000006e702308202e3308201cba00302010202140100000000000000000000001100000000000004300d06092a864886f70d0101050500308196310b3009060355040613025553310b3009060355040813024341311230100603550407130953616e20446965676f3131302f060355040a1328534f4e5920436f6d707574657220456e7465727461696e6d656e7420416d657269636120496e632e31143012060355040b130b53434552542047726f7570311d301b06035504031314534345525420526f6f7420417574686f72697479301e170d3035303432373231303233335a170d3335303432363233353935395a308187310b3009060355040613025553310b3009060355040813024341311230100603550407130953616e20446965676f3131302f060355040a1328534f4e5920436f6d707574657220456e7465727461696e6d656e7420416d657269636120496e632e31143012060355040b130b53434552542047726f7570310e300c060355040313054d4c532030305c300d06092a864886f70d0101010500034b003048024100cf16b818a204ba6db8fc85d866e4f708e6cfa754a5a2399d08eafdfdbbff852d3f1c86944e157dd8f6408d7cd9cfdab409d32fddee05bdde8cff303187b374690203000011300d06092a864886f70d010105050003820101007cc5ccb73e8bffb1888d870279767063a8ea2a619fdd3bbc0b1209b5384853408ec61aafa8b9071f9e41ab93bb56dbcea59ebf18ca113775fd146c3e97fb673db572f849dc906e9f6ee6817cdcf104c4ac4758020ff2443b770d0979fce7cd8807c69ef787e51660e22e35ca19f43da41346ee619d1a707c335684f183ea432c38aaf5dbb277c8527ad98412d7624362d89d52af9f39459db0c5159a8d737262b4a9abdd95b1b8d9d586230bc3cef9fafab68b0fa8e516a89672aa4f0b3956c0a1fbb392b4b7cfca233bee6b83a4b90fe9a8211803f35f3ab83ef81dd2077e185cfc86204adc2538225951a6e9473540d647cbca5d2f6d189644006f2af7d85386dbb9b3d4885887fbb394268da005317eff6eed";

                            byte[] a = new byte[s.Length / 2];
                            for (int i = 0, h = 0; h < s.Length; i++, h += 2)
                            {
                                a[i] = (byte)Int32.Parse(s.Substring(h, 2), System.Globalization.NumberStyles.HexNumber);
                            }


                            Queue(new RT_MSG_SERVER_HELLO() { Certificate = a }, clientChannel);
                        }
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
                case RT_MSG_CLIENT_CONNECT_TCP clientConnectTcp:
                    {
                        data.ApplicationId = clientConnectTcp.AppId;

                        if (scertClient.MediusVersion >= 109)
                        {
                            Queue(new RT_MSG_SERVER_CONNECT_REQUIRE() { Contents = Utils.FromString("024802") }, clientChannel);
                        }
                        else
                        {
                            Queue(new RT_MSG_SERVER_CONNECT_ACCEPT_TCP()
                            {
                                IP = (clientChannel.RemoteAddress as IPEndPoint)?.Address,
                            }, clientChannel);
                            Queue(new RT_MSG_SERVER_CONNECT_COMPLETE() { ARG1 = 0x0001 }, clientChannel);
                        }
                        break;
                    }
                case RT_MSG_CLIENT_CONNECT_READY_REQUIRE clientConnectReadyRequire:
                    {
                        Queue(new RT_MSG_SERVER_CRYPTKEY_GAME() { Key = scertClient.CipherService.GetPublicKey(CipherContext.RC_CLIENT_SESSION) }, clientChannel);
                        Queue(new RT_MSG_SERVER_CONNECT_ACCEPT_TCP()
                        {
                            UNK_00 = 0,
                            UNK_02 = GenerateNewScertClientId(),
                            UNK_06 = 0x0001,
                            IP = (clientChannel.RemoteAddress as IPEndPoint)?.Address
                        }, clientChannel);
                        break;
                    }
                case RT_MSG_CLIENT_CONNECT_READY_TCP clientConnectReadyTcp:
                    {
                        Queue(new RT_MSG_SERVER_CONNECT_COMPLETE() { ARG1 = 0x0001 }, clientChannel);
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

            switch (message)
            {
                case MediusGetUniverseInformationRequest getUniverseInfo:
                    {
                        if (Program.Settings.Universes.TryGetValue(data.ApplicationId, out var info))
                        {
                            // 
                            if (getUniverseInfo.InfoType.HasFlag(MediusUniverseVariableInformationInfoFilter.INFO_SVO_URL))
                            {
                                Queue(new RT_MSG_SERVER_APP()
                                {
                                    Message = new MediusUniverseVariableSvoURLResponse()
                                    {
                                        MessageID = new MessageId(),
                                        URL = info.SvoURL
                                    }
                                }, clientChannel);
                            }

                            Queue(new RT_MSG_SERVER_APP()
                            {
                                Message = new MediusUniverseVariableInformationResponse()
                                {
                                    MessageID = getUniverseInfo.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                    InfoFilter = getUniverseInfo.InfoType,
                                    UniverseID = info.UniverseId,
                                    ExtendedInfo = info.ExtendedInfo,
                                    UniverseName = info.Name,
                                    UniverseDescription = info.Description,
                                    SvoURL = info.SvoURL,
                                    DNS = info.Endpoint,
                                    Port = info.Port,
                                    EndOfList = true
                                }
                            }, clientChannel);

                            if (getUniverseInfo.InfoType.HasFlag(MediusUniverseVariableInformationInfoFilter.INFO_NEWS))
                            {
                                Queue(new RT_MSG_SERVER_APP()
                                {
                                    Message = new MediusUniverseNewsResponse()
                                    {
                                        MessageID = getUniverseInfo.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        News = "News!",
                                        EndOfList = true
                                    }
                                }, clientChannel);
                            }
                        }
                        else
                        {
                            Logger.Warn($"Unable to find universe for app id {data.ApplicationId}");

                            Queue(new RT_MSG_SERVER_APP()
                            {
                                Message = new MediusUniverseVariableInformationResponse()
                                {
                                    MessageID = getUniverseInfo.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult,
                                    InfoFilter = getUniverseInfo.InfoType,
                                    EndOfList = true
                                }
                            }, clientChannel);
                        }
                        break;
                    }
                default:
                    {
                        Logger.Warn($"UNHANDLED MEDIUS MESSAGE: {message}");
                        break;
                    }
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


        protected uint GenerateNewScertClientId()
        {
            return _clientCounter++;
        }
    }
}
