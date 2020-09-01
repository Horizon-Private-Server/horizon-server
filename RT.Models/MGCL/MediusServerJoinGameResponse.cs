using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
	[MediusMessage(NetMessageTypes.MessageClassLobbyReport, MediusMGCLMessageIds.ServerJoinGameResponse)]
    public class MediusServerJoinGameResponse : BaseMGCLMessage, IMediusResponse
    {

		public override byte PacketType => (byte)MediusMGCLMessageIds.ServerJoinGameResponse;

        public MessageId MessageID { get; set; }
        public MGCL_ERROR_CODE Confirmation;
        public string AccessKey;
        public RSA_KEY pubKey;
        public int DmeClientIndex;

        public bool IsSuccess => Confirmation >= 0;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            MessageID = reader.Read<MessageId>();
            Confirmation = reader.Read<MGCL_ERROR_CODE>();
            AccessKey = reader.ReadString(Constants.MGCL_ACCESSKEY_MAXLEN);
            reader.ReadBytes(1);
            pubKey = reader.Read<RSA_KEY>();
            DmeClientIndex = reader.ReadInt32();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(MessageID ?? MessageId.Empty);
            writer.Write(Confirmation);
            writer.Write(AccessKey, Constants.MGCL_ACCESSKEY_MAXLEN);
            writer.Write(new byte[1]);
            writer.Write(pubKey ?? RSA_KEY.Empty);
            writer.Write(DmeClientIndex);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID:{MessageID} " +
                $"Confirmation:{Confirmation} " +
                $"AccessKey:{AccessKey} " +
                $"pubKey:{pubKey} " +
                $"DmeClientIndex:{DmeClientIndex}";
        }
    }
}
