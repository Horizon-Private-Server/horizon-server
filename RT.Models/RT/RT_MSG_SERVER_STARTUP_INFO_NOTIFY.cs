using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_SERVER_STARTUP_INFO_NOTIFY)]
    public class RT_MSG_SERVER_STARTUP_INFO_NOTIFY : BaseScertMessage
    {
        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_SERVER_STARTUP_INFO_NOTIFY;

        public byte GameHostType { get; set; } = (byte)MGCL_GAME_HOST_TYPE.MGCLGameHostClientServerAuxUDP;
        public uint Timestamp { get; set; } = Utils.GetUnixTime();

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            GameHostType = reader.ReadByte();
            Timestamp = reader.ReadUInt32();
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            writer.Write(GameHostType);
            writer.Write(Timestamp);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"GameHostType:{(MGCL_GAME_HOST_TYPE)GameHostType} " +
                $"Timestamp:{Timestamp}";
        }
    }
}
