using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
	[MediusMessage(NetMessageTypes.MessageClassLobbyReport, MediusMGCLMessageIds.ServerDisconnectPlayerRequest)]
    public class MediusServerDisconnectPlayerRequest : BaseMGCLMessage, IMediusRequest
    {

		public override byte PacketType => (byte)MediusMGCLMessageIds.ServerDisconnectPlayerRequest;

        public string MessageID { get; set; }
        public int DmeWorldID;
        public int DmeClientIndex;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            MessageID = reader.ReadString(Constants.MESSAGEID_MAXLEN);
            reader.ReadBytes(3);
            DmeWorldID = reader.ReadInt32();
            DmeClientIndex = reader.ReadInt32();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(MessageID, Constants.MESSAGEID_MAXLEN);
            writer.Write(new byte[3]);
            writer.Write(DmeWorldID);
            writer.Write(DmeClientIndex);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID:{MessageID} " +
                $"DmeWorldID:{DmeWorldID} " +
                $"DmeClientIndex:{DmeClientIndex}";
        }
    }
}
