using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.RTIME
{
    [Message(RT_MSG_TYPE.RT_MSG_SERVER_CONNECT_REQUIRE)]
    public class RT_MSG_SERVER_CONNECT_REQUIRE : BaseMessage
    {

        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_SERVER_CONNECT_REQUIRE;

        // 
        public byte ARG1 = 0x02;
        public byte ARG2 = 0x48;
        public byte ARG3 = 0x02;

        public override void Deserialize(BinaryReader reader)
        {
            ARG1 = reader.ReadByte();
            ARG2 = reader.ReadByte();
            ARG3 = reader.ReadByte();
        }

        protected override void Serialize(BinaryWriter writer)
        {
            writer.Write(ARG1);
            writer.Write(ARG2);
            writer.Write(ARG3);
        }
    }
}
