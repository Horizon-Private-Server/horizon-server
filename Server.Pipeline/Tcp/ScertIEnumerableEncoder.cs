using RT.Models;
using RT.Common;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Common;

namespace Server.Pipeline.Tcp
{
    public class ScertIEnumerableEncoder : MessageToMessageEncoder<IEnumerable<BaseScertMessage>>
    {
        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<ScertIEnumerableEncoder>();

        public ScertIEnumerableEncoder()
        { }

        protected override void Encode(IChannelHandlerContext ctx, IEnumerable<BaseScertMessage> messages, List<object> output)
        {
            if (messages is null)
                return;

            // Serialize and add
            foreach (var msg in messages)
                output.Add(msg);
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            Logger.Error(exception);
            context.CloseAsync();
        }

    }
}
