using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models.Lobby
{
    [MediusMessage(NetMessageTypes.MessageClassLobby, MediusLobbyMessageIds.GetAllClanMessagesResponse)]
    public class MediusGetAllClanMessagesResponse : BaseLobbyMessage, IMediusResponse
    {
        public override byte PacketType => (byte)MediusLobbyMessageIds.GetAllClanMessagesResponse;

        public bool IsSuccess => StatusCode >= 0;

        public MessageId MessageID { get; set; }
        public MediusCallbackStatus StatusCode;
        public int ClanMessageID;
        public string Message; // CLANMSG_MAXLEN
        public MediusClanMessageStatus Status;
        public char EndOfList;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            MessageID = reader.Read<MessageId>();
            StatusCode = reader.Read<MediusCallbackStatus>();
            ClanMessageID = reader.ReadInt32();
            Message = reader.ReadString(Constants.CLANMSG_MAXLEN);
            Status = reader.Read<MediusClanMessageStatus>();
            EndOfList = reader.ReadChar();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(MessageID ?? MessageId.Empty);
            writer.Write(StatusCode);
            writer.Write(ClanMessageID);
            writer.Write(Message, Constants.CLANMSG_MAXLEN);
            writer.Write(Status);
            writer.Write(EndOfList);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID:{MessageID}" + " " +
                $"StatusCode:{StatusCode}" + " " +
                $"ClanMessageID:{ClanMessageID}" + " " +
                $"Message:{Message}" + " " +
                $"Status:{Status}" + " " +
                $"EndOfList:{EndOfList}";
        }
    }
}