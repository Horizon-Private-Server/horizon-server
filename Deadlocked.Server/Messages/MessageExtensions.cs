using Deadlocked.Server.Messages.DME;
using Deadlocked.Server.Messages.RTIME;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Deadlocked.Server.Messages
{
    public static class MessageExtensions
    {

        /// <summary>
        /// Sends a collection of messages to the clients.
        /// </summary>
        public static void Send(this IEnumerable<BaseMessage> messages, params ClientSocket[] clients)
        {
            if (messages == null || messages.Count() == 0 || clients == null || clients.Length == 0)
                return;

            List<byte[]> msgs = new List<byte[]>();

            foreach (var msg in messages)
            {
                //
                Console.WriteLine($"Send to <{String.Join(",", clients.Select(x=>x.Client?.ClientAccount?.AccountName))}> || {msg}");

                // Serialize
                msg.Serialize(out var msgBuffers);

                // Add
                msgs.AddRange(msgBuffers);
            }

            // Condense as much as possible
            var condensedMsgs = msgs.GroupWhileAggregating(0, (sum, item) => sum + item.Length, (sum, item) => sum < MediusConstants.MEDIUS_MESSAGE_MAXLEN).SelectMany(x => x);

            // 
            foreach (var client in clients)
            {
                foreach (var msg in condensedMsgs)
                {
                    Console.WriteLine($"!! SEND {client.RemoteEndPoint} !! {BitConverter.ToString(msg)}");
                    client.Send(msg);
                }
            }
        }

    }
}
