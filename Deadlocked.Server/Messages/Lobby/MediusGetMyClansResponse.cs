using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.Lobby
{
    [MediusApp(MediusAppPacketIds.GetMyClansResponse)]
    public class MediusGetMyClansResponse : BaseLobbyMessage
    {

        public override MediusAppPacketIds Id => MediusAppPacketIds.GetMyClansResponse;

        public MediusCallbackStatus StatusCode;
        public int ClanID;
        public int ApplicationID;
        public string ClanName; // CLANNAME_MAXLEN
        public int LeaderAccountID;
        public string LeaderAccountName; // ACCOUNTNAME_MAXLEN
        public string Stats; // CLANSTATS_MAXLEN
        public MediusClanStatus Status;
        public bool EndOfList;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            StatusCode = reader.Read<MediusCallbackStatus>();
            ClanID = reader.ReadInt32();
            ApplicationID = reader.ReadInt32();
            ClanName = reader.ReadString(MediusConstants.CLANNAME_MAXLEN);
            LeaderAccountID = reader.ReadInt32();
            LeaderAccountName = reader.ReadString(MediusConstants.ACCOUNTNAME_MAXLEN);
            Stats = reader.ReadString(MediusConstants.CLANSTATS_MAXLEN);
            Status = reader.Read<MediusClanStatus>();
            EndOfList = reader.ReadBoolean();
            reader.ReadBytes(3);
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(StatusCode);
            writer.Write(ClanID);
            writer.Write(ApplicationID);
            writer.Write(ClanName, MediusConstants.CLANNAME_MAXLEN);
            writer.Write(LeaderAccountID);
            writer.Write(LeaderAccountName, MediusConstants.ACCOUNTNAME_MAXLEN);
            writer.Write(Stats, MediusConstants.CLANSTATS_MAXLEN);
            writer.Write(Status);
            writer.Write(EndOfList);
            writer.Write(new byte[3]);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"StatusCode:{StatusCode}" + " " +
$"ClanID:{ClanID}" + " " +
$"ApplicationID:{ApplicationID}" + " " +
$"ClanName:{ClanName}" + " " +
$"LeaderAccountID:{LeaderAccountID}" + " " +
$"LeaderAccountName:{LeaderAccountName}" + " " +
$"Stats:{Stats}" + " " +
$"Status:{Status}" + " " +
$"EndOfList:{EndOfList}";
        }
    }
}