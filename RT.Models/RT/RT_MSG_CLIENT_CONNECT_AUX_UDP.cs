using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using RT.Common;
using Server.Common;

namespace RT.Models
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_CLIENT_CONNECT_AUX_UDP)]
    public class RT_MSG_CLIENT_CONNECT_AUX_UDP : BaseScertMessage
    {
        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_CLIENT_CONNECT_AUX_UDP;

        // 
        public uint WorldId;
        public int ApplicationId;
        public IPEndPoint EndPoint;
        public ushort PlayerId;
        public ushort ScertId;
        public ushort UNK_26;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            WorldId = reader.ReadUInt32();
            ApplicationId = reader.ReadInt32();
            EndPoint = new IPEndPoint(reader.ReadIPAddress(), (int)reader.ReadUInt16());
            PlayerId = reader.ReadUInt16();
            ScertId = reader.ReadUInt16();
            UNK_26 = reader.ReadUInt16();
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
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
