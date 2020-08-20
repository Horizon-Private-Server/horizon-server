using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.Lobby
{
    [MediusMessage(TypesAAA.LadderPosition_ExtraInfoResponse)]
    public class MediusLadderPosition_ExtraInfoResponse : BaseLobbyMessage
    {

        public override TypesAAA MessageType => TypesAAA.LadderPosition_ExtraInfoResponse;

        public MediusCallbackStatus StatusCode;
        public uint LadderPosition;
        public uint TotalRankings;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            StatusCode = reader.Read<MediusCallbackStatus>();
            LadderPosition = reader.ReadUInt32();
            TotalRankings = reader.ReadUInt32();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(StatusCode);
            writer.Write(LadderPosition);
            writer.Write(TotalRankings);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"StatusCode:{StatusCode}" + " " +
$"LadderPosition:{LadderPosition}" + " " +
$"TotalRankings:{TotalRankings}";
        }
    }
}