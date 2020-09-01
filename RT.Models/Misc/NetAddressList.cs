using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RT.Models
{
    public class NetAddressList : IStreamSerializer
    {
        public NetAddress[] AddressList = null;

        public NetAddressList()
        {
            AddressList = new NetAddress[Constants.NET_ADDRESS_LIST_COUNT];
            for (int i = 0; i < Constants.NET_ADDRESS_LIST_COUNT; ++i)
                AddressList[i] = new NetAddress();
        }

        public void Deserialize(BinaryReader reader)
        {
            AddressList = new NetAddress[Constants.NET_ADDRESS_LIST_COUNT];
            for (int i = 0; i < Constants.NET_ADDRESS_LIST_COUNT; ++i)
            {
                AddressList[i] = reader.Read<NetAddress>();
            }
        }

        public void Serialize(BinaryWriter writer)
        {
            for (int i = 0; i < Constants.NET_ADDRESS_LIST_COUNT; ++i)
            {
                writer.Write((AddressList == null || i >= AddressList.Length) ? NetAddress.Empty : AddressList[i]);
            }
        }

        public override string ToString()
        {
            return "NetAddresses:<" + String.Join(" ", AddressList?.Select(x => x.ToString())) + "> ";
        }
    }
}
