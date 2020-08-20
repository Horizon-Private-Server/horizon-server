using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Deadlocked.Server.SCERT.Models.Packets
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_CLIENT_CONNECT_AUX_UDP)]
    public class RT_MSG_CLIENT_CONNECT_AUX_UDP : BaseScertMessage
    {
        // 03 00 00 00 B0 2B 00 00 31 36 30 2E 33 33 2E 33 34 2E 39 30 00 00 00 00 5F 27 00 00 D4 52 00 00 


        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_CLIENT_CONNECT_AUX_UDP;

        // 
        public uint ARG1;
        public int ApplicationId;
        public IPEndPoint EndPoint;
        public byte UNK_22;
        public byte UNK_23;
        public byte UNK_24;
        public byte UNK_25;
        public byte UNK_26;
        public byte UNK_27;

        public override void Deserialize(BinaryReader reader)
        {
            ARG1 = reader.ReadUInt32();
            ApplicationId = reader.ReadInt32();
            EndPoint = new IPEndPoint(reader.ReadIPAddress(), (int)reader.ReadUInt16());
            UNK_22 = reader.ReadByte();
            UNK_23 = reader.ReadByte();
            UNK_24 = reader.ReadByte();
            UNK_25 = reader.ReadByte();
            UNK_26 = reader.ReadByte();
            UNK_27 = reader.ReadByte();
        }

        protected override void Serialize(BinaryWriter writer)
        {
            writer.Write(ARG1);
            writer.Write(ApplicationId);
            writer.Write(EndPoint.Address);
            writer.Write((ushort)EndPoint.Port);
            writer.Write(UNK_22);
            writer.Write(UNK_23);
            writer.Write(UNK_24);
            writer.Write(UNK_25);
            writer.Write(UNK_26);
            writer.Write(UNK_27);
        }
    }
}
