using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.RTIME
{
    [Message(RT_MSG_TYPE.RT_MSG_CLIENT_CONNECT_TCP)]
    public class RT_MSG_CLIENT_CONNECT_TCP : BaseMessage
    {
        // 01 00 00 00 B0 2B 00 00
        // 1C EE 30 3C 03 53 B1 DA 8A 9E BD 48 0B DD C5 8F F4 FE FF A5 09 16 D2 8F C5 B1 F6 09 0C E6 E8 91 AD 21 FC CF E2 94 CB D6 02 F5 FA 64 3D 12 1A DD E9 57 19 A8 36 C9 E7 CB BF 6D 8D 80 7B 7D 4C 60 


        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_CLIENT_CONNECT_TCP;

        // 
        public uint ARG1;
        public uint ARG2; // This is like a version identifier or something
        public byte[] UNK;

        public string SessionKey = null;
        public string AccessToken = null;

        public override void Deserialize(BinaryReader reader)
        {
            SessionKey = null;
            AccessToken = null;

            ARG1 = reader.ReadUInt32();
            ARG2 = reader.ReadUInt32();
            UNK = reader.ReadBytes(0x40);

            if (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                SessionKey = reader.ReadString(MediusConstants.SESSIONKEY_MAXLEN);
                AccessToken = reader.ReadString(MediusConstants.NET_ACCESS_KEY_LEN);
            }
        }

        protected override void Serialize(BinaryWriter writer)
        {
            if (UNK == null || UNK.Length != 0x40)
                throw new InvalidOperationException($"Unable to serialize {Id} UNK because UNK is either null or not 64 bytes long!");

            writer.Write(ARG1);
            writer.Write(ARG2);
            writer.Write(UNK);
        }
    }
}
