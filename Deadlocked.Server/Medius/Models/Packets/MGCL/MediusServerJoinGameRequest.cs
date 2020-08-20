using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.MGCL
{
	[MediusMessage(NetMessageTypes.MessageClassLobbyReport, MediusMGCLMessageIds.ServerJoinGameRequest)]
    public class MediusServerJoinGameRequest : BaseMGCLMessage
    {

		public override byte PacketType => (byte)MediusMGCLMessageIds.ServerJoinGameRequest;

        public NetConnectionInfo ConnectInfo;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            ConnectInfo = reader.Read<NetConnectionInfo>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(ConnectInfo);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"ConnectInfo:{ConnectInfo}";
        }
    }
}
