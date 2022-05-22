using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
    [MediusMessage(NetMessageTypes.MessageClassLobby, MediusLobbyMessageIds.GetTotalUsersResponse)]
    public class MediusGetTotalUsersResponse : BaseLobbyMessage, IMediusRequest
    {
        public override byte PacketType => (byte)MediusLobbyMessageIds.GetTotalUsersResponse;

        public MessageId MessageID { get; set; }

        public uint TotalInSystem;
        public uint TotalInGame;
        public MediusCallbackStatus StatusCode;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            MessageID = reader.Read<MessageId>();

            reader.ReadBytes(3);

            // 
            TotalInSystem = reader.ReadUInt32();
            TotalInGame = reader.ReadUInt32();
            StatusCode = reader.Read<MediusCallbackStatus>();
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            //
            writer.Write(MessageID ?? MessageId.Empty);

            writer.Write(new byte[3]);

            // 
            writer.Write(TotalInSystem);
            writer.Write(TotalInGame);
            writer.Write(StatusCode);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID: {MessageID} " +
             $"TotalInSystem: {TotalInSystem} " +
             $"TotalInGame: {TotalInGame} " +
             $"StatusCode: {StatusCode}";
        }
    }
}
