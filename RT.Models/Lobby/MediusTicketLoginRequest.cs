using RT.Common;
using Server.Common;

namespace RT.Models
{
    [MediusMessage(NetMessageClass.MessageClassLobbyExt, MediusLobbyExtMessageIds.TicketLogin)]
    public class MediusTicketLoginRequest : BaseLobbyExtMessage, IMediusRequest
    {
        public override byte PacketType => (byte)MediusLobbyExtMessageIds.TicketLogin;

        public MessageId MessageID { get; set; }

        public string SessionKey; // SESSIONKEY_MAXLEN
        public byte[] UNK0;
        public string AccountName;
        public byte[] UNK1;
        public string Password = "";
        public string ServiceID;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            MessageID = reader.Read<MessageId>();

            // 
            SessionKey = reader.ReadString(Constants.SESSIONKEY_MAXLEN);
            UNK0 = reader.ReadBytes(88);
            AccountName = reader.ReadString(Constants.ACCOUNTNAME_MAXLEN);
            UNK1 = reader.ReadBytes(20);
            ServiceID = reader.ReadString(24);
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            //
            writer.Write(MessageID ?? MessageId.Empty);

            // 
            writer.Write(SessionKey, Constants.SESSIONKEY_MAXLEN);
            writer.Write(UNK0 ?? new byte[88], 88);
            writer.Write(AccountName, Constants.ACCOUNTNAME_MAXLEN);
            writer.Write(UNK1 ?? new byte[20], 20);
            writer.Write(ServiceID ?? "", 24);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID:{MessageID} " +
                $"SessionKey:{SessionKey} " +
                $"UNK0: {UNK0} " +
                $"AccountName: {AccountName} " +
                $"UNK1: {UNK1} " +
                $"ServiceID: {ServiceID}";
        }
    }
}