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

        public MessageId MessageID { get; set; }
        public int DmeWorldID;
        public int DmeClientIndex;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            MessageID = reader.Read<MessageId>();
            reader.ReadBytes(3);
            DmeWorldID = reader.ReadInt32();
            DmeClientIndex = reader.ReadInt32();
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(MessageID ?? MessageId.Empty);
            writer.Write(new byte[3]);
            writer.Write(DmeWorldID);
            writer.Write(DmeClientIndex);
        }


        public IMediusResponse GetDefaultFailedResponse(IMediusRequest request)
        {
            throw new NotImplementedException();
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
