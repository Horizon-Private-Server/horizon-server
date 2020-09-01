using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
	[MediusMessage(NetMessageTypes.MessageClassLobby, MediusLobbyMessageIds.AnonymousLoginResponse)]
    public class MediusAnonymousLoginResponse : BaseLobbyMessage, IMediusResponse
    {

		public override byte PacketType => (byte)MediusLobbyMessageIds.AnonymousLoginResponse;

        public bool IsSuccess => StatusCode >= 0;

        public MessageId MessageID { get; set; }


        public MediusCallbackStatus StatusCode;
        public int AccountID;
        public MediusAccountType AccountType;
        public int MediusWorldID;
        public NetConnectionInfo ConnectInfo;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            MessageID = reader.Read<MessageId>();

            // 
            reader.ReadBytes(3);
            StatusCode = reader.Read<MediusCallbackStatus>();
            AccountID = reader.ReadInt32();
            AccountType = reader.Read<MediusAccountType>();
            MediusWorldID = reader.ReadInt32();
            ConnectInfo = reader.Read<NetConnectionInfo>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            //
            writer.Write(MessageID ?? MessageId.Empty);

            // 
            writer.Write(new byte[3]);
            writer.Write(StatusCode);
            writer.Write(AccountID);
            writer.Write(AccountType);
            writer.Write(MediusWorldID);
            writer.Write(ConnectInfo);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID:{MessageID} " +
    $"StatusCode:{StatusCode}" +
    $"AccountID:{AccountID}" +
    $"AccountType:{AccountType}" +
    $"MediusWorldID:{MediusWorldID}" +
    $"ConnectInfo:{ConnectInfo}";
        }
    }
}
