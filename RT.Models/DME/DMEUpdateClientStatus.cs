using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace RT.Models
{
    [MediusMessage(NetMessageTypes.MessageClassDME, MediusDmeMessageIds.UpdateClientStatus)]
    public class DMEUpdateClientStatus : BaseDMEMessage
    {

        public override byte PacketType => (byte)MediusDmeMessageIds.UpdateClientStatus;

        public NetClientStatus Status = NetClientStatus.ClientStatusJoined;
        public byte PlayerIndex = 0;
        public ushort UNK_04 = 0x03;
        public ushort UNK_06 = 0x00; // 1 for host
        public ushort UNK_08 = 0x00;
        public ushort UNK_0A = 0x02; // 1 for host
        public ushort UNK_0C = 0x00;
        public ushort UNK_0E = 0x00;
        public ushort UNK_10 = 0x00;


        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            Status = reader.Read<NetClientStatus>();
            PlayerIndex = reader.ReadByte();
            UNK_04 = reader.ReadUInt16();
            UNK_06 = reader.ReadUInt16();
            UNK_08 = reader.ReadUInt16();
            UNK_0A = reader.ReadUInt16();
            UNK_0C = reader.ReadUInt16();
            UNK_0E = reader.ReadUInt16();
            UNK_10 = reader.ReadUInt16();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(Status);
            writer.Write(PlayerIndex);
            writer.Write(UNK_04);
            writer.Write(UNK_06);
            writer.Write(UNK_08);
            writer.Write(UNK_0A);
            writer.Write(UNK_0C);
            writer.Write(UNK_0E);
            writer.Write(UNK_10);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"Status:{Status} " +
             $"PlayerIndex:{PlayerIndex} " +
             $"UNK_04:{UNK_04} " +
             $"UNK_06:{UNK_06} " +
             $"UNK_08:{UNK_08} " +
             $"UNK_0A:{UNK_0A} " +
             $"UNK_0C:{UNK_0C} " +
             $"UNK_0E:{UNK_0E} " +
             $"UNK_10:{UNK_10}";
        }
    }
}
