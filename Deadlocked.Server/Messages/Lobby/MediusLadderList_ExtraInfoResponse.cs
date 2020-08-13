using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.Lobby
{
    [MediusApp(MediusAppPacketIds.LadderList_ExtraInfoResponse)]
    public class MediusLadderList_ExtraInfoResponse : BaseLobbyMessage
    {

        public override MediusAppPacketIds Id => MediusAppPacketIds.LadderList_ExtraInfoResponse;

        public MediusCallbackStatus StatusCode;
        public uint LadderPosition;
        public int LadderStat;
        public int AccountID;
        public string AccountName; // ACCOUNTNAME_MAXLEN
        public byte[] AccountStats = new byte[MediusConstants.ACCOUNTSTATS_MAXLEN];
        public MediusPlayerOnlineState OnlineState;
        public bool EndOfList;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            StatusCode = reader.Read<MediusCallbackStatus>();
            LadderPosition = reader.ReadUInt32();
            LadderStat = reader.ReadInt32();
            AccountID = reader.ReadInt32();
            AccountName = reader.ReadString(MediusConstants.ACCOUNTNAME_MAXLEN);
            AccountStats = reader.ReadBytes(MediusConstants.ACCOUNTSTATS_MAXLEN);
            OnlineState = reader.Read<MediusPlayerOnlineState>();
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
            writer.Write(LadderPosition);
            writer.Write(LadderStat);
            writer.Write(AccountID);
            writer.Write(AccountName, MediusConstants.ACCOUNTNAME_MAXLEN);
            writer.Write(AccountStats);
            writer.Write(OnlineState);
            writer.Write(EndOfList);
            writer.Write(new byte[3]);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"StatusCode:{StatusCode}" + " " +
$"LadderPosition:{LadderPosition}" + " " +
$"LadderStat:{LadderStat}" + " " +
$"AccountID:{AccountID}" + " " +
$"AccountName:{AccountName}" + " " +
$"AccountStats:{AccountStats}" + " " +
$"OnlineState:{OnlineState}" + " " +
$"EndOfList:{EndOfList}";
        }
    }
}