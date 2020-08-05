using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.Lobby
{
    [MediusApp(MediusAppPacketIds.JoinChannel)]
    public class MediusJoinChannelRequest : BaseLobbyMessage
    {

        public override MediusAppPacketIds Id => MediusAppPacketIds.JoinChannel;

        public string SessionKey; // SESSIONKEY_MAXLEN
        public int MediusWorldID;
        public string LobbyChannelPassword; // LOBBYPASSWORD_MAXLEN

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            SessionKey = reader.ReadString(MediusConstants.SESSIONKEY_MAXLEN);
            reader.ReadBytes(2);
            MediusWorldID = reader.ReadInt32();
            LobbyChannelPassword = reader.ReadString(MediusConstants.LOBBYPASSWORD_MAXLEN);
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(SessionKey, MediusConstants.SESSIONKEY_MAXLEN);
            writer.Write(new byte[2]);
            writer.Write(MediusWorldID);
            writer.Write(LobbyChannelPassword, MediusConstants.LOBBYPASSWORD_MAXLEN);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"SessionKey:{SessionKey}" + " " +
$"MediusWorldID:{MediusWorldID}" + " " +
$"LobbyChannelPassword:{LobbyChannelPassword}";
        }
    }
}