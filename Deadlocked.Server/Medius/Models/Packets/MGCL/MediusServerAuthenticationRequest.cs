using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.MGCL
{
    [MediusMessage(NetMessageTypes.MessageClassLobbyReport, MediusMGCLMessageIds.ServerAuthenticationRequest)]
    public class MediusServerAuthenticationRequest : BaseMGCLMessage
    {

        public override byte PacketType => (byte)MediusMGCLMessageIds.ServerAuthenticationRequest;

        public MGCL_TRUST_LEVEL TrustLevel;
        public NetAddressList AddressList;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            TrustLevel = reader.Read<MGCL_TRUST_LEVEL>();
            AddressList = reader.Read<NetAddressList>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(TrustLevel);
            writer.Write(AddressList);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"TrustLevel:{TrustLevel}" + " " +
$"AddressList:{AddressList}";
        }
    }
}
