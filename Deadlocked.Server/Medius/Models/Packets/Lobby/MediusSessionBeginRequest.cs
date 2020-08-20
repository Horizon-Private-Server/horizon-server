using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.Lobby
{
	[MediusMessage(NetMessageTypes.MessageClassLobby, MediusLobbyMessageIds.SessionBegin)]
    public class MediusSessionBeginRequest : BaseLobbyMessage
    {
		public override byte PacketType => (byte)MediusLobbyMessageIds.SessionBegin;

        public MediusConnectionType ConnectionClass;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            ConnectionClass = reader.Read<MediusConnectionType>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(ConnectionClass);
        }


        public override string ToString()
        {
            return base.ToString() + " " + 
                $"ConnectionClass:{ConnectionClass}";
        }
    }
}
