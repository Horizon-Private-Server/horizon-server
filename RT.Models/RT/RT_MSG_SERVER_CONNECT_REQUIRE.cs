using RT.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_SERVER_CONNECT_REQUIRE)]
    public class RT_MSG_SERVER_CONNECT_REQUIRE : BaseScertMessage
    {

        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_SERVER_CONNECT_REQUIRE;

        public byte ReqServerPassword;
        public byte[] Contents = new byte[] { 0x48, 0x02 };

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            ReqServerPassword = reader.ReadByte();
            Contents = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            writer.Write(ReqServerPassword);
            writer.Write(Contents);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"ServerPassword: {ReqServerPassword} " +
                $"Contents: {BitConverter.ToString(Contents)}";

        }
    }
}
