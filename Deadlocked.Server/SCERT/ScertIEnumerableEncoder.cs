using Deadlocked.Server.Medius.Models.Packets;
using Deadlocked.Server.SCERT.Models;
using Deadlocked.Server.SCERT.Models.Packets;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Deadlocked.Server.SCERT
{
    public class ScertIEnumerableEncoder : MessageToMessageEncoder<IEnumerable<BaseScertMessage>>
    {
        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<ScertIEnumerableEncoder>();

        public ScertIEnumerableEncoder()
        { }

        protected override void Encode(IChannelHandlerContext ctx, IEnumerable<BaseScertMessage> messages, List<object> output)
        {
            List<byte[]> msgs = new List<byte[]>();
            if (messages is null)
                return;

            // Serialize and add
            foreach (var msg in messages)
            {
                if (Program.Settings.IsLog(msg.Id))
                    Logger.Info($"SEND to {ctx.Channel}: {msg}");

                msgs.AddRange(msg.Serialize());
            }

            // Condense as much as possible
            var condensedMsgs = msgs.GroupWhileAggregating(0, (sum, item) => sum + item.Length, (sum, item) => sum < MediusConstants.MEDIUS_MESSAGE_MAXLEN).SelectMany(x => x);

            // 
            foreach (var msg in condensedMsgs)
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
