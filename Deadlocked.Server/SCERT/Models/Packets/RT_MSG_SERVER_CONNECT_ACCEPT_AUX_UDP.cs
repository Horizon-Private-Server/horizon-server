using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Deadlocked.Server.SCERT.Models.Packets
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_SERVER_CONNECT_ACCEPT_AUX_UDP)]
    public class RT_MSG_SERVER_CONNECT_ACCEPT_AUX_UDP : BaseScertMessage
    {

        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_SERVER_CONNECT_ACCEPT_AUX_UDP;

        // 
        public byte UNK_00;
        public byte UNK_01;
        public byte UNK_02 = 0xD4;
        public byte UNK_03;
        public byte UNK_04;
        public byte UNK_05;
        public ushort UNK_06 = 0x0001;

        public IPEndPoint EndPoint;

        public override void Deserialize(BinaryReader reader)
        {
            UNK_00 = reader.ReadByte();
            UNK_01 = reader.ReadByte();
            UNK_02 = reader.ReadByte();
            UNK_03 = reader.ReadByte();
            UNK_04 = reader.ReadByte();
            UNK_05 = reader.ReadByte();
            UNK_06 = reader.ReadUInt16();

            EndPoint = new IPEndPoint(reader.ReadIPAddress(), (int)reader.ReadUInt16());
        }

        protected override void Serialize(BinaryWriter writer)
        {
            writer.Write(UNK_00);
            writer.Write(UNK_01);
            writer.Write(UNK_02);
            writer.Write(UNK_03);
            writer.Write(UNK_04);
            writer.Write(UNK_05);
            writer.Write(UNK_06);

            writer.Write(EndPoint.Address);
            writer.Write((ushort)EndPoint.Port);
        }
    }
}
