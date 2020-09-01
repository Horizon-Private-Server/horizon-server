using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Server.Common;

namespace RT.Models
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_CLIENT_CONNECT_AUX_UDP)]
    public class RT_MSG_CLIENT_CONNECT_AUX_UDP : BaseScertMessage
    {
        // 03 00 00 00 B0 2B 00 00 31 36 30 2E 33 33 2E 33 34 2E 39 30 00 00 00 00 5F 27 00 00 D4 52 00 00 


        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_CLIENT_CONNECT_AUX_UDP;

        // 
        public uint WorldId;
        public int ApplicationId;
        public IPEndPoint EndPoint;
        public ushort PlayerId;
        public ushort ScertId;
        public ushort UNK_26;

        public override void Deserialize(BinaryReader reader)
        {
            WorldId = reader.ReadUInt32();
            ApplicationId = reader.ReadInt32();
            EndPoint = new IPEndPoint(reader.ReadIPAddress(), (int)reader.ReadUInt16());
            PlayerId = reader.ReadUInt16();
            ScertId = reader.ReadUInt16();
            UNK_26 = reader.ReadUInt16();
        }

        protected override void Serialize(BinaryWriter writer)
        {
            writer.Write(WorldId);
            writer.Write(ApplicationId);
            writer.Write(EndPoint.Address);
            writer.Write((ushort)EndPoint.Port);
            writer.Write(PlayerId);
            writer.Write(ScertId);
            writer.Write(UNK_26);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"WorldId:{WorldId} " +
                $"ApplicationId:{ApplicationId} " +
                $"EndPoint:{EndPoint} " +
                $"PlayerId:{PlayerId} " +
                $"ScertId:{ScertId} " +
                $"UNK_26:{UNK_26}";
        }
    }
}
