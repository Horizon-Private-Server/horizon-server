using RT.Common;
using Server.Common;
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

        public override void Deserialize(BinaryReader reader)
        {
            Payload = reader.ReadRest();
        }

        protected override void Serialize(BinaryWriter writer)
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
