using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.Lobby
{
    [MediusMessage(TypesAAA.TextFilter)]
    public class MediusTextFilterRequest : BaseLobbyMessage
    {

        public override TypesAAA MessageType => TypesAAA.TextFilter;

        public string SessionKey; // SESSIONKEY_MAXLEN
        public MediusTextFilterType TextFilterType;
        public string Text; // CHATMESSAGE_MAXLEN

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            SessionKey = reader.ReadString(MediusConstants.SESSIONKEY_MAXLEN);
            reader.ReadBytes(2);
            TextFilterType = reader.Read<MediusTextFilterType>();
            Text = reader.ReadString(MediusConstants.CHATMESSAGE_MAXLEN);
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(SessionKey, MediusConstants.SESSIONKEY_MAXLEN);
            writer.Write(new byte[2]);
            writer.Write(TextFilterType);
            writer.Write(Text, MediusConstants.CHATMESSAGE_MAXLEN);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"SessionKey:{SessionKey}" + " " +
$"TextFilterType:{TextFilterType}" + " " +
$"Text:{Text}";
        }
    }
}