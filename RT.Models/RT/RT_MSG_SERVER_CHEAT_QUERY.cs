using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_SERVER_CHEAT_QUERY)]
    public class RT_MSG_SERVER_CHEAT_QUERY : BaseScertMessage
    {
        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_SERVER_CHEAT_QUERY;

        // 
        public CheatQueryType QueryType;
        public int SequenceId;
        public uint Address;
        public int Length;
        public byte[] Data;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            QueryType = reader.Read<CheatQueryType>();
            SequenceId = reader.ReadInt32();
            Address = reader.ReadUInt32();
            Length = reader.ReadInt32();
            Data = reader.ReadRest();
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            writer.Write(QueryType);
            writer.Write(SequenceId);
            writer.Write(Address);
            writer.Write(Length);
            if (Data != null)
                writer.Write(Data);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"QueryType:{QueryType} " +
                $"SequenceId:{SequenceId} " +
                $"Address:{Address:X8} " +
                $"Length:{Length} " +
                $"Data:{(Data == null ? "" : BitConverter.ToString(Data))}";
        }
    }
}
