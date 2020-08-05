using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.App
{
    [MediusApp(MediusAppPacketIds.ExtendedSessionBeginRequest)]
    public class MediusExtendedSessionBeginRequest : MediusSessionBeginRequest
    {

        public override MediusAppPacketIds Id => MediusAppPacketIds.ExtendedSessionBeginRequest;

        public int ClientVersionMajor;
        public int ClientVersionMinor;
        public int ClientVersionBuild;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            ClientVersionMajor = reader.ReadInt32();
            ClientVersionMinor = reader.ReadInt32();
            ClientVersionBuild = reader.ReadInt32();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(ClientVersionMajor);
            writer.Write(ClientVersionMinor);
            writer.Write(ClientVersionBuild);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
$"ClientVersionMajor:{ClientVersionMajor}" + " " +
$"ClientVersionMinor:{ClientVersionMinor}" + " " +
$"ClientVersionBuild:{ClientVersionBuild}";
        }
    }
}