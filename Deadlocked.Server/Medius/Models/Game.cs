using Deadlocked.Server.Medius;
using Deadlocked.Server.SCERT.Models;
using Deadlocked.Server.Mods;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Deadlocked.Server.Medius.Models.Packets;
using Deadlocked.Server.Medius.Models.Packets.Lobby;
using Deadlocked.Server.Medius.Models.Packets.MGCL;
using DotNetty.Common.Internal.Logging;

namespace Deadlocked.Server.Medius.Models
{
    public class Game
    {
        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<Game>();

        public static int IdCounter = 1;

        public class GameClient
        {
            public ClientObject Client;
            public bool InGame;
        }

        public int Id = 0;
        public int DMEWorldId = 0;
        public int ChannelId = 0;
        public int ApplicationId = 0;
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
        public MediusWorldStatus WorldStatus = MediusWorldStatus.WorldPendingCreation;
        public MediusWorldAttributesType Attributes;
        public DMEObject DMEServer;
        public Channel ChatChannel;
        public ClientObject Host;

        public Gamemode CustomGamemode = null;

        private DateTime utcTimeCreated;
        private DateTime? utcTimeEmpty;

        public uint Time => (uint)(DateTime.UtcNow - utcTimeCreated).TotalMilliseconds;

        public int PlayerCount => Clients.Count(x => x != null && x.Client.IsConnected);

        public bool ReadyToDestroy => WorldStatus == MediusWorldStatus.WorldClosed && (DateTime.UtcNow - utcTimeEmpty)?.TotalSeconds > 1f;

        public Game(ClientObject client, MediusCreateGameRequest createGame, DMEObject dmeServer)
        {
            Id = IdCounter++;
            ApplicationId = createGame.ApplicationID;
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
            utcTimeEmpty = null;
            DMEServer = dmeServer;
            ChannelId = client.CurrentChannelId;
            ChatChannel = Program.GetChannelById(ChannelId);
            ChatChannel?.RegisterGame(this);
            Host = client;

            Logger.Info($"Game {Id}:{GameName}: Created by {client}");
        }

        public void Tick()
        {
            // Remove timedout clients
            for (int i = 0; i < Clients.Count; ++i)
            {
                var client = Clients[i];

                if (client == null || client.Client == null || !client.Client.IsConnected || client.Client.CurrentGameId != Id)
                {
                    Clients.RemoveAt(i);
                    --i;
                }
            }

            // Auto close when everyone leaves or if host fails to connect after timeout time
            if (!utcTimeEmpty.HasValue && Clients.Count(x=>x.InGame) == 0 && (DateTime.UtcNow - utcTimeCreated).TotalSeconds > Program.Settings.GameTimeoutSeconds)
            {
                utcTimeEmpty = DateTime.UtcNow;
                WorldStatus = MediusWorldStatus.WorldClosed;
            }
        }

        public void OnMediusServerConnectNotification(MediusServerConnectNotification notification)
        {
            var player = Clients.FirstOrDefault(x => x.Client.SessionKey == notification.PlayerSessionKey);
            if (player == null)
                return;

            switch (notification.ConnectEventType)
            {
                case MGCL_EVENT_TYPE.MGCL_EVENT_CLIENT_CONNECT:
                    {
                        player.InGame = true;
                        break;
                    }
                case MGCL_EVENT_TYPE.MGCL_EVENT_CLIENT_DISCONNECT:
                    {
                        player.InGame = false;
                        OnPlayerLeft(player);
                        break;
                    }
            }
        }

        public void OnPlayerJoined(ClientObject client)
        {
            // Don't add again
            if (Clients.Any(x => x.Client == client))
                return;

            // 
            Logger.Info($"Game {Id}:{GameName}: {client} joined.");

            Clients.Add(new GameClient()
            {
                Client = client
            });

            client.Status = MediusPlayerStatus.MediusPlayerInGameWorld;
            client.CurrentGameId = Id;

            if (CustomGamemode != null)
                client.CurrentChannel?.SendSystemMessage(client, $"Gamemode is {CustomGamemode.FullName}.");
        }

        private void OnPlayerLeft(GameClient client)
        {
            // 
            Logger.Info($"Game {Id}:{GameName}: {client} left.");

            // Remove host
            if (Host == client.Client)
                Host = null;

            if (Clients.Contains(client))
            {
                // Remove reference
                if (client.Client != null)
                    client.Client.CurrentGameId = -1;

                Clients.Remove(client);
            }
        }

        public void OnEndGameReport(MediusEndGameReport report)
        {
            WorldStatus = MediusWorldStatus.WorldClosed;
        }


        public void OnPlayerReport(MediusPlayerReport report)
        {
            // Ensure report is for correct game world
            if (report.MediusWorldID != Id)
                return;

            
        }
        public void OnWorldReport(MediusWorldReport report)
        {
            // Ensure report is for correct game world
            if (report.MediusWorldID != Id)
                return;

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

            // Once the world has been closed then we force it closed.
            // This is because when the host hits 'Play Again' they tell the server the world has closed (EndGameReport)
            // but the existing clients tell the server the world is still active.
            // This gives the host a "Game Name Already Exists" when they try to remake with the same name.
            // This just fixes that. At the cost of the game not showing after a host leaves a game.
            if (WorldStatus != MediusWorldStatus.WorldClosed)
            {
                // When game starts, send game mode payload
                if (report.WorldStatus == MediusWorldStatus.WorldActive && WorldStatus != MediusWorldStatus.WorldActive)
                {
                    if (CustomGamemode != null)
                        CustomGamemode.Apply(Clients.Select(x => x.Client));
                    else
                        Gamemode.Disable(Clients.Select(x => x.Client));
                }

                WorldStatus = report.WorldStatus;
            }
        }

        public void EndGame()
        {
            // 
            Logger.Info($"Game {Id}:{GameName}: EndGame() called.");

            // Unregister from channel
            ChatChannel?.UnregisterGame(this);

            // Remove players from game world
            foreach (var client in Clients)
            {
                if (client.Client != null)
                    client.Client.CurrentGameId = -1;
            }

            // Send end game
            DMEServer?.Queue(new MediusServerEndGameRequest()
            {
                WorldID = this.DMEWorldId,
                BrutalFlag = false
            });
        }
    }
}
