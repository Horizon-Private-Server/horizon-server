using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RT.Models
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_CLIENT_APP_LIST)]
    public class RT_MSG_CLIENT_APP_LIST : BaseScertMessage
    {
        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_CLIENT_APP_LIST;

        public List<int> Targets { get; set; } = new List<int>();
        public byte[] Payload { get; set; }

        public override void Deserialize(BinaryReader reader)
        {
            var size = reader.ReadByte();
            var mask = reader.ReadBytes(size);
            Payload = reader.ReadRest();

            Targets = new List<int>();
            for (int b = 0; b < size; ++b)
                for (int i = 0; i < 8; ++i)
                    if ((mask[b] & (1 << i)) != 0)
                        Targets.Add(i + (b * 8));
        }

        protected override void Serialize(BinaryWriter writer)
        {
            byte[] mask = new byte[(int)Math.Ceiling(Math.Log(Targets.Max(), 2))];
            foreach (var target in Targets)
                mask[target / 8] |= (byte)(1 << (target % 8));

            writer.Write(mask.Length);
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
