using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.Lobby
{
    [MediusMessage(TypesAAA.GenericChatSetFilterResponse)]
    public class MediusGenericChatSetFilterResponse : BaseLobbyMessage
    {

        public override TypesAAA MessageType => TypesAAA.GenericChatSetFilterResponse;

        public MediusCallbackStatus StatusCode;
        public MediusGenericChatFilter ChatFilter;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            StatusCode = reader.Read<MediusCallbackStatus>();
            ChatFilter = reader.Read<MediusGenericChatFilter>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(StatusCode);
            writer.Write(ChatFilter);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"StatusCode:{StatusCode}" + " " +
$"ChatFilter:{ChatFilter}";
        }
    }
}