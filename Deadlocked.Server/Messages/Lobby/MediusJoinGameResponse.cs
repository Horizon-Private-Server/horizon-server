using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.Lobby
{
    [MediusApp(MediusAppPacketIds.JoinGameResponse)]
    public class MediusJoinGameResponse : BaseLobbyMessage
    {

        public override MediusAppPacketIds Id => MediusAppPacketIds.JoinGameResponse;

        public MediusCallbackStatus StatusCode;
        public MediusGameHostType GameHostType;
        public NetConnectionInfo ConnectInfo;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            reader.ReadBytes(3);
            StatusCode = reader.Read<MediusCallbackStatus>();
            GameHostType = reader.Read<MediusGameHostType>();
            ConnectInfo = reader.Read<NetConnectionInfo>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(StatusCode);
            writer.Write(GameHostType);
            writer.Write(ConnectInfo);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"StatusCode:{StatusCode}" + " " +
$"GameHostType:{GameHostType}" + " " +
$"ConnectInfo:{ConnectInfo}";
        }
    }
}