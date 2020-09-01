using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.Lobby
{
	[MediusMessage(NetMessageTypes.MessageClassLobbyExt, MediusLobbyExtMessageIds.GenericChatMessage)]
    public class MediusGenericChatMessage : BaseLobbyExtMessage
    {
		public override byte PacketType => (byte)MediusLobbyExtMessageIds.GenericChatMessage;

        public string SessionKey; // SESSIONKEY_MAXLEN
        public MediusChatMessageType MessageType;
        public int TargetID;
        public string Message; // CHATMESSAGE_MAXLEN

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            SessionKey = reader.ReadString(Constants.SESSIONKEY_MAXLEN);
            reader.ReadBytes(2);
            MessageType = reader.Read<MediusChatMessageType>();
            TargetID = reader.ReadInt32();
            Message = reader.ReadString(Constants.CHATMESSAGE_MAXLEN);
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(SessionKey, Constants.SESSIONKEY_MAXLEN);
            writer.Write(new byte[2]);
            writer.Write(MessageType);
            writer.Write(TargetID);
            writer.Write(Message, Constants.CHATMESSAGE_MAXLEN);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"SessionKey:{SessionKey}" + " " +
$"MessageType:{MessageType}" + " " +
$"TargetID:{TargetID}" + " " +
$"Message:{Message}";
        }
    }
}
