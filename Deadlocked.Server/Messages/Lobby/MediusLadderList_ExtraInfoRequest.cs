using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.Lobby
{
    [MediusApp(MediusAppPacketIds.LadderList_ExtraInfo)]
    public class MediusLadderList_ExtraInfoRequest : BaseLobbyMessage
    {

        public override MediusAppPacketIds Id => MediusAppPacketIds.LadderList_ExtraInfo;

        public int LadderStatIndex;
        public MediusSortOrder SortOrder;
        public uint StartPosition;
        public uint PageSize;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            reader.ReadBytes(3);
            LadderStatIndex = reader.ReadInt32();
            SortOrder = reader.Read<MediusSortOrder>();
            StartPosition = reader.ReadUInt32();
            PageSize = reader.ReadUInt32();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(LadderStatIndex);
            writer.Write(SortOrder);
            writer.Write(StartPosition);
            writer.Write(PageSize);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"LadderStatIndex:{LadderStatIndex}" + " " +
$"SortOrder:{SortOrder}" + " " +
$"StartPosition:{StartPosition}" + " " +
$"PageSize:{PageSize}";
        }
    }
}