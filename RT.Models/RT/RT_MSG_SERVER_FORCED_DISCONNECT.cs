using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_SERVER_FORCED_DISCONNECT)]
    public class RT_MSG_SERVER_FORCED_DISCONNECT : BaseScertMessage
    {
        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_SERVER_FORCED_DISCONNECT;

        public SERVER_FORCE_DISCONNECT_REASON Reason;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            Reason = reader.Read<SERVER_FORCE_DISCONNECT_REASON>();
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            writer.Write(Reason);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"Reason:{Reason}";
        }
    }
}
