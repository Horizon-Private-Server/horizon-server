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
using System.Threading.Tasks;
using DotNetty.Handlers.Timeout;

namespace Server.UniverseInformation
{
    /// <summary>
    /// Introduced in Medius 1.43
    /// Modified in Medius 1.50 deprecating INFO_UNIVERSES Standard Flow
    /// </summary>
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
            _port = port;
        }

        /// <summary>
        /// Start the MUIS TCP Server.
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
                case RT_MSG_CLIENT_CONNECT_TCP clientConnectTcp:
                    {
                        data.ApplicationId = clientConnectTcp.AppId;

                        Logger.Info($"Retrieved ApplicationID {data.ApplicationId} from client connection");

                        // HSG:F Pubeta, HW:O, LemmingsPS2, Arc the Lad, or EyeToy Chat Beta
                        if (data.ApplicationId == 10538 ||
                            data.ApplicationId == 10582 ||
                            data.ApplicationId == 10130 ||
                            data.ApplicationId == 20474 ||
                            data.ApplicationId == 10211 ||
                            data.ApplicationId == 10984 ||
                            data.ApplicationId == 10550)
                        {
                            // If this is NOT Arc the Lad, continue
                            if(data.ApplicationId != 10984)
                            {
                                Queue(new RT_MSG_SERVER_CONNECT_ACCEPT_TCP()
                                {
                                    IP = (clientChannel.RemoteAddress as IPEndPoint)?.Address,
                                }, clientChannel);

                                //if this isn't Lemmings PS2 or Arc the Lad, continue the handshake
                                if (data.ApplicationId != 20474 || data.ApplicationId != 10984 || data.ApplicationId != 10550)
                                {
                                    Queue(new RT_MSG_SERVER_CONNECT_COMPLETE() { ClientCountAtConnect = 0x0001 }, clientChannel);
                                }
                            } 
                            else
                            {
                                if (scertClient.MediusVersion <= 109 || scertClient.MediusVersion == 113)
                                {
                                    Queue(new RT_MSG_SERVER_CONNECT_REQUIRE() { ReqServerPassword = 0x00, Contents = Utils.FromString("4802") }, clientChannel);
                                }
                                Queue(new RT_MSG_SERVER_CRYPTKEY_GAME() { Key = scertClient.CipherService.GetPublicKey(CipherContext.RC_CLIENT_SESSION) }, clientChannel);
                            }
                        } else
                        //Default flow
                        {
                            if (scertClient.MediusVersion <= 109 || data.ApplicationId == 22920)
                            {
                                Queue(new RT_MSG_SERVER_CONNECT_REQUIRE() { Contents = Utils.FromString("004802") }, clientChannel);
                            } else
                            {
                                Queue(new RT_MSG_SERVER_CONNECT_ACCEPT_TCP()
                                {
                                    IP = (clientChannel.RemoteAddress as IPEndPoint)?.Address,
                                }, clientChannel);

                                Queue(new RT_MSG_SERVER_CRYPTKEY_GAME() { Key = scertClient.CipherService.GetPublicKey(CipherContext.RC_CLIENT_SESSION) }, clientChannel);
                                Queue(new RT_MSG_SERVER_CONNECT_COMPLETE() { ClientCountAtConnect = 0x0001 }, clientChannel);
                            }
                        }
                         break;
                    }
                case RT_MSG_CLIENT_CONNECT_READY_REQUIRE clientConnectReadyRequire:
                    {
                        if (scertClient.MediusVersion >= 109)
                        {
                            //Queue(new RT_MSG_SERVER_CRYPTKEY_GAME() { Key = scertClient.CipherService.GetPublicKey(CipherContext.RC_CLIENT_SESSION) }, clientChannel);
                        }
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
                        Queue(new RT_MSG_SERVER_CONNECT_COMPLETE() { ClientCountAtConnect = 0x0001 }, clientChannel);

                        if(data.ApplicationId == 22920)
                        {

                            byte[] payload = Convert.FromBase64String("http");

                            Queue(new RT_MSG_SERVER_MEMORY_POKE()
                            { 
                                Address = 0x130FCB8,
                                MsgDataLen = payload.Length,
                                Payload = payload
                            }, clientChannel);
                        }
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
                        await ProcessMediusMessage(clientAppToServer.Message, clientChannel, data);
                        break;
                    }
                case RT_MSG_CLIENT_APP_LIST clientAppList:
                    {

                        break;
                    }
                case RT_MSG_CLIENT_DISCONNECT _:
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

        protected virtual async Task ProcessMediusMessage(BaseMediusMessage message, IChannel clientChannel, ChannelData data)
        {
            if (message == null)
                return;

            switch (message)
            {
                case MediusGetUniverseInformationRequest getUniverseInfo:
                    {
                        if (Program.Settings.Universes.TryGetValue(data.ApplicationId, out var info))
                        {
                            //GT4 or DDOA, HOA, or Socom 1 PUBBETA
                            if(data.ApplicationId == 10782 || data.ApplicationId == 10538 || data.ApplicationId == 10130 || data.ApplicationId == 10211)
                            {
                                // MUIS Standard Flow - Deprecated after Medius Client/Server Library 1.50
                                if (getUniverseInfo.InfoType.HasFlag(MediusUniverseVariableInformationInfoFilter.INFO_UNIVERSES))
                                {
                                    Queue(new RT_MSG_SERVER_APP()
                                    {
                                        Message = new MediusUniverseStatusListResponse()
                                        {
                                            MessageID = new MessageId(),
                                            StatusCode = MediusCallbackStatus.MediusSuccess,
                                            UniverseName = info.Name,
                                            DNS = info.Endpoint,
                                            Port = info.Port,
                                            UniverseDescription = info.Description,
                                            Status = info.Status,
                                            UserCount = info.UserCount,
                                            MaxUsers = info.MaxUsers,
                                            EndOfList = true,
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
                                                News = "Simulated News!",
                                                EndOfList = true
                                            }
                                        }, clientChannel);
                                    }
                                    /*
                                    Queue(new RT_MSG_SERVER_APP()
                                    {
                                        Message = new MediusUniverseNewsResponse()
                                        {
                                            MessageID = getUniverseInfo.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusSuccess,
                                            News = "Simulated News!",
                                            EndOfList = true
                                        }
                                    }, clientChannel);
                                    */
                                }
                            } 
                            else
                            {
                                //Send Variable Flow
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
                                        Status = info.Status,
                                        UserCount = info.UserCount,
                                        MaxUsers = info.MaxUsers,
                                        DNS = info.Endpoint,
                                        Port = info.Port,
                                        UniverseBilling = info.UniverseBilling,
                                        BillingSystemName = info.BillingSystemName,
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
                                            News = "Simulated News!",
                                            EndOfList = true
                                        }
                                    }, clientChannel);
                                }
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
