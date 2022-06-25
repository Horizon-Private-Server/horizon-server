using RT.Common;
using Server.Common;
using System.Net;

namespace RT.Models
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_SERVER_CONNECT_NOTIFY)]
    public class RT_MSG_SERVER_CONNECT_NOTIFY : BaseScertMessage
    {
        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_SERVER_CONNECT_NOTIFY;

        //
        public ushort PlayerIndex;
        public int ScertId;
        public IPAddress IP = IPAddress.Any;
        public RSA_KEY Key = new RSA_KEY();

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            PlayerIndex = reader.ReadUInt16();
            ScertId = reader.ReadInt32();
            IP = reader.Read<IPAddress>();
            Key = reader.Read<RSA_KEY>();
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            writer.Write(PlayerIndex);
            writer.Write(ScertId);
            writer.Write(IP ?? IPAddress.Any);
            writer.Write(Key);
        }
    }
}