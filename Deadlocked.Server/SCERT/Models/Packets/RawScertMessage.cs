using Deadlocked.Server.Stream;
using DotNetty.Buffers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.SCERT.Models.Packets
{
    public class RawScertMessage : BaseScertMessage
    {

        private readonly RT_MSG_TYPE _id;
        public override RT_MSG_TYPE Id => _id;

        public byte[] Contents { get; set; } = null;


        public RawScertMessage(RT_MSG_TYPE id)
        {
            _id = id;
        }

        #region Serialization

        public override void Deserialize(BinaryReader reader)
        {
            // 
            Contents = reader.ReadRest();
        }

        protected override void Serialize(BinaryWriter writer)
        {
            if (Contents != null)
                writer.Write(Contents);
        }

        #endregion

    }
}
