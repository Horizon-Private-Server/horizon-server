using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.SCERT.Models.Packets
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_CLIENT_CONNECT_READY_TCP)]
    public class RT_MSG_CLIENT_CONNECT_READY_TCP : BaseScertMessage
    {

        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_CLIENT_CONNECT_READY_TCP;

        // 
        public ushort ARG1 = 0x010E;

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
