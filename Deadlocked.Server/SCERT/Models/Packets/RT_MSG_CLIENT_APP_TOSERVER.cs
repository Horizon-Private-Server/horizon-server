using Deadlocked.Server.Messages.Lobby;
using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Deadlocked.Server.SCERT.Models.Packets
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_CLIENT_APP_TOSERVER)]
    public class RT_MSG_CLIENT_APP_TOSERVER : BaseScertMessage
    {
        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_CLIENT_APP_TOSERVER;

        public byte[] AppMessage = null;

        public override void Deserialize(BinaryReader reader)
        {
            AppMessage = reader.ReadRest();
        }

        protected override void Serialize(BinaryWriter writer)
        {
            if (AppMessage != null)
                writer.Write(AppMessage);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"AppMessage:{AppMessage}";
        }
    }
}
