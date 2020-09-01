using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.Lobby
{
	[MediusMessage(NetMessageTypes.MessageClassLobbyExt, MediusLobbyExtMessageIds.GenericChatSetFilterRequest)]
    public class MediusGenericChatSetFilterRequest : BaseLobbyExtMessage
    {
		public override byte PacketType => (byte)MediusLobbyExtMessageIds.GenericChatSetFilterRequest;

        public string SessionKey; // SESSIONKEY_MAXLEN
        public MediusGenericChatFilter GenericChatFilter;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            SessionKey = reader.ReadString(Constants.SESSIONKEY_MAXLEN);
            GenericChatFilter = reader.Read<MediusGenericChatFilter>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(SessionKey, Constants.SESSIONKEY_MAXLEN);
            writer.Write(GenericChatFilter);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"SessionKey:{SessionKey}" + " " +
$"GenericChatFilter:{GenericChatFilter}";
        }
    }
}
