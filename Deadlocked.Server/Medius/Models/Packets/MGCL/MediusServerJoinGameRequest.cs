using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.MGCL
{
    [MediusMessage(TypesAAA.MediusServerJoinGameRequest)]
    public class MediusServerJoinGameRequest : BaseMGCLMessage
    {

        public override TypesAAA MessageType => TypesAAA.MediusServerJoinGameRequest;

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