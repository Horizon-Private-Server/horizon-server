﻿using RT.Common;

namespace RT.Models
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_CLIENT_TIMEBASE_QUERY)]
    public class RT_MSG_CLIENT_TIMEBASE_QUERY : BaseScertMessage
    {
        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_CLIENT_TIMEBASE_QUERY;

        public uint Timestamp { get; set; }

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            Timestamp = reader.ReadUInt32();
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            writer.Write(Timestamp);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"Timestamp: {Timestamp}";
        }
    }
}