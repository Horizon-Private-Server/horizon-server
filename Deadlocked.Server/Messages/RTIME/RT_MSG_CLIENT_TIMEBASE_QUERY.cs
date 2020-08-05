using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.RTIME
{
    [Message(RT_MSG_TYPE.RT_MSG_CLIENT_TIMEBASE_QUERY)]
    public class RT_MSG_CLIENT_TIMEBASE_QUERY : BaseMessage
    {
        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_CLIENT_TIMEBASE_QUERY;

        public uint Timestamp { get; set; }

        public override void Deserialize(BinaryReader reader)
        {
            Timestamp = reader.ReadUInt32();
        }

        protected override void Serialize(BinaryWriter writer)
        {
            writer.Write(Timestamp);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"Timestamp:{Timestamp}";
        }
    }
}
