using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.Lobby
{
    [MediusMessage(TypesAAA.GetMyIPResponse)]
    public class MediusGetMyIPResponse : BaseLobbyMessage
    {

        public override TypesAAA MessageType => TypesAAA.GetMyIPResponse;

        public IPAddress IP = IPAddress.Any;
        public MediusCallbackStatus StatusCode;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            IP = IPAddress.Parse(reader.ReadString(MediusConstants.IP_MAXLEN));
            reader.ReadBytes(3);
            StatusCode = reader.Read<MediusCallbackStatus>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(IP?.ToString(), MediusConstants.IP_MAXLEN);
            writer.Write(new byte[3]);
            writer.Write(StatusCode);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"IP:{IP}" + " " +
$"StatusCode:{StatusCode}";
        }
    }
}