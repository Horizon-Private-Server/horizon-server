using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.Lobby
{
	[MediusMessage(NetMessageTypes.MessageClassLobby, MediusLobbyMessageIds.GetClanMemberList_ExtraInfoResponse)]
    public class MediusGetClanMemberList_ExtraInfoResponse : BaseLobbyMessage
    {
		public override byte PacketType => (byte)MediusLobbyMessageIds.GetClanMemberList_ExtraInfoResponse;

        public MediusCallbackStatus StatusCode;
        public int AccountID;
        public string AccountName; // ACCOUNTNAME_MAXLEN
        public string Stats; // ACCOUNTSTATS_MAXLEN
        public MediusPlayerOnlineState OnlineState;
        public int LadderStat;
        public uint LadderPosition;
        public uint TotalRankings;
        public bool EndOfList;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            StatusCode = reader.Read<MediusCallbackStatus>();
            AccountID = reader.ReadInt32();
            AccountName = reader.ReadString(Constants.ACCOUNTNAME_MAXLEN);
            Stats = reader.ReadString(Constants.ACCOUNTSTATS_MAXLEN);
            OnlineState = reader.Read<MediusPlayerOnlineState>();
            LadderStat = reader.ReadInt32();
            LadderPosition = reader.ReadUInt32();
            TotalRankings = reader.ReadUInt32();
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
            writer.Write(AccountID);
            writer.Write(AccountName, Constants.ACCOUNTNAME_MAXLEN);
            writer.Write(Stats, Constants.ACCOUNTSTATS_MAXLEN);
            writer.Write(OnlineState);
            writer.Write(LadderStat);
            writer.Write(LadderPosition);
            writer.Write(TotalRankings);
            writer.Write(EndOfList);
            writer.Write(new byte[3]);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"StatusCode:{StatusCode}" + " " +
$"AccountID:{AccountID}" + " " +
$"AccountName:{AccountName}" + " " +
$"Stats:{Stats}" + " " +
$"OnlineState:{OnlineState}" + " " +
$"LadderStat:{LadderStat}" + " " +
$"LadderPosition:{LadderPosition}" + " " +
$"TotalRankings:{TotalRankings}" + " " +
$"EndOfList:{EndOfList}";
        }
    }
}
