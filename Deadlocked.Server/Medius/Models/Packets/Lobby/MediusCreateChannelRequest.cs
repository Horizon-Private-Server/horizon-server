using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.Lobby
{
	[MediusMessage(NetMessageTypes.MessageClassLobbyExt, MediusLobbyExtMessageIds.CreateChannel)]
    public class MediusCreateChannelRequest : BaseLobbyExtMessage
    {

		public override byte PacketType => (byte)MediusLobbyExtMessageIds.CreateChannel;

        public string SessionKey; // SESSIONKEY_MAXLEN
        public int ApplicationID;
        public int MaxPlayers;
        public string LobbyName; // LOBBYNAME_MAXLEN
        public string LobbyPassword; // LOBBYPASSWORD_MAXLEN
        public uint GenericField1;
        public uint GenericField2;
        public uint GenericField3;
        public uint GenericField4;
        public MediusWorldGenericFieldLevelType GenericFieldLevel;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            SessionKey = reader.ReadString(MediusConstants.SESSIONKEY_MAXLEN);
            reader.ReadBytes(2);
            ApplicationID = reader.ReadInt32();
            MaxPlayers = reader.ReadInt32();
            LobbyName = reader.ReadString(MediusConstants.LOBBYNAME_MAXLEN);
            LobbyPassword = reader.ReadString(MediusConstants.LOBBYPASSWORD_MAXLEN);
            GenericField1 = reader.ReadUInt32();
            GenericField2 = reader.ReadUInt32();
            GenericField3 = reader.ReadUInt32();
            GenericField4 = reader.ReadUInt32();
            GenericFieldLevel = reader.Read<MediusWorldGenericFieldLevelType>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(SessionKey, MediusConstants.SESSIONKEY_MAXLEN);
            writer.Write(new byte[2]);
            writer.Write(ApplicationID);
            writer.Write(MaxPlayers);
            writer.Write(LobbyName, MediusConstants.LOBBYNAME_MAXLEN);
            writer.Write(LobbyPassword, MediusConstants.LOBBYPASSWORD_MAXLEN);
            writer.Write(GenericField1);
            writer.Write(GenericField2);
            writer.Write(GenericField3);
            writer.Write(GenericField4);
            writer.Write(GenericFieldLevel);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"SessionKey:{SessionKey}" + " " +
$"ApplicationID:{ApplicationID}" + " " +
$"MaxPlayers:{MaxPlayers}" + " " +
$"LobbyName:{LobbyName}" + " " +
$"LobbyPassword:{LobbyPassword}" + " " +
$"GenericField1:{GenericField1:X8}" + " " +
$"GenericField2:{GenericField2:X8}" + " " +
$"GenericField3:{GenericField3:X8}" + " " +
$"GenericField4:{GenericField4:X8}" + " " +
$"GenericFieldLevel:{GenericFieldLevel}";
        }
    }
}
