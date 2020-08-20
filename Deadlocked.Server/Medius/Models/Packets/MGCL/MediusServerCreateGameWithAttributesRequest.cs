using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.MGCL
{
	[MediusMessage(NetMessageTypes.MessageClassLobbyReport, MediusMGCLMessageIds.ServerCreateGameWithAttributesRequest)]
    public class MediusServerCreateGameWithAttributesRequest : BaseMGCLMessage
    {

		public override byte MessageType => (byte)MediusMGCLMessageIds.ServerCreateGameWithAttributesRequest;

        public int ApplicationID;
        public int MaxClients;
        public MediusWorldAttributesType Attributes;
        public uint MediusWorldUID;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            ApplicationID = reader.ReadInt32();
            MaxClients = reader.ReadInt32();
            Attributes = reader.Read<MediusWorldAttributesType>();
            MediusWorldUID = reader.ReadUInt32();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(ApplicationID);
            writer.Write(MaxClients);
            writer.Write(Attributes);
            writer.Write(MediusWorldUID);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"ApplicationID:{ApplicationID}" + " " +
$"MaxClients:{MaxClients}" + " " +
$"Attributes:{Attributes}" + " " +
$"MediusWorldUID:{MediusWorldUID}";
        }
    }
}
