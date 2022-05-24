using RT.Common;
using Server.Common;
using System;

namespace RT.Models
{
    [MediusMessage(NetMessageClass.MessageClassLobbyExt, MediusLobbyExtMessageIds.MediusTextFilter1)]
    public class MediusTextFilterRequest1 : BaseLobbyExtMessage, IMediusRequest
    {

		public override byte PacketType => (byte)MediusLobbyExtMessageIds.MediusTextFilter1;

        public MessageId MessageID { get; set; }

        public string SessionKey; // SESSIONKEY_MAXLEN
        public MediusTextFilterType TextFilter;
        public uint TextSize;
        public char[] Text; // variable len

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            MessageID = reader.Read<MessageId>();

            // 
            SessionKey = reader.ReadString(Constants.SESSIONKEY_MAXLEN);
            TextFilter = reader.Read<MediusTextFilterType>();
            TextSize = reader.ReadUInt32();
            Text = reader.ReadChars(Convert.ToInt32(TextSize));
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            //
            writer.Write(MessageID ?? MessageId.Empty);

            // 
            writer.Write(SessionKey, Constants.SESSIONKEY_MAXLEN);
            writer.Write(TextFilter);
            writer.Write(TextSize);
            writer.Write(Text);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID: {MessageID} " +
                $"SessionKey: {SessionKey} " +
                $"TextFilter: {TextFilter} " +
                $"TextSize: {TextSize} " +
                $"Text:{Convert.ToString(Text)}";
        }
    }
}