using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.Lobby
{
	[MediusMessage(NetMessageTypes.MessageClassLobby, MediusLobbyMessageIds.GetTotalRankings)]
    public class MediusGetTotalRankingsRequest : BaseLobbyMessage
    {
		public override byte PacketType => (byte)MediusLobbyMessageIds.GetTotalRankings;

        public MediusLadderType LadderType;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            LadderType = reader.Read<MediusLadderType>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(LadderType);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"LadderType:{LadderType}";
        }
    }
}
