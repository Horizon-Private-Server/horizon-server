using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.RTIME
{
    [Message(RT_MSG_TYPE.RT_MSG_SERVER_SYSTEM_MESSAGE)]
    public class RT_MSG_SERVER_SYSTEM_MESSAGE : BaseMessage
    {
        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_SERVER_SYSTEM_MESSAGE;

        public byte Severity;
        public byte EncodingType;
        public byte LanguageType;
        public bool EndOfMessage;
        public byte[] Message;

        public override void Deserialize(BinaryReader reader)
        {
            Severity = reader.ReadByte();
            EncodingType = reader.ReadByte();
            LanguageType = reader.ReadByte();
            EndOfMessage = reader.ReadBoolean();
            Message = reader.ReadRest();
        }

        protected override void Serialize(BinaryWriter writer)
        {
            writer.Write(Severity);
            writer.Write(EncodingType);
            writer.Write(LanguageType);
            writer.Write(EndOfMessage);
            if (Message != null)
                writer.Write(Message);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"Severity:{Severity} " +
                $"EncodingType:{EncodingType} " +
                $"MediusLanguageType:{LanguageType} " +
                $"EndOfMessage:{EndOfMessage} " +
                $"Message:{BitConverter.ToString(Message)}";
        }
    }
}
