using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages
{
    public class RawAppMessage : BaseAppMessage
    {

        protected ushort _id;
        public override MediusAppPacketIds Id => (MediusAppPacketIds)_id;

        public byte[] Contents { get; set; }

        public RawAppMessage()
        {

        }

        public RawAppMessage(ushort id)
        {
            _id = id;
        }

        public RawAppMessage(MediusAppPacketIds id)
        {
            _id = (ushort)id;
        }
        public override void Deserialize(BinaryReader reader)
        {
            Contents = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(Contents);
        }

    }
}
