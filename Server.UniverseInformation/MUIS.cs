﻿using DotNetty.Common.Internal.Logging;
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
                    Logger.Debug($"TCP RECV {channel}: {message}");
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

                        if (scertClient.MediusVersion >= 109)
                        {
                            Queue(new RT_MSG_SERVER_CONNECT_REQUIRE(), clientChannel);
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
                            PlayerId = 0,
                            ScertId = GenerateNewScertClientId(),
                            PlayerCount = 0x0001,
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
