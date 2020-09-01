using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
    [MediusMessage(NetMessageTypes.MessageClassDME, MediusDmeMessageIds.ServerVersion)]
    public class DMEServerVersion : BaseDMEMessage
    {

        public override byte PacketType => (byte)MediusDmeMessageIds.ServerVersion;

        public string Version = "2.10.1143227940";

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            Version = reader.ReadString(Constants.DME_VERSION_LENGTH);
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(Version, Constants.DME_VERSION_LENGTH);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                $"Version:{Version}";
        }
    }
}
