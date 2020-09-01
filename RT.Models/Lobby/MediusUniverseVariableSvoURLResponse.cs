using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
	[MediusMessage(NetMessageTypes.MessageClassLobbyExt, MediusLobbyExtMessageIds.UniverseVariableSvoURLResponse)]
    public class MediusUniverseVariableSvoURLResponse : BaseLobbyExtMessage
    {
		public override byte PacketType => (byte)MediusLobbyExtMessageIds.UniverseVariableSvoURLResponse;

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
