using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.RTIME
{
    [Message(RT_MSG_TYPE.RT_MSG_SERVER_CONNECT_COMPLETE)]
    public class RT_MSG_SERVER_CONNECT_COMPLETE : BaseMessage
    {

        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_SERVER_CONNECT_COMPLETE;

        // 
        public ushort ARG1 = 0x0001;

        public override void Deserialize(BinaryReader reader)
        {
            ARG1 = reader.ReadUInt16();
        }

        protected override void Serialize(BinaryWriter writer)
        {
            writer.Write(ARG1);
        }
    }
}
