using RT.Common;
using Server.Common;
using System.IO;

namespace RT.Models
{
    public class NetAddress : IStreamSerializer
    {
        public static readonly NetAddress Empty = new NetAddress() { AddressType = NetAddressType.NetAddressNone };

        public NetAddressType AddressType;
        public string Address;
        public uint Port;

        public void Deserialize(BinaryReader reader)
        {
            AddressType = reader.Read<NetAddressType>();
            Address = reader.ReadString(Constants.NET_MAX_NETADDRESS_LENGTH);
            Port = reader.ReadUInt32();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(AddressType);
            writer.Write(Address, Constants.NET_MAX_NETADDRESS_LENGTH);
            writer.Write(Port);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"AddressType: {AddressType} " +
                $"Address: {Address} " +
                $"Port: {Port}";
        }
    }
}