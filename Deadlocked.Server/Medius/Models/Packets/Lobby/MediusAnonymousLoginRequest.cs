using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.Lobby
{
	[MediusMessage(NetMessageTypes.MessageClassLobby, MediusLobbyMessageIds.AnonymousLogin)]
    public class MediusAnonymousLoginRequest : BaseLobbyMessage
    {

		public override byte PacketType => (byte)MediusLobbyMessageIds.AnonymousLogin;

        public string SessionKey; // SESSIONKEY_MAXLEN
        public string SessionDisplayName; // ACCOUNTNAME_MAXLEN
        public string SessionDisplayStats; // ACCOUNTSTATS_MAXLEN

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            SessionKey = reader.ReadString(Constants.SESSIONKEY_MAXLEN);
            SessionDisplayName = reader.ReadString(Constants.ACCOUNTNAME_MAXLEN);
            SessionDisplayStats = reader.ReadString(Constants.ACCOUNTSTATS_MAXLEN);
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(SessionKey, Constants.SESSIONKEY_MAXLEN);
            writer.Write(SessionDisplayName, Constants.ACCOUNTNAME_MAXLEN);
            writer.Write(SessionDisplayStats, Constants.ACCOUNTSTATS_MAXLEN);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"SessionKey:{SessionKey}" + " " +
$"SessionDisplayName:{SessionDisplayName}" + " " +
$"SessionDisplayStats:{SessionDisplayStats}";
        }
    }
}
