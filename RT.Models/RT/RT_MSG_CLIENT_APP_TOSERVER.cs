using RT.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Server.Common;

namespace RT.Models
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_CLIENT_APP_TOSERVER)]
    public class RT_MSG_CLIENT_APP_TOSERVER : BaseScertMessage
    {
        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_CLIENT_APP_TOSERVER;

        public BaseMediusMessage Message { get; set; } = null;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            Message = BaseMediusMessage.Instantiate(reader);
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            if (Message != null)
            {
                writer.Write(Message.PacketClass);
                writer.Write(Message.PacketType);
                Message.Serialize(writer);
            }
        }

        public override bool CanLog()
        {
            return base.CanLog() && (Message?.CanLog() ?? true);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"Message:{Message}";
        }
    }
}
