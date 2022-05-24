using RT.Common;
using Server.Common;

namespace RT.Models
{
    [MediusMessage(NetMessageClass.MessageClassLobbyExt, MediusLobbyExtMessageIds.VoteToBanPlayer)]
    public class MediusVoteToBanPlayerRequest : BaseLobbyMessage, IMediusRequest
    {
        public override byte PacketType => (byte)MediusLobbyExtMessageIds.VoteToBanPlayer;

        public MessageId MessageID { get; set; }

        public MediusVoteActionType VoteAction;
        public MediusBanReasonType BanReason;
        public int MediusWorldID;
        public int DmeClientIndex;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            MessageID = reader.Read<MessageId>();

            // 
            VoteAction = reader.Read<MediusVoteActionType>();
            BanReason = reader.Read<MediusBanReasonType>();
            MediusWorldID = reader.ReadInt32();
            DmeClientIndex = reader.ReadInt32();
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            //
            writer.Write(MessageID ?? MessageId.Empty);

            // 
            writer.Write(VoteAction);
            writer.Write(BanReason);
            writer.Write(MediusWorldID);
            writer.Write(DmeClientIndex);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"VoteAction: {VoteAction} " +
                $"BanReason: {BanReason} " +
                $"MediusWorldID: {MediusWorldID} " +
                $"DmeClientIndex: {DmeClientIndex}";
        }
    }
}