using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.App
{
    [MediusApp(MediusAppPacketIds.JoinGame)]
    public class MediusJoinGameRequest : BaseLobbyMessage
    {

        public override MediusAppPacketIds Id => MediusAppPacketIds.JoinGame;

        public string SessionKey; // SESSIONKEY_MAXLEN
        public int MediusWorldID;
        public MediusJoinType JoinType;
        public string GamePassword; // GAMEPASSWORD_MAXLEN
        public MediusGameHostType GameHostType;
        public RSA_KEY pubKey;
        public NetAddressList AddressList;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            SessionKey = reader.ReadString(MediusConstants.SESSIONKEY_MAXLEN);
            reader.ReadBytes(2);
            MediusWorldID = reader.ReadInt32();
            JoinType = reader.Read<MediusJoinType>();
            GamePassword = reader.ReadString(MediusConstants.GAMEPASSWORD_MAXLEN);
            GameHostType = reader.Read<MediusGameHostType>();
            pubKey = reader.Read<RSA_KEY>();
            AddressList = reader.Read<NetAddressList>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(SessionKey, MediusConstants.SESSIONKEY_MAXLEN);
            writer.Write(new byte[2]);
            writer.Write(MediusWorldID);
            writer.Write(JoinType);
            writer.Write(GamePassword, MediusConstants.GAMEPASSWORD_MAXLEN);
            writer.Write(GameHostType);
            writer.Write(pubKey);
            writer.Write(AddressList);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"SessionKey:{SessionKey}" + " " +
$"MediusWorldID:{MediusWorldID}" + " " +
$"JoinType:{JoinType}" + " " +
$"GamePassword:{GamePassword}" + " " +
$"GameHostType:{GameHostType}" + " " +
$"pubKey:{pubKey}" + " " +
$"AddressList:{AddressList}";
        }
    }
}