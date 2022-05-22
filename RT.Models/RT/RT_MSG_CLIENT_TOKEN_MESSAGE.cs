﻿using RT.Common;
using Server.Common;
using System;

namespace RT.Models
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_CLIENT_TOKEN_MESSAGE)]
    public class RT_MSG_CLIENT_TOKEN_MESSAGE : BaseScertMessage
    {
        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_CLIENT_TOKEN_MESSAGE;

        public byte[] Contents;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            Contents = reader.ReadRest();
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            writer.Write(Contents);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"Contents: {BitConverter.ToString(Contents)}";
        }
    }
}