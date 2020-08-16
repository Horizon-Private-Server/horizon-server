using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Deadlocked.Server.Messages.RTIME
{
    [Message(RT_MSG_TYPE.RT_MSG_SERVER_CONNECT_ACCEPT_TCP)]
    public class RT_MSG_SERVER_CONNECT_ACCEPT_TCP : BaseMessage
    {

        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_SERVER_CONNECT_ACCEPT_TCP;

        // 
        public byte UNK_00 = 0x00;
        public byte UNK_01 = 0x00;
        public byte UNK_02 = 0xEC;
        public byte UNK_03 = 0x10;
        public byte UNK_04 = 0x00;
        public byte UNK_05 = 0x00;

        public ushort UNK_06 = 0x0001;

        public IPAddress IP;

        public override void Deserialize(BinaryReader reader)
        {
            UNK_00 = reader.ReadByte();
            UNK_01 = reader.ReadByte();
            UNK_02 = reader.ReadByte();
            UNK_03 = reader.ReadByte();
            UNK_04 = reader.ReadByte();
            UNK_05 = reader.ReadByte();
            UNK_06 = reader.ReadUInt16();

            IP = reader.ReadIPAddress();
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

            if (IP == null)
                writer.Write(IPAddress.Any);
            else
                writer.Write(IP);
        }
    }
}
