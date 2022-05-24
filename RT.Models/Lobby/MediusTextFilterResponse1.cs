using RT.Common;
using Server.Common;

namespace RT.Models
{

    [MediusMessage(NetMessageClass.MessageClassLobbyExt, MediusLobbyExtMessageIds.MediusTextFilterResponse1)]
    public class MediusTextFilterResponse1 : BaseLobbyExtMessage, IMediusResponse
    {

		public override byte PacketType => (byte)MediusLobbyExtMessageIds.MediusTextFilterResponse1;
        public bool IsSuccess => StatusCode >= 0;

        public MessageId MessageID { get; set; }
        public uint TextSize;
        public char[] Text;
        public MediusCallbackStatus StatusCode;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            MessageID = reader.Read<MessageId>();

            // 
            //reader.ReadBytes(3);

            StatusCode = reader.Read<MediusCallbackStatus>();
            TextSize = reader.ReadUInt32();
            Text = reader.ReadChars(Constants.CHATMESSAGE_MAXLEN);
            //reader.ReadBytes(4);

            //
            //Text = reader.ReadString(Constants.CHATMESSAGE_MAXLEN);
            //reader.ReadBytes(3);
            //StatusCode = reader.Read<MediusCallbackStatus>();
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            //
            writer.Write(MessageID ?? MessageId.Empty);

            // 
            //writer.Write(new byte[3]);

            writer.Write(StatusCode);
            writer.Write(TextSize);
            writer.Write(Text);
            //writer.Write(new byte[4]);

            // 
            //writer.Write(Text, Constants.CHATMESSAGE_MAXLEN);
            //writer.Write(new byte[3]);
            //writer.Write(StatusCode);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID: {MessageID} " +
                $"TextSize: {TextSize} " +
                $"Text: {Text}" +
                $"StatusCode: {StatusCode}";
        }
    }
}
