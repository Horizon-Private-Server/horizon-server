using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.MGCL
{
	[MediusMessage(NetMessageTypes.MessageClassLobbyReport, MediusMGCLMessageIds.ServerMoveGameWorldOnMeRequest)]
    public class MediusServerMoveGameWorldOnMeRequest : BaseMGCLMessage
    {

		public override byte PacketType => (byte)MediusMGCLMessageIds.ServerMoveGameWorldOnMeRequest;

        public int CurrentMediusWorldID;
        public int NewGameWorldID;
        public NetAddressList AddressList;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            CurrentMediusWorldID = reader.ReadInt32();
            NewGameWorldID = reader.ReadInt32();
            AddressList = reader.Read<NetAddressList>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(CurrentMediusWorldID);
            writer.Write(NewGameWorldID);
            writer.Write(AddressList);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"CurrentMediusWorldID:{CurrentMediusWorldID}" + " " +
$"NewGameWorldID:{NewGameWorldID}" + " " +
$"AddressList:{AddressList}";
        }
    }
}
