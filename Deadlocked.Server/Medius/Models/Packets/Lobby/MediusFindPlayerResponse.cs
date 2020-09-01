using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.Lobby
{
	[MediusMessage(NetMessageTypes.MessageClassLobby, MediusLobbyMessageIds.FindPlayerResponse)]
    public class MediusFindPlayerResponse : BaseLobbyMessage
    {

		public override byte PacketType => (byte)MediusLobbyMessageIds.FindPlayerResponse;

        public MediusCallbackStatus StatusCode;
        public int ApplicationID;
        public string ApplicationName; // APPNAME_MAXLEN
        public MediusApplicationType ApplicationType;
        public int MediusWorldID;
        public int AccountID;
        public string AccountName; // ACCOUNTNAME_MAXLEN
        public bool EndOfList;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            StatusCode = reader.Read<MediusCallbackStatus>();
            ApplicationID = reader.ReadInt32();
            ApplicationName = reader.ReadString(Constants.APPNAME_MAXLEN);
            ApplicationType = reader.Read<MediusApplicationType>();
            MediusWorldID = reader.ReadInt32();
            AccountID = reader.ReadInt32();
            AccountName = reader.ReadString(Constants.ACCOUNTNAME_MAXLEN);
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
            writer.Write(ApplicationID);
            writer.Write(ApplicationName, Constants.APPNAME_MAXLEN);
            writer.Write(ApplicationType);
            writer.Write(MediusWorldID);
            writer.Write(AccountID);
            writer.Write(AccountName, Constants.ACCOUNTNAME_MAXLEN);
            writer.Write(EndOfList);
            writer.Write(new byte[3]);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"StatusCode:{StatusCode}" + " " +
$"ApplicationID:{ApplicationID}" + " " +
$"ApplicationName:{ApplicationName}" + " " +
$"ApplicationType:{ApplicationType}" + " " +
$"MediusWorldID:{MediusWorldID}" + " " +
$"AccountID:{AccountID}" + " " +
$"AccountName:{AccountName}" + " " +
$"EndOfList:{EndOfList}";
        }
    }
}
