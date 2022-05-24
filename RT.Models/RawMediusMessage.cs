﻿using RT.Common;
using Server.Common;
using Server.Common.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RT.Models
{
    public class RawMediusMessage : BaseMediusMessage
    {

        protected NetMessageClass _class;
        public override NetMessageClass PacketClass => _class;

        protected byte _messageType;
        public override byte PacketType => _messageType;

        public byte[] Contents { get; set; }

        public RawMediusMessage()
        {

        }

        public RawMediusMessage(NetMessageClass msgClass, byte messageType)
        {
            _class = msgClass;
            _messageType = messageType;
        }

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            Contents = reader.ReadRest();
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            if (Contents != null)
                writer.Write(Contents);
        }

        public override string ToString()
        {
            return base.ToString() + $" MsgClass:{PacketClass} MsgType:{PacketType} Contents:{BitConverter.ToString(Contents)}";
        }
    }
}
