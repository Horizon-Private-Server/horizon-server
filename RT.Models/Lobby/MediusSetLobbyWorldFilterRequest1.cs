using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
	[MediusMessage(NetMessageTypes.MessageClassLobbyExt, MediusLobbyExtMessageIds.SetLobbyWorldFilter1)]
    public class MediusSetLobbyWorldFilterRequest1 : BaseLobbyExtMessage, IMediusRequest
    {
		public override byte PacketType => (byte)MediusLobbyExtMessageIds.SetLobbyWorldFilter1;

        public MessageId MessageID { get; set; }

        public uint FilterMask1;
        public uint FilterMask2;
        public uint FilterMask3;
        public uint FilterMask4;
        public MediusLobbyFilterType LobbyFilterType;
        public MediusLobbyFilterMaskLevelType FilterMaskLevel;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            MessageID = reader.Read<MessageId>();

            // 
            reader.ReadBytes(3);
            FilterMask1 = reader.ReadUInt32();
            FilterMask2 = reader.ReadUInt32();
            FilterMask3 = reader.ReadUInt32();
            FilterMask4 = reader.ReadUInt32();
            LobbyFilterType = reader.Read<MediusLobbyFilterType>();
            FilterMaskLevel = reader.Read<MediusLobbyFilterMaskLevelType>();
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            //
            writer.Write(MessageID ?? MessageId.Empty);

            //
            writer.Write(new byte[3]);
            writer.Write(FilterMask1);
            writer.Write(FilterMask2);
            writer.Write(FilterMask3);
            writer.Write(FilterMask4);
            writer.Write(LobbyFilterType);
            writer.Write(FilterMaskLevel);
        }


        public IMediusResponse GetDefaultFailedResponse(IMediusRequest request)
        {
            if (request == null)
                return null;

            return new MediusSetLobbyWorldFilterResponse1()
            {
                MessageID = request.MessageID,
                StatusCode = MediusCallbackStatus.MediusFail
            };
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID:{MessageID} " +
             $"FilterMask1:{FilterMask1} " +
$"FilterMask2:{FilterMask2} " +
$"FilterMask3:{FilterMask3} " +
$"FilterMask4:{FilterMask4} " +
$"LobbyFilterType:{LobbyFilterType} " +
$"FilterMaskLevel:{FilterMaskLevel}";
        }
    }
}
