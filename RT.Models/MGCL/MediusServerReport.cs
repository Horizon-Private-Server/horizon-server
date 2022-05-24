using RT.Common;
using Server.Common;

namespace RT.Models
{
    [MediusMessage(NetMessageClass.MessageClassLobbyReport, MediusMGCLMessageIds.ServerReport)]
    public class MediusServerReport : BaseMGCLMessage
    {
        public override NetMessageClass PacketClass => NetMessageClass.MessageClassLobbyReport;
        public override byte PacketType => (byte)MediusMGCLMessageIds.ServerReport;

        public string SessionKey;
        public short MaxWorlds;
        public short MaxPlayersPerWorld;
        public short ActiveWorldCount;
        public short TotalActivePlayers;
        public MGCL_ALERT_LEVEL AlertLevel;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            SessionKey = reader.ReadString(Constants.MGCL_SESSIONKEY_MAXLEN);
            reader.ReadBytes(1);
            MaxWorlds = reader.ReadInt16();
            MaxPlayersPerWorld = reader.ReadInt16();
            ActiveWorldCount = reader.ReadInt16();
            TotalActivePlayers = reader.ReadInt16();
            reader.ReadBytes(2);
            AlertLevel = reader.Read<MGCL_ALERT_LEVEL>();
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(SessionKey, Constants.MGCL_SESSIONKEY_MAXLEN);
            writer.Write(new byte[1]);
            writer.Write(MaxWorlds);
            writer.Write(MaxPlayersPerWorld);
            writer.Write(ActiveWorldCount);
            writer.Write(TotalActivePlayers);
            writer.Write(new byte[2]);
            writer.Write(AlertLevel);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"SessionKey: {SessionKey} " +
                $"MaxWorlds: {MaxWorlds} " +
                $"MaxPlayersPerWorld: {MaxPlayersPerWorld} " +
                $"ActiveWorldCount: {ActiveWorldCount} " +
                $"TotalActivePlayers: {TotalActivePlayers} " +
                $"AlertLevel: {AlertLevel}";
        }
    }
}