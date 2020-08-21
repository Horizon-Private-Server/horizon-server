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
    public class ByteArrayEncoder : MessageToMessageEncoder<byte[]>
    {
        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<ByteArrayEncoder>();

        public ByteArrayEncoder()
        { }

        protected override void Encode(IChannelHandlerContext ctx, byte[] message, List<object> output)
        {
            if (message is null)
                return;

            // 
            var byteBuffer = ctx.Allocator.Buffer(message.Length);
            byteBuffer.WriteBytes(message);
            output.Add(byteBuffer);
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            Logger.Error(exception);
            context.CloseAsync();
        }

    }
}
