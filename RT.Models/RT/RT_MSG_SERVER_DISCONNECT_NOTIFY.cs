using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace RT.Models
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_SERVER_DISCONNECT_NOTIFY)]
    public class RT_MSG_SERVER_DISCONNECT_NOTIFY : BaseScertMessage
    {

        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_SERVER_DISCONNECT_NOTIFY;

        //
        public short PlayerIndex;
        public short ScertId;
        public short UNK_04 = 0;
        public IPAddress IP = IPAddress.Any;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            PlayerIndex = reader.ReadInt16();
            ScertId = reader.ReadInt16();
            UNK_04 = reader.ReadInt16();
            IP = reader.Read<IPAddress>();
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            writer.Write(PlayerIndex);
            writer.Write(ScertId);
            writer.Write(UNK_04);
            writer.Write(IP ?? IPAddress.Any);
        }
    }
}
