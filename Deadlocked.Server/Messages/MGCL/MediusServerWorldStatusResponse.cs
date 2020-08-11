using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.MGCL
{
    [MediusApp(MediusAppPacketIds.MediusServerWorldStatusResponse)]
    public class MediusServerWorldStatusResponse : BaseMGCLMessage
    {

        public override MediusAppPacketIds Id => MediusAppPacketIds.MediusServerWorldStatusResponse;

        public int ApplicationID;
        public int MaxClients;
        public int ActiveClients;
        public MGCL_ERROR_CODE Confirmation;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            ApplicationID = reader.ReadInt32();
            MaxClients = reader.ReadInt32();
            ActiveClients = reader.ReadInt32();
            Confirmation = reader.Read< MGCL_ERROR_CODE>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(ApplicationID);
            writer.Write(MaxClients);
            writer.Write(ActiveClients);
            writer.Write(Confirmation);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"ApplicationID:{ApplicationID}" + " " +
$"MaxClients:{MaxClients}" + " " +
$"ActiveClients:{ActiveClients}" + " " +
$"Confirmation:{Confirmation}";
        }
    }
}