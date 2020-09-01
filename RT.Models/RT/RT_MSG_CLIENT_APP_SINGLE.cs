using RT.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Server.Common;

namespace RT.Models
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_CLIENT_APP_SINGLE)]
    public class RT_MSG_CLIENT_APP_SINGLE : BaseScertMessage
    {
        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_CLIENT_APP_SINGLE;

        public short TargetOrSource = 0;
        public byte[] Payload;

        public override void Deserialize(BinaryReader reader)
        {
            TargetOrSource = reader.ReadInt16();
            Payload = reader.ReadRest();
        }

        protected override void Serialize(BinaryWriter writer)
        {
            writer.Write(TargetOrSource);
            writer.Write(Payload);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"TargetOrSource:{TargetOrSource} " +
                $"Payload:{BitConverter.ToString(Payload)}";
        }
    }
}
