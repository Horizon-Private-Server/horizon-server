using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.MGCL
{
    [MediusMessage(TypesAAA.MediusServerSetAttributesRequest)]
    public class MediusServerSetAttributesRequest : BaseMGCLMessage
    {

        public override TypesAAA MessageType => TypesAAA.MediusServerSetAttributesRequest;

        public int Attributes;
        public NetAddress ListenServerAddress;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            Attributes = reader.ReadInt32();
            ListenServerAddress = reader.Read<NetAddress>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(Attributes);
            writer.Write(ListenServerAddress);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"Attributes:{Attributes}" + " " +
$"ListenServerAddress:{ListenServerAddress}";
        }
    }
}