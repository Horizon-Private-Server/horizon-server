using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
    [MediusMessage(NetMessageTypes.MessageClassLobbyReport, MediusMGCLMessageIds.ServerAuthenticationRequest)]
    public class MediusServerAuthenticationRequest : BaseMGCLMessage, IMediusRequest
    {

        public override byte PacketType => (byte)MediusMGCLMessageIds.ServerAuthenticationRequest;

        public string MessageID { get; set; }
        public MGCL_TRUST_LEVEL TrustLevel;
        public NetAddressList AddressList;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            MessageID = reader.ReadString(Constants.MESSAGEID_MAXLEN);
            reader.ReadBytes(3);
            TrustLevel = reader.Read<MGCL_TRUST_LEVEL>();
            AddressList = reader.Read<NetAddressList>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(MessageID, Constants.MESSAGEID_MAXLEN);
            writer.Write(new byte[3]);
            writer.Write(TrustLevel);
            writer.Write(AddressList);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID:{MessageID} " +
                $"TrustLevel:{TrustLevel} " +
                $"AddressList:{AddressList}";
        }
    }
}
