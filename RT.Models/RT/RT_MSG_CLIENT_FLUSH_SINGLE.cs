using RT.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Server.Common;
using Server.Common.Stream;

namespace RT.Models
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_CLIENT_FLUSH_SINGLE)]
    public class RT_MSG_CLIENT_FLUSH_SINGLE : BaseScertMessage
    {
        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_CLIENT_FLUSH_SINGLE;

        public short TargetOrSource = 0;
        public byte[] Payload;

        public override void Deserialize(MessageReader reader)
        {
            TargetOrSource = reader.ReadInt16();
            Payload = reader.ReadRest();
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            writer.Write(TargetOrSource);
            writer.Write(Payload);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"TargetOrSource:{TargetOrSource} " +
                $"Payload:{(Payload == null ? "" : BitConverter.ToString(Payload)) }";
        }
    }
}
