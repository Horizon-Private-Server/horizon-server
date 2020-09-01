using RT.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_CLIENT_ECHO)]
    public class RT_MSG_CLIENT_ECHO : BaseScertMessage
    {
        //
        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_CLIENT_ECHO;

        //
        public byte Value = 0xA5;

        public override void Deserialize(BinaryReader reader)
        {
            Value = reader.ReadByte();
        }

        protected override void Serialize(BinaryWriter writer)
        {
            writer.Write(Value);
        }

    }
}
