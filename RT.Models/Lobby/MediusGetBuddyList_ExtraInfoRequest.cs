using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
	[MediusMessage(NetMessageClass.MessageClassLobby, MediusLobbyMessageIds.GetBuddyList_ExtraInfo)]
    public class MediusGetBuddyList_ExtraInfoRequest : BaseLobbyMessage, IMediusRequest
    {
		public override byte PacketType => (byte)MediusLobbyMessageIds.GetBuddyList_ExtraInfo;

        public MessageId MessageID { get; set; }

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            MessageID = reader.Read<MessageId>();
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            //
            writer.Write(MessageID ?? MessageId.Empty);
        }


        public override string ToString()
        {
return base.ToString() + " " +
                $"MessageID:{MessageID}";
        }
    }
}
