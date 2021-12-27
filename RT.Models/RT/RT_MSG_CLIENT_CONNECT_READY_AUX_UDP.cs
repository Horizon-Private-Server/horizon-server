using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RT.Common;
using Server.Common;

namespace RT.Models
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_CLIENT_CONNECT_READY_AUX_UDP)]
    public class RT_MSG_CLIENT_CONNECT_READY_AUX_UDP : BaseScertMessage
    {
        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_CLIENT_CONNECT_READY_AUX_UDP;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {

        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {

        }
    }
}
