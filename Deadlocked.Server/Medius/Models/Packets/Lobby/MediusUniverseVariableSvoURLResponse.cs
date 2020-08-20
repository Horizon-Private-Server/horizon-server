using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.Lobby
{
    [MediusMessage(TypesAAA.UniverseVariableSvoURLResponse)]
    public class MediusUniverseVariableSvoURLResponse : BaseLobbyMessage
    {

        public override TypesAAA MessageType => TypesAAA.UniverseVariableSvoURLResponse;

        public ushort Result;

        public override void Deserialize(BinaryReader reader)
        {
            //
            base.Deserialize(reader);

            // 
            Result = reader.ReadUInt16();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(Result);
        }

    }
}
