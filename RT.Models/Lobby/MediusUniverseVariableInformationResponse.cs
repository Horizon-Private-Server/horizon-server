using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
	[MediusMessage(NetMessageTypes.MessageClassLobbyExt, MediusLobbyExtMessageIds.UniverseVariableInformationResponse)]
    public class MediusUniverseVariableInformationResponse : BaseLobbyExtMessage, IMediusResponse
    {
		public override byte PacketType => (byte)MediusLobbyExtMessageIds.UniverseVariableInformationResponse;

        public bool IsSuccess => StatusCode >= 0;

        public MessageId MessageID { get; set; }

        public MediusCallbackStatus StatusCode;
        public MediusUniverseVariableInformationInfoFilter InfoFilter;
        public uint UniverseID;
        public string UniverseName; // UNIVERSENAME_MAXLEN
        public string DNS; // UNIVERSEDNS_MAXLEN
        public int Port;
        public string UniverseDescription; // UNIVERSEDESCRIPTION_MAXLEN
        public int Status;
        public int UserCount;
        public int MaxUsers;
        public string UniverseBilling; // UNIVERSE_BSP_MAXLEN
        public string BillingSystemName; // UNIVERSE_BSP_NAME_MAXLEN
        public string ExtendedInfo; // UNIVERSE_EXTENDED_INFO_MAXLEN
        public string SvoURL; // UNIVERSE_SVO_URL_MAXLEN
        public bool EndOfList;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            MessageID = reader.Read<MessageId>();

            // 
            StatusCode = reader.Read<MediusCallbackStatus>();
            InfoFilter = reader.Read<MediusUniverseVariableInformationInfoFilter>();

            if (InfoFilter.IsSet(MediusUniverseVariableInformationInfoFilter.INFO_ID))
                UniverseID = reader.ReadUInt32();

            if (InfoFilter.IsSet(MediusUniverseVariableInformationInfoFilter.INFO_NAME))
                UniverseName = reader.ReadString(Constants.UNIVERSENAME_MAXLEN);


            if (InfoFilter.IsSet(MediusUniverseVariableInformationInfoFilter.INFO_DNS))
            {
                DNS = reader.ReadString(Constants.UNIVERSEDNS_MAXLEN);
                Port = reader.ReadInt32();
            }


            if (InfoFilter.IsSet(MediusUniverseVariableInformationInfoFilter.INFO_DESCRIPTION))
                UniverseDescription = reader.ReadString(Constants.UNIVERSEDESCRIPTION_MAXLEN);


            if (InfoFilter.IsSet(MediusUniverseVariableInformationInfoFilter.INFO_STATUS))
            {
                Status = reader.ReadInt32();
                UserCount = reader.ReadInt32();
                MaxUsers = reader.ReadInt32();
            }

            if (InfoFilter.IsSet(MediusUniverseVariableInformationInfoFilter.INFO_BILLING))
            {
                UniverseBilling = reader.ReadString(Constants.UNIVERSE_BSP_MAXLEN);
                BillingSystemName = reader.ReadString(Constants.UNIVERSE_BSP_NAME_MAXLEN);
            }

            if (InfoFilter.IsSet(MediusUniverseVariableInformationInfoFilter.INFO_EXTRAINFO))
                ExtendedInfo = reader.ReadString(Constants.UNIVERSE_EXTENDED_INFO_MAXLEN);

            //if (InfoFilter.IsSet(MediusUniverseVariableInformationInfoFilter.INFO_SVO_URL))
            //    SvoURL = reader.ReadString(Constants.UNIVERSE_SVO_URL_MAXLEN);

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
            writer.Write(StatusCode);
            writer.Write(InfoFilter);

            if (InfoFilter.IsSet(MediusUniverseVariableInformationInfoFilter.INFO_ID))
                writer.Write(UniverseID);

            if (InfoFilter.IsSet(MediusUniverseVariableInformationInfoFilter.INFO_NAME))
                writer.Write(UniverseName, Constants.UNIVERSENAME_MAXLEN);

            if (InfoFilter.IsSet(MediusUniverseVariableInformationInfoFilter.INFO_DNS))
            {
                writer.Write(DNS, Constants.UNIVERSEDNS_MAXLEN);
                writer.Write(Port);
            }

            if (InfoFilter.IsSet(MediusUniverseVariableInformationInfoFilter.INFO_DESCRIPTION))
                writer.Write(UniverseDescription, Constants.UNIVERSEDESCRIPTION_MAXLEN);

            if (InfoFilter.IsSet(MediusUniverseVariableInformationInfoFilter.INFO_STATUS))
            {
                writer.Write(Status);
                writer.Write(UserCount);
                writer.Write(MaxUsers);
            }

            if (InfoFilter.IsSet(MediusUniverseVariableInformationInfoFilter.INFO_BILLING))
            {
                writer.Write(UniverseBilling, Constants.UNIVERSE_BSP_MAXLEN);
                writer.Write(BillingSystemName, Constants.UNIVERSE_BSP_NAME_MAXLEN);
            }

            if (InfoFilter.IsSet(MediusUniverseVariableInformationInfoFilter.INFO_EXTRAINFO))
                writer.Write(ExtendedInfo, Constants.UNIVERSE_EXTENDED_INFO_MAXLEN);

            //if (InfoFilter.IsSet(MediusUniverseVariableInformationInfoFilter.INFO_SVO_URL))
            //    writer.Write(SvoURL, Constants.UNIVERSE_SVO_URL_MAXLEN);


            writer.Write(EndOfList);
            writer.Write(new byte[3]);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID:{MessageID} " +
             $"StatusCode:{StatusCode} " +
$"InfoFilter:{InfoFilter} " +
$"UniverseID:{UniverseID} " +
$"UniverseName:{UniverseName} " +
$"DNS:{DNS} " +
$"Port:{Port} " +
$"UniverseDescription:{UniverseDescription} " +
$"Status:{Status} " +
$"UserCount:{UserCount} " +
$"MaxUsers:{MaxUsers} " +
$"UniverseBilling:{UniverseBilling} " +
$"BillingSystemName:{BillingSystemName} " +
$"ExtendedInfo:{ExtendedInfo} " +
$"SvoURL:{SvoURL} " +
$"EndOfList:{EndOfList}";
        }
    }
}
