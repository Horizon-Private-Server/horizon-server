using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
	[MediusMessage(NetMessageTypes.MessageClassLobby, MediusLobbyMessageIds.AnonymousLogin)]
    public class MediusAnonymousLoginRequest : BaseLobbyMessage, IMediusRequest
    {

		public override byte PacketType => (byte)MediusLobbyMessageIds.AnonymousLogin;

        public MessageId MessageID { get; set; }

        public string SessionKey; // SESSIONKEY_MAXLEN
        public string SessionDisplayName; // ACCOUNTNAME_MAXLEN
        public string SessionDisplayStats; // ACCOUNTSTATS_MAXLEN

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            MessageID = reader.Read<MessageId>();

            // 
            SessionKey = reader.ReadString(Constants.SESSIONKEY_MAXLEN);
            SessionDisplayName = reader.ReadString(Constants.ACCOUNTNAME_MAXLEN);
            SessionDisplayStats = reader.ReadString(Constants.ACCOUNTSTATS_MAXLEN);
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            //
            writer.Write(MessageID ?? MessageId.Empty);

            // 
            writer.Write(SessionKey, Constants.SESSIONKEY_MAXLEN);
            writer.Write(SessionDisplayName, Constants.ACCOUNTNAME_MAXLEN);
            writer.Write(SessionDisplayStats, Constants.ACCOUNTSTATS_MAXLEN);
        }


        public IMediusResponse GetDefaultFailedResponse(IMediusRequest request)
        {
            if (request == null)
                return null;

            return new MediusAnonymousLoginResponse()
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
$"SessionDisplayName:{SessionDisplayName} " +
$"SessionDisplayStats:{SessionDisplayStats}";
        }
    }
}
