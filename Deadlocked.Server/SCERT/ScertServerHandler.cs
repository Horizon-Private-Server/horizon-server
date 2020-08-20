using Deadlocked.Server.SCERT.Models.Packets;
using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;

namespace Deadlocked.Server.SCERT
{
    public class ScertServerHandler : SimpleChannelInboundHandler<BaseScertMessage>
    {
        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<ScertServerHandler>();

        public override void ChannelActive(IChannelHandlerContext ctx)
        {
            // Detect when client disconnects
            ctx.Channel.CloseCompletion.ContinueWith((x) => Logger.Info("Channel Closed"));
        }

        // The Channel is closed hence the connection is closed
        public override void ChannelInactive(IChannelHandlerContext ctx) => Logger.Info("Client disconnected");

        protected override void ChannelRead0(IChannelHandlerContext ctx, BaseScertMessage message)
        {
            Logger.Info("Received message: " + message);

        }

        public override bool IsSharable => true;
    }
}
