using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.Lobby
{
	[MediusMessage(NetMessageTypes.MessageClassLobby, MediusLobbyMessageIds.GetBuddyList_ExtraInfo)]
    public class MediusGetBuddyList_ExtraInfoRequest : BaseLobbyMessage
    {
		public override byte PacketType => (byte)MediusLobbyMessageIds.GetBuddyList_ExtraInfo;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);
        }


        public override string ToString()
        {
            return base.ToString();
        }
    }
}
