using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
	[MediusMessage(NetMessageTypes.MessageClassLobbyReport, MediusMGCLMessageIds.ServerConnectGamesRequest)]
    public class MediusServerConnectGamesRequest : BaseMGCLMessage, IMediusRequest
    {
		public override byte PacketType => (byte)MediusMGCLMessageIds.ServerConnectGamesRequest;

        public MessageId MessageID { get; set; }
        public string ServerIP; // MGCL_SERVERIP_MAXLEN
        public int ServerPort;
        public int GameWorldID;
        public int SpectatorWorldID;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            MessageID = reader.Read<MessageId>();
            ServerIP = reader.ReadString(Constants.MGCL_SERVERIP_MAXLEN);
            reader.ReadBytes(3);
            ServerPort = reader.ReadInt32();
            GameWorldID = reader.ReadInt32();
            SpectatorWorldID = reader.ReadInt32();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(MessageID ?? MessageId.Empty);
            writer.Write(ServerIP, Constants.MGCL_SERVERIP_MAXLEN);
            writer.Write(new byte[3]);
            writer.Write(ServerPort);
            writer.Write(GameWorldID);
            writer.Write(SpectatorWorldID);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID:{MessageID} " +
                $"ServerIP:{ServerIP} " +
                $"ServerPort:{ServerPort} " +
                $"GameWorldID:{GameWorldID} " +
                $"SpectatorWorldID:{SpectatorWorldID}";
        }
    }
}
