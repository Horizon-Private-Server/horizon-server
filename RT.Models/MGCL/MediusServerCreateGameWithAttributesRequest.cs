using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
	[MediusMessage(NetMessageTypes.MessageClassLobbyReport, MediusMGCLMessageIds.ServerCreateGameWithAttributesRequest)]
    public class MediusServerCreateGameWithAttributesRequest : BaseMGCLMessage, IMediusRequest
    {

		public override byte PacketType => (byte)MediusMGCLMessageIds.ServerCreateGameWithAttributesRequest;

        public MessageId MessageID { get; set; }
        public int ApplicationID;
        public int MaxClients;
        public MediusWorldAttributesType Attributes;
        public uint MediusWorldUID;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            MessageID = reader.Read<MessageId>();
            reader.ReadBytes(3);
            ApplicationID = reader.ReadInt32();
            MaxClients = reader.ReadInt32();
            Attributes = reader.Read<MediusWorldAttributesType>();
            MediusWorldUID = reader.ReadUInt32();
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(MessageID ?? MessageId.Empty);
            writer.Write(new byte[3]);
            writer.Write(ApplicationID);
            writer.Write(MaxClients);
            writer.Write(Attributes);
            writer.Write(MediusWorldUID);
        }


        public IMediusResponse GetDefaultFailedResponse(IMediusRequest request)
        {
            if (request == null)
                return null;

            return new MediusServerCreateGameWithAttributesResponse()
            {
                MessageID = request.MessageID,
                Confirmation = MGCL_ERROR_CODE.MGCL_UNSUCCESSFUL
            };
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID:{MessageID} " +
                $"ApplicationID:{ApplicationID} " +
                $"MaxClients:{MaxClients} " +
                $"Attributes:{Attributes} " +
                $"MediusWorldUID:{MediusWorldUID}";
        }
    }
}
