using Dme.Server.Medius.Models.Packets;
using Dme.Server.Pipeline.Udp;
using Dme.Server.SCERT.Models;
using Dme.Server.SCERT.Models.Packets;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Dme.Server.Pipeline.Udp
{
    public class ScertDatagramIEnumerableEncoder : MessageToMessageEncoder<IEnumerable<ScertDatagramPacket>>
    {
        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<ScertDatagramIEnumerableEncoder>();

        readonly int maxPacketLength;

        public ScertDatagramIEnumerableEncoder(int maxPacketLength)
        {
            this.maxPacketLength = maxPacketLength;
        }

        protected override void Encode(IChannelHandlerContext ctx, IEnumerable<ScertDatagramPacket> messages, List<object> output)
        {
            List<byte[]> msgs;
            Dictionary<EndPoint, List<byte[]>> msgsByEndpoint = new Dictionary<EndPoint, List<byte[]>>();
            if (messages is null)
                return;

            // Serialize and add
            foreach (var msg in messages)
            {
                if (Program.Settings.IsLog(msg.Message.Id))
                    Logger.Info($"SEND to {msg.Destination}: {msg.Message}");

                if (!msgsByEndpoint.TryGetValue(msg.Destination, out msgs))
                    msgsByEndpoint.Add(msg.Destination, msgs = new List<byte[]>());

                msgs.AddRange(msg.Message.Serialize());
            }

            foreach (var kvp in msgsByEndpoint)
            {
                // Condense as much as possible
                var condensedMsgs = kvp.Value.GroupWhileAggregating(0, (sum, item) => sum + item.Length, (sum, item) => sum < maxPacketLength).SelectMany(x => x);

                // 
                foreach (var msg in condensedMsgs)
                {
                    var byteBuffer = ctx.Allocator.Buffer(msg.Length);
                    byteBuffer.WriteBytes(msg);
                    output.Add(new DatagramPacket(byteBuffer, kvp.Key));
                }
            }
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            Logger.Error(exception);
            context.CloseAsync();
        }

    }
}
