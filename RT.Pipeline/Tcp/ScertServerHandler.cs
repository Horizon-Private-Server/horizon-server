using Dme.Server.SCERT.Models.Packets;
using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Groups;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Dme.Server.Pipeline.Tcp
{
    public class ScertServerHandler : SimpleChannelInboundHandler<BaseScertMessage>
    {
        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<ScertServerHandler>();

        public override bool IsSharable => true;

        public IChannelGroup Group = null;


        public Action<IChannel> OnChannelActive;
        public Action<IChannel> OnChannelInactive;
        public Action<IChannel, BaseScertMessage> OnChannelMessage;

        private class OnCallback<T>
        {
            public Type Type;
            public DateTime TimeoutUtc;
            public Task<T> Task;
            public Action<T> Callback;
        }

        private ConcurrentDictionary<Type, dynamic> _onCallbacks = new ConcurrentDictionary<Type, dynamic>();

        public override void ChannelActive(IChannelHandlerContext ctx)
        {
            IChannelGroup g = Group;
            if (g == null)
            {
                lock (this)
                {
                    if (Group == null)
                    {
                        Group = g = new DefaultChannelGroup(ctx.Executor);
                    }
                }
            }

            // Detect when client disconnects
            ctx.Channel.CloseCompletion.ContinueWith((x) =>
            {
                Logger.Info("Channel Closed");
                OnChannelInactive?.Invoke(ctx.Channel);
            });

            // Add to channels list
            g.Add(ctx.Channel);

            // Send event upstream
            OnChannelActive?.Invoke(ctx.Channel);
        }

        // The Channel is closed hence the connection is closed
        public override void ChannelInactive(IChannelHandlerContext ctx)
        {
            IChannelGroup g = Group;

            Logger.Info("Client disconnected");

            // Remove
            g?.Remove(ctx.Channel);

            // Send event upstream
            OnChannelInactive?.Invoke(ctx.Channel);
        }


        protected override void ChannelRead0(IChannelHandlerContext ctx, BaseScertMessage message)
        {
            // Send upstream
            OnChannelMessage?.Invoke(ctx.Channel, message);

            // Process callback
            if (message != null)
            {
                var key = message.GetType();

                if (_onCallbacks.TryRemove(key, out var callback))
                {
                    callback.Callback.Invoke(Convert.ChangeType(message, callback.Type));
                    callback.Task.Start();
                }
            }
        }

        public override void ChannelReadComplete(IChannelHandlerContext context) => context.Flush();

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            Logger.Error(exception);
            context.CloseAsync();
        }

        public void Tick()
        {
            Queue<Type> removeQueue = new Queue<Type>();

            foreach (var callback in _onCallbacks.Values)
            {
                // Timeout
                if (DateTime.UtcNow > callback.TimeoutUtc)
                {
                    // Timeout
                    callback.Task.Start();

                    // 
                    removeQueue.Enqueue(callback.Type);
                }
            }

            // Remove
            while (removeQueue.TryDequeue(out var key))
                _onCallbacks.TryRemove(key, out _);
        }

        public Task<T> On<T>(int timeoutMs = 5000) where T : BaseScertMessage
        {
            T result = null;
            Task<T> task = new Task<T>(() =>
            {
                if (result == null)
                    throw new Exception();

                return result;
            });
            var key = typeof(T);
            var item = new OnCallback<T>()
            {
                Type = key,
                TimeoutUtc = DateTime.UtcNow + TimeSpan.FromMilliseconds(timeoutMs),
                Task = task,
                Callback = (value) => result = value
            };

            // Timeout existing callback
            if (_onCallbacks.TryGetValue(key, out var existingItem))
            {
                // 
                (existingItem as OnCallback<T>).Task.Start();

                // Remove
                _onCallbacks.TryRemove(key, out _);
            }

            // 
            _onCallbacks.TryAdd(typeof(T), item);

            return task;
        }
    }
}
