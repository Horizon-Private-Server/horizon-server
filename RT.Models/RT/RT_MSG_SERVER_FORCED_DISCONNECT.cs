using RT.Common;
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


        public override void Deserialize(BinaryReader reader)
        {

        }

        protected override void Serialize(BinaryWriter writer)
        {

        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
