using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.Lobby
{
    [MediusApp(MediusAppPacketIds.UpdateLadderStatsWide)]
    public class MediusUpdateLadderStatsWideRequest : BaseLobbyMessage
    {

        public override MediusAppPacketIds Id => MediusAppPacketIds.UpdateLadderStatsWide;

        public MediusLadderType LadderType;
        public int[] Stats = new int[MediusConstants.LADDERSTATSWIDE_MAXLEN];

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            reader.ReadBytes(3);
            LadderType = reader.Read<MediusLadderType>();
            for (int i = 0; i < MediusConstants.LADDERSTATSWIDE_MAXLEN; ++i) { Stats[i] = reader.ReadInt32(); }
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(LadderType);
            for (int i = 0; i < MediusConstants.LADDERSTATSWIDE_MAXLEN; ++i) { writer.Write(Stats[i]); }
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"LadderType:{LadderType}" + " " +
$"Stats:{Stats}";
        }
    }
}