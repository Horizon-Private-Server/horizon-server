using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.Lobby
{
	[MediusMessage(NetMessageTypes.MessageClassLobby, MediusLobbyMessageIds.AccountDelete)]
    public class MediusAccountDeleteRequest : BaseLobbyMessage
    {

		public override byte PacketType => (byte)MediusLobbyMessageIds.AccountDelete;

        public string SessionKey; // SESSIONKEY_MAXLEN
        public string MasterPassword; // PASSWORD_MAXLEN

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            SessionKey = reader.ReadString(MediusConstants.SESSIONKEY_MAXLEN);
            MasterPassword = reader.ReadString(MediusConstants.PASSWORD_MAXLEN);
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(SessionKey, MediusConstants.SESSIONKEY_MAXLEN);
            writer.Write(MasterPassword, MediusConstants.PASSWORD_MAXLEN);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"SessionKey:{SessionKey}" + " " +
$"MasterPassword:{MasterPassword}";
        }
    }
}
