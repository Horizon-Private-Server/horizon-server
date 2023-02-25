using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
    [MediusMessage(NetMessageTypes.MessageClassLobbyExt, MediusLobbyExtMessageIds.TicketLogin)]
    public class MediusTicketLoginRequest : BaseLobbyExtMessage, IMediusRequest
    {
        public override byte PacketType => (byte)MediusLobbyExtMessageIds.TicketLogin;

        public MessageId MessageID { get; set; }

        public string SessionKey; // SESSIONKEY_MAXLEN
        public byte[] UNK0;
        public string Username;
        public byte[] UNK1;
        public string Password = "TestPass";
        public string UNK2;


        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            MessageID = reader.Read<MessageId>();

            // 
            SessionKey = reader.ReadString(Constants.SESSIONKEY_MAXLEN);
            UNK0 = reader.ReadBytes(88);
            Username = reader.ReadString(Constants.ACCOUNTNAME_MAXLEN);
            UNK1 = reader.ReadBytes(20);
            UNK2 = reader.ReadString(24);


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
            writer.Write(Username, Constants.ACCOUNTNAME_MAXLEN);
            writer.Write(UNK1 ?? new byte[20], 20);
            writer.Write(UNK2 ?? "", 24);
        }


        public IMediusResponse GetDefaultFailedResponse(IMediusRequest request)
        {
            if (request == null)
                return null;

            return new MediusTicketLoginResponse()
            {
                MessageID = request.MessageID,
                StatusCode = MediusCallbackStatus.MediusFail
            };
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID:{MessageID} " +
             $"SessionKey:{SessionKey} " +
             $"UNK0:{UNK0} " +
             $"Username:{Username} " +
             $"UNK1:{UNK1} " +
             $"UNK2:{UNK2}";
        }
    }
}