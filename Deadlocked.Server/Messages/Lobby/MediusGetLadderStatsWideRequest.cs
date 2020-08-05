using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.App
{
    [MediusApp(MediusAppPacketIds.GetLadderStatsWide)]
    public class MediusGetLadderStatsWideRequest : BaseLobbyMessage
    {

        public override MediusAppPacketIds Id => MediusAppPacketIds.GetLadderStatsWide;

        public int AccountID_or_ClanID;
        public MediusLadderType LadderType;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            AccountID_or_ClanID = reader.ReadInt32();
            LadderType = reader.Read<MediusLadderType>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(AccountID_or_ClanID);
            writer.Write(LadderType);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"AccountID_or_ClanID:{AccountID_or_ClanID}" + " " +
$"LadderType:{LadderType}";
        }
    }
}