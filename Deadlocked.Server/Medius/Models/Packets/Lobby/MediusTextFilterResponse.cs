using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.Lobby
{
	[MediusMessage(NetMessageTypes.MessageClassLobby, MediusLobbyMessageIds.TextFilterResponse)]
    public class MediusTextFilterResponse : BaseLobbyMessage
    {
		public override byte PacketType => (byte)MediusLobbyMessageIds.TextFilterResponse;

        public string Text; // CHATMESSAGE_MAXLEN
        public MediusCallbackStatus StatusCode;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            Text = reader.ReadString(MediusConstants.CHATMESSAGE_MAXLEN);
            reader.ReadBytes(3);
            StatusCode = reader.Read<MediusCallbackStatus>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(Text, MediusConstants.CHATMESSAGE_MAXLEN);
            writer.Write(new byte[3]);
            writer.Write(StatusCode);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"Text:{Text}" + " " +
$"StatusCode:{StatusCode}";
        }
    }
}
