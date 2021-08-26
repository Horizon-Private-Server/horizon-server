using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models.Lobby
{
    [MediusMessage(NetMessageTypes.MessageClassLobby, MediusLobbyMessageIds.GetMyClanMessagesResponse)]
    public class MediusGetMyClanMessagesResponse : BaseLobbyMessage, IMediusResponse
    {
        public override byte PacketType => (byte)MediusLobbyMessageIds.GetMyClanMessagesResponse;

        public bool IsSuccess => StatusCode >= 0;

        public MessageId MessageID { get; set; }
        public MediusCallbackStatus StatusCode;
        public int ClanID;
        public string Message; // CLANMSG_MAXLEN
        public char EndOfList;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            MessageID = reader.Read<MessageId>();
            StatusCode = reader.Read<MediusCallbackStatus>();
            ClanID = reader.ReadInt32();
            Message = reader.ReadString(Constants.CLANMSG_MAXLEN);
            EndOfList = reader.ReadChar();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(MessageID ?? MessageId.Empty);
            writer.Write(StatusCode);
            writer.Write(ClanID);
            writer.Write(Message, Constants.CLANMSG_MAXLEN);
            writer.Write(EndOfList);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID:{MessageID}" + " " +
                $"StatusCode:{StatusCode}" + " " +
                $"ClanID:{ClanID}" + " " +
                $"Message:{Message}" + " " +
                $"EndOfList:{EndOfList}";
        }
    }
}