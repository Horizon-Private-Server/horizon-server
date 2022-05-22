using RT.Common;
using Server.Common;
using System.IO;

namespace RT.Models
{
    public class NetConnectionInfo : IStreamSerializer
    {
        public NetConnectionType Type;
        public NetAddressList AddressList = new NetAddressList();
        public int WorldID;
        public RSA_KEY ServerKey = new RSA_KEY();
        public string SessionKey;
        public string AccessKey;

        public void Deserialize(BinaryReader reader)
        {
            Type = reader.Read<NetConnectionType>();
            AddressList = reader.Read<NetAddressList>();
            WorldID = reader.ReadInt32();
            ServerKey = reader.Read<RSA_KEY>();
            SessionKey = reader.ReadString(Constants.NET_SESSION_KEY_LEN);
            AccessKey = reader.ReadString(Constants.NET_ACCESS_KEY_LEN);
            reader.ReadBytes(2);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Type);
            writer.Write(AddressList);
            writer.Write(WorldID);
            writer.Write(ServerKey);
            writer.Write(SessionKey, Constants.NET_SESSION_KEY_LEN);
            writer.Write(AccessKey, Constants.NET_ACCESS_KEY_LEN);
            writer.Write(new byte[2]);
        }

        public override string ToString()
        {
            return $"Type: {Type} " +
                $"AddressList: {AddressList} " +
                $"WorldID: {WorldID} " +
                $"ServerKey: {ServerKey} " +
                $"SessionKey: {SessionKey} " +
                $"AccessKey: {AccessKey}";
        }
    }
}