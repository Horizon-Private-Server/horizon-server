using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
	[MediusMessage(NetMessageTypes.MessageClassLobbyExt, MediusLobbyExtMessageIds.ClearGameListFilter)]
    public class MediusClearGameListFilterRequest : BaseLobbyMessage
    {

		public override byte PacketType => (byte)MediusLobbyExtMessageIds.ClearGameListFilter;

        public uint FilterID;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            FilterID = reader.ReadUInt32();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(FilterID);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"FilterID:{FilterID}";
        }
    }
}
