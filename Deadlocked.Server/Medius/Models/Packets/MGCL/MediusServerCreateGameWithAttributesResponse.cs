using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.MGCL
{
	[MediusMessage(NetMessageTypes.MessageClassLobbyReport, MediusMGCLMessageIds.ServerCreateGameWithAttributesResponse)]
    public class MediusServerCreateGameWithAttributesResponse : BaseMGCLMessage
    {

		public override byte MessageType => (byte)MediusMGCLMessageIds.ServerCreateGameWithAttributesResponse;

        public MGCL_ERROR_CODE Confirmation;
        public int WorldID;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            Confirmation = reader.Read<MGCL_ERROR_CODE>();
            reader.ReadBytes(2);
            WorldID = reader.ReadInt32();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(Confirmation);
            writer.Write(new byte[2]);
            writer.Write(WorldID);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"Confirmation:{Confirmation}" + " " +
$"WorldID:{WorldID}";
        }
    }
}
