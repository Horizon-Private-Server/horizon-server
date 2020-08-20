using Deadlocked.Server.SCERT.Models;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.Text;

namespace Deadlocked.Server.SCERT
{
    public class ScertEncoder : MessageToMessageEncoder<BaseScertMessage>
    {
        public ScertEncoder()
        { }

        protected override void Encode(IChannelHandlerContext ctx, BaseScertMessage message, List<object> output)
        {
            if (message is null)
                return;

            output.AddRange(message.Serialize());
        }

        public override bool IsSharable => true;
    }
}
