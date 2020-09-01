using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RT.Common;
using Server.Common;

namespace RT.Models
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_CLIENT_CONNECT_READY_REQUIRE)]
    public class RT_MSG_CLIENT_CONNECT_READY_REQUIRE : BaseScertMessage
    {

        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_CLIENT_CONNECT_READY_REQUIRE;

        // 
        public byte ARG1 = 0x00;

        public override void Deserialize(BinaryReader reader)
        {
            ARG1 = reader.ReadByte();
        }

        protected override void Serialize(BinaryWriter writer)
        {
            writer.Write(ARG1);
        }
    }
}
