using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.Lobby
{
    [MediusApp(MediusAppPacketIds.GetLadderStatsWideResponse)]
    public class MediusGetLadderStatsWideResponse : BaseLobbyMessage
    {

        public override MediusAppPacketIds Id => MediusAppPacketIds.GetLadderStatsWideResponse;

        public MediusCallbackStatus StatusCode;
        public int AccountID_or_ClanID;
        public int[] Stats = new int[MediusConstants.LADDERSTATSWIDE_MAXLEN];

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            StatusCode = reader.Read<MediusCallbackStatus>();
            AccountID_or_ClanID = reader.ReadInt32();
            for (int i = 0; i < MediusConstants.LADDERSTATSWIDE_MAXLEN; ++i) { Stats[i] = reader.ReadInt32(); }
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(StatusCode);
            writer.Write(AccountID_or_ClanID);
            for (int i = 0; i < MediusConstants.LADDERSTATSWIDE_MAXLEN; ++i) { writer.Write(Stats[i]); }
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"StatusCode:{StatusCode}" + " " +
$"AccountID_or_ClanID:{AccountID_or_ClanID}" + " " +
$"Stats:{Stats}";
        }
    }
}