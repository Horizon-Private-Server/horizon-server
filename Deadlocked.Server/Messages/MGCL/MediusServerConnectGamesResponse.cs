using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.MGCL
{
    [MediusApp(MediusAppPacketIds.MediusServerConnectGamesResponse)]
    public class MediusServerConnectGamesResponse : BaseMGCLMessage
    {

        public override MediusAppPacketIds Id => MediusAppPacketIds.MediusServerConnectGamesResponse;

        public int GameWorldID;
        public int SpectatorWorldID;
        public char Confirmation;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            GameWorldID = reader.ReadInt32();
            SpectatorWorldID = reader.ReadInt32();
            Confirmation = reader.ReadChar();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(GameWorldID);
            writer.Write(SpectatorWorldID);
            writer.Write(Confirmation);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"GameWorldID:{GameWorldID}" + " " +
$"SpectatorWorldID:{SpectatorWorldID}" + " " +
$"Confirmation:{Confirmation}";
        }
    }
}