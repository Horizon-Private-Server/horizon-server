﻿using RT.Common;
using Server.Common;

namespace RT.Models
{
    [MediusMessage(NetMessageTypes.MessageClassLobby, MediusLobbyMessageIds.FileGetAttributesResponse)]
    public class MediusFileGetAttributesResponse : BaseLobbyMessage, IMediusResponse
    {
        public override byte PacketType => (byte)MediusLobbyMessageIds.FileGetAttributesResponse;

        public bool IsSuccess => StatusCode >= 0;

        public MediusFile MediusFileInfo;
        public MediusFileAttributes MediusFileAttributesResponse;
        public MediusCallbackStatus StatusCode;
        public MessageId MessageID { get; set; }

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            MediusFileInfo = reader.Read<MediusFile>();
            MediusFileAttributesResponse = reader.Read<MediusFileAttributes>();

            //
            StatusCode = reader.Read<MediusCallbackStatus>();
            MessageID = reader.Read<MessageId>();
            reader.ReadBytes(3);
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);
            // 
            writer.Write(MediusFileInfo);
            writer.Write(MediusFileAttributesResponse);

            //
            writer.Write(StatusCode);
            writer.Write(MessageID ?? MessageId.Empty);
            writer.Write(3);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
             $"MediusFileInfo: {MediusFileInfo}" +
             $"MediusFileAttributesResponse: {MediusFileAttributesResponse} " +
             $"StatusCode: {StatusCode} " +
             $"MessageID: {MessageID} ";
        }
    }
}