using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
	[MediusMessage(NetMessageTypes.MessageClassLobbyReport, MediusMGCLMessageIds.ServerMoveGameWorldOnMeRequest)]
    public class MediusServerMoveGameWorldOnMeRequest : BaseMGCLMessage, IMediusRequest
    {

		public override byte PacketType => (byte)MediusMGCLMessageIds.ServerMoveGameWorldOnMeRequest;

        public MessageId MessageID { get; set; }
        public int CurrentMediusWorldID;
        public int NewGameWorldID;
        public NetAddressList AddressList;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            MessageID = reader.Read<MessageId>();
            reader.ReadBytes(3);
            CurrentMediusWorldID = reader.ReadInt32();
            NewGameWorldID = reader.ReadInt32();
            AddressList = reader.Read<NetAddressList>();
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(MessageID ?? MessageId.Empty);
            writer.Write(new byte[3]);
            writer.Write(CurrentMediusWorldID);
            writer.Write(NewGameWorldID);
            writer.Write(AddressList);
        }


        public IMediusResponse GetDefaultFailedResponse(IMediusRequest request)
        {
            if (request == null)
                return null;

            return new MediusServerMoveGameWorldOnMeResponse()
            {
                MessageID = request.MessageID,
                Confirmation = MGCL_ERROR_CODE.MGCL_UNSUCCESSFUL
            };
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID:{MessageID} " +
                $"CurrentMediusWorldID:{CurrentMediusWorldID} " +
                $"NewGameWorldID:{NewGameWorldID} " +
                $"AddressList:{AddressList}";
        }
    }
}
