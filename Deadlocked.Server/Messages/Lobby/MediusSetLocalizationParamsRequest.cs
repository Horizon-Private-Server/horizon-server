using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.App
{
    [MediusApp(MediusAppPacketIds.SetLocalizationParams)]
    public class MediusSetLocalizationParamsRequest : BaseLobbyMessage
    {

        public override MediusAppPacketIds Id => MediusAppPacketIds.SetLocalizationParams;

        public string SessionKey; // SESSIONKEY_MAXLEN
        public MediusCharacterEncodingType CharacterEncoding;
        public MediusLanguageType Language;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            SessionKey = reader.ReadString(MediusConstants.SESSIONKEY_MAXLEN);
            reader.ReadBytes(2);
            CharacterEncoding = reader.Read<MediusCharacterEncodingType>();
            Language = reader.Read<MediusLanguageType>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(SessionKey, MediusConstants.SESSIONKEY_MAXLEN);
            writer.Write(new byte[2]);
            writer.Write(CharacterEncoding);
            writer.Write(Language);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"SessionKey:{SessionKey}" + " " +
$"CharacterEncoding:{CharacterEncoding}" + " " +
$"Language:{Language}";
        }
    }
}