using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.MGCL
{
    [MediusMessage(TypesAAA.MediusServerSessionBeginRequest)]
    public class MediusServerSessionBeginRequest : BaseMGCLMessage
    {

        public override TypesAAA MessageType => TypesAAA.MediusServerSessionBeginRequest;

        public int LocationID;
        public int ApplicationID;
        public MGCL_GAME_HOST_TYPE ServerType;
        public string ServerVersion; // MGCL_SERVERVERSION_MAXLEN
        public int Port;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            LocationID = reader.ReadInt32();
            ApplicationID = reader.ReadInt32();
            ServerType = reader.Read<MGCL_GAME_HOST_TYPE>();
            ServerVersion = reader.ReadString(MediusConstants.MGCL_SERVERVERSION_MAXLEN);
            Port = reader.ReadInt32();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(LocationID);
            writer.Write(ApplicationID);
            writer.Write(ServerType);
            writer.Write(ServerVersion, MediusConstants.MGCL_SERVERVERSION_MAXLEN);
            writer.Write(Port);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"LocationID:{LocationID}" + " " +
$"ApplicationID:{ApplicationID}" + " " +
$"ServerType:{ServerType}" + " " +
$"ServerVersion:{ServerVersion}" + " " +
$"Port:{Port}";
        }
    }
}