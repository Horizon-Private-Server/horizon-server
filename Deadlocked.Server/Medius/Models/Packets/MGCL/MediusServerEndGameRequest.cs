using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.MGCL
{
	[MediusMessage(NetMessageTypes.MessageClassLobbyReport, MediusMGCLMessageIds.ServerEndGameRequest)]
    public class MediusServerEndGameRequest : BaseMGCLMessage
    {

		public override byte MessageType => (byte)MediusMGCLMessageIds.ServerEndGameRequest;

        public int WorldID;
        public bool BrutalFlag;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            WorldID = reader.ReadInt32();
            BrutalFlag = reader.ReadBoolean();
            reader.ReadBytes(3);
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(WorldID);
            writer.Write(BrutalFlag);
            writer.Write(new byte[3]);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"WorldID:{WorldID}" + " " +
$"BrutalFlag:{BrutalFlag}";
        }
    }
}
