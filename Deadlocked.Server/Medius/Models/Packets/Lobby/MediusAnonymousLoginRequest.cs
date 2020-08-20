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
            SessionKey = reader.ReadString(MediusConstants.SESSIONKEY_MAXLEN);
            SessionDisplayName = reader.ReadString(MediusConstants.ACCOUNTNAME_MAXLEN);
            SessionDisplayStats = reader.ReadString(MediusConstants.ACCOUNTSTATS_MAXLEN);
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(SessionKey, MediusConstants.SESSIONKEY_MAXLEN);
            writer.Write(SessionDisplayName, MediusConstants.ACCOUNTNAME_MAXLEN);
            writer.Write(SessionDisplayStats, MediusConstants.ACCOUNTSTATS_MAXLEN);
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
