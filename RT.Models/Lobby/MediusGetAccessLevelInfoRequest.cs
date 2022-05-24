using RT.Common;
using Server.Common;

namespace RT.Models
{
	[MediusMessage(NetMessageClass.MessageClassLobbyExt, MediusLobbyExtMessageIds.GetAccessLevelInfoRequest)]
    public class MediusGetAccessLevelInfoRequest : BaseLobbyExtMessage, IMediusRequest
    {
		public override byte PacketType => (byte)MediusLobbyExtMessageIds.GetAccessLevelInfoRequest;

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
                $"MessageID: {MessageID} ";
        }
    }
}
