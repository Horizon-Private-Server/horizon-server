using Deadlocked.Server.Messages;
using Deadlocked.Server.Messages.Lobby;
using Deadlocked.Server.Messages.RTIME;
using Org.BouncyCastle.Ocsp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Deadlocked.Server
{
    public enum ChannelType
    {
        Lobby,
        Game
    }
    public class Channel
    {
        public static int IdCounter = 1;

        public class ChannelClient
        {
            public ClientObject Client;
        }

        

        public int Id = 0;
        public ChannelType Type = ChannelType.Game;
        public string Name = "Default";
        public string Password = null;
        public int MaxPlayers = 10;
        public MediusWorldSecurityLevelType SecurityLevel = MediusWorldSecurityLevelType.WORLD_SECURITY_NONE;
        public uint GenericField1 = 0;
        public uint GenericField2 = 0;
        public uint GenericField3 = 0;
        public uint GenericField4 = 0;
        public MediusWorldGenericFieldLevelType GenericFieldLevel = MediusWorldGenericFieldLevelType.MediusWorldGenericFieldLevel0;

        public bool ReadyToDestroy => Type == ChannelType.Game && removeChannel;
        public int PlayerCount => Clients.Count;
        public int GameCount => games.Count;

        private List<Game> games = new List<Game>();
        public List<ChannelClient> Clients = new List<ChannelClient>();
        private bool removeChannel = false;

        public Channel()
        {
            Id = IdCounter++;
        }

        public Channel(MediusCreateChannelRequest request)
        {
            Id = IdCounter++;

            Name = request.LobbyName;
            Password = request.LobbyPassword;
            SecurityLevel = string.IsNullOrEmpty(Password) ? MediusWorldSecurityLevelType.WORLD_SECURITY_NONE : MediusWorldSecurityLevelType.WORLD_SECURITY_PLAYER_PASSWORD;
            MaxPlayers = request.MaxPlayers;
            GenericField1 = request.GenericField1;
            GenericField2 = request.GenericField2;
            GenericField3 = request.GenericField3;
            GenericField4 = request.GenericField4;
            GenericFieldLevel = request.GenericFieldLevel;
        }

        public void Tick()
        {
            // Remove inactive clients
            for (int i = 0; i < Clients.Count; ++i)
            {
                if (!Clients[i].Client.IsConnected)
                {
                    Clients.RemoveAt(i);
                    --i;
                }
            }
        }

        public void OnPlayerJoined(ClientObject client)
        {
            Clients.Add(new ChannelClient()
            {
                Client = client
            });
        }

        public void OnPlayerLeft(ClientObject client)
        {
            Clients.RemoveAll(x => x.Client == client);
        }

        public void RegisterGame(Game game)
        {
            games.Add(game);
        }

        public void UnregisterGame(Game game)
        {
            // Remove game
            games.Remove(game);

            // If empty, just end channel
            if (games.Count == 0)
            {
                removeChannel = true;
            }
        }

        public void BroadcastBinaryMessage(ClientObject source, MediusBinaryMessage msg)
        {
            foreach (var client in Clients)
            {
                if (client.Client != null && client.Client.IsConnected && client.Client.ClientAccount != null && client.Client != source)
                {
                    client.Client?.AddLobbyMessage(new RT_MSG_SERVER_APP()
                    {
                        AppMessage = new MediusBinaryFwdMessage()
                        {
                            MessageType = msg.MessageType,
                            OriginatorAccountID = client.Client.ClientAccount.AccountId,
                            Message = msg.Message
                        }
                    });
                }
            }
        }
    }
}
