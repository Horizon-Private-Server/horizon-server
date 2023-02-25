using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
	[MediusMessage(NetMessageTypes.MessageClassLobby, MediusLobbyMessageIds.GetClanMemberList_ExtraInfo)]
    public class MediusGetClanMemberList_ExtraInfoRequest : BaseLobbyMessage, IMediusRequest
    {
		public override byte PacketType => (byte)MediusLobbyMessageIds.GetClanMemberList_ExtraInfo;

        public MessageId MessageID { get; set; }

        public int ClanID;
        public int LadderStatIndex;
        public MediusSortOrder SortOrder;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            MessageID = reader.Read<MessageId>();

            // 
            reader.ReadBytes(3);
            ClanID = reader.ReadInt32();
            LadderStatIndex = reader.ReadInt32();
            SortOrder = reader.Read<MediusSortOrder>();
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            //
            writer.Write(MessageID ?? MessageId.Empty);

            // 
            writer.Write(new byte[3]);
            writer.Write(ClanID);
            writer.Write(LadderStatIndex);
            writer.Write(SortOrder);
        }


        public IMediusResponse GetDefaultFailedResponse(IMediusRequest request)
        {
            if (request == null)
                return null;

            return new MediusGetClanMemberList_ExtraInfoResponse()
            {
                MessageID = request.MessageID,
                StatusCode = MediusCallbackStatus.MediusNoResult,
                EndOfList = true
            };
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID:{MessageID} " +
             $"ClanID:{ClanID} " +
$"LadderStatIndex:{LadderStatIndex} " +
$"SortOrder:{SortOrder}";
        }
    }
}
