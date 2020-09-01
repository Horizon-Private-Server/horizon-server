using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_CLIENT_CONNECT_TCP)]
    public class RT_MSG_CLIENT_CONNECT_TCP : BaseScertMessage
    {
        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_CLIENT_CONNECT_TCP;

        // 
        public uint ARG1;
        public int AppId;
        public byte[] UNK;

        public string SessionKey = null;
        public string AccessToken = null;

        public override void Deserialize(BinaryReader reader)
        {
            SessionKey = null;
            AccessToken = null;

            ARG1 = reader.ReadUInt32();
            AppId = reader.ReadInt32();
            UNK = reader.ReadBytes(0x40);

            if (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                SessionKey = reader.ReadString(Constants.SESSIONKEY_MAXLEN);
                AccessToken = reader.ReadString(Constants.NET_ACCESS_KEY_LEN);
            }
        }

        protected override void Serialize(BinaryWriter writer)
        {
            if (UNK == null || UNK.Length != 0x40)
                throw new InvalidOperationException($"Unable to serialize {Id} UNK because UNK is either null or not 64 bytes long!");

            writer.Write(ARG1);
            writer.Write(AppId);
            writer.Write(UNK);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"ARG1:{ARG1:X8} " +
                $"ARG2:{AppId:X8} " +
                $"UNK:{(UNK == null ? "null" : BitConverter.ToString(UNK))}";
        }
    }
}
