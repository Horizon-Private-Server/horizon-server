using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Deadlocked.Server.SCERT.Models.Packets
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_SERVER_INFO_AUX_UDP)]
    public class RT_MSG_SERVER_INFO_AUX_UDP : BaseScertMessage
    {
        // 31 36 30 2E 33 33 2E 33 34 2E 39 30 00 00 00 00 52 C3 

        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_SERVER_INFO_AUX_UDP;

        // 
        public IPAddress Ip = IPAddress.Any;
        public ushort Port;

        public override void Deserialize(BinaryReader reader)
        {
            Ip = IPAddress.Parse(reader.ReadString(16));
            Port = reader.ReadUInt16();
        }

        protected override void Serialize(BinaryWriter writer)
        {
            writer.Write(Ip.ToString(), 16);
            writer.Write(Port);
        }
    }
}
