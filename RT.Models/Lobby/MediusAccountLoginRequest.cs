using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
	[MediusMessage(NetMessageTypes.MessageClassLobby, MediusLobbyMessageIds.AccountLogin)]
    public class MediusAccountLoginRequest : BaseLobbyMessage
    {
		public override byte PacketType => (byte)MediusLobbyMessageIds.AccountLogin;

        public string SessionKey = "13088";
        public string Username;
        public string Password;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            SessionKey = reader.ReadString(Constants.SESSIONKEY_MAXLEN);
            Username = reader.ReadString(Constants.ACCOUNTNAME_MAXLEN);
            Password = reader.ReadString(Constants.PASSWORD_MAXLEN);
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(SessionKey, Constants.SESSIONKEY_MAXLEN);
            writer.Write(Username, Constants.ACCOUNTNAME_MAXLEN);
            writer.Write(Password, Constants.PASSWORD_MAXLEN);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                $"SessionKey:{SessionKey} " +
                $"USERNAME:{Username} " +
                $"PASS:{Password}";
        }
    }
}
