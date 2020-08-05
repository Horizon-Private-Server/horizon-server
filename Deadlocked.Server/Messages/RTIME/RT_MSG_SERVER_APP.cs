using Deadlocked.Server.Messages.App;
using Deadlocked.Server.Messages.DME;
using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Deadlocked.Server.Messages.RTIME
{
    [Message(RT_MSG_TYPE.RT_MSG_SERVER_APP)]
    public class RT_MSG_SERVER_APP : BaseMessage
    {
        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_SERVER_APP;

        public BaseAppMessage AppMessage = null;

        public override void Deserialize(BinaryReader reader)
        {
            MediusAppPacketIds id = reader.Read<MediusAppPacketIds>();
            AppMessage = BaseAppMessage.Instantiate(id, reader);
        }

        protected override void Serialize(BinaryWriter writer)
        {
            // Write normal unfragmented message
            writer.Write(AppMessage.Id);
            AppMessage.Serialize(writer);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"AppMessage:{AppMessage}";
        }
    }
}
