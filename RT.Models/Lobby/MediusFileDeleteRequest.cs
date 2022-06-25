using RT.Common;
using Server.Common;

namespace RT.Models
{
    /// <summary>
    /// Introduced in 1.50<br></br>
    /// Request to delete a file.
    /// </summary>
    [MediusMessage(NetMessageClass.MessageClassLobby, MediusLobbyMessageIds.FileDelete)]
    public class MediusFileDeleteRequest : BaseLobbyMessage, IMediusRequest
    {

        public override byte PacketType => (byte)MediusLobbyMessageIds.FileDelete;

        public MessageId MessageID { get; set; }

        public MediusFile MediusFileToDelete;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        { 
            // 
            base.Deserialize(reader);

            // 
            MediusFileToDelete = reader.Read<MediusFile>();


            //
            MessageID = reader.Read<MessageId>();
            reader.ReadBytes(3);
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(MediusFileToDelete);


            //
            writer.Write(MessageID ?? MessageId.Empty);
            writer.Write(new byte[3]);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID: {MessageID} " +
                $"MediusFileToCreate: {MediusFileToDelete}";
        }
    }
}