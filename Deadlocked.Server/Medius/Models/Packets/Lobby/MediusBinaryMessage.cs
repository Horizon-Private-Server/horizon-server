using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.Lobby
{
	[MediusMessage(NetMessageTypes.MessageClassLobbyExt, MediusLobbyExtMessageIds.BinaryMessage)]
    public class MediusBinaryMessage : BaseLobbyMessage
    {

		public override byte PacketType => (byte)MediusLobbyExtMessageIds.BinaryMessage;

        public string SessionKey; // SESSIONKEY_MAXLEN
        public MediusBinaryMessageType MessageType;
        public int TargetAccountID;
        public byte[] Message = new byte[Constants.BINARYMESSAGE_MAXLEN];

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            SessionKey = reader.ReadString(Constants.SESSIONKEY_MAXLEN);
            reader.ReadBytes(2);
            MessageType = reader.Read<MediusBinaryMessageType>();
            TargetAccountID = reader.ReadInt32();
            Message = reader.ReadBytes(Constants.BINARYMESSAGE_MAXLEN);
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(SessionKey, Constants.SESSIONKEY_MAXLEN);
            writer.Write(new byte[2]);
            writer.Write(MessageType);
            writer.Write(TargetAccountID);
            writer.Write(Message, Constants.BINARYMESSAGE_MAXLEN);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"SessionKey:{SessionKey}" + " " +
$"MessageType:{MessageType}" + " " +
$"TargetAccountID:{TargetAccountID}" + " " +
$"Message:{BitConverter.ToString(Message)}";
        }
    }
}
