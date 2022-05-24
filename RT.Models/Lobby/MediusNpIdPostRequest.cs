using RT.Common;
using Server.Common;
using System.Text;

namespace RT.Models
{
    [MediusMessage(NetMessageClass.MessageClassLobbyExt, MediusLobbyExtMessageIds.NpIdPostRequest)]
    public class MediusNpIdPostRequest : BaseLobbyExtMessage, IMediusRequest
    {
        public override byte PacketType => (byte)MediusLobbyExtMessageIds.NpIdPostRequest;

        public MessageId MessageID { get; set; }

        public string SessionKey; // SESSIONKEY_MAXLEN
        //SCE_NPID_MAXLEN = 36;
        public byte[] data;
        public byte term;
        public byte[] dummy;

        public byte[] opt;
        public byte[] reserved;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            MessageID = reader.Read<MessageId>();

            // 
            SessionKey = reader.ReadString(Constants.SESSIONKEY_MAXLEN);

            //SCE_NPID Data Blob
            data = reader.ReadBytes(16);
            term = reader.ReadByte();
            dummy = reader.ReadBytes(3);

            //
            opt = reader.ReadBytes(8);
            reserved = reader.ReadBytes(8);
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            //
            writer.Write(MessageID ?? MessageId.Empty);

            // 
            writer.Write(SessionKey, Constants.SESSIONKEY_MAXLEN);

            //SCE_NPID Data Blob
            //SCENpOnlineId
            writer.Write(data);
            writer.Write(term);
            writer.Write(dummy);

            //
            writer.Write(opt);
            writer.Write(reserved);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID: {MessageID} " +
                $"SessionKey: {SessionKey} " +
                $"Data: {Encoding.Default.GetString(data)} " +
                $"Term: {term} " +
                $"Dummy: {Encoding.Default.GetString(dummy)} " +
                $"Opt: {Encoding.Default.GetString(opt)}" +
                $"Reserved: {Encoding.Default.GetString(reserved)}";
        }
    }
}