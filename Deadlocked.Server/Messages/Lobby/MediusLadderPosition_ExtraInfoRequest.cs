using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.Lobby
{
    [MediusApp(MediusAppPacketIds.LadderPosition_ExtraInfo)]
    public class MediusLadderPosition_ExtraInfoRequest : BaseLobbyMessage
    {

        public override MediusAppPacketIds Id => MediusAppPacketIds.LadderPosition_ExtraInfo;

        public int AccountID;
        public int LadderStatIndex;
        public MediusSortOrder SortOrder;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            AccountID = reader.ReadInt32();
            LadderStatIndex = reader.ReadInt32();
            SortOrder = reader.Read<MediusSortOrder>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(AccountID);
            writer.Write(LadderStatIndex);
            writer.Write(SortOrder);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"AccountID:{AccountID}" + " " +
$"LadderStatIndex:{LadderStatIndex}" + " " +
$"SortOrder:{SortOrder}";
        }
    }
}