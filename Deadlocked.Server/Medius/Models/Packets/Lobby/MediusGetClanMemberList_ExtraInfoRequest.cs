using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.Lobby
{
	[MediusMessage(NetMessageTypes.MessageClassLobby, MediusLobbyMessageIds.GetClanMemberList_ExtraInfo)]
    public class MediusGetClanMemberList_ExtraInfoRequest : BaseLobbyMessage
    {
		public override byte PacketType => (byte)MediusLobbyMessageIds.GetClanMemberList_ExtraInfo;

        public int ClanID;
        public int LadderStatIndex;
        public MediusSortOrder SortOrder;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            ClanID = reader.ReadInt32();
            LadderStatIndex = reader.ReadInt32();
            SortOrder = reader.Read<MediusSortOrder>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(ClanID);
            writer.Write(LadderStatIndex);
            writer.Write(SortOrder);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"ClanID:{ClanID}" + " " +
$"LadderStatIndex:{LadderStatIndex}" + " " +
$"SortOrder:{SortOrder}";
        }
    }
}
