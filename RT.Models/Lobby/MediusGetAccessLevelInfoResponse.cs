using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
	[MediusMessage(NetMessageTypes.MessageClassLobbyExt, MediusLobbyExtMessageIds.GetAccessLevelInfoResponse)]
    public class MediusGetAccessLevelInfoResponse : BaseLobbyExtMessage, IMediusRequest
    {

		public override byte PacketType => (byte)MediusLobbyExtMessageIds.GetAccessLevelInfoResponse;

        public MessageId MessageID { get; set; }

        public MediusCallbackStatus StatusCode;
        public MediusAccessLevelType AccessLevel;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            MessageID = reader.Read<MessageId>();

            reader.ReadBytes(3);

            StatusCode = reader.Read<MediusCallbackStatus>();
            AccessLevel = reader.Read<MediusAccessLevelType>();
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            //
            writer.Write(MessageID ?? MessageId.Empty);

            // 
            writer.Write(new byte[3]);
            writer.Write(StatusCode);
            writer.Write(AccessLevel);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID: {MessageID} " +
                $"StatusCode: {StatusCode} " +
                $"AccessLevel: {AccessLevel}";
        }
    }
}
