using Deadlocked.Server.Messages;
using Deadlocked.Server.Messages.Lobby;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Deadlocked.Server
{
    public class Game
    {
        public static int IdCounter = 1;

        public int Id = 0;
        public List<ClientObject> Clients = new List<ClientObject>();
        public string GameName;
        public string GamePassword;
        public string SpectatorPassword;
        public string GameStats;
        public MediusGameHostType GameHostType;
        public int MinPlayers;
        public int MaxPlayers;
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
        public MediusWorldStatus WorldStatus;
        public MediusWorldAttributesType Attributes;


        private DateTime utcTimeCreated;

        public uint Time => (uint)(DateTime.UtcNow - utcTimeCreated).TotalMilliseconds;

        public Game()
        {
            Id = IdCounter++;
            WorldStatus = MediusWorldStatus.WorldPendingCreation;
            utcTimeCreated = DateTime.UtcNow;
        }

        public Game(MediusCreateGameRequest createGame)
        {
            Id = IdCounter++;
            GameName = createGame.GameName;
            MinPlayers = createGame.MinPlayers;
            MaxPlayers = createGame.MaxPlayers;
            GameLevel = createGame.GameLevel;
            PlayerSkillLevel = createGame.PlayerSkillLevel;
            RulesSet = createGame.RulesSet;
            GenericField1 = createGame.GenericField1;
            GenericField2 = createGame.GenericField2;
            GenericField3 = createGame.GenericField3;
            GenericField4 = createGame.GenericField4;
            GenericField5 = createGame.GenericField5;
            GenericField6 = createGame.GenericField6;
            GenericField7 = createGame.GenericField7;
            GenericField8 = createGame.GenericField8;
            GamePassword = createGame.GamePassword;
            SpectatorPassword = createGame.SpectatorPassword;
            GameHostType = createGame.GameHostType;
            Attributes = createGame.Attributes;
            WorldStatus = MediusWorldStatus.WorldPendingCreation;
            utcTimeCreated = DateTime.UtcNow;
        }

        public void OnPlayerJoined(ClientObject client)
        {
            Clients.Add(client);
        }

        public void OnWorldReport(MediusWorldReport report)
        {
            GameName = report.GameName;
            MinPlayers = report.MinPlayers;
            MaxPlayers = report.MaxPlayers;
            GameLevel = report.GameLevel;
            PlayerSkillLevel = report.PlayerSkillLevel;
            RulesSet = report.RulesSet;
            GenericField1 = report.GenericField1;
            GenericField2 = report.GenericField2;
            GenericField3 = report.GenericField3;
            GenericField4 = report.GenericField4;
            GenericField5 = report.GenericField5;
            GenericField6 = report.GenericField6;
            GenericField7 = report.GenericField7;
            GenericField8 = report.GenericField8;
            WorldStatus = report.WorldStatus;
        }
    }
}
