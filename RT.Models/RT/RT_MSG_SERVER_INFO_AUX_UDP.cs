using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace RT.Models
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_SERVER_INFO_AUX_UDP)]
    public class RT_MSG_SERVER_INFO_AUX_UDP : BaseScertMessage
    {

        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_SERVER_INFO_AUX_UDP;

        // 
        public IPAddress Ip = IPAddress.Any;
        public ushort Port;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            Ip = IPAddress.Parse(reader.ReadString(16));
            Port = reader.ReadUInt16();
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            writer.Write(Ip?.MapToIPv4()?.ToString(), 16);
            writer.Write(Port);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"Ip:{Ip} " +
                $"Port:{Port}";
        }
    }
}
