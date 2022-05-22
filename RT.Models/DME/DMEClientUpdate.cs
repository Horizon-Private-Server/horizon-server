using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace RT.Models
{
    [MediusMessage(NetMessageTypes.MessageClassDME, MediusDmeMessageIds.ClientUpdate)]
    public class DMEClientUpdate : BaseDMEMessage
    {

        public override byte PacketType => (byte)MediusDmeMessageIds.ClientUpdate;

        public byte UNK_01;
        public byte UNK_02;
        public ushort UNK_03;
        public ushort UNK_04;
        public ushort UNK_05;

        public ushort UNK_06;
        public ushort UNK_07;
        public ushort UNK_08;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            UNK_01 = reader.ReadByte();
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(UNK_01);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                 $"UNK_01: {UNK_01} ";
        }
    }
}