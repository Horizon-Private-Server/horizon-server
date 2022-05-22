﻿using RT.Common;
using System.Collections.Generic;
using System.IO;
using Server.Common;
using System.Linq;

namespace RT.Models
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_CLIENT_MULTI_APP_TOSERVER)]
    public class RT_MSG_CLIENT_MULTI_APP_TOSERVER : BaseScertMessage
    {
        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_CLIENT_MULTI_APP_TOSERVER;

        public List<BaseScertMessage> Messages { get; set; } = new List<BaseScertMessage>();

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            Messages = new List<BaseScertMessage>();
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                var message = BaseScertMessage.Instantiate(reader);
                Messages.Add(message);
            }
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            if (Messages != null)
            {
                foreach (var message in Messages)
                {
                    writer.Write(new byte[3]);
                    var start = writer.BaseStream.Position;
                    message.Serialize(writer);
                    var len = writer.BaseStream.Position - start;
                    writer.Seek((int)(start - 3), SeekOrigin.Begin);
                    writer.Write(message.Id);
                    writer.Write((short)len);
                    writer.Seek((int)(start + len), SeekOrigin.Begin);
                }
            }
        }

        public override bool CanLog()
        {
            return base.CanLog();
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"Messages: {string.Join("\r\n", Messages?.Select(x => x.ToString()))}";
        }
    }
}
