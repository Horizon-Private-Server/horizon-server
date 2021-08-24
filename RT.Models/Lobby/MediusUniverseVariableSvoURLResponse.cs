using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
	[MediusMessage(NetMessageTypes.MessageClassLobbyExt, MediusLobbyExtMessageIds.UniverseVariableSvoURLResponse)]
    public class MediusUniverseVariableSvoURLResponse : BaseLobbyExtMessage, IMediusResponse
    {
		public override byte PacketType => (byte)MediusLobbyExtMessageIds.UniverseVariableSvoURLResponse;

        public bool IsSuccess => true;

        public MessageId MessageID { get; set; }

        public string URL { get; set; }

        public override void Deserialize(BinaryReader reader)
        {
            //
            base.Deserialize(reader);

            //
            MessageID = reader.Read<MessageId>();

            // 
            URL = reader.ReadString(128);
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            //
            writer.Write(MessageID ?? MessageId.Empty);

            // 
            writer.Write(URL, 128);
        }

    }
}
