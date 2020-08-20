using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.MGCL
{
	[MediusMessage(NetMessageTypes.MessageClassLobbyReport, MediusMGCLMessageIds.ServerEndGameOnMeRequest)]
    public class MediusServerEndGameOnMeRequest : BaseMGCLMessage
    {

		public override byte PacketType => (byte)MediusMGCLMessageIds.ServerEndGameOnMeRequest;

        public int MediusWorldID;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            MediusWorldID = reader.ReadInt32();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(MediusWorldID);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"MediusWorldID:{MediusWorldID}";
        }
    }
}
