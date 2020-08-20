using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.Lobby
{
	[MediusMessage(NetMessageTypes.MessageClassLobbyExt, MediusLobbyExtMessageIds.SetGameListFilter)]
    public class MediusSetGameListFilterRequest : BaseLobbyExtMessage
    {
		public override byte PacketType => (byte)MediusLobbyExtMessageIds.SetGameListFilter;

        public MediusGameListFilterField FilterField;
        public uint Mask;
        public MediusComparisonOperator ComparisonOperator;
        public int BaselineValue;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            FilterField = reader.Read<MediusGameListFilterField>();
            Mask = reader.ReadUInt32();
            ComparisonOperator = reader.Read<MediusComparisonOperator>();
            BaselineValue = reader.ReadInt32();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(FilterField);
            writer.Write(Mask);
            writer.Write(ComparisonOperator);
            writer.Write(BaselineValue);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"FilterField:{FilterField}" + " " +
$"Mask:{Mask}" + " " +
$"ComparisonOperator:{ComparisonOperator}" + " " +
$"BaselineValue:{BaselineValue}";
        }
    }
}
