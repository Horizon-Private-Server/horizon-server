using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
    [MediusMessage(NetMessageTypes.MessageClassLobby, MediusLobbyMessageIds.GetLocationsResponse)]
    public class MediusGetLocationsResponse : BaseLobbyMessage, IMediusResponse
    {
        public override byte PacketType => (byte)MediusLobbyMessageIds.GetLocationsResponse;

        public bool IsSuccess => StatusCode >= 0;

        public MessageId MessageID { get; set; }
        public int LocationId { get; set; }
        public string LocationName { get; set; } // LOCATIONNAME_MAXLEN
        public MediusCallbackStatus StatusCode;
        public bool EndOfList;

       public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            MessageID = reader.Read<MessageId>();
            reader.ReadBytes(3);
            LocationId = reader.ReadInt32();
            LocationName = reader.ReadString(Constants.LOCATIONNAME_MAXLEN);
            StatusCode = reader.Read<MediusCallbackStatus>();
            EndOfList = reader.ReadBoolean();
            reader.ReadBytes(3);
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(MessageID ?? MessageId.Empty);
            writer.Write(new byte[3]);
            writer.Write(LocationId);
            writer.Write(LocationName, Constants.LOCATIONNAME_MAXLEN);
            writer.Write(StatusCode);
            writer.Write(EndOfList);
            writer.Write(new byte[3]);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID: {MessageID}" + " " +
                $"LocationId: {LocationId}" + " " +
                $"LocationName: {LocationName}" + " " +
                $"StatusCode: {StatusCode}" + " " +
                $"EndOfList: {EndOfList}";
        }
    }
}
