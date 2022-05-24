using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
	[MediusMessage(NetMessageClass.MessageClassLobbyExt, MediusLobbyExtMessageIds.GenericChatFwdMessage1)]
    public class MediusGenericChatFwdMessage1 : BaseLobbyExtMessage
    {


		public override byte PacketType => (byte)MediusLobbyExtMessageIds.GenericChatFwdMessage1;

        public uint TimeStamp;
        public int OriginatorAccountID;
        public MediusChatMessageType MessageType;
        public string OriginatorAccountName; // ACCOUNTNAME_MAXLEN
        public string Message; // CHATMESSAGE_MAXLEN

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            TimeStamp = reader.ReadUInt32();
            OriginatorAccountID = reader.ReadInt32();
            MessageType = reader.Read<MediusChatMessageType>();
            OriginatorAccountName = reader.ReadString(Constants.ACCOUNTNAME_MAXLEN);
            Message = reader.ReadString(Constants.CHATMESSAGE_MAXLEN);
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(TimeStamp);
            writer.Write(OriginatorAccountID);
            writer.Write(MessageType);
            writer.Write(OriginatorAccountName, Constants.ACCOUNTNAME_MAXLEN);
            if (Message == null || Message.Length == 0)
            {
                writer.Write(0);
            }
            else
            {
                int len = Message.Length;
                writer.Write(len + 1);
                writer.Write(Message, len + 1);
            }
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                $"TimeStamp:{TimeStamp} " +
                $"OriginatorAccountID:{OriginatorAccountID} " +
                $"MessageType:{MessageType} " +
                $"OriginatorAccountName:{OriginatorAccountName} " +
                $"Message:{Message}";
        }
    }
}
