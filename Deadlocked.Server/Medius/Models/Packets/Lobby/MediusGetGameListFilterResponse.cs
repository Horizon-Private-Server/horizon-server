using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.Lobby
{
    [MediusMessage(TypesAAA.GetGameListFilterResponse)]
    public class MediusGetGameListFilterResponse : BaseLobbyMessage
    {

        public override TypesAAA MessageType => TypesAAA.GetGameListFilterResponse;

        public MediusCallbackStatus StatusCode;
        public uint FilterID;
        public MediusGameListFilterField FilterField;
        public uint Mask;
        public MediusComparisonOperator ComparisonOperator;
        public int BaselineValue;
        public bool EndOfList;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            StatusCode = reader.Read<MediusCallbackStatus>();
            FilterID = reader.ReadUInt32();
            FilterField = reader.Read<MediusGameListFilterField>();
            Mask = reader.ReadUInt32();
            ComparisonOperator = reader.Read<MediusComparisonOperator>();
            BaselineValue = reader.ReadInt32();
            EndOfList = reader.ReadBoolean();
            reader.ReadBytes(3);
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(StatusCode);
            writer.Write(FilterID);
            writer.Write(FilterField);
            writer.Write(Mask);
            writer.Write(ComparisonOperator);
            writer.Write(BaselineValue);
            writer.Write(EndOfList);
            writer.Write(new byte[3]);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"StatusCode:{StatusCode}" + " " +
$"FilterID:{FilterID}" + " " +
$"FilterField:{FilterField}" + " " +
$"Mask:{Mask}" + " " +
$"ComparisonOperator:{ComparisonOperator}" + " " +
$"BaselineValue:{BaselineValue}" + " " +
$"EndOfList:{EndOfList}";
        }
    }
}