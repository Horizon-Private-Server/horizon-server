using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages
{
    public class RawMessage : BaseMessage
    {
        protected RT_MSG_TYPE _id = RT_MSG_TYPE.RT_MSG_CLIENT_CONNECT_TCP;
        public override RT_MSG_TYPE Id => _id;

        public byte[] Contents { get; set; }

        public RawMessage()
        {

        }

        public RawMessage(RT_MSG_TYPE id)
        {
            _id = id;
        }
        public override void Deserialize(BinaryReader reader)
        {
            Contents = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
        }

        protected override void Serialize(BinaryWriter writer)
        {
            writer.Write(Contents);
        }
    }
}
