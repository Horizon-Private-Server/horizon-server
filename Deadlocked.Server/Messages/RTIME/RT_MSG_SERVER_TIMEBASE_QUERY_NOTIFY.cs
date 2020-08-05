using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.RTIME
{
    [Message(RT_MSG_TYPE.RT_MSG_SERVER_TIMEBASE_QUERY_NOTIFY)]
    public class RT_MSG_SERVER_TIMEBASE_QUERY_NOTIFY : BaseMessage
    {
        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_SERVER_TIMEBASE_QUERY_NOTIFY;

        public uint ClientTime { get; set; }
        public uint ServerTime { get; set; }

        public override void Deserialize(BinaryReader reader)
        {
            ClientTime = reader.ReadUInt32();
            ServerTime = reader.ReadUInt32();
        }

        protected override void Serialize(BinaryWriter writer)
        {
            writer.Write(ClientTime);
            writer.Write(ServerTime);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"ClientTime:{ClientTime}" + " " +
                $"ServerTime:{ServerTime}";
        }
    }
}
