using RT.Common;
using Server.Common;

namespace RT.Models
{
    [MediusMessage(NetMessageClass.MessageClassLobbyReport, MediusMGCLMessageIds.ServerSessionEndResponse)]
    public class MediusServerSessionEndResponse : BaseMGCLMessage, IMediusRequest
    {
		public override byte PacketType => (byte)MediusMGCLMessageIds.ServerSessionEndResponse;

        public MessageId MessageID { get; set; }
        public MGCL_ERROR_CODE ErrorCode;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            MessageID = reader.Read<MessageId>();
            ErrorCode = reader.Read<MGCL_ERROR_CODE>();
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(MessageID ?? MessageId.Empty);
            writer.Write(ErrorCode);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID: {MessageID}" +
                $"MGCL_Error_Code: {ErrorCode} ";
        }
    }
}