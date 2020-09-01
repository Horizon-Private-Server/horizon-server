using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
    public class MediusFileAttributes : IStreamSerializer
    {
        public byte[] Description = new byte[Constants.MEDIUS_FILE_MAX_DESCRIPTION_LENGTH];
        public uint LastChangedTimeStamp;
        public uint LastChangedByUserID;
        public uint NumberAccesses;
        public uint StreamableFlag;
        public uint StreamingDataRate;

        public virtual void Deserialize(BinaryReader reader)
        {
            // 
            Description = reader.ReadBytes(Constants.MEDIUS_FILE_MAX_DESCRIPTION_LENGTH);
            LastChangedTimeStamp = reader.ReadUInt32();
            LastChangedByUserID = reader.ReadUInt32();
            NumberAccesses = reader.ReadUInt32();
            StreamableFlag = reader.ReadUInt32();
            StreamingDataRate = reader.ReadUInt32();
        }

        public virtual void Serialize(BinaryWriter writer)
        {
            // 
            writer.Write(Description, Constants.MEDIUS_FILE_MAX_DESCRIPTION_LENGTH);
            writer.Write(LastChangedTimeStamp);
            writer.Write(LastChangedByUserID);
            writer.Write(NumberAccesses);
            writer.Write(StreamableFlag);
            writer.Write(StreamingDataRate);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                $"Description:{Description} " +
                $"LastChangedTimeStamp:{LastChangedTimeStamp} " +
                $"LastChangedByUserID:{LastChangedByUserID} " +
                $"NumberAccesses:{NumberAccesses} " +
                $"StreamableFlag:{StreamableFlag} " +
                $"StreamingDataRate:{StreamingDataRate}";
        }
    }
}
