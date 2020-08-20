using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.Lobby
{
	[MediusMessage(NetMessageTypes.MessageClassLobbyExt, MediusLobbyExtMessageIds.GenericChatFwdMessage)]
    public class MediusGenericChatFwdMessage : BaseMediusMessage
    {

		public override NetMessageTypes PacketClass => NetMessageTypes.MessageClassLobbyExt;
		public override byte PacketType => (byte)MediusLobbyExtMessageIds.GenericChatFwdMessage;

        public uint TimeStamp;
        public int OriginatorAccountID;
        public MediusChatMessageType MessageType;
        public string OriginatorAccountName; // ACCOUNTNAME_MAXLEN
        public string Message; // CHATMESSAGE_MAXLEN

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            TimeStamp = reader.ReadUInt32();
            OriginatorAccountID = reader.ReadInt32();
            MessageType = reader.Read<MediusChatMessageType>();
            OriginatorAccountName = reader.ReadString(MediusConstants.ACCOUNTNAME_MAXLEN);
            Message = reader.ReadString(MediusConstants.CHATMESSAGE_MAXLEN);
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(TimeStamp);
            writer.Write(OriginatorAccountID);
            writer.Write(MessageType);
            writer.Write(OriginatorAccountName, MediusConstants.ACCOUNTNAME_MAXLEN);
            writer.Write(Message, MediusConstants.CHATMESSAGE_MAXLEN);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"TimeStamp:{TimeStamp}" + " " +
$"OriginatorAccountID:{OriginatorAccountID}" + " " +
$"MessageType:{MessageType}" + " " +
$"OriginatorAccountName:{OriginatorAccountName}" + " " +
$"Message:{Message}";
        }
    }
}
