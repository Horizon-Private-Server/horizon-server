using RT.Common;
using Server.Common;

namespace RT.Models
{
    [MediusMessage(NetMessageClass.MessageClassLobby, MediusLobbyMessageIds.GetAnnouncementsResponse)]
    public class MediusGetAnnouncementsResponse : BaseLobbyMessage, IMediusResponse
    {
		public override byte PacketType => (byte)MediusLobbyMessageIds.GetAnnouncementsResponse;

        public bool IsSuccess => StatusCode >= 0;

        public MessageId MessageID { get; set; }

        public MediusCallbackStatus StatusCode;
        public int AnnouncementID;
        public string Announcement; // ANNOUNCEMENT_MAXLEN
        public bool EndOfList;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            MessageID = reader.Read<MessageId>();

            //
            reader.ReadBytes(3);
            StatusCode = reader.Read<MediusCallbackStatus>();
            AnnouncementID = reader.ReadInt32();

            //DL HD, UYA HD, Motorstorm1
            if(reader.AppId == 20095 || reader.AppId == 24000 || reader.AppId == 20754)
            {
                Announcement = reader.ReadString(Constants.ANNOUNCEMENT_MAXLEN);
            }

            if (reader.MediusVersion <= 112)
            {
                Announcement = reader.ReadString(Constants.ANNOUNCEMENT_MAXLEN);
            }
            else if (reader.MediusVersion == 113)
            {
                Announcement = reader.ReadString(Constants.ANNOUNCEMENT1_MAXLEN);
            }

            EndOfList = reader.ReadBoolean();
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
            writer.Write(AnnouncementID);

            //DL HD, UYA HD, Motorstorm1
            if (writer.AppId == 20095 || writer.AppId == 24000 || writer.AppId == 20754)
            {
                writer.Write(Announcement, Constants.ANNOUNCEMENT_MAXLEN);
            }

            if (writer.MediusVersion <= 112)
            {
                writer.Write(Announcement, Constants.ANNOUNCEMENT_MAXLEN);
            }
            else if (writer.MediusVersion == 113)
            {
                writer.Write(Announcement, Constants.ANNOUNCEMENT1_MAXLEN);
            }
            writer.Write(EndOfList);
            writer.Write(new byte[3]);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID: {MessageID} " +
                $"StatusCode: {StatusCode} " +
                $"AnnouncementID: {AnnouncementID} " +
                $"Announcement: {Announcement} " +
                $"EndOfList: {EndOfList}";
        }
    }
}