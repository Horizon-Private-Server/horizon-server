using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.Lobby
{
    [MediusApp(MediusAppPacketIds.GetUniverseInformation)]
    public class MediusGetUniverseInformationRequest : BaseLobbyMessage
    {

        public override MediusAppPacketIds Id => MediusAppPacketIds.GetUniverseInformation;

        public MediusUniverseVariableInformationInfoFilter InfoType;
        public MediusCharacterEncodingType CharacterEncoding;
        public MediusLanguageType Language;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            InfoType = reader.Read<MediusUniverseVariableInformationInfoFilter>();
            CharacterEncoding = reader.Read<MediusCharacterEncodingType>();
            Language = reader.Read<MediusLanguageType>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(InfoType);
            writer.Write(CharacterEncoding);
            writer.Write(Language);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"InfoType:{InfoType}" + " " +
$"CharacterEncoding:{CharacterEncoding}" + " " +
$"Language:{Language}";
        }
    }
}