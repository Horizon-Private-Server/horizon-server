using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.Lobby
{
	[MediusMessage(NetMessageTypes.MessageClassLobbyExt, MediusLobbyExtMessageIds.GetServerTimeResponse)]
    public class MediusGetServerTimeResponse : BaseLobbyExtMessage
    {
		public override byte PacketType => (byte)MediusLobbyExtMessageIds.GetServerTimeResponse;

        public MediusCallbackStatus StatusCode;
        public uint GMT_time = Utils.GetUnixTime();
        public MediusTimeZone Local_server_timezone;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            StatusCode = reader.Read<MediusCallbackStatus>();
            GMT_time = reader.ReadUInt32();
            Local_server_timezone = reader.Read<MediusTimeZone>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(StatusCode);
            writer.Write(GMT_time);
            writer.Write(Local_server_timezone);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"StatusCode:{StatusCode}" + " " +
$"GMT_time:{GMT_time}" + " " +
$"Local_server_timezone:{Local_server_timezone}";
        }
    }
}
