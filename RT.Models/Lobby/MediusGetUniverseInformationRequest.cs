using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
	[MediusMessage(NetMessageTypes.MessageClassLobby, MediusLobbyMessageIds.GetUniverseInformation)]
    public class MediusGetUniverseInformationRequest : BaseLobbyMessage, IMediusRequest
    {
		public override byte PacketType => (byte)MediusLobbyMessageIds.GetUniverseInformation;

        public MessageId MessageID { get; set; }

        public MediusUniverseVariableInformationInfoFilter InfoType;
        public MediusCharacterEncodingType CharacterEncoding;
        public MediusLanguageType Language;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            MessageID = reader.Read<MessageId>();

            // 
            reader.ReadBytes(3);
            InfoType = reader.Read<MediusUniverseVariableInformationInfoFilter>();
            CharacterEncoding = reader.Read<MediusCharacterEncodingType>();
            Language = reader.Read<MediusLanguageType>();
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            //
            writer.Write(MessageID ?? MessageId.Empty);

            // 
            writer.Write(new byte[3]);
            writer.Write(InfoType);
            writer.Write(CharacterEncoding);
            writer.Write(Language);
        }


        public IMediusResponse GetDefaultFailedResponse(IMediusRequest request)
        {
            if (request == null)
                return null;

            return new MediusUniverseVariableInformationResponse()
            {
                MessageID = request.MessageID,
                StatusCode = MediusCallbackStatus.MediusNoResult,
                EndOfList = true
            };
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID:{MessageID} " +
             $"InfoType:{InfoType} " +
$"CharacterEncoding:{CharacterEncoding} " +
$"Language:{Language}";
        }
    }
}
