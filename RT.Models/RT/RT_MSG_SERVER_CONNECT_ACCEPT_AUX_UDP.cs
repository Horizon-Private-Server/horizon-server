using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace RT.Models
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_SERVER_CONNECT_ACCEPT_AUX_UDP)]
    public class RT_MSG_SERVER_CONNECT_ACCEPT_AUX_UDP : BaseScertMessage
    {

        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_SERVER_CONNECT_ACCEPT_AUX_UDP;

        // 
        public ushort PlayerId;
        public uint ScertId = 0xD4;
        public ushort PlayerCount = 0x0001;

        public IPEndPoint EndPoint;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            PlayerId = reader.ReadUInt16();
            ScertId = reader.ReadUInt32();
            PlayerCount = reader.ReadUInt16();

            EndPoint = new IPEndPoint(reader.ReadIPAddress(), (int)reader.ReadUInt16());
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            writer.Write(PlayerId);
            writer.Write(ScertId);
            writer.Write(PlayerCount);

            writer.Write(EndPoint.Address);
            writer.Write((ushort)EndPoint.Port);
        }
    }
}
