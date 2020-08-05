using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.RTIME
{
    [Message(RT_MSG_TYPE.RT_MSG_CLIENT_CONNECT_READY_REQUIRE)]
    public class RT_MSG_CLIENT_CONNECT_READY_REQUIRE : BaseMessage
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
