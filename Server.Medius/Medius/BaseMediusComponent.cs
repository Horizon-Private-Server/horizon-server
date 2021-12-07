using DotNetty.Common.Internal.Logging;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using RT.Cryptography;
using Microsoft.Extensions.Logging.Console;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RT.Models;
using RT.Common;
using Server.Pipeline.Tcp;
using Server.Medius.Models;
using Server.Medius.PluginArgs;
using DotNetty.Handlers.Timeout;
using Server.Common;

namespace Server.Medius
{
    public abstract class BaseMediusComponent : IMediusComponent
    {
        public static Random RNG = new Random();

        public enum ClientState
        {
            DISCONNECTED,
            CONNECTED,
            HELLO,
            HANDSHAKE,
            CONNECT_1,
            AUTHENTICATED
        }

        protected abstract IInternalLogger Logger { get; }
        public abstract int Port { get; }
        public IPAddress IPAddress => Program.SERVER_IP;

        protected IEventLoopGroup _bossGroup = null;
        protected IEventLoopGroup _workerGroup = null;
        protected IChannel _boundChannel = null;
        protected ScertServerHandler _scertHandler = null;
        private uint _clientCounter = 0;

        protected internal class ChannelData
        {
            public int ApplicationId { get; set; } = 0;
            public ClientObject ClientObject { get; set; } = null;
            public ConcurrentQueue<BaseScertMessage> RecvQueue { get; } = new ConcurrentQueue<BaseScertMessage>();
            public ConcurrentQueue<BaseScertMessage> SendQueue { get; } = new ConcurrentQueue<BaseScertMessage>();

            public ClientState State { get; set; } = ClientState.DISCONNECTED;

            public bool? IsBanned { get; set; } = null;

            /// <summary>
            /// When true, all messages from this client will be ignored.
            /// </summary>
            public bool Ignore { get; set; } = false;
            public DateTime TimeConnected { get; set; } = Utils.GetHighPrecisionUtcTime();


            /// <summary>
            /// Timesout client if they authenticated after a given number of seconds.
            /// </summary>
            public bool ShouldDestroy => ClientObject == null && (Utils.GetHighPrecisionUtcTime() - TimeConnected).TotalSeconds > Program.Settings.ClientTimeoutSeconds;
        }

        protected ConcurrentQueue<IChannel> _forceDisconnectQueue = new ConcurrentQueue<IChannel>();
        protected ConcurrentDictionary<string, ChannelData> _channelDatas = new ConcurrentDictionary<string, ChannelData>();

        protected PS2_RC4 _sessionCipher = null;

        protected DateTime _timeLastEcho = Utils.GetHighPrecisionUtcTime();


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
                var data = new ChannelData()
                {
                    State = ClientState.CONNECTED
                };
                _channelDatas.TryAdd(key, data);

                // Check if IP is banned
                Program.Database.GetIsIpBanned((channel.RemoteAddress as IPEndPoint).Address.MapToIPv4().ToString()).ContinueWith((r) =>
                {
                    data.IsBanned = r.IsCompletedSuccessfully && r.Result;
                    if (data.IsBanned == true)
                    {
                        QueueBanMessage(data);
                    }
                    else
                    {
                        // Check if in maintenance mode
                        Program.Database.GetServerFlags().ContinueWith((r) =>
                        {
                            if (r.IsCompletedSuccessfully && r.Result != null && r.Result.MaintenanceMode != null)
                            {
                                // Ensure that maintenance is active
                                // Ensure that we're past the from date
                                // Ensure that we're before the to date (if set)
                                if (r.Result.MaintenanceMode.IsActive
                                        && Utils.GetHighPrecisionUtcTime() > r.Result.MaintenanceMode.FromDt
                                        && (!r.Result.MaintenanceMode.ToDt.HasValue
                                            || r.Result.MaintenanceMode.ToDt > Utils.GetHighPrecisionUtcTime()))
                                {
                                    QueueBanMessage(data, "Server in maintenance.");
                                }
                            }
                        });
                    }
                });
            };

            // Remove client on disconnect
            _scertHandler.OnChannelInactive += async (channel) =>
            {
                await Tick(channel);
                string key = channel.Id.AsLongText();
                if (_channelDatas.TryRemove(key, out var data))
                {
                    data.State = ClientState.DISCONNECTED;
                    data.ClientObject?.OnDisconnected();
                }

            };

            // Queue all incoming messages
            _scertHandler.OnChannelMessage += (channel, message) =>
            {
                string key = channel.Id.AsLongText();
                if (_channelDatas.TryGetValue(key, out var data))
                {
                    // Don't queue message if client is ignored
                    if (!data.Ignore)
                    {
                        // Don't queue if banned
                        if (data.IsBanned == null || data.IsBanned == false)
                        {
                            data.RecvQueue.Enqueue(message);

                            if (message is RT_MSG_SERVER_ECHO serverEcho)
                                data.ClientObject?.OnRecvServerEcho(serverEcho);
                        }
                    }
                }

                // Log if id is set
                if (message.CanLog())
                    Logger.Info($"RECV {data?.ClientObject},{channel}: {message}");
            };

            try
            {
                var bootstrap = new ServerBootstrap();
                bootstrap
                    .Group(_bossGroup, _workerGroup)
                    .Channel<TcpServerSocketChannel>()
                    //.Option(ChannelOption.SoBacklog, 100)
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
            finally
            {

            }
        }

        public void Log()
        {
            
        }

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

        public async Task Tick()
        {
            if (_scertHandler == null || _scertHandler.Group == null)
                return;

            // Tick clients
            await Task.WhenAll(_scertHandler.Group.Select(c => Tick(c)));

            // Disconnect and remove timedout unauthenticated channels
            while (_forceDisconnectQueue.TryDequeue(out var channel))
            {

                // Send disconnect message
                _ = ForceDisconnectClient(channel);

                // Remove
                _channelDatas.TryRemove(channel.Id.AsLongText(), out var d);

                // Logout
                d?.ClientObject?.Logout();

                // 
                Logger.Warn($"REMOVING CHANNEL {channel},{d},{d?.ClientObject}");

                // close after 5 seconds
                _ = Task.Run(async () =>
                {
                    await Task.Delay(5000);
                    try { await channel.CloseAsync(); } catch (Exception) { }
                });
            }
        }

        protected virtual async Task Tick(IChannel clientChannel)
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

                    // Ignore
                    if (data.Ignore)
                        return;

                    // Process all messages in queue
                    while (data.RecvQueue.TryDequeue(out var message))
                    {
                        try
                        {
                            // Send to plugins
                            var onMsg = new OnMessageArgs()
                            {
                                Player = data.ClientObject,
                                Message = message,
                                Channel = clientChannel
                            };
                            Program.Plugins.OnEvent(Plugins.PluginEvent.MEDIUS_ON_RECV, onMsg);

                            // Ignore if ignored
                            if (!onMsg.Ignore && data.State != ClientState.DISCONNECTED)
                                await ProcessMessage(message, clientChannel, data);
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e);
                            Logger.Error($"FORCE DISCONNECTING CLIENT 1 {data} || {data.ClientObject}");
                            _ = ForceDisconnectClient(clientChannel);
                            data.Ignore = true;
                        }
                    }

                    // Send if writeable
                    if (clientChannel.IsWritable)
                    {
                        // Add send queue to responses
                        while (data.SendQueue.TryDequeue(out var message))
                        {
                            // Send to plugins
                            var onMsg = new OnMessageArgs()
                            {
                                Player = data.ClientObject,
                                Channel = clientChannel,
                                Message = message
                            };
                            Program.Plugins.OnEvent(Plugins.PluginEvent.MEDIUS_ON_SEND, onMsg);

                            // Ignore if ignored
                            if (!onMsg.Ignore)
                                responses.Add(message);
                        }

                        if (data.ClientObject != null)
                        {
                            // Echo
                            if ((Utils.GetHighPrecisionUtcTime() - data.ClientObject.UtcLastServerEchoSent).TotalSeconds > Program.Settings.ServerEchoInterval)
                                data.ClientObject.QueueServerEcho();

                            // Add client object's send queue to responses
                            while (data.ClientObject.SendMessageQueue.TryDequeue(out var message))
                            {
                                // Send to plugins
                                var onMsg = new OnMessageArgs()
                                {
                                    Player = data.ClientObject,
                                    Message = message,
                                    Channel = clientChannel
                                };
                                Program.Plugins.OnEvent(Plugins.PluginEvent.MEDIUS_ON_SEND, onMsg);

                                // Ignore if ignored
                                if (!onMsg.Ignore)
                                    responses.Add(message);
                            }
                        }

                        //
                        if (responses.Count > 0)
                            _ = clientChannel.WriteAndFlushAsync(responses);
                    }
                }
                else
                {

                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                _forceDisconnectQueue.Enqueue(clientChannel);
                //await DisconnectClient(clientChannel);
            }
        }

        protected virtual void QueueBanMessage(ChannelData data, string msg = "You have been banned!")
        {
            // Send ban message
            data.SendQueue.Enqueue(new RT_MSG_SERVER_SYSTEM_MESSAGE()
            {
                Severity = Program.Settings.BanSystemMessageSeverity,
                EncodingType = 1,
                LanguageType = 2,
                EndOfMessage = true,
                Message = msg
            });
        }

        protected abstract Task ProcessMessage(BaseScertMessage message, IChannel clientChannel, ChannelData data);

        #region Channel

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

        protected uint GenerateNewScertClientId()
        {
            return _clientCounter++;
        }

    }
}
