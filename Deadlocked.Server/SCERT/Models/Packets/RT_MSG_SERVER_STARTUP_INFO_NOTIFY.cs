using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.SCERT.Models.Packets
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_SERVER_STARTUP_INFO_NOTIFY)]
    public class RT_MSG_SERVER_STARTUP_INFO_NOTIFY : BaseScertMessage
    {
        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_SERVER_STARTUP_INFO_NOTIFY;

        public byte[] Contents { get; set; }

        public override void Deserialize(BinaryReader reader)
        {
            Contents = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
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
