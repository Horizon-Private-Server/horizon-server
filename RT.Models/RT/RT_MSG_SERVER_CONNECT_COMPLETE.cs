using RT.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_SERVER_CONNECT_COMPLETE)]
    public class RT_MSG_SERVER_CONNECT_COMPLETE : BaseScertMessage
    {

        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_SERVER_CONNECT_COMPLETE;

        // 
        public ushort ARG1 = 0x0001;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            ARG1 = reader.ReadUInt16();
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            writer.Write(ARG1);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"ARG1:{ARG1}";
        }
    }
}
