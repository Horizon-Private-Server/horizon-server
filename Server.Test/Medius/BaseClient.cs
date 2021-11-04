using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using RT.Cryptography;
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
using DotNetty.Handlers.Logging;
using DotNetty.Handlers.Timeout;
using System.Data;
using Server.Common;

namespace Server.Test.Medius
{
    public abstract class BaseClient
    {
        public static Random RNG = new Random();

        public enum ClientState
        {
            DISCONNECTED = -1,
            NONE,
            CONNECTED,
            HELLO,
            HANDSHAKE,
            CONNECT_TCP,
            AUTHENTICATED
        }

        protected abstract IInternalLogger Logger { get; }

        public abstract PS2_RSA AuthKey { get; }
        public abstract int ApplicationId { get; }

        public ConcurrentQueue<BaseScertMessage> RecvQueue { get; } = new ConcurrentQueue<BaseScertMessage>();
        public ConcurrentQueue<BaseScertMessage> SendQueue { get; } = new ConcurrentQueue<BaseScertMessage>();

        public ClientState State { get; set; } = ClientState.NONE;

        /// <summary>
        /// When true, all messages from this client will be ignored.
        /// </summary>
        public DateTime LastSentEcho { get; set; } = DateTime.UnixEpoch;
        public DateTime LastRecvEcho { get; set; } = DateTime.UnixEpoch;
        public DateTime TimeConnected { get; set; } = Utils.GetHighPrecisionUtcTime();


        /// <summary>
        /// Destroy client if disconnected.
        /// </summary>
        public bool ShouldDestroy => State < 0;

        private IEventLoopGroup _group = null;
        protected IChannel _boundChannel = null;
        protected ScertServerHandler _scertHandler = null;
        protected PS2_RC4 _sessionCipher = null;


        public BaseClient(string serverIp, short serverPort)
        {
            Start(serverIp, serverPort);
        }


        private async void Start(string serverIp, short serverPort)
        {
            Func<RT_MSG_TYPE, CipherContext, ICipher> getCipher = (id, context) =>
            {
                switch (context)
                {
                    case CipherContext.RC_CLIENT_SESSION: return _sessionCipher;
                    case CipherContext.RSA_AUTH: return AuthKey;
                    default: return null;
                }
            };
            _group = new MultithreadEventLoopGroup();
            _scertHandler = new ScertServerHandler();

            // Initialize on connect
            _scertHandler.OnChannelActive += async (channel) =>
            {
                RecvQueue.Clear();
                SendQueue.Clear();
                State = ClientState.CONNECTED;

                await OnConnected(channel);
            };

            // 
            _scertHandler.OnChannelInactive += async (channel) =>
            {
                await OnDisconnected(channel);
            };

            // Queue all incoming messages
            _scertHandler.OnChannelMessage += (channel, message) =>
            {
                RecvQueue.Enqueue(message);

                // Log if id is set
                if (message.CanLog())
                    Logger.Info($"RECV {channel}: {message}");
            };

            try
            {
                var bootstrap = new Bootstrap();
                bootstrap
                    .Group(_group)
                    .Channel<TcpSocketChannel>()
                    .Option(ChannelOption.TcpNodelay, true)
                    .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;

                        pipeline.AddLast(new ScertEncoder());
                        pipeline.AddLast(new ScertIEnumerableEncoder());
                        pipeline.AddLast(new ScertTcpFrameDecoder(DotNetty.Buffers.ByteOrder.LittleEndian, Constants.MEDIUS_MESSAGE_MAXLEN, 1, 2, 0, 0, false));
                        pipeline.AddLast(new ScertDecoder(_sessionCipher, AuthKey));
                        pipeline.AddLast(_scertHandler);
                    }));

                try
                {
                    _boundChannel = await bootstrap.ConnectAsync(new IPEndPoint(IPAddress.Parse(serverIp), serverPort));
                }
                catch (Exception e)
                {
                    Logger.Error($"Failed to connect to server {e}");
                    State = ClientState.DISCONNECTED;
                    return;
                }
            }
            finally
            {

            }
        }

        public async Task Stop()
        {
            await _group.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));

            // 
            RecvQueue.Clear();
            SendQueue.Clear();
        }

        public virtual void Log()
        {
            Logger.Warn($"State: {State}");
        }

        public virtual async Task Tick()
        {
            if (_boundChannel == null)
                return;

            // 
            List<BaseScertMessage> responses = new List<BaseScertMessage>();

            //
            if (State == ClientState.DISCONNECTED)
                throw new Exception("Failed to authenticate with the server.");

            try
            {
                // Process all messages in queue
                while (RecvQueue.TryDequeue(out var message))
                {
                    try
                    {
                        await ProcessMessage(message, _boundChannel);
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
                    while (SendQueue.TryDequeue(out var message))
                        responses.Add(message);


                    //
                    if (responses.Count > 0)
                        await _boundChannel.WriteAndFlushAsync(responses);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);

            }
        }

        protected virtual void Echo(ref List<BaseScertMessage> responses)
        {
            if ((Utils.GetHighPrecisionUtcTime() - LastSentEcho).TotalSeconds > 5f)
            {
                LastSentEcho = Utils.GetHighPrecisionUtcTime();
                responses.Add(new RT_MSG_SERVER_ECHO() { });
            }
        }

        protected abstract Task OnConnected(IChannel channel);

        protected abstract Task OnDisconnected(IChannel channel);

        protected abstract Task ProcessMessage(BaseScertMessage message, IChannel channel);

        #region Queue

        public void Queue(BaseScertMessage message)
        {
            SendQueue.Enqueue(message);
        }

        public void Queue(IEnumerable<BaseScertMessage> messages)
        {
            foreach (var message in messages)
                SendQueue.Enqueue(message);
        }

        #endregion

    }
}
