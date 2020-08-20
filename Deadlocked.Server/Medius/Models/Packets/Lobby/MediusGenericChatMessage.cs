using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.Lobby
{
    [MediusMessage(TypesAAA.GenericChatMessage)]
    public class MediusGenericChatMessage : BaseLobbyMessage
    {

        public override TypesAAA MessageType => TypesAAA.GenericChatMessage;

        public string SessionKey; // SESSIONKEY_MAXLEN
        public MediusChatMessageType MessageType;
        public int TargetID;
        public string Message; // CHATMESSAGE_MAXLEN

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            SessionKey = reader.ReadString(MediusConstants.SESSIONKEY_MAXLEN);
            reader.ReadBytes(2);
            MessageType = reader.Read<MediusChatMessageType>();
            TargetID = reader.ReadInt32();
            Message = reader.ReadString(MediusConstants.CHATMESSAGE_MAXLEN);
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(SessionKey, MediusConstants.SESSIONKEY_MAXLEN);
            writer.Write(new byte[2]);
            writer.Write(MessageType);
            writer.Write(TargetID);
            writer.Write(Message, MediusConstants.CHATMESSAGE_MAXLEN);
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