using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.Lobby
{
    [MediusApp(MediusAppPacketIds.PlayerInfoResponse)]
    public class MediusPlayerInfoResponse : BaseLobbyMessage
    {

        public override MediusAppPacketIds Id => MediusAppPacketIds.PlayerInfoResponse;

        public MediusCallbackStatus StatusCode;
        public string AccountName; // ACCOUNTNAME_MAXLEN
        public int ApplicationID;
        public MediusPlayerStatus PlayerStatus;
        public MediusConnectionType ConnectionClass;
        public byte[] Stats = new byte[MediusConstants.ACCOUNTSTATS_MAXLEN];

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            StatusCode = reader.Read<MediusCallbackStatus>();
            AccountName = reader.ReadString(MediusConstants.ACCOUNTNAME_MAXLEN);
            ApplicationID = reader.ReadInt32();
            PlayerStatus = reader.Read<MediusPlayerStatus>();
            ConnectionClass = reader.Read<MediusConnectionType>();
            Stats = reader.ReadBytes(MediusConstants.ACCOUNTSTATS_MAXLEN);
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(StatusCode);
            writer.Write(AccountName, MediusConstants.ACCOUNTNAME_MAXLEN);
            writer.Write(ApplicationID);
            writer.Write(PlayerStatus);
            writer.Write(ConnectionClass);
            writer.Write(Stats);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"StatusCode:{StatusCode}" + " " +
$"AccountName:{AccountName}" + " " +
$"ApplicationID:{ApplicationID}" + " " +
$"PlayerStatus:{PlayerStatus}" + " " +
$"ConnectionClass:{ConnectionClass}" + " " +
$"Stats:{BitConverter.ToString(Stats)}";
        }
    }
}