﻿using RT.Common;
using Server.Common;
using System.IO;

namespace RT.Models
{
    public class MessageId : IStreamSerializer
    {
        public readonly static MessageId Empty = new MessageId("");

        public string Value { get; protected set; }

        public MessageId()
        {
            Value = GenerateMessageId();
        }

        public MessageId(string value)
        {
            this.Value = value;
        }

        #region Serialization

        public void Deserialize(BinaryReader reader)
        {
            Value = reader.ReadString(Constants.MESSAGEID_MAXLEN);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Value, Constants.MESSAGEID_MAXLEN);
        }

        #endregion

        public override string ToString()
        {
            return Value;
        }

        private static string GenerateMessageId()
        {
            return "1";
        }
    }
}