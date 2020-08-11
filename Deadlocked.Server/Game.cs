using Deadlocked.Server.Medius;
using Deadlocked.Server.Messages;
using Deadlocked.Server.Messages.Lobby;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Deadlocked.Server
{
    public class Game
    {
        public static int IdCounter = 1;

        public class GameClient
        {
            public ClientObject Client;
            public int GameId;
        }

        public int Id = 0;
        public int DMEWorldId = 0;
        public List<GameClient> Clients = new List<GameClient>();
        public string GameName;
        public string GamePassword;
        public string SpectatorPassword;
        public byte[] GameStats = new byte[MediusConstants.GAMESTATS_MAXLEN];
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

        public DMEServer DME = null;

        private DateTime utcTimeCreated;
        private int gameClientIdCounter = 1;

        public uint Time => (uint)(DateTime.UtcNow - utcTimeCreated).TotalMilliseconds;

        public int PlayerCount => Clients.Count(x => x != null && x.Client.IsConnected);

        public Game()
        {
            Id = IdCounter++;
            WorldStatus = MediusWorldStatus.WorldPendingCreation;
            utcTimeCreated = DateTime.UtcNow;
            //DME = new DMEServer(this);
            //DME.Start();
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
            //DME = new DMEServer(this);
            //DME.Start();
        }

        public void Tick()
        {
            DME?.Tick();
        }

        public void OnPlayerJoined(ClientObject client)
        {
            Clients.Add(new GameClient()
            {
                Client = client,
                GameId = gameClientIdCounter++
            });
        }

        public void OnEndGameReport(MediusEndGameReport report)
        {
            Clients.Clear();
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
