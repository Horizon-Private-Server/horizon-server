﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Deadlocked.Server.SCERT.Models.Packets
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_SERVER_CONNECT_ACCEPT_TCP)]
    public class RT_MSG_SERVER_CONNECT_ACCEPT_TCP : BaseScertMessage
    {

        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_SERVER_CONNECT_ACCEPT_TCP;

        // 
        public ushort UNK_00 = 0x0000;
        public ushort UNK_02 = 0x10EC;
        public byte UNK_04 = 0x00;
        public byte UNK_05 = 0x00;

        public ushort UNK_06 = 0x0001;

        public IPAddress IP;

        public override void Deserialize(BinaryReader reader)
        {
            UNK_00 = reader.ReadUInt16();
            UNK_02 = reader.ReadUInt16();
            UNK_04 = reader.ReadByte();
            UNK_05 = reader.ReadByte();
            UNK_06 = reader.ReadUInt16();

            IP = reader.ReadIPAddress();
        }

        protected override void Serialize(BinaryWriter writer)
        {
            writer.Write(UNK_00);
            writer.Write(UNK_02);
            writer.Write(UNK_04);
            writer.Write(UNK_05);
            writer.Write(UNK_06);

            if (IP == null)
                writer.Write(IPAddress.Any);
            else
                writer.Write(IP);
        }
    }
}