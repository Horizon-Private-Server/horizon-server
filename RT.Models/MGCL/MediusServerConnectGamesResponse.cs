using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
	[MediusMessage(NetMessageTypes.MessageClassLobbyReport, MediusMGCLMessageIds.ServerConnectGamesResponse)]
    public class MediusServerConnectGamesResponse : BaseMGCLMessage, IMediusResponse
    {

		public override byte PacketType => (byte)MediusMGCLMessageIds.ServerConnectGamesResponse;

        public MessageId MessageID { get; set; }
        public int GameWorldID;
        public int SpectatorWorldID;
        public MGCL_ERROR_CODE Confirmation;

        public bool IsSuccess => Confirmation >= 0;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            MessageID = reader.Read<MessageId>();
            reader.ReadBytes(3);
            GameWorldID = reader.ReadInt32();
            SpectatorWorldID = reader.ReadInt32();
            Confirmation = reader.Read<MGCL_ERROR_CODE>();
            reader.ReadBytes(3);
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(MessageID ?? MessageId.Empty);
            writer.Write(new byte[3]);
            writer.Write(GameWorldID);
            writer.Write(SpectatorWorldID);
            writer.Write(Confirmation);
            writer.Write(new byte[3]);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID:{MessageID} " +
                $"GameWorldID:{GameWorldID} " +
                $"SpectatorWorldID:{SpectatorWorldID} " +
                $"Confirmation:{Confirmation}";
        }
    }
}
