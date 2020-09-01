using RT.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_SERVER_CONNECT_REQUIRE)]
    public class RT_MSG_SERVER_CONNECT_REQUIRE : BaseScertMessage
    {

        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_SERVER_CONNECT_REQUIRE;

        // 
        public byte[] Contents = new byte[] { 0x02, 0x48, 0x02 };

        public override void Deserialize(BinaryReader reader)
        {
            Contents = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
        }

        protected override void Serialize(BinaryWriter writer)
        {
            writer.Write(Contents);
        }
    }
}
