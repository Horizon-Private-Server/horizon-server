using RT.Common;
using Server.Common;

namespace RT.Models
{
    [MediusMessage(NetMessageTypes.MessageClassLobby, MediusLobbyMessageIds.Policy)]
    public class MediusGetPolicyRequest : BaseLobbyMessage, IMediusRequest
    {
		public override byte PacketType => (byte)MediusLobbyMessageIds.Policy;

        public MessageId MessageID { get; set; }

        public string SessionKey; // SESSIONKEY_MAXLEN
        public MediusPolicyType Policy;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            MessageID = reader.Read<MessageId>();

            // 
            SessionKey = reader.ReadString(Constants.SESSIONKEY_MAXLEN);
            reader.ReadBytes(2);
            Policy = reader.Read<MediusPolicyType>();
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            //
            writer.Write(MessageID ?? MessageId.Empty);

            // 
            writer.Write(SessionKey, Constants.SESSIONKEY_MAXLEN);
            writer.Write(new byte[2]);
            writer.Write(Policy);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID: {MessageID} " +
                $"SessionKey: {SessionKey} " +
                $"Policy: {Policy}";
        }
    }
}