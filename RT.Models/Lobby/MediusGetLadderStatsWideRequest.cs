using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
	[MediusMessage(NetMessageTypes.MessageClassLobbyExt, MediusLobbyExtMessageIds.GetLadderStatsWide)]
    public class MediusGetLadderStatsWideRequest : BaseLobbyExtMessage, IMediusRequest
    {
		public override byte PacketType => (byte)MediusLobbyExtMessageIds.GetLadderStatsWide;

        public MessageId MessageID { get; set; }

        public int AccountID_or_ClanID;
        public MediusLadderType LadderType;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            MessageID = reader.Read<MessageId>();

            // 
            reader.ReadBytes(3);
            AccountID_or_ClanID = reader.ReadInt32();
            LadderType = reader.Read<MediusLadderType>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            //
            writer.Write(MessageID ?? MessageId.Empty);

            // 
            writer.Write(new byte[3]);
            writer.Write(AccountID_or_ClanID);
            writer.Write(LadderType);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID:{MessageID} " +
             $"AccountID_or_ClanID:{AccountID_or_ClanID} " +
$"LadderType:{LadderType}";
        }
    }
}
