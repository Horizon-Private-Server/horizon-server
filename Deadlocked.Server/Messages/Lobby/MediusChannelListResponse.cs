using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.Lobby
{
    [MediusApp(MediusAppPacketIds.ChannelListResponse)]
    public class MediusChannelListResponse : BaseLobbyMessage
    {

        public override MediusAppPacketIds Id => MediusAppPacketIds.ChannelListResponse;

        public MediusCallbackStatus StatusCode;
        public int MediusWorldID;
        public string LobbyName; // LOBBYNAME_MAXLEN
        public int PlayerCount;
        public bool EndOfList;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            StatusCode = reader.Read<MediusCallbackStatus>();
            MediusWorldID = reader.ReadInt32();
            LobbyName = reader.ReadString(MediusConstants.LOBBYNAME_MAXLEN);
            PlayerCount = reader.ReadInt32();
            EndOfList = reader.ReadBoolean();
            reader.ReadBytes(3);
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(StatusCode);
            writer.Write(MediusWorldID);
            writer.Write(LobbyName, MediusConstants.LOBBYNAME_MAXLEN);
            writer.Write(PlayerCount);
            writer.Write(EndOfList);
            writer.Write(new byte[3]);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"StatusCode:{StatusCode}" + " " +
$"MediusWorldID:{MediusWorldID}" + " " +
$"LobbyName:{LobbyName}" + " " +
$"PlayerCount:{PlayerCount}" + " " +
$"EndOfList:{EndOfList}";
        }
    }
}