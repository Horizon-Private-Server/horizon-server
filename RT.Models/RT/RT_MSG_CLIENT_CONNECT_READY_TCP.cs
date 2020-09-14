using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_CLIENT_CONNECT_READY_TCP)]
    public class RT_MSG_CLIENT_CONNECT_READY_TCP : BaseScertMessage
    {

        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_CLIENT_CONNECT_READY_TCP;

        public byte StartOpt = 0x0E;
        public RT_RECV_FLAG RecvFlag = RT_RECV_FLAG.RECV_BROADCAST;

        public override void Deserialize(BinaryReader reader)
        {
            StartOpt = reader.ReadByte();
            RecvFlag = reader.Read<RT_RECV_FLAG>();
        }

        protected override void Serialize(BinaryWriter writer)
        {
            writer.Write(StartOpt);
            writer.Write(RecvFlag);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"StartOpt:{StartOpt} " +
                $"RecvFlag:{RecvFlag}";
        }
    }
}
