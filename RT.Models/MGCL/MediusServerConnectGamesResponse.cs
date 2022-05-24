using RT.Common;
using Server.Common;

namespace RT.Models
{
    [MediusMessage(NetMessageClass.MessageClassLobbyReport, MediusMGCLMessageIds.ServerConnectGamesResponse)]
    public class MediusServerConnectGamesResponse : BaseMGCLMessage, IMediusResponse
    {
		public override byte PacketType => (byte)MediusMGCLMessageIds.ServerConnectGamesResponse;

        public MessageId MessageID { get; set; }
        public int GameWorldID;
        public int SpectatorWorldID;
        public MGCL_ERROR_CODE Confirmation;

        public bool IsSuccess => Confirmation >= 0;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            MessageID = reader.Read<MessageId>();
            reader.ReadBytes(3);
            GameWorldID = reader.ReadInt32();
            SpectatorWorldID = reader.ReadInt32();
            Confirmation = reader.Read<MGCL_ERROR_CODE>();
            reader.ReadBytes(3);
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(MessageID ?? MessageId.Empty);
            writer.Write(new byte[3]);
            writer.Write(GameWorldID);
            writer.Write(SpectatorWorldID);
            writer.Write(Confirmation);
            writer.Write(new byte[3]);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID: {MessageID} " +
                $"GameWorldID: {GameWorldID} " +
                $"SpectatorWorldID: {SpectatorWorldID} " +
                $"Confirmation: {Confirmation}";
        }
    }
}