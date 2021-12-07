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
        public uint TargetWorldId;
        public int AppId;
        public RSA_KEY Key;

        public string SessionKey = null;
        public string AccessToken = null;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            SessionKey = null;
            AccessToken = null;

            TargetWorldId = reader.ReadUInt32();
            AppId = reader.ReadInt32();
            Key = reader.Read<RSA_KEY>();

            if (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                SessionKey = reader.ReadString(Constants.SESSIONKEY_MAXLEN);
                AccessToken = reader.ReadString(Constants.NET_ACCESS_KEY_LEN);
            }
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            writer.Write(TargetWorldId);
            writer.Write(AppId);
            writer.Write(Key ?? RSA_KEY.Empty);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"ARG1:{TargetWorldId:X8} " +
                $"ARG2:{AppId:X8} " +
                $"Key:{Key}";
        }
    }
}
