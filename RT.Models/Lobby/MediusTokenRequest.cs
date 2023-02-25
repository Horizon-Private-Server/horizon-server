using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
    [MediusMessage(NetMessageTypes.MessageClassLobbyExt, MediusLobbyExtMessageIds.TokenRequest)]
    public class MediusTokenRequest : BaseLobbyExtMessage, IMediusRequest
    {
        public override byte PacketType => (byte)MediusLobbyExtMessageIds.TokenRequest;



        public MessageId MessageID { get; set; }
        public MediusTokenActionType TokenAction;
        public MediusTokenCategoryType TokenCategory;
        public uint EntityID;
        public byte[] TokenToReplace = new byte[Constants.MEDIUS_TOKEN_MAXSIZE];
        public byte[] Token = new byte[Constants.MEDIUS_TOKEN_MAXSIZE];

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            MessageID = reader.Read<MessageId>();
            reader.ReadBytes(3); // padding
            TokenAction = reader.Read<MediusTokenActionType>();
            TokenCategory = reader.Read<MediusTokenCategoryType>();
            EntityID = reader.ReadUInt32();
            TokenToReplace = reader.ReadBytes(Constants.MEDIUS_TOKEN_MAXSIZE);
            Token = reader.ReadBytes(Constants.MEDIUS_TOKEN_MAXSIZE);
            //reader.ReadBytes(3); // padding
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(MessageID ?? MessageId.Empty);
            writer.Write(new byte[3]);
            writer.Write(TokenAction);
            writer.Write(TokenCategory);
            writer.Write(EntityID);
            writer.Write(TokenToReplace, Constants.MEDIUS_TOKEN_MAXSIZE);
            writer.Write(Token, Constants.MEDIUS_TOKEN_MAXSIZE);
            //writer.Write(new byte[3]);
        }


        public IMediusResponse GetDefaultFailedResponse(IMediusRequest request)
        {
            var r = request as BaseMediusMessage;
            if (r == null)
                return null;

            return new MediusStatusResponse()
            {
                Class = r.PacketClass,
                Type = r.PacketType,
                MessageID = request.MessageID,
                StatusCode = MediusCallbackStatus.MediusSuccess
            };
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID:{MessageID}" + " " +
                $"TokenAction:{TokenAction}" + " " +
                $"TokenCategory:{TokenCategory}" + " " +
                $"EntityID:{EntityID}" + " " +
                $"TokenToReplace:{TokenToReplace}" + " " +
                $"Token:{Token}";
        }
    }
}