using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.Lobby
{
    [MediusApp(MediusAppPacketIds.GameList_ExtraInfoResponse)]
    public class MediusGameList_ExtraInfoResponse : BaseLobbyMessage
    {

        public override MediusAppPacketIds Id => MediusAppPacketIds.GameList_ExtraInfoResponse;

        public MediusCallbackStatus StatusCode;
        public int MediusWorldID;
        public ushort PlayerCount;
        public ushort MinPlayers;
        public ushort MaxPlayers;
        public int GameLevel;
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
        public MediusWorldSecurityLevelType SecurityLevel;
        public MediusWorldStatus WorldStatus;
        public MediusGameHostType GameHostType;
        public string GameName; // GAMENAME_MAXLEN
        public byte[] GameStats = new byte[MediusConstants.GAMESTATS_MAXLEN];
        public bool EndOfList;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            StatusCode = reader.Read<MediusCallbackStatus>();
            MediusWorldID = reader.ReadInt32();
            PlayerCount = reader.ReadUInt16();
            MinPlayers = reader.ReadUInt16();
            MaxPlayers = reader.ReadUInt16();
            reader.ReadBytes(2);
            GameLevel = reader.ReadInt32();
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
            SecurityLevel = reader.Read<MediusWorldSecurityLevelType>();
            WorldStatus = reader.Read<MediusWorldStatus>();
            GameHostType = reader.Read<MediusGameHostType>();
            GameName = reader.ReadString(MediusConstants.GAMENAME_MAXLEN);
            GameStats = reader.ReadBytes(MediusConstants.GAMESTATS_MAXLEN);
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
            writer.Write(PlayerCount);
            writer.Write(MinPlayers);
            writer.Write(MaxPlayers);
            writer.Write(new byte[2]);
            writer.Write(GameLevel);
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
            writer.Write(SecurityLevel);
            writer.Write(WorldStatus);
            writer.Write(GameHostType);
            writer.Write(GameName, MediusConstants.GAMENAME_MAXLEN);
            writer.Write(GameStats);
            writer.Write(EndOfList);
            writer.Write(new byte[3]);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"StatusCode:{StatusCode}" + " " +
$"MediusWorldID:{MediusWorldID}" + " " +
$"PlayerCount:{PlayerCount}" + " " +
$"MinPlayers:{MinPlayers}" + " " +
$"MaxPlayers:{MaxPlayers}" + " " +
$"GameLevel:{GameLevel}" + " " +
$"PlayerSkillLevel:{PlayerSkillLevel}" + " " +
$"RulesSet:{RulesSet}" + " " +
$"GenericField1:{GenericField1:X8}" + " " +
$"GenericField2:{GenericField2:X8}" + " " +
$"GenericField3:{GenericField3:X8}" + " " +
$"GenericField4:{GenericField4:X8}" + " " +
$"GenericField5:{GenericField5:X8}" + " " +
$"GenericField6:{GenericField6:X8}" + " " +
$"GenericField7:{GenericField7:X8}" + " " +
$"GenericField8:{GenericField8:X8}" + " " +
$"SecurityLevel:{SecurityLevel}" + " " +
$"WorldStatus:{WorldStatus}" + " " +
$"GameHostType:{GameHostType}" + " " +
$"GameName:{GameName}" + " " +
$"GameStats:{GameStats}" + " " +
$"EndOfList:{EndOfList}";
        }
    }
}