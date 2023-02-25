using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
	[MediusMessage(NetMessageTypes.MessageClassLobby, MediusLobbyMessageIds.FileUpload)]
    public class MediusFileUploadRequest : BaseLobbyMessage, IMediusRequest
    {

		public override byte PacketType => (byte)MediusLobbyMessageIds.FileUpload;

        public MessageId MessageID { get; set; }

        public MediusFile MediusFileInfo;
        public uint PucDataStart;
        public uint UiDataSize;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            MediusFileInfo = reader.Read<MediusFile>();
            PucDataStart = reader.ReadUInt32();
            UiDataSize = reader.ReadUInt32();

            // 
            base.Deserialize(reader);

            //
            MessageID = reader.Read<MessageId>();
            reader.ReadBytes(3);
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            writer.Write(MediusFileInfo);
            writer.Write(PucDataStart);
            writer.Write(UiDataSize);

            // 
            base.Serialize(writer);

            //
            writer.Write(MessageID ?? MessageId.Empty);
            writer.Write(new byte[3]);
        }


        public IMediusResponse GetDefaultFailedResponse(IMediusRequest request)
        {
            if (request == null)
                return null;

            return new MediusFileUploadResponse()
            {
                MessageID = request.MessageID,
                StatusCode = MediusCallbackStatus.MediusFail
            };
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID:{MessageID} " +
                $"MediusFileInfo:{MediusFileInfo} " +
                $"PucDataStart:{PucDataStart} " +
                $"UiDataSize:{UiDataSize}";
        }
    }
}
