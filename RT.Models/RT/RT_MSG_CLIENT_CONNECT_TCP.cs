using RT.Common;
using Server.Common;
using System;

namespace RT.Models
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_CLIENT_CONNECT_TCP)]
    public class RT_MSG_CLIENT_CONNECT_TCP : BaseScertMessage
    {
        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_CLIENT_CONNECT_TCP;

        // 
        public uint TargetWorldId;
        public byte UNK0;
        public int AppId;
        public RSA_KEY Key;

        public string SessionKey = null;
        public string AccessToken = null;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            SessionKey = null;
            AccessToken = null;

            TargetWorldId = reader.ReadUInt32();
            if (reader.MediusVersion < 109)
                UNK0 = reader.ReadByte();
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
            if (writer.MediusVersion < 109)
                writer.Write(UNK0);
            writer.Write(AppId);
            writer.Write(Key ?? RSA_KEY.Empty);

            if(writer.BaseStream.Position < writer.BaseStream.Length)
            {
                writer.Write(SessionKey);
                writer.Write(AccessToken);
            }
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"TargetWorldId: {TargetWorldId:X8} " +
                $"UNK0: {UNK0:X2} " +
                $"AppId: {Convert.ToInt32(AppId)} " +
                $"Key: {Key} " +
                $"SessionKey: {SessionKey} " +
                $"AccessToken: {AccessToken}";
        }
    }
}