using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.Lobby
{
	[MediusMessage(NetMessageTypes.MessageClassLobby, MediusLobbyMessageIds.ChannelInfoResponse)]
    public class MediusChannelInfoResponse : BaseLobbyMessage
    {

		public override byte PacketType => (byte)MediusLobbyMessageIds.ChannelInfoResponse;

        public MediusCallbackStatus StatusCode;
        public string LobbyName; // LOBBYNAME_MAXLEN
        public int ActivePlayerCount;
        public int MaxPlayers;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            StatusCode = reader.Read<MediusCallbackStatus>();
            LobbyName = reader.ReadString(Constants.LOBBYNAME_MAXLEN);
            ActivePlayerCount = reader.ReadInt32();
            MaxPlayers = reader.ReadInt32();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(StatusCode);
            writer.Write(LobbyName, Constants.LOBBYNAME_MAXLEN);
            writer.Write(ActivePlayerCount);
            writer.Write(MaxPlayers);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"StatusCode:{StatusCode}" + " " +
$"LobbyName:{LobbyName}" + " " +
$"ActivePlayerCount:{ActivePlayerCount}" + " " +
$"MaxPlayers:{MaxPlayers}";
        }
    }
}
