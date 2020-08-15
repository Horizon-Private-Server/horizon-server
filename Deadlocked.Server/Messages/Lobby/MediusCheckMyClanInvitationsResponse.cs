using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.Lobby
{
    [MediusApp(MediusAppPacketIds.CheckMyClanInvitationsResponse)]
    public class MediusCheckMyClanInvitationsResponse : BaseLobbyMessage
    {

        public override MediusAppPacketIds Id => MediusAppPacketIds.CheckMyClanInvitationsResponse;

        public MediusCallbackStatus StatusCode;
        public int ClanInvitationID;
        public int ClanID;
        public MediusClanInvitationsResponseStatus ResponseStatus;
        public string Message; // CLANMSG_MAXLEN
        public int LeaderAccountID;
        public string LeaderAccountName; // ACCOUNTNAME_MAXLEN
        public bool EndOfList;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            StatusCode = reader.Read<MediusCallbackStatus>();
            ClanInvitationID = reader.ReadInt32();
            ClanID = reader.ReadInt32();
            ResponseStatus = reader.Read<MediusClanInvitationsResponseStatus>();
            Message = reader.ReadString(MediusConstants.CLANMSG_MAXLEN);
            LeaderAccountID = reader.ReadInt32();
            LeaderAccountName = reader.ReadString(MediusConstants.ACCOUNTNAME_MAXLEN);
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
            writer.Write(ClanInvitationID);
            writer.Write(ClanID);
            writer.Write(ResponseStatus);
            writer.Write(Message, MediusConstants.CLANMSG_MAXLEN);
            writer.Write(LeaderAccountID);
            writer.Write(LeaderAccountName, MediusConstants.ACCOUNTNAME_MAXLEN);
            writer.Write(EndOfList);
            writer.Write(new byte[3]);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"StatusCode:{StatusCode}" + " " +
$"ClanInvitationID:{ClanInvitationID}" + " " +
$"ClanID:{ClanID}" + " " +
$"ResponseStatus:{ResponseStatus}" + " " +
$"Message:{Message}" + " " +
$"LeaderAccountID:{LeaderAccountID}" + " " +
$"LeaderAccountName:{LeaderAccountName}" + " " +
$"EndOfList:{EndOfList}";
        }
    }
}