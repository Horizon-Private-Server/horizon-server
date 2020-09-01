using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
	[MediusMessage(NetMessageTypes.MessageClassLobbyExt, MediusLobbyExtMessageIds.BinaryFwdMessage)]
    public class MediusBinaryFwdMessage : BaseLobbyExtMessage
    {

		public override byte PacketType => (byte)MediusLobbyExtMessageIds.BinaryFwdMessage;

        public int OriginatorAccountID;
        public MediusBinaryMessageType MessageType;
        public byte[] Message = new byte[Constants.BINARYMESSAGE_MAXLEN];

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            reader.ReadBytes(3);
            OriginatorAccountID = reader.ReadInt32();
            MessageType = reader.Read<MediusBinaryMessageType>();
            Message = reader.ReadBytes(Constants.BINARYMESSAGE_MAXLEN);
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(OriginatorAccountID);
            writer.Write(MessageType);
            writer.Write(Message, Constants.BINARYMESSAGE_MAXLEN);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"OriginatorAccountID:{OriginatorAccountID} " +
$"MessageType:{MessageType} " +
$"Message:{Message}";
        }
    }
}
