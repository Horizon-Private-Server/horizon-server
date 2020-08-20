using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.Lobby
{
	[MediusMessage(NetMessageTypes.MessageClassLobby, MediusLobbyMessageIds.FileCreateResponse)]
    public class MediusFileCreateResponse : BaseLobbyMessage
    {

		public override byte PacketType => (byte)MediusLobbyMessageIds.FileCreateResponse;

        public MediusFile MediusFileInfo = new MediusFile();
        public MediusCallbackStatus StatusCode;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            MediusFileInfo = reader.Read<MediusFile>();
            StatusCode = reader.Read<MediusCallbackStatus>();

            // 
            base.Deserialize(reader);
            reader.ReadBytes(3);
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            writer.Write(MediusFileInfo);
            writer.Write(StatusCode);

            // 
            base.Serialize(writer);
            writer.Write(new byte[3]);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"MediusFileInfo:{MediusFileInfo}" + " " +
$"StatusCode:{StatusCode}";
        }
    }
}
