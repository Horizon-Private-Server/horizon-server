using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
    [MediusMessage(NetMessageTypes.MessageClassLobbyExt, MediusLobbyExtMessageIds.MediusNpIdsGetByAccountNamesResponse)]
    public class MediusNpIdsGetByAccountNamesResponse : BaseLobbyMessage, IMediusRequest
    {
        public override byte PacketType => (byte)MediusLobbyExtMessageIds.MediusNpIdsGetByAccountNamesResponse;

        public MessageId MessageID { get; set; }
        
        public MediusCallbackStatus StatusCode;
        public string AccountName; //ACCOUNTNAME_MAXLEN
        public string SceNpId; //SCE_NPID_MAXLEN
        public bool EndOfList;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            MessageID = reader.Read<MessageId>();

            StatusCode = reader.Read<MediusCallbackStatus>();

            // 
            AccountName = reader.ReadString(Constants.ACCOUNTNAME_MAXLEN);
            //SceNpId = reader.ReadString(Constants.SCE_NPID_MAXLEN);
            EndOfList = reader.ReadBoolean();
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            //
            writer.Write(MessageID ?? MessageId.Empty);

            // 
            writer.Write(StatusCode);
            writer.Write(AccountName);
            writer.Write(SceNpId);
            writer.Write(EndOfList);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID: {MessageID} " +
                $"StatusCode: {StatusCode} " +
                $"AccountName: {AccountName} " +
                $"SceNpId: {SceNpId} " +
                $"EndOfList: {EndOfList}";
        }
    }
}
