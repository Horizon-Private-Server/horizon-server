using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.Lobby
{
    [MediusApp(MediusAppPacketIds.GetAnnouncementsResponse)]
    public class MediusGetAnnouncementsResponse : BaseLobbyMessage
    {

        public override MediusAppPacketIds Id => MediusAppPacketIds.GetAnnouncementsResponse;

        public MediusCallbackStatus StatusCode;
        public int AnnouncementID;
        public string Announcement; // ANNOUNCEMENT_MAXLEN
        public bool EndOfList;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            StatusCode = reader.Read<MediusCallbackStatus>();
            AnnouncementID = reader.ReadInt32();
            Announcement = reader.ReadString(MediusConstants.ANNOUNCEMENT_MAXLEN);
            EndOfList = reader.ReadBoolean();
            reader.ReadBytes(3);
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(StatusCode);
            writer.Write(AnnouncementID);
            writer.Write(Announcement, MediusConstants.ANNOUNCEMENT_MAXLEN);
            writer.Write(EndOfList);
            writer.Write(new byte[3]);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"StatusCode:{StatusCode}" + " " +
$"AnnouncementID:{AnnouncementID}" + " " +
$"Announcement:{Announcement}" + " " +
$"EndOfList:{EndOfList}";
        }
    }
}