using RT.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_SERVER_TIMEBASE_QUERY_NOTIFY)]
    public class RT_MSG_SERVER_TIMEBASE_QUERY_NOTIFY : BaseScertMessage
    {
        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_SERVER_TIMEBASE_QUERY_NOTIFY;

        public uint ClientTime { get; set; }
        public uint ServerTime { get; set; }

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            ClientTime = reader.ReadUInt32();
            ServerTime = reader.ReadUInt32();
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            writer.Write(ClientTime);
            writer.Write(ServerTime);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"ClientTime:{ClientTime} " +
                $"ServerTime:{ServerTime}";
        }
    }
}
