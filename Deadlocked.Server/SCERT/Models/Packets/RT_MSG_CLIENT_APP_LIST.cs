using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.SCERT.Models.Packets
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_CLIENT_APP_LIST)]
    public class RT_MSG_CLIENT_APP_LIST : BaseScertMessage
    {
        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_CLIENT_APP_LIST;

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
