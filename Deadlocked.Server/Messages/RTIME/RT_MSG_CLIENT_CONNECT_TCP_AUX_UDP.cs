using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.RTIME
{
    [Message(RT_MSG_TYPE.RT_MSG_CLIENT_CONNECT_TCP_AUX_UDP)]
    public class RT_MSG_CLIENT_CONNECT_TCP_AUX_UDP : BaseMessage
    {
        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_CLIENT_CONNECT_TCP_AUX_UDP;

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

        public override string ToString()
        {
            return base.ToString() + " " +
                $"ARG1:{ARG1} " +
                $"ARG2:{ARG2} " +
                $"UNK:{BitConverter.ToString(UNK)} " +
                $"SessionKey:{SessionKey} " +
                $"AccessToken:{AccessToken}";
        }
    }
}
