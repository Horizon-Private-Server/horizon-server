using RT.Common;
using Server.Common;

namespace RT.Models
{
    [MediusMessage(NetMessageClass.MessageClassLobbyReport, MediusMGCLMessageIds.ServerConnectNotification)]
    public class MediusServerConnectNotification : BaseMGCLMessage
    {
        public override byte PacketType => (byte)MediusMGCLMessageIds.ServerConnectNotification;

        public MGCL_EVENT_TYPE ConnectEventType;
        public uint MediusWorldUID;
        public string PlayerSessionKey;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            ConnectEventType = reader.Read<MGCL_EVENT_TYPE>();
            MediusWorldUID = reader.ReadUInt32();
            PlayerSessionKey = reader.ReadString(Constants.MGCL_SESSIONKEY_MAXLEN);
            reader.ReadBytes(3);
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(ConnectEventType);
            writer.Write(MediusWorldUID);
            writer.Write(PlayerSessionKey, Constants.MGCL_SESSIONKEY_MAXLEN);
            writer.Write(new byte[3]);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"ConnectEventType: {ConnectEventType} " +
                $"MediusWorldUID: {MediusWorldUID} " +
                $"PlayerSessionKey: {PlayerSessionKey}";
        }
    }
}