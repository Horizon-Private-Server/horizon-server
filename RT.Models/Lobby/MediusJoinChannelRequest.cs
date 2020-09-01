using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
	[MediusMessage(NetMessageTypes.MessageClassLobby, MediusLobbyMessageIds.JoinChannel)]
    public class MediusJoinChannelRequest : BaseLobbyMessage
    {
		public override byte PacketType => (byte)MediusLobbyMessageIds.JoinChannel;

        public string SessionKey; // SESSIONKEY_MAXLEN
        public int MediusWorldID;
        public string LobbyChannelPassword; // LOBBYPASSWORD_MAXLEN

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            SessionKey = reader.ReadString(Constants.SESSIONKEY_MAXLEN);
            reader.ReadBytes(2);
            MediusWorldID = reader.ReadInt32();
            LobbyChannelPassword = reader.ReadString(Constants.LOBBYPASSWORD_MAXLEN);
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(SessionKey, Constants.SESSIONKEY_MAXLEN);
            writer.Write(new byte[2]);
            writer.Write(MediusWorldID);
            writer.Write(LobbyChannelPassword, Constants.LOBBYPASSWORD_MAXLEN);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"SessionKey:{SessionKey} " +
$"MediusWorldID:{MediusWorldID} " +
$"LobbyChannelPassword:{LobbyChannelPassword}";
        }
    }
}
