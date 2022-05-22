using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
	[MediusMessage(NetMessageTypes.MessageClassLobbyExt, MediusLobbyExtMessageIds.MatchGetSupersetListResponse)]
    public class MediusMatchGetSupersetListResponse : BaseLobbyExtMessage, IMediusRequest
    {

		public override byte PacketType => (byte)MediusLobbyExtMessageIds.MatchGetSupersetListResponse;

        public MessageId MessageID { get; set; }
        public MediusCallbackStatus StatusCode;
        public bool EndOfList;
        public uint SupersetID;
        public string SupersetName;
        public string SupersetDescription;
        public string SupersetExtraInfo;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            MessageID = reader.Read<MessageId>();

            //
            StatusCode = reader.Read<MediusCallbackStatus>();
            EndOfList = reader.Read<bool>();
            SupersetID = reader.ReadUInt32();
            SupersetName = reader.ReadString(Constants.SUPERSETNAME_MAXLEN);
            SupersetDescription = reader.ReadString(Constants.SUPERSETDESCRIPTION_MAXLEN);
            SupersetExtraInfo = reader.ReadString(Constants.SUPERSETEXTRAINFO_MAXLEN);
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            //
            writer.Write(MessageID ?? MessageId.Empty);

            // 
            writer.Write(StatusCode);
            writer.Write(EndOfList);
            writer.Write(SupersetID);
            writer.Write(SupersetName, Constants.SUPERSETNAME_MAXLEN);
            writer.Write(SupersetDescription, Constants.SUPERSETDESCRIPTION_MAXLEN);
            writer.Write(SupersetExtraInfo, Constants.SUPERSETEXTRAINFO_MAXLEN);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID: {MessageID} " +
                $"StatusCode: {StatusCode} " +
                $"SupersetID: {SupersetID} " +
                $"SupersetName: {SupersetName} " +
                $"SupersetDescription: {SupersetDescription} " +
                $"SupersetExtraInfo: {SupersetExtraInfo} " +
                $"EndOfList: {EndOfList} ";
        }
    }
}
