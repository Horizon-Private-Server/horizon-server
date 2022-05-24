using RT.Common;
using Server.Common;

namespace RT.Models
{
    [MediusMessage(NetMessageClass.MessageClassDME, MediusDmeMessageIds.ServerVersion)]
    public class DMEServerVersion : BaseDMEMessage
    {

        public override byte PacketType => (byte)MediusDmeMessageIds.ServerVersion;

        public string Version = "2.10.1143227940";

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            Version = reader.ReadString(Constants.DME_VERSION_LENGTH);
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(Version, Constants.DME_VERSION_LENGTH);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                $"Version: {Version}";
        }
    }
}