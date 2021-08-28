using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
	[MediusMessage(NetMessageTypes.MessageClassLobby, MediusLobbyMessageIds.UniverseNewsResponse)]
    public class MediusUniverseNewsResponse : BaseLobbyMessage, IMediusResponse
    {
		public override byte PacketType => (byte)MediusLobbyMessageIds.UniverseNewsResponse;

        public bool IsSuccess => StatusCode >= 0;

        public MessageId MessageID { get; set; }

        public string Text; // CHATMESSAGE_MAXLEN
        public MediusCallbackStatus StatusCode;
        public int EndOfList;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            MessageID = reader.Read<MessageId>();

            // 
            Text = reader.ReadString(Constants.CHATMESSAGE_MAXLEN);
            //reader.ReadBytes(3);
            //StatusCode = reader.Read<MediusCallbackStatus>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            //
            writer.Write(MessageID ?? MessageId.Empty);
            writer.Write(StatusCode);

            // 
            writer.Write(new byte[3]);
            writer.Write(Text, 256);
            writer.Write(EndOfList);
            
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID:{MessageID} " +
             $"Text:{Text} ";
        }
    }
}
