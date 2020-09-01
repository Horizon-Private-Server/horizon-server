using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
	[MediusMessage(NetMessageTypes.MessageClassLobbyReport, MediusMGCLMessageIds.ServerSessionEndRequest)]
    public class MediusServerSessionEndRequest : BaseMGCLMessage, IMediusRequest
    {
		public override byte PacketType => (byte)MediusMGCLMessageIds.ServerSessionEndRequest;

        public string MessageID { get; set; }

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            MessageID = reader.ReadString(Constants.MESSAGEID_MAXLEN);
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(MessageID, Constants.MESSAGEID_MAXLEN);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID:{MessageID}";
        }
    }
}
