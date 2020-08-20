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

        protected ConcurrentDictionary<string, ClientObject> _nettyToClientObject = new ConcurrentDictionary<string, ClientObject>();
        protected ConcurrentDictionary<string, ConcurrentQueue<BaseScertMessage>> _nettyToClientRecvMessageQueue = new ConcurrentDictionary<string, ConcurrentQueue<BaseScertMessage>>();
        protected ConcurrentDictionary<string, ConcurrentQueue<BaseScertMessage>> _nettyToClientSendMessageQueue = new ConcurrentDictionary<string, ConcurrentQueue<BaseScertMessage>>();

        protected PS2_RC4 _sessionCipher = null;

        protected DateTime _timeLastEcho = DateTime.UtcNow;


        public virtual async void Start()
        {
            //
            _bossGroup = new MultithreadEventLoopGroup(1);
            _workerGroup = new MultithreadEventLoopGroup();

            var scertDecoder = new ScertDecoder(1500, true, (id, context) =>
            {
                switch (context)
                {
                    case CipherContext.RC_CLIENT_SESSION: return _sessionCipher;
                    case CipherContext.RSA_AUTH: return AuthKey;
                    default: return null;
                }
            });
            var scertEncoder = new ScertEncoder();
            _scertHandler = new ScertServerHandler();

            // Add client on connect
            _scertHandler.OnChannelActive += (channel) =>
            {
                string key = channel.Id.AsLongText();
                var clientObject = new ClientObject() { DotNettyId = key };
                Program.Clients.Add(clientObject);
                _nettyToClientObject.TryAdd(key, clientObject);
                _nettyToClientRecvMessageQueue.TryAdd(key, new ConcurrentQueue<BaseScertMessage>());
                _nettyToClientSendMessageQueue.TryAdd(key, new ConcurrentQueue<BaseScertMessage>());
            };

            // Remove client on disconnect
            _scertHandler.OnChannelInactive += (channel) =>
            {
                string key = channel.Id.AsLongText();
                _nettyToClientObject.TryRemove(key, out _);
                _nettyToClientRecvMessageQueue.TryRemove(key, out _);
                _nettyToClientSendMessageQueue.TryRemove(key, out _);
            };

            // Queue all incoming messages
            _scertHandler.OnChannelMessage += (channel, message) =>
            {
                string key = channel.Id.AsLongText();
                if (_nettyToClientRecvMessageQueue.TryGetValue(key, out var queue))
                    queue.Enqueue(message);
                if (_nettyToClientObject.TryGetValue(key, out var clientObject))
                    clientObject.OnEcho(DateTime.UtcNow);

                // Log if id is set
                if (Program.Settings.IsLog(message.Id))
                    Logger.Info($"{Name} {clientObject}: {message}");
            };

            try
            {
                var bootstrap = new ServerBootstrap();
                bootstrap
                    .Group(_bossGroup, _workerGroup)
                    .Channel<TcpServerSocketChannel>()
                    .Option(ChannelOption.SoBacklog, 100)
                    .Handler(new LoggingHandler(LogLevel.INFO))
                    .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;

                        pipeline.AddLast(scertEncoder, scertDecoder, _scertHandler);
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
            await Task.WhenAll(_scertHandler.Group.Select(c => Tick(c)));
        }

        protected virtual async Task Tick(IChannel clientChannel)
        {
            if (clientChannel == null)
                return;


            List<BaseScertMessage> responses = new List<BaseScertMessage>();
            string key = clientChannel.Id.AsLongText();

            // 
            if (_nettyToClientObject.TryGetValue(key, out var clientObject) && _nettyToClientRecvMessageQueue.TryGetValue(key, out var queue))
            {
                // Process all messages in queue
                while (queue.TryDequeue(out var message))
                    await ProcessMessage(message, clientChannel, clientObject);

                // Add send queue to responses
                if (_nettyToClientSendMessageQueue.TryGetValue(key, out var sendQueue))
                    while (sendQueue.TryDequeue(out var message))
                        responses.Add(message);

                // Echo
                if ((DateTime.UtcNow - clientObject.UtcLastEcho).TotalSeconds > Program.Settings.ServerEchoInterval)
                    Echo(ref responses);

                // 
                await responses.Send(clientChannel);
            }
        }

        protected virtual void Echo(ref List<BaseScertMessage> responses)
        {
            responses.Add(new RT_MSG_SERVER_ECHO() { });
        }

        protected abstract Task ProcessMessage(BaseScertMessage message, IChannel clientChannel, ClientObject clientObject);

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

        public void Queue(BaseScertMessage message, params ClientObject[] clientObjects)
        {
            Queue(message, (IEnumerable<ClientObject>)clientObjects);
        }

        public void Queue(BaseScertMessage message, IEnumerable<ClientObject> clientObjects)
        {
            foreach (var clientObject in clientObjects)
                if (clientObject != null)
                    if (_nettyToClientSendMessageQueue.TryGetValue(clientObject.DotNettyId, out var sendQueue))
                        sendQueue.Enqueue(message);
        }

        public void Queue(IEnumerable<BaseScertMessage> messages, params ClientObject[] clientObjects)
        {
            Queue(messages, (IEnumerable<ClientObject>)clientObjects);
        }

        public void Queue(IEnumerable<BaseScertMessage> messages, IEnumerable<ClientObject> clientObjects)
        {
            foreach (var clientObject in clientObjects)
                if (clientObject != null)
                    if (_nettyToClientSendMessageQueue.TryGetValue(clientObject.DotNettyId, out var sendQueue))
                        foreach (var message in messages)
                            sendQueue.Enqueue(message);
        }

        public void Queue(BaseMediusMessage message, params ClientObject[] clientObjects)
        {
            Queue(message, (IEnumerable<ClientObject>)clientObjects);
        }

        public void Queue(BaseMediusMessage message, IEnumerable<ClientObject> clientObjects)
        {
            var scertMessage = new RT_MSG_SERVER_APP() { Message = message };
            foreach (var clientObject in clientObjects)
                if (clientObject != null)
                    if (_nettyToClientSendMessageQueue.TryGetValue(clientObject.DotNettyId, out var sendQueue))
                        sendQueue.Enqueue(scertMessage);
        }

        public void Queue(IEnumerable<BaseMediusMessage> messages, params ClientObject[] clientObjects)
        {
            Queue(messages, (IEnumerable<ClientObject>)clientObjects);
        }

        public void Queue(IEnumerable<BaseMediusMessage> messages, IEnumerable<ClientObject> clientObjects)
        {
            var scertMessages = messages.Select(x => new RT_MSG_SERVER_APP() { Message = x });
            foreach (var clientObject in clientObjects)
                if (clientObject != null)
                    if (_nettyToClientSendMessageQueue.TryGetValue(clientObject.DotNettyId, out var sendQueue))
                        foreach (var scertMessage in scertMessages)
                            sendQueue.Enqueue(scertMessage);
        }

        #endregion

    }
}
