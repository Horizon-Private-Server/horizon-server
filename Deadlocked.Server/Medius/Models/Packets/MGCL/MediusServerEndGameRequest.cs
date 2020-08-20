using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.MGCL
{
    [MediusMessage(TypesAAA.MediusServerEndGameRequest)]
    public class MediusServerEndGameRequest : BaseMGCLMessage
    {

        public override TypesAAA MessageType => TypesAAA.MediusServerEndGameRequest;

        public int WorldID;
        public bool BrutalFlag;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            WorldID = reader.ReadInt32();
            BrutalFlag = reader.ReadBoolean();
            reader.ReadBytes(3);
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(WorldID);
            writer.Write(BrutalFlag);
            writer.Write(new byte[3]);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"WorldID:{WorldID}" + " " +
$"BrutalFlag:{BrutalFlag}";
        }
    }
}