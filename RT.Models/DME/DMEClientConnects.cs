using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace RT.Models
{
    [MediusMessage(NetMessageTypes.MessageClassDME, MediusDmeMessageIds.ClientConnects)]
    public class DMEClientConnects : BaseDMEMessage
    {

        public override byte PacketType => (byte)MediusDmeMessageIds.ClientConnects;

        public byte UNK_00 = 0x02;
        public byte PlayerIndex = 0;
        public IPAddress PlayerIp;
        public ushort UNK_06 = 0;
        public RSA_KEY Key;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            UNK_00 = reader.ReadByte();
            PlayerIndex = reader.ReadByte();
            PlayerIp = new IPAddress(reader.ReadBytes(4));
            UNK_06 = reader.ReadUInt16();
            Key = reader.Read<RSA_KEY>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(UNK_00);
            writer.Write(PlayerIndex);
            writer.Write(PlayerIp.GetAddressBytes());
            writer.Write(UNK_06);
            writer.Write(Key);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                 $"UNK_00:{UNK_00} " +
                 $"PlayerIndex:{PlayerIndex} " +
                 $"PlayerIp:{PlayerIp} " +
                 $"UNK_06:{UNK_06} " +
                 $"Key:{Key}";
        }
    }
}
