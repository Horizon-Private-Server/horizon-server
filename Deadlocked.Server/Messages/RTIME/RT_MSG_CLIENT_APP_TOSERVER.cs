using Deadlocked.Server.Messages.Lobby;
using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Deadlocked.Server.Messages.RTIME
{
    [Message(RT_MSG_TYPE.RT_MSG_CLIENT_APP_TOSERVER)]
    public class RT_MSG_CLIENT_APP_TOSERVER : BaseMessage
    {
        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_CLIENT_APP_TOSERVER;

        public BaseAppMessage AppMessage = null;

        public override void Deserialize(BinaryReader reader)
        {
            MediusAppPacketIds id = reader.Read<MediusAppPacketIds>();

            try
            {

                AppMessage = BaseAppMessage.Instantiate(id, reader);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error deserializing {Id}:{id}");
                throw e;
            }
        }

        protected override void Serialize(BinaryWriter writer)
        {
            writer.Write(AppMessage.Id);
            AppMessage?.Serialize(writer);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"AppMessage:{AppMessage}";
        }
    }
}
