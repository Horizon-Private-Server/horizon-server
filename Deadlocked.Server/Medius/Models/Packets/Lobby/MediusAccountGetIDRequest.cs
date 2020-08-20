using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.Lobby
{
	[MediusMessage(NetMessageTypes.MessageClassLobby, MediusLobbyMessageIds.AccountGetID)]
    public class MediusAccountGetIDRequest : BaseLobbyMessage
    {

		public override byte PacketType => (byte)MediusLobbyMessageIds.AccountGetID;

        public string SessionKey; // SESSIONKEY_MAXLEN
        public string AccountName; // ACCOUNTNAME_MAXLEN

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            SessionKey = reader.ReadString(MediusConstants.SESSIONKEY_MAXLEN);
            AccountName = reader.ReadString(MediusConstants.ACCOUNTNAME_MAXLEN);
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(SessionKey, MediusConstants.SESSIONKEY_MAXLEN);
            writer.Write(AccountName, MediusConstants.ACCOUNTNAME_MAXLEN);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"SessionKey:{SessionKey}" + " " +
$"AccountName:{AccountName}";
        }
    }
}
