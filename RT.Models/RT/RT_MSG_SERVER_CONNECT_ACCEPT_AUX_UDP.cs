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
        public ushort ScertId = 0xD4;
        public ushort UNK_04;
        public ushort PlayerCount = 0x0001;

        public IPEndPoint EndPoint;

        public override void Deserialize(BinaryReader reader)
        {
            PlayerId = reader.ReadUInt16();
            ScertId = reader.ReadUInt16();
            UNK_04 = reader.ReadUInt16();
            PlayerCount = reader.ReadUInt16();

            EndPoint = new IPEndPoint(reader.ReadIPAddress(), (int)reader.ReadUInt16());
        }

        protected override void Serialize(BinaryWriter writer)
        {
            writer.Write(PlayerId);
            writer.Write(ScertId);
            writer.Write(UNK_04);
            writer.Write(PlayerCount);

            writer.Write(EndPoint.Address);
            writer.Write((ushort)EndPoint.Port);
        }
    }
}
