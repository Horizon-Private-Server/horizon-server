using RT.Common;
using Server.Common;

namespace RT.Models
{
    [MediusMessage(NetMessageClass.MessageClassLobbyExt, MediusLobbyExtMessageIds.UniverseStatusList_ExtraInfoResponse)]
    public class MediusUniverseStatusList_ExtraInfoResponse : BaseLobbyExtMessage, IMediusResponse
    {
        public override byte PacketType => (byte)MediusLobbyExtMessageIds.UniverseStatusList_ExtraInfoResponse;

        public bool IsSuccess => StatusCode >= 0;

        public MessageId MessageID { get; set; }

        public MediusCallbackStatus StatusCode;
        public string UniverseName;
        public string DNS;
        public int Port;
        public string UniverseDescription;
        public int Status;
        public int UserCount;
        public int MaxUsers;
        public string UniverseBilling;
        public string BillingSystemName;
        public bool EndOfList;
        public string ExtendedInfo;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            MessageID = reader.Read<MessageId>();

            //
            reader.ReadBytes(3);
            StatusCode = reader.Read<MediusCallbackStatus>();
            UniverseName = reader.ReadString();
            DNS = reader.ReadString();
            Port = reader.ReadInt32();
            UniverseDescription = reader.ReadString();
            Status = reader.ReadInt32();
            UserCount = reader.ReadInt32();
            MaxUsers = reader.ReadInt32();
            UniverseBilling = reader.ReadString();
            BillingSystemName = reader.ReadString();
            EndOfList = reader.ReadBoolean();
            ExtendedInfo = reader.ReadString();
            reader.ReadBytes(3);
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
            writer.Write(UniverseName);
            writer.Write(DNS);
            writer.Write(Port);
            writer.Write(UniverseDescription);
            writer.Write(Status);
            writer.Write(UserCount);
            writer.Write(MaxUsers);
            writer.Write(EndOfList);
            writer.Write(new byte[3]);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID: {MessageID} " +
                $"StatusCode: {StatusCode} " +
                $"UniverseName: {UniverseName} " +
                $"DNS: {DNS} " +
                $"Port: {Port} " +
                $"UniverseDescription: {UniverseDescription} " +
                $"Status: {Status} " +
                $"UserCount: {UserCount} " +
                $"MaxUsers: {MaxUsers} " +
                $"EndOfList: {EndOfList}";
        }
    }
}