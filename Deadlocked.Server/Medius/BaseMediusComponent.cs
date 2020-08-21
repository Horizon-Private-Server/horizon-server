using Deadlocked.Server.Medius.Models;
using Deadlocked.Server.Medius.Models.Packets;
using Deadlocked.Server.SCERT;
using Deadlocked.Server.SCERT.Models;
using Deadlocked.Server.SCERT.Models.Packets;
using DotNetty.Common.Internal.Logging;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Medius.Crypto;
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

namespace Deadlocked.Server.Medius
{
    public abstract class BaseMediusComponent : IMediusComponent
    {
        public static Random RNG = new Random();

        protected abstract IInternalLogger Logger { get; }
        public abstract int Port { get; }
        public abstract string Name { get; }


        public abstract PS2_RSA AuthKey { get; }

        protected IEventLoopGroup _bossGroup = null;
        protected IEventLoopGroup _workerGroup = null;
        protected IChannel _boundChannel = null;
        protected ScertServerHandler _scertHandler = null;

        protected internal class ChannelData
        {
            public int ApplicationId { get; set; } = 0;
            public ClientObject ClientObject { get; set; } = null;
            public ConcurrentQueue<BaseScertMessage> RecvQueue { get; } = new ConcurrentQueue<BaseScertMessage>();
            public ConcurrentQueue<BaseScertMessage> SendQueue { get; } = new ConcurrentQueue<BaseScertMessage>();
        }

        protected ConcurrentDictionary<string, ChannelData> _channelDatas = new ConcurrentDictionary<string, ChannelData>();

        protected PS2_RC4 _sessionCipher = null;

        protected DateTime _timeLastEcho = DateTime.UtcNow;


        public virtual async void Start()
        {
            //
            _bossGroup = new MultithreadEventLoopGroup(1);
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
                _channelDatas.TryRemove(key, out _);
            };

            // Queue all incoming messages
            _scertHandler.OnChannelMessage += (channel, message) =>
            {
                string key = channel.Id.AsLongText();
                if (_channelDatas.TryGetValue(key, out var data))
                {
                    data.RecvQueue.Enqueue(message);
                    data.ClientObject?.OnEcho(DateTime.UtcNow);
                }

                // Log if id is set
                if (Program.Settings.IsLog(message.Id))
                    Logger.Info($"{Name} {data?.ClientObject},{channel}: {message}");
            };

            try
            {
                var bootstrap = new ServerBootstrap();
                bootstrap
                    .Group(_bossGroup, _workerGroup)
                    .Channel<TcpServerSocketChannel>()
                    .Option(ChannelOption.SoBacklog, 100)
                    .Handler(new LoggingHandler(LogLevel.INFO))
                    .ChildOption(ChannelOption.TcpNodelay, true)
                    .ChildOption(ChannelOption.RcvbufAllocator, new FixedRecvByteBufAllocator(1024 * 4))
                    .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;

                        pipeline.AddLast(new ByteArrayEncoder());
                        pipeline.AddLast(new ScertEncoder());
                        pipeline.AddLast(new ScertIEnumerableEncoder());
                        pipeline.AddLast(new ScertLengthFieldBasedFrameDecoder(DotNetty.Buffers.ByteOrder.LittleEndian, 1024, 1, 2, 0, 0, true));
                        pipeline.AddLast(new ScertDecoder(getCipher));
                        pipeline.AddLast(_scertHandler);
                    }));

                _boundChannel = await bootstrap.BindAsync(Port);
            }
            finally
            {

            }
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

            await Task.WhenAll(_scertHandler.Group.Select(c => Tick(c)));
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
                    // Process all messages in queue
                    while (data.RecvQueue.TryDequeue(out var message))
                        await ProcessMessage(message, clientChannel, data);

                    // Send if writeable
                    if (clientChannel.IsWritable)
                    {
                        // Add send queue to responses
                        while (data.SendQueue.TryDequeue(out var message))
                            responses.Add(message);

                        if (data.ClientObject != null)
                        {
                            // Add client object's send queue to responses
                            while (data.ClientObject.SendMessageQueue.TryDequeue(out var message))
                                responses.Add(message);

                            // Echo
                            if ((DateTime.UtcNow - data.ClientObject.UtcLastEcho).TotalSeconds > Program.Settings.ServerEchoInterval)
                                Echo(ref responses);
                        }

                        //
                        //await responses.Send(clientChannel);
                        if (responses.Count > 0)
                            await clientChannel.WriteAndFlushAsync(responses);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                await DisconnectClient(clientChannel);
            }
        }

        protected virtual void Echo(ref List<BaseScertMessage> responses)
        {
            responses.Add(new RT_MSG_SERVER_ECHO() { });
        }

        protected abstract Task ProcessMessage(BaseScertMessage message, IChannel clientChannel, ChannelData data);

        #region Channel

        protected async Task DisconnectClient(IChannel channel)
        {
            try
            {
                await channel.WriteAndFlushAsync(new RT_MSG_SERVER_FORCED_DISCONNECT());
            }
            finally
            {
                await channel.DisconnectAsync();
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

    }
}
