using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_CLIENT_APP_LIST)]
    public class RT_MSG_CLIENT_APP_LIST : BaseScertMessage
    {
        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_CLIENT_APP_LIST;

        public byte UNK { get; set; } = 2;
        public List<int> Targets { get; set; } = new List<int>();
        public byte[] Payload { get; set; }

        public override void Deserialize(BinaryReader reader)
        {
            UNK = reader.ReadByte();
            var mask = reader.ReadUInt16();
            Payload = reader.ReadRest();

            Targets = new List<int>();
            for (int i = 0; i < sizeof(short) * 8; ++i)
                if ((mask & (1 << i)) != 0)
                    Targets.Add(i);
        }

        protected override void Serialize(BinaryWriter writer)
        {
            ushort mask = 0;
            foreach (var target in Targets)
                mask |= (ushort)(1 << target);

            writer.Write(UNK);
            writer.Write(mask);
            writer.Write(Payload);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"Targets:{string.Join(",", Targets)} " +
                $"Payload:{BitConverter.ToString(Payload)}";
        }
    }
}
