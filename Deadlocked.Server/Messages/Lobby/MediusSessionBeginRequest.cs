using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.App
{
    [MediusApp(MediusAppPacketIds.SessionBegin)]
    public class MediusSessionBeginRequest : BaseLobbyMessage
    {

        public override MediusAppPacketIds Id => MediusAppPacketIds.SessionBegin;

        public MediusConnectionType ConnectionClass;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            ConnectionClass = reader.Read<MediusConnectionType>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(ConnectionClass);
        }


        public override string ToString()
        {
            return base.ToString() + " " + 
                $"ConnectionClass:{ConnectionClass}";
        }
    }
}