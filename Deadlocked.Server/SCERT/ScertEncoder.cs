using Deadlocked.Server.SCERT.Models;
using Deadlocked.Server.SCERT.Models.Packets;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.Text;

namespace Deadlocked.Server.SCERT
{
    public class ScertEncoder : MessageToMessageEncoder<BaseScertMessage>
    {
        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<ScertIEnumerableEncoder>();

        public ScertEncoder()
        { }

        protected override void Encode(IChannelHandlerContext ctx, BaseScertMessage message, List<object> output)
        {
            if (message is null)
                return;

            if (Program.Settings.IsLog(message.Id))
                Logger.Info($"SEND to {ctx.Channel}: {message}");

            // Serialize
            var msgs = message.Serialize();

            // 
            foreach (var msg in msgs)
            {
                var byteBuffer = ctx.Allocator.Buffer(msg.Length);
                byteBuffer.WriteBytes(msg);
                output.Add(byteBuffer);
            }
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            Logger.Error(exception);
            context.CloseAsync();
        }

    }
}
