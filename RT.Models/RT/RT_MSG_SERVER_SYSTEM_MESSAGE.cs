using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_SERVER_SYSTEM_MESSAGE)]
    public class RT_MSG_SERVER_SYSTEM_MESSAGE : BaseScertMessage
    {
        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_SERVER_SYSTEM_MESSAGE;

        public byte Severity;
        public byte EncodingType;
        public byte LanguageType;
        public bool EndOfMessage;
        public string Message;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            Severity = reader.ReadByte();
            EncodingType = reader.ReadByte();
            LanguageType = reader.ReadByte();
            EndOfMessage = reader.ReadBoolean();
            Message = reader.ReadRestAsString();
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            writer.Write(Severity);
            writer.Write(EncodingType);
            writer.Write(LanguageType);
            writer.Write(EndOfMessage);
            if (Message != null)
                writer.Write(Message, Message.Length + 1);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"Severity:{Severity} " +
                $"EncodingType:{EncodingType} " +
                $"MediusLanguageType:{LanguageType} " +
                $"EndOfMessage:{EndOfMessage} " +
                $"Message:{Message}";
        }
    }
}
