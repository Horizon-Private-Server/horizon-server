using RT.Common;
using Server.Common;

namespace RT.Models
{
    [MediusMessage(NetMessageTypes.MessageClassLobbyReport, MediusMGCLMessageIds.ServerCreateGameWithAttributesResponse)]
    public class MediusServerCreateGameWithAttributesResponse : BaseMGCLMessage, IMediusResponse
    {
		public override byte PacketType => (byte)MediusMGCLMessageIds.ServerCreateGameWithAttributesResponse;

        public MessageId MessageID { get; set; }
        public MGCL_ERROR_CODE Confirmation;
        public int WorldID;

        public bool IsSuccess => Confirmation >= 0;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            MessageID = reader.Read<MessageId>();
            Confirmation = reader.Read<MGCL_ERROR_CODE>();
            reader.ReadBytes(2);
            WorldID = reader.ReadInt32();
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(MessageID ?? MessageId.Empty);
            writer.Write(Confirmation);
            writer.Write(new byte[2]);
            writer.Write(WorldID);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID: {MessageID} " +
                $"Confirmation: {Confirmation} " +
                $"WorldID: {WorldID}";
        }
    }
}