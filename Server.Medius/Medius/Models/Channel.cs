using RT.Common;
using RT.Models;
using Server.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server.Medius.Models
{
    public enum ChannelType
    {
        Lobby,
        Game
    }

    public class Channel
    {
        public static int IdCounter = 0;

        public List<ClientObject> Clients = new List<ClientObject>();

        public int Id = 0;
        public int ApplicationId = 0;
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

        public bool ReadyToDestroy => Type == ChannelType.Game && (_removeChannel || ((DateTime.UtcNow - _timeCreated).TotalSeconds > Program.Settings.GameTimeoutSeconds) && GameCount == 0);
        public int PlayerCount => Clients.Count;
        public int GameCount => _games.Count;

        private List<Game> _games = new List<Game>();
        private bool _removeChannel = false;
        private DateTime _timeCreated = DateTime.UtcNow;

        public Channel()
        {
            Id = IdCounter++;
        }

        public Channel(MediusCreateChannelRequest request)
        {
            Id = IdCounter++;

            ApplicationId = request.ApplicationID;
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
                if (!Clients[i].IsConnected)
                {
                    Clients.RemoveAt(i);
                    --i;
                }
            }
        }

        public void OnPlayerJoined(ClientObject client)
        {
            Clients.Add(client);
        }

        public void OnPlayerLeft(ClientObject client)
        {
            Clients.RemoveAll(x => x == client);
        }


        public void RegisterGame(Game game)
        {
            _games.Add(game);
        }

        public void UnregisterGame(Game game)
        {
            // Remove game
            _games.Remove(game);

            // If empty, just end channel
            if (_games.Count == 0)
            {
                _removeChannel = true;
            }
        }

        public void BroadcastBinaryMessage(ClientObject source, MediusBinaryMessage msg)
        {
            foreach (var client in Clients.Where(x => x != source))
            {
                client?.Queue(new MediusBinaryFwdMessage()
                {
                    MessageType = msg.MessageType,
                    OriginatorAccountID = source.AccountId,
                    Message = msg.Message
                });
            }
        }

        public void BroadcastChatMessage(IEnumerable<ClientObject> targets, ClientObject source, string message)
        {
            foreach (var target in targets)
            {
                target?.Queue(new MediusGenericChatFwdMessage()
                {
                    OriginatorAccountID = source.AccountId,
                    OriginatorAccountName = source.AccountName,
                    Message = "A" + message,
                    MessageType = MediusChatMessageType.Broadcast,
                    TimeStamp = Utils.GetUnixTime()
                });
            }
        }

        public void SendSystemMessage(ClientObject client, string message)
        {
            client.Queue(new MediusGenericChatFwdMessage()
            {
                OriginatorAccountID = 0,
                OriginatorAccountName = "SYSTEM",
                Message = "A" + message,
                MessageType = MediusChatMessageType.Broadcast,
                TimeStamp = Utils.GetUnixTime()
            });
        }

        public void BroadcastSystemMessage(IEnumerable<ClientObject> targets, string message)
        {
            foreach (var target in targets)
            {
                target?.Queue(new MediusGenericChatFwdMessage()
                {
                    OriginatorAccountID = 0,
                    OriginatorAccountName = "SYSTEM",
                    Message = "A" + message,
                    MessageType = MediusChatMessageType.Broadcast,
                    TimeStamp = Utils.GetUnixTime()
                });
            }
        }
    }
}
