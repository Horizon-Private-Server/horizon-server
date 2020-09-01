using RT.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_SERVER_HELLO)]
    public class RT_MSG_SERVER_HELLO : BaseScertMessage
    {
        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_SERVER_HELLO;

        public ushort ARG1 = 0x006E;
        public ushort ARG2 = 0x0001;

        public RT_MSG_SERVER_HELLO()
        {

        }

        public override void Deserialize(BinaryReader reader)
        {
            ARG1 = reader.ReadUInt16();
            ARG2 = reader.ReadUInt16();
        }

        protected override void Serialize(BinaryWriter writer)
        {
            writer.Write(ARG1);
            writer.Write(ARG2);
        }
    }
}
