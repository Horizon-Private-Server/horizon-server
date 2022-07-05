using RT.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_CLIENT_DISCONNECT)]
    public class RT_MSG_CLIENT_DISCONNECT : BaseScertMessage
    {
        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_CLIENT_DISCONNECT;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {

        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {

        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
