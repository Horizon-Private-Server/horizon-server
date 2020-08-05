using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.Lobby
{
    [MediusApp(MediusAppPacketIds.CreateGame)]
    public class MediusCreateGameRequest : BaseLobbyMessage
    {

        public override MediusAppPacketIds Id => MediusAppPacketIds.CreateGame;

        public string SessionKey; // SESSIONKEY_MAXLEN
        public int ApplicationID;
        public int MinPlayers;
        public int MaxPlayers;
        public int GameLevel;
        public string GameName; // GAMENAME_MAXLEN
        public string GamePassword; // GAMEPASSWORD_MAXLEN
        public string SpectatorPassword; // GAMEPASSWORD_MAXLEN
        public int PlayerSkillLevel;
        public int RulesSet;
        public int GenericField1;
        public int GenericField2;
        public int GenericField3;
        public int GenericField4;
        public int GenericField5;
        public int GenericField6;
        public int GenericField7;
        public int GenericField8;
        public MediusGameHostType GameHostType;
        public MediusWorldAttributesType Attributes;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            SessionKey = reader.ReadString(MediusConstants.SESSIONKEY_MAXLEN);
            reader.ReadBytes(2);
            ApplicationID = reader.ReadInt32();
            MinPlayers = reader.ReadInt32();
            MaxPlayers = reader.ReadInt32();
            GameLevel = reader.ReadInt32();
            GameName = reader.ReadString(MediusConstants.GAMENAME_MAXLEN);
            GamePassword = reader.ReadString(MediusConstants.GAMEPASSWORD_MAXLEN);
            SpectatorPassword = reader.ReadString(MediusConstants.GAMEPASSWORD_MAXLEN);
            PlayerSkillLevel = reader.ReadInt32();
            RulesSet = reader.ReadInt32();
            GenericField1 = reader.ReadInt32();
            GenericField2 = reader.ReadInt32();
            GenericField3 = reader.ReadInt32();
            GenericField4 = reader.ReadInt32();
            GenericField5 = reader.ReadInt32();
            GenericField6 = reader.ReadInt32();
            GenericField7 = reader.ReadInt32();
            GenericField8 = reader.ReadInt32();
            GameHostType = reader.Read<MediusGameHostType>();
            Attributes = reader.Read<MediusWorldAttributesType>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(SessionKey, MediusConstants.SESSIONKEY_MAXLEN);
            writer.Write(new byte[2]);
            writer.Write(ApplicationID);
            writer.Write(MinPlayers);
            writer.Write(MaxPlayers);
            writer.Write(GameLevel);
            writer.Write(GameName, MediusConstants.GAMENAME_MAXLEN);
            writer.Write(GamePassword, MediusConstants.GAMEPASSWORD_MAXLEN);
            writer.Write(SpectatorPassword, MediusConstants.GAMEPASSWORD_MAXLEN);
            writer.Write(PlayerSkillLevel);
            writer.Write(RulesSet);
            writer.Write(GenericField1);
            writer.Write(GenericField2);
            writer.Write(GenericField3);
            writer.Write(GenericField4);
            writer.Write(GenericField5);
            writer.Write(GenericField6);
            writer.Write(GenericField7);
            writer.Write(GenericField8);
            writer.Write(GameHostType);
            writer.Write(Attributes);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"SessionKey:{SessionKey}" + " " +
$"ApplicationID:{ApplicationID}" + " " +
$"MinPlayers:{MinPlayers}" + " " +
$"MaxPlayers:{MaxPlayers}" + " " +
$"GameLevel:{GameLevel}" + " " +
$"GameName:{GameName}" + " " +
$"GamePassword:{GamePassword}" + " " +
$"SpectatorPassword:{SpectatorPassword}" + " " +
$"PlayerSkillLevel:{PlayerSkillLevel}" + " " +
$"RulesSet:{RulesSet}" + " " +
$"GenericField1:{GenericField1}" + " " +
$"GenericField2:{GenericField2}" + " " +
$"GenericField3:{GenericField3}" + " " +
$"GenericField4:{GenericField4}" + " " +
$"GenericField5:{GenericField5}" + " " +
$"GenericField6:{GenericField6}" + " " +
$"GenericField7:{GenericField7}" + " " +
$"GenericField8:{GenericField8}" + " " +
$"GameHostType:{GameHostType}" + " " +
$"Attributes:{Attributes}";
        }
    }
}