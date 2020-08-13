using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.Lobby
{
    [MediusApp(MediusAppPacketIds.SetAutoChatHistoryRequest)]
    public class MediusSetAutoChatHistoryRequest : BaseLobbyMessage
    {

        public override MediusAppPacketIds Id => MediusAppPacketIds.SetAutoChatHistoryRequest;

        public int AutoChatHistoryNumMessages;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            AutoChatHistoryNumMessages = reader.ReadInt32();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(AutoChatHistoryNumMessages);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"AutoChatHistoryNumMessages:{AutoChatHistoryNumMessages}";
        }
    }
}