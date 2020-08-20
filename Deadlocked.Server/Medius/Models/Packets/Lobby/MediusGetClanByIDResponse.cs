using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.Lobby
{
    [MediusMessage(TypesAAA.GetClanByIDResponse)]
    public class MediusGetClanByIDResponse : BaseLobbyMessage
    {

        public override TypesAAA MessageType => TypesAAA.GetClanByIDResponse;

        public MediusCallbackStatus StatusCode;
        public int ApplicationID;
        public string ClanName; // CLANNAME_MAXLEN
        public int LeaderAccountID;
        public string LeaderAccountName; // ACCOUNTNAME_MAXLEN
        public string Stats; // CLANSTATS_MAXLEN
        public MediusClanStatus Status;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            StatusCode = reader.Read<MediusCallbackStatus>();
            ApplicationID = reader.ReadInt32();
            ClanName = reader.ReadString(MediusConstants.CLANNAME_MAXLEN);
            LeaderAccountID = reader.ReadInt32();
            LeaderAccountName = reader.ReadString(MediusConstants.ACCOUNTNAME_MAXLEN);
            Stats = reader.ReadString(MediusConstants.CLANSTATS_MAXLEN);
            Status = reader.Read<MediusClanStatus>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(StatusCode);
            writer.Write(ApplicationID);
            writer.Write(ClanName, MediusConstants.CLANNAME_MAXLEN);
            writer.Write(LeaderAccountID);
            writer.Write(LeaderAccountName, MediusConstants.ACCOUNTNAME_MAXLEN);
            writer.Write(Stats, MediusConstants.CLANSTATS_MAXLEN);
            writer.Write(Status);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"StatusCode:{StatusCode}" + " " +
$"ApplicationID:{ApplicationID}" + " " +
$"ClanName:{ClanName}" + " " +
$"LeaderAccountID:{LeaderAccountID}" + " " +
$"LeaderAccountName:{LeaderAccountName}" + " " +
$"Stats:{Stats}" + " " +
$"Status:{Status}";
        }
    }
}