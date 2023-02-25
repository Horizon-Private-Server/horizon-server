using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
	[MediusMessage(NetMessageTypes.MessageClassLobbyExt, MediusLobbyExtMessageIds.UpdateLadderStatsWide)]
    public class MediusUpdateLadderStatsWideRequest : BaseLobbyExtMessage, IMediusRequest
    {
		public override byte PacketType => (byte)MediusLobbyExtMessageIds.UpdateLadderStatsWide;

        public MessageId MessageID { get; set; }

        public MediusLadderType LadderType;
        public int[] Stats = new int[Constants.LADDERSTATSWIDE_MAXLEN];

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            MessageID = reader.Read<MessageId>();

            //
            reader.ReadBytes(3);
            LadderType = reader.Read<MediusLadderType>();
            for (int i = 0; i < Constants.LADDERSTATSWIDE_MAXLEN; ++i) { Stats[i] = reader.ReadInt32(); }
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            //
            writer.Write(MessageID ?? MessageId.Empty);

            // 
            writer.Write(new byte[3]);
            writer.Write(LadderType);
            for (int i = 0; i < Constants.LADDERSTATSWIDE_MAXLEN; ++i) { writer.Write(i >= Stats.Length ? 0 : Stats[i]); }
        }


        public IMediusResponse GetDefaultFailedResponse(IMediusRequest request)
        {
            if (request == null)
                return null;

            return new MediusUpdateLadderStatsWideResponse()
            {
                MessageID = request.MessageID,
                StatusCode = MediusCallbackStatus.MediusFail
            };
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID:{MessageID} " +
             $"LadderType:{LadderType} " +
$"Stats:{Stats}";
        }
    }
}
