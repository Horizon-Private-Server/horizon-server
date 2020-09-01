using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_SERVER_CHEAT_QUERY)]
    public class RT_MSG_SERVER_CHEAT_QUERY : BaseScertMessage
    {
        // SERVER: 05 D0 FA 00 00 00 00 08 00 00 01 00 00 93 E2 9A F5 05 1F E1 4B A9 1E D2 FF 17 E3 C4 C4 
        // CLIENT: 05 D0 FA 00 00 00 00 08 00 10 00 00 00 62 C6 14 61 32 25 3B 7E 44 21 17 79 33 79 88 E1 

        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_SERVER_CHEAT_QUERY;

        // 
        public byte UNK_00 = 0x05;
        public byte UNK_01 = 0xD0;
        public byte UNK_02 = 0xFA;
        public ushort UNK_03 = 0x0000;
        public ushort UNK_05 = 0x0000;
        public ushort UNK_07 = 0x0008;
        public ushort UNK_09 = 0x0100;
        public ushort UNK_0B = 0x0000;
        public byte[] UNK_0D = new byte[16];

        public override void Deserialize(BinaryReader reader)
        {
            UNK_00 = reader.ReadByte();
            UNK_01 = reader.ReadByte();
            UNK_02 = reader.ReadByte();
            UNK_03 = reader.ReadUInt16();
            UNK_05 = reader.ReadUInt16();
            UNK_07 = reader.ReadUInt16();
            UNK_09 = reader.ReadUInt16();
            UNK_0B = reader.ReadUInt16();
            UNK_0D = reader.ReadBytes(16);
        }

        protected override void Serialize(BinaryWriter writer)
        {
            writer.Write(UNK_00);
            writer.Write(UNK_01);
            writer.Write(UNK_02);
            writer.Write(UNK_03);
            writer.Write(UNK_05);
            writer.Write(UNK_07);
            writer.Write(UNK_09);
            writer.Write(UNK_0B);
            writer.Write(UNK_0D);
        }
    }
}
