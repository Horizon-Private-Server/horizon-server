using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_CLIENT_SET_RECV_FLAG)]
    public class RT_MSG_CLIENT_SET_RECV_FLAG : BaseScertMessage
    {
        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_CLIENT_SET_RECV_FLAG;

        public RT_RECV_FLAG Flag { get; set; }

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            Flag = reader.Read<RT_RECV_FLAG>();
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            writer.Write(Flag);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"Flag:{Flag}";
        }
    }
}
