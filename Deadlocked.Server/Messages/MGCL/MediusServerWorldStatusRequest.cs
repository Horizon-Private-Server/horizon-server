using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.MGCL
{
    [MediusApp(MediusAppPacketIds.MediusServerWorldStatusRequest)]
    public class MediusServerWorldStatusRequest : BaseMGCLMessage
    {

        public override MediusAppPacketIds Id => MediusAppPacketIds.MediusServerWorldStatusRequest;

        public int WorldID;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            WorldID = reader.ReadInt32();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(WorldID);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"WorldID:{WorldID}";
        }
    }
}