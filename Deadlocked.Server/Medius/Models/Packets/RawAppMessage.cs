using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets
{
    public class RawAppMessage : BaseMediusMessage
    {

        protected NetMessageTypes _class;
        public override NetMessageTypes MessageClass => _class;

        protected byte _messageType;
        public override byte MessageType => _messageType;

        public byte[] Contents { get; set; }

        public RawAppMessage()
        {

        }

        public RawAppMessage(NetMessageTypes msgClass, byte messageType)
        {
            _class = msgClass;
            _messageType = messageType;
        }

        public override void Deserialize(BinaryReader reader)
        {
            Contents = reader.ReadRest();
        }

        public override void Serialize(BinaryWriter writer)
        {
            if (Contents != null)
                writer.Write(Contents);
        }

        public override string ToString()
        {
            return base.ToString() + $" MsgClass:{MessageType} MsgType:{MessageType} Contents:{BitConverter.ToString(Contents)}";
        }

    }
}
