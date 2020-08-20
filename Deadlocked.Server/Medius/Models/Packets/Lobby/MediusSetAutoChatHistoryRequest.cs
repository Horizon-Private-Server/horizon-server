using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.Lobby
{
	[MediusMessage(NetMessageTypes.MessageClassLobbyExt, MediusLobbyExtMessageIds.SetAutoChatHistoryRequest)]
    public class MediusSetAutoChatHistoryRequest : BaseLobbyExtMessage
    {
		public override byte PacketType => (byte)MediusLobbyExtMessageIds.SetAutoChatHistoryRequest;

        public int AutoChatHistoryNumMessages;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            AutoChatHistoryNumMessages = reader.ReadInt32();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(AutoChatHistoryNumMessages);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"AutoChatHistoryNumMessages:{AutoChatHistoryNumMessages}";
        }
    }
}
