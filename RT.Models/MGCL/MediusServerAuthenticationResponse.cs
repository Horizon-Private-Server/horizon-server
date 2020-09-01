using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
    [MediusMessage(NetMessageTypes.MessageClassLobbyReport, MediusMGCLMessageIds.ServerAuthenticationResponse)]
    public class MediusServerAuthenticationResponse : BaseMGCLMessage, IMediusResponse
    {

        public override byte PacketType => (byte)MediusMGCLMessageIds.ServerAuthenticationResponse;

        public string MessageID { get; set; }
        public MGCL_ERROR_CODE Confirmation;
        public NetConnectionInfo ConnectInfo;

        public bool IsSuccess => Confirmation >= 0;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            MessageID = reader.ReadString(Constants.MESSAGEID_MAXLEN);
            Confirmation = reader.Read<MGCL_ERROR_CODE>();
            reader.ReadBytes(2);
            ConnectInfo = reader.Read<NetConnectionInfo>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(MessageID, Constants.MESSAGEID_MAXLEN);
            writer.Write(Confirmation);
            writer.Write(new byte[2]);
            writer.Write(ConnectInfo);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID:{MessageID} " +
                $"Confirmation:{Confirmation} " +
                $"ConnectInfo:{ConnectInfo}";
        }
    }
}
