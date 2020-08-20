using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.Lobby
{
    [MediusMessage(TypesAAA.AccountUpdateStatsResponse)]
    public class MediusAccountUpdateStatsResponse : BaseLobbyMessage
    {
        public override TypesAAA MessageType => TypesAAA.AccountUpdateStatsResponse;

        public uint Response = 0;

        public override void Deserialize(BinaryReader reader)
        {
            //
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            Response = reader.ReadUInt32();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(Response);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"Response:{Response}" + " ";
        }
    }
}