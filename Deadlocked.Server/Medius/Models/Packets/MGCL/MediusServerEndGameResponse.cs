using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.MGCL
{
	[MediusMessage(NetMessageTypes.MessageClassLobbyReport, MediusMGCLMessageIds.ServerEndGameResponse)]
    public class MediusServerEndGameResponse : BaseMGCLMessage
    {

		public override byte MessageType => (byte)MediusMGCLMessageIds.ServerEndGameResponse;

        public MGCL_ERROR_CODE Confirmation;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            Confirmation = reader.Read<MGCL_ERROR_CODE>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(Confirmation);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"Confirmation:{Confirmation}";
        }
    }
}
