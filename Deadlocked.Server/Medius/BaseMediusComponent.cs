using Deadlocked.Server.Messages;
using Deadlocked.Server.SCERT;
using Deadlocked.Server.SCERT.Models;
using DotNetty.Common.Internal.Logging;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Medius.Crypto;
using Microsoft.Extensions.Logging.Console;
using System;
using System.Collections.Generic;
using System.IO;
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
        public abstract int Port { get; }
        public abstract string Name { get; }


        public abstract PS2_RSA AuthKey { get; }

        protected IEventLoopGroup _bossGroup = null;
        protected IEventLoopGroup _workerGroup = null;
        protected IChannel _boundChannel = null;

        protected PS2_RC4 _sessionCipher = null;

        protected DateTime _timeLastEcho = DateTime.UtcNow;


        public virtual async void Start()
        {
            //
            _bossGroup = new MultithreadEventLoopGroup(1);
            _workerGroup = new MultithreadEventLoopGroup();

            var decoder = new ScertDecoder(1500, true, (id, context) =>
            {
                switch (context)
                {
                    case CipherContext.RC_CLIENT_SESSION: return _sessionCipher;
                    case CipherContext.RSA_AUTH: return AuthKey;
                    default: return null;
                }
            });
            var encoder = new ScertEncoder();
            var scertHandler = new ScertServerHandler();

            try
            {
                var bootstrap = new ServerBootstrap();
                bootstrap
                    .Group(_bossGroup, _workerGroup)
                    .Channel<TcpServerSocketChannel>()
                    .Option(ChannelOption.SoBacklog, 100)
                    .Handler(new LoggingHandler(DotNetty.Handlers.Logging.LogLevel.INFO))
                    .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;

                        pipeline.AddLast(encoder, decoder, scertHandler);

                    }));

                _boundChannel = await bootstrap.BindAsync(Port);
            }
            finally
            {
                
            }
        }

        public virtual async void Stop()
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

        protected virtual void Tick(ClientSocket client)
        {

        }

        protected virtual void Echo(ClientSocket client, ref List<BaseScertMessage> responses)
        {
            responses.Add(new RT_MSG_SERVER_ECHO() { });
        }

        protected virtual int HandleCommand(BaseScertMessage message, ClientSocket client, ref List<BaseScertMessage> responses)
        {
            return 0;
        }
    }
}
