using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.Lobby
{
    [MediusApp(MediusAppPacketIds.LobbyWorldPlayerListResponse)]
    public class MediusLobbyWorldPlayerListResponse : BaseLobbyMessage
    {

        public override MediusAppPacketIds Id => MediusAppPacketIds.LobbyWorldPlayerListResponse;

        public MediusCallbackStatus StatusCode;
        public MediusPlayerStatus PlayerStatus;
        public int AccountID;
        public string AccountName; // ACCOUNTNAME_MAXLEN
        public string Stats; // ACCOUNTSTATS_MAXLEN
        public MediusConnectionType ConnectionClass;
        public bool EndOfList;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            StatusCode = reader.Read<MediusCallbackStatus>();
            PlayerStatus = reader.Read<MediusPlayerStatus>();
            AccountID = reader.ReadInt32();
            AccountName = reader.ReadString(MediusConstants.ACCOUNTNAME_MAXLEN);
            Stats = reader.ReadString(MediusConstants.ACCOUNTSTATS_MAXLEN);
            ConnectionClass = reader.Read<MediusConnectionType>();
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
            writer.Write(PlayerStatus);
            writer.Write(AccountID);
            writer.Write(AccountName, MediusConstants.ACCOUNTNAME_MAXLEN);
            writer.Write(Stats, MediusConstants.ACCOUNTSTATS_MAXLEN);
            writer.Write(ConnectionClass);
            writer.Write(EndOfList);
            writer.Write(new byte[3]);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"StatusCode:{StatusCode}" + " " +
$"PlayerStatus:{PlayerStatus}" + " " +
$"AccountID:{AccountID}" + " " +
$"AccountName:{AccountName}" + " " +
$"Stats:{Stats}" + " " +
$"ConnectionClass:{ConnectionClass}" + " " +
$"EndOfList:{EndOfList}";
        }
    }
}