using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
	[MediusMessage(NetMessageTypes.MessageClassLobby, MediusLobbyMessageIds.PlayerInfoResponse)]
    public class MediusPlayerInfoResponse : BaseLobbyMessage, IMediusResponse
    {
		public override byte PacketType => (byte)MediusLobbyMessageIds.PlayerInfoResponse;

        public bool IsSuccess => StatusCode >= 0;

        public MessageId MessageID { get; set; }

        public MediusCallbackStatus StatusCode;
        public string AccountName; // ACCOUNTNAME_MAXLEN
        public int ApplicationID;
        public MediusPlayerStatus PlayerStatus;
        public MediusConnectionType ConnectionClass;
        public byte[] Stats = new byte[Constants.ACCOUNTSTATS_MAXLEN];

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            MessageID = reader.Read<MessageId>();

            // 
            reader.ReadBytes(3);
            StatusCode = reader.Read<MediusCallbackStatus>();
            AccountName = reader.ReadString(Constants.ACCOUNTNAME_MAXLEN);
            ApplicationID = reader.ReadInt32();
            PlayerStatus = reader.Read<MediusPlayerStatus>();
            ConnectionClass = reader.Read<MediusConnectionType>();
            Stats = reader.ReadBytes(Constants.ACCOUNTSTATS_MAXLEN);
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
            writer.Write(AccountName, Constants.ACCOUNTNAME_MAXLEN);
            writer.Write(ApplicationID);
            writer.Write(PlayerStatus);
            writer.Write(ConnectionClass);
            writer.Write(Stats, Constants.ACCOUNTSTATS_MAXLEN);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID:{MessageID} " +
             $"StatusCode:{StatusCode} " +
$"AccountName:{AccountName} " +
$"ApplicationID:{ApplicationID} " +
$"PlayerStatus:{PlayerStatus} " +
$"ConnectionClass:{ConnectionClass} " +
$"Stats:{BitConverter.ToString(Stats)}";
        }
    }
}
