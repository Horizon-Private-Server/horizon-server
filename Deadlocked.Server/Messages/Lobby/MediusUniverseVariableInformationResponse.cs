using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.Lobby
{
    [MediusApp(MediusAppPacketIds.UniverseVariableInformationResponse)]
    public class MediusUniverseVariableInformationResponse : BaseLobbyMessage
    {

        public override MediusAppPacketIds Id => MediusAppPacketIds.UniverseVariableInformationResponse;

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

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            //reader.ReadBytes(3);
            StatusCode = reader.Read<MediusCallbackStatus>();
            InfoFilter = reader.Read<MediusUniverseVariableInformationInfoFilter>();

            if (InfoFilter.IsSet(MediusUniverseVariableInformationInfoFilter.INFO_ID))
                UniverseID = reader.ReadUInt32();

            if (InfoFilter.IsSet(MediusUniverseVariableInformationInfoFilter.INFO_NAME))
                UniverseName = reader.ReadString(MediusConstants.UNIVERSENAME_MAXLEN);


            if (InfoFilter.IsSet(MediusUniverseVariableInformationInfoFilter.INFO_DNS))
            {
                DNS = reader.ReadString(MediusConstants.UNIVERSEDNS_MAXLEN);
                Port = reader.ReadInt32();
            }


            if (InfoFilter.IsSet(MediusUniverseVariableInformationInfoFilter.INFO_DESCRIPTION))
                UniverseDescription = reader.ReadString(MediusConstants.UNIVERSEDESCRIPTION_MAXLEN);


            if (InfoFilter.IsSet(MediusUniverseVariableInformationInfoFilter.INFO_STATUS))
            {
                Status = reader.ReadInt32();
                UserCount = reader.ReadInt32();
                MaxUsers = reader.ReadInt32();
            }

            if (InfoFilter.IsSet(MediusUniverseVariableInformationInfoFilter.INFO_BILLING))
            {
                UniverseBilling = reader.ReadString(MediusConstants.UNIVERSE_BSP_MAXLEN);
                BillingSystemName = reader.ReadString(MediusConstants.UNIVERSE_BSP_NAME_MAXLEN);
            }

            if (InfoFilter.IsSet(MediusUniverseVariableInformationInfoFilter.INFO_EXTRAINFO))
                ExtendedInfo = reader.ReadString(MediusConstants.UNIVERSE_EXTENDED_INFO_MAXLEN);

            //if (InfoFilter.IsSet(MediusUniverseVariableInformationInfoFilter.INFO_SVO_URL))
            //    SvoURL = reader.ReadString(MediusConstants.UNIVERSE_SVO_URL_MAXLEN);

            EndOfList = reader.ReadBoolean();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            //writer.Write(new byte[3]);
            writer.Write(StatusCode);
            writer.Write(InfoFilter);

            if (InfoFilter.IsSet(MediusUniverseVariableInformationInfoFilter.INFO_ID))
                writer.Write(UniverseID);

            if (InfoFilter.IsSet(MediusUniverseVariableInformationInfoFilter.INFO_NAME))
                writer.Write(UniverseName, MediusConstants.UNIVERSENAME_MAXLEN);

            if (InfoFilter.IsSet(MediusUniverseVariableInformationInfoFilter.INFO_DNS))
            {
                writer.Write(DNS, MediusConstants.UNIVERSEDNS_MAXLEN);
                writer.Write(Port);
            }

            if (InfoFilter.IsSet(MediusUniverseVariableInformationInfoFilter.INFO_DESCRIPTION))
                writer.Write(UniverseDescription, MediusConstants.UNIVERSEDESCRIPTION_MAXLEN);

            if (InfoFilter.IsSet(MediusUniverseVariableInformationInfoFilter.INFO_STATUS))
            {
                writer.Write(Status);
                writer.Write(UserCount);
                writer.Write(MaxUsers);
            }

            if (InfoFilter.IsSet(MediusUniverseVariableInformationInfoFilter.INFO_BILLING))
            {
                writer.Write(UniverseBilling, MediusConstants.UNIVERSE_BSP_MAXLEN);
                writer.Write(BillingSystemName, MediusConstants.UNIVERSE_BSP_NAME_MAXLEN);
            }

            if (InfoFilter.IsSet(MediusUniverseVariableInformationInfoFilter.INFO_EXTRAINFO))
                writer.Write(ExtendedInfo, MediusConstants.UNIVERSE_EXTENDED_INFO_MAXLEN);

            //if (InfoFilter.IsSet(MediusUniverseVariableInformationInfoFilter.INFO_SVO_URL))
            //    writer.Write(SvoURL, MediusConstants.UNIVERSE_SVO_URL_MAXLEN);


            writer.Write(EndOfList);
            writer.Write(new byte[3]);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"StatusCode:{StatusCode}" + " " +
$"InfoFilter:{InfoFilter}" + " " +
$"UniverseID:{UniverseID}" + " " +
$"UniverseName:{UniverseName}" + " " +
$"DNS:{DNS}" + " " +
$"Port:{Port}" + " " +
$"UniverseDescription:{UniverseDescription}" + " " +
$"Status:{Status}" + " " +
$"UserCount:{UserCount}" + " " +
$"MaxUsers:{MaxUsers}" + " " +
$"UniverseBilling:{UniverseBilling}" + " " +
$"BillingSystemName:{BillingSystemName}" + " " +
$"ExtendedInfo:{ExtendedInfo}" + " " +
$"SvoURL:{SvoURL}" + " " +
$"EndOfList:{EndOfList}";
        }
    }
}