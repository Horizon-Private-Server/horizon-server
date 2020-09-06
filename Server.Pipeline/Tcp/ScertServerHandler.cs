using RT.Models;
using RT.Common;
using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Groups;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;

namespace Server.Pipeline.Tcp
{
    public class ScertServerHandler : SimpleChannelInboundHandler<BaseScertMessage>
    {
        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<ScertServerHandler>();

        public override bool IsSharable => true;

        public IChannelGroup Group = null;


        public Action<IChannel> OnChannelActive;
        public Action<IChannel> OnChannelInactive;
        public Action<IChannel, BaseScertMessage> OnChannelMessage;

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
        }

        public override void ChannelReadComplete(IChannelHandlerContext context) => context.Flush();

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            Logger.Error(exception);
            context.CloseAsync();
        }
    }
}
