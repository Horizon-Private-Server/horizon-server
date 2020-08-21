using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.Lobby
{
	[MediusMessage(NetMessageTypes.MessageClassLobbyExt, MediusLobbyExtMessageIds.BinaryFwdMessage)]
    public class MediusBinaryFwdMessage : BaseLobbyExtMessage
    {

		public override byte PacketType => (byte)MediusLobbyExtMessageIds.BinaryFwdMessage;

        public int OriginatorAccountID;
        public MediusBinaryMessageType MessageType;
        public byte[] Message = new byte[MediusConstants.BINARYMESSAGE_MAXLEN];

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            reader.ReadBytes(3);
            OriginatorAccountID = reader.ReadInt32();
            MessageType = reader.Read<MediusBinaryMessageType>();
            Message = reader.ReadBytes(MediusConstants.BINARYMESSAGE_MAXLEN);
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(OriginatorAccountID);
            writer.Write(MessageType);
            writer.Write(Message);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"OriginatorAccountID:{OriginatorAccountID}" + " " +
$"MessageType:{MessageType}" + " " +
$"Message:{Message}";
        }
    }
}
