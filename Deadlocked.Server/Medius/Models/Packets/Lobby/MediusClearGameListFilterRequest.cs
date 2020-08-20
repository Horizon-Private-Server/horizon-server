using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.Lobby
{
    [MediusMessage(TypesAAA.ClearGameListFilter)]
    public class MediusClearGameListFilterRequest : BaseLobbyMessage
    {

        public override TypesAAA MessageType => TypesAAA.ClearGameListFilter;

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