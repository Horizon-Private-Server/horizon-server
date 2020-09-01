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

        public byte[] Contents { get; set; }

        public override void Deserialize(BinaryReader reader)
        {
            Contents = reader.ReadRest();
        }

        protected override void Serialize(BinaryWriter writer)
        {
            writer.Write(Contents);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"Contents:{BitConverter.ToString(Contents)}";
        }
    }
}
