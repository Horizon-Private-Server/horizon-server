using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.DME
{
    [MediusMessage(NetMessageTypes.MessageClassDME, MediusDmeMessageIds.ServerVersion)]
    public class DMEServerVersion : BaseDMEMessage
    {

        public override byte MessageType => (byte)MediusDmeMessageIds.ServerVersion;

        public string Version = "2.10.1143227940";

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            Version = reader.ReadString(MediusConstants.DME_VERSION_LENGTH);
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(Version, MediusConstants.DME_VERSION_LENGTH);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"Version:{Version}";
        }
    }
}
