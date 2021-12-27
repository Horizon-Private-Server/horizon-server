using RT.Common;
using Server.Common;
using Server.Common.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RT.Models
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_CLIENT_APP_BROADCAST)]
    public class RT_MSG_CLIENT_APP_BROADCAST : BaseScertMessage
    {
        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_CLIENT_APP_BROADCAST;

        public byte[] Payload { get; set; }

        public override void Deserialize(MessageReader reader)
        {
            Payload = reader.ReadRest();
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            writer.Write(Payload);
        }

        public bool Equals(RT_MSG_CLIENT_APP_BROADCAST broadcast)
        {
            return Payload == broadcast.Payload || (Payload?.SequenceEqual(broadcast.Payload) ?? false);
        }

        public override bool Equals(object obj)
        {
            if (obj is RT_MSG_CLIENT_APP_BROADCAST broadcast)
                return this.Equals(broadcast);

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"Contents:{BitConverter.ToString(Payload)}";
        }
    }
}
