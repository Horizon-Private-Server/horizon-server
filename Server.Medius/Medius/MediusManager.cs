using DotNetty.Common.Internal.Logging;
using RT.Common;
using RT.Models;
using Server.Medius.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Medius
{
    public class MediusManager
    {
        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<MediusManager>();

        private Dictionary<int, ClientObject> _accountIdToClient = new Dictionary<int, ClientObject>();
        private Dictionary<string, ClientObject> _accountNameToClient = new Dictionary<string, ClientObject>();
        private Dictionary<string, ClientObject> _accessTokenToClient = new Dictionary<string, ClientObject>();
        private Dictionary<string, ClientObject> _sessionKeyToClient = new Dictionary<string, ClientObject>();
        private Dictionary<int, ClientObject> _playersLoggedIn = new Dictionary<int, ClientObject>();


        private Dictionary<string, DMEObject> _accessTokenToDmeClient = new Dictionary<string, DMEObject>();
        private Dictionary<string, DMEObject> _sessionKeyToDmeClient = new Dictionary<string, DMEObject>();

        private List<Channel> _channels = new List<Channel>();
        private List<MediusFile> _mediusFiles = new List<MediusFile>();
        private Dictionary<int, Game> _gameIdToGame = new Dictionary<int, Game>();

        private Dictionary<int, Clan> _clanIdToClan = new Dictionary<int, Clan>();
        private Dictionary<string, Clan> _clanNameToClan = new Dictionary<string, Clan>();

        private ConcurrentQueue<ClientObject> _addQueue = new ConcurrentQueue<ClientObject>();

        #region Clients
        public List<ClientObject> GetClients(int appId)
        {
            return _accountIdToClient.Select(x => x.Value).Where(x => x.ApplicationId == appId).ToList();
        }

        public ClientObject GetClientByAccountId(int? accountId)
        {
            if (_accountIdToClient.TryGetValue((int)accountId, out var result))
                return result;

            return null;
        }

        public ClientObject GetClientByWorldId(int worldId)
        {
            if (_playersLoggedIn.TryGetValue(worldId, out var result))
                return result;

            return null;
        }

        public ClientObject GetClientByAccountName(string accountName)
        {
            accountName = accountName.ToLower();
            if (_accountNameToClient.TryGetValue(accountName, out var result))
                return result;

            return null;
        }

        public ClientObject GetClientByAccessToken(string accessToken)
        {
            if (_accessTokenToClient.TryGetValue(accessToken, out var result))
                return result;

            return null;
        }

        public DMEObject GetDmeByAccessToken(string accessToken)
        {
            if (_accessTokenToDmeClient.TryGetValue(accessToken, out var result))
                return result;

            return null;
        }

        public DMEObject GetDmeBySessionKey(string sessionKey)
        {
            if (_sessionKeyToDmeClient.TryGetValue(sessionKey, out var result))
                return result;

            return null;
        }

        public void AddDmeClient(DMEObject dmeClient)
        {
            if (!dmeClient.IsLoggedIn)
                throw new InvalidOperationException($"Attempting to add DME client {dmeClient} to MediusManager but client has not yet logged in.");

            try
            {
                _accessTokenToDmeClient.Add(dmeClient.Token, dmeClient);
                _sessionKeyToDmeClient.Add(dmeClient.SessionKey, dmeClient);
            }
            catch (Exception e)
            {
                // clean up
                if (dmeClient != null)
                {
                    if (dmeClient.Token != null)
                        _accessTokenToDmeClient.Remove(dmeClient.Token);

                    if (dmeClient.SessionKey != null)
                        _sessionKeyToDmeClient.Remove(dmeClient.SessionKey);
                }

                throw e;
            }
        }

        public void AddClient(ClientObject client)
        {
            if (!client.IsLoggedIn)
                throw new InvalidOperationException($"Attempting to add {client} to MediusManager but client has not yet logged in.");

            _addQueue.Enqueue(client);
        }

        #endregion

        #region Games

        public Game GetGameByDmeWorldId(int dmeWorldId)
        {
            return _gameIdToGame.FirstOrDefault(x => x.Value?.DMEWorldId == dmeWorldId).Value;
        }

        public Game GetGameByGameId(int gameId)
        {
            if (_gameIdToGame.TryGetValue(gameId, out var result))
                return result;

            return null;
        }

        public void AddGame(Game game)
        {
            _gameIdToGame.Add(game.Id, game);
            _ = Program.Database.CreateGame(game.ToGameDTO());
        }

        public IEnumerable<Game> GetGameList(int appId, int pageIndex, int pageSize, IEnumerable<GameListFilter> filters)
        {
            return _gameIdToGame
                            .Select(x => x.Value)
                            .Where(x => x.ApplicationId == appId &&
                                        (x.WorldStatus == MediusWorldStatus.WorldActive || x.WorldStatus == MediusWorldStatus.WorldStaging) &&
                                        (filters.Count() == 0 || filters.Any(y => y.IsMatch(x))))
                            .Skip((pageIndex - 1) * pageSize)
                            .Take(pageSize);
        }

        public IEnumerable<Game> GetGameListByGameId(int appId, int pageIndex, int pageSize, int? game)
        {
            return _gameIdToGame
                            .Select(x => x.Value)
                            .Where(x => x.ApplicationId == appId &&
                                        (x.WorldStatus == MediusWorldStatus.WorldActive || x.WorldStatus == MediusWorldStatus.WorldStaging)
                                        && x.Id == game)
                            .Skip((pageIndex - 1) * pageSize)
                            .Take(pageSize);
        }

        public IEnumerable<Game> GetGameList(int appId, int pageIndex, int pageSize)
        {
            return _gameIdToGame
                            .Select(x => x.Value)
                            .Where(x => x.ApplicationId == appId && 
                                        (x.WorldStatus == MediusWorldStatus.WorldActive || x.WorldStatus == MediusWorldStatus.WorldStaging))
                            .Skip((pageIndex - 1) * pageSize)
                            .Take(pageSize);
        }

        public void CreateGame(ClientObject client, IMediusRequest request)
        {
            string gameName = null;
            if (request is MediusCreateGameRequest r)
                gameName = r.GameName;
            else if (request is MediusCreateGameRequest0 r0)
                gameName = r0.GameName;
            else if (request is MediusCreateGameRequest1 r1)
                gameName = r1.GameName;

            // Ensure the name is unique
            // If the host leaves then we unreserve the name
            if (_gameIdToGame.Select(x => x.Value).Any(x => x.WorldStatus != MediusWorldStatus.WorldClosed && x.WorldStatus != MediusWorldStatus.WorldInactive && x.GameName == gameName && x.Host != null && x.Host.IsConnected))
            {
                client.Queue(new RT_MSG_SERVER_APP()
                {
                    Message = new MediusCreateGameResponse()
                    {
                        MessageID = request.MessageID,
                        MediusWorldID = -1,
                        StatusCode = MediusCallbackStatus.MediusGameNameExists
                    }
                });
                return;
            }

            // Try to get next free dme server
            // If none exist, return error to client
            var dme = Program.ProxyServer.GetFreeDme(client.ApplicationId);
            if (dme == null)
            {
                client.Queue(new MediusCreateGameResponse()
                {
                    MessageID = request.MessageID,
                    MediusWorldID = -1,
                    StatusCode = MediusCallbackStatus.MediusExceedsMaxWorlds
                });
                return;
            }

            // Create and add
            try
            {
                var game = new Game(client, request, client.CurrentChannel, dme);
                AddGame(game);
                
                // Send create game request to dme server
                dme.Queue(new MediusServerCreateGameWithAttributesRequest()
                {
                    MessageID = new MessageId($"{game.Id}-{client.AccountId}-{request.MessageID}"),
                    MediusWorldUID = (uint)game.Id,
                    Attributes = game.Attributes,
                    ApplicationID = client.ApplicationId,
                    MaxClients = game.MaxPlayers
                });
            }
            catch (Exception e)
            {
                // 
                Logger.Error(e);

                // Failure adding game for some reason
                client.Queue(new MediusCreateGameResponse()
                {
                    MessageID = request.MessageID,
                    MediusWorldID = -1,
                    StatusCode = MediusCallbackStatus.MediusFail
                });
            }
        }

        #region JoinGameRequest
        public void JoinGame(ClientObject client, MediusJoinGameRequest request)
        {
            var game = GetGameByGameId(request.MediusWorldID);
            if (game == null)
            {
                client.Queue(new MediusJoinGameResponse()
                {
                    MessageID = request.MessageID,
                    StatusCode = MediusCallbackStatus.MediusGameNotFound
                });
            }
            else if (game.GamePassword != null && game.GamePassword != string.Empty && game.GamePassword != request.GamePassword)
            {
                client.Queue(new MediusJoinGameResponse()
                {
                    MessageID = request.MessageID,
                    StatusCode = MediusCallbackStatus.MediusInvalidPassword
                });
            }
            else if (game.PlayerCount >= game.MaxPlayers)
            {
                client.Queue(new MediusJoinGameResponse()
                {
                    MessageID = request.MessageID,
                    StatusCode = MediusCallbackStatus.MediusWorldIsFull
                });
            }
            else
            {
                var dme = game.DMEServer;
                // if This is a Peer to Peer Player Host as DME we treat differently
                if (game.GameHostType == MediusGameHostType.MediusGameHostPeerToPeer)
                {
                    dme.Queue(new MediusServerJoinGameRequest()
                    {
                        MessageID = new MessageId($"{game.Id}-{client.AccountId}-{request.MessageID}"),
                        ConnectInfo = new NetConnectionInfo()
                        {
                            Type = NetConnectionType.NetConnectionTypePeerToPeerUDP,
                            WorldID = game.DMEWorldId,
                            SessionKey = client.SessionKey,
                            ServerKey = Program.GlobalAuthPublic
                        }
                    });
                }
                // Else send normal Connection type
                else
                {
                    dme.Queue(new MediusServerJoinGameRequest()
                    {
                        MessageID = new MessageId($"{game.Id}-{client.AccountId}-{request.MessageID}"),
                        ConnectInfo = new NetConnectionInfo()
                        {
                            Type = NetConnectionType.NetConnectionTypeClientServerTCPAuxUDP,
                            WorldID = game.DMEWorldId,
                            SessionKey = client.SessionKey,
                            ServerKey = Program.GlobalAuthPublic
                        }
                    });
                }

            }
        }
        #endregion

        #region JoinGameRequest0
        public void JoinGame0(ClientObject client, MediusJoinGameRequest0 request)
        {
            var game = GetGameByGameId(request.MediusWorldID);
            if (game == null)
            {
                client.Queue(new MediusJoinGameResponse()
                {
                    MessageID = request.MessageID,
                    StatusCode = MediusCallbackStatus.MediusGameNotFound
                });
            }
            else if (game.GamePassword != null && game.GamePassword != string.Empty && game.GamePassword != request.GamePassword)
            {
                client.Queue(new MediusJoinGameResponse()
                {
                    MessageID = request.MessageID,
                    StatusCode = MediusCallbackStatus.MediusInvalidPassword
                });
            }
            /*
            else if (game.PlayerCount >= game.MaxPlayers)
            {
                client.Queue(new MediusJoinGameResponse()
                {
                    MessageID = request.MessageID,
                    StatusCode = MediusCallbackStatus.MediusWorldIsFull
                });
            }
            */
            else
            {
                var dme = game.DMEServer;
                // if This is a Peer to Peer Player Host as DME we treat differently
                if (game.GameHostType == MediusGameHostType.MediusGameHostPeerToPeer)
                {
                    dme.Queue(new MediusServerJoinGameRequest()
                    {
                        MessageID = new MessageId($"{game.Id}-{client.AccountId}-{request.MessageID}"),
                        ConnectInfo = new NetConnectionInfo()
                        {
                            Type = NetConnectionType.NetConnectionTypePeerToPeerUDP,
                            WorldID = game.DMEWorldId,
                            SessionKey = client.SessionKey,
                            ServerKey = Program.GlobalAuthPublic
                        }
                    });
                }
                // Else send normal Connection type
                else
                {
                    dme.Queue(new MediusServerJoinGameRequest()
                    {
                        MessageID = new MessageId($"{game.Id}-{client.AccountId}-{request.MessageID}"),
                        ConnectInfo = new NetConnectionInfo()
                        {
                            Type = NetConnectionType.NetConnectionTypeClientServerTCP,
                            WorldID = game.DMEWorldId,
                            SessionKey = client.SessionKey,
                            ServerKey = Program.GlobalAuthPublic
                        }
                    });
                }
            }
        }
        #endregion

        #endregion

        #region Channels

        public Channel GetChannelByChannelId(int channelId, int appId)
        {
            lock (_channels)
            {
                return _channels.FirstOrDefault(x => x.Id == channelId && x.ApplicationId == appId);
            }
        }

        public Channel GetChannelByChannelName(string channelName, int appId)
        {
            lock (_channels)
            {
                return _channels.FirstOrDefault(x => x.Name == channelName && x.ApplicationId == appId);
            }
        }

        public uint GetChannelCount(ChannelType type, int appId)
        {
            lock (_channels)
            {
                return (uint)_channels.Count(x => x.Type == type && x.ApplicationId == appId);
            }
        }

        public Channel GetDefaultLobbyChannel(int appId) { 
            lock (_channels)
            {
                // If all app ids are compatible then return the default
                if (Program.Settings.ApplicationIds == null)
                {
                    return _channels
                        .FirstOrDefault(x => x.Type == ChannelType.Lobby && x.ApplicationId == 0);
                }
                else
                {
                    return _channels
                        .FirstOrDefault(x => x.Type == ChannelType.Lobby && x.ApplicationId == appId);
                }
            }
        }

        public void AddChannel(Channel channel)
        {
            lock (_channels)
            {
                _channels.Add(channel);
            }
        }

        public IEnumerable<Channel> GetChannelList(int appId, int pageIndex, int pageSize, ChannelType type)
        {
            lock (_channels)
            {
                return _channels
                    .Where(x => x.ApplicationId == appId && x.Type == type)
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize);
            }
        }

        #endregion
        
        public IEnumerable<MediusFile> GetFilesList()
        {
            lock (_mediusFiles)
            {
                return _mediusFiles;
            }
        }
        
        #region Clans

        public Clan GetClanByAccountId(int clanId)
        {
            if (_clanIdToClan.TryGetValue(clanId, out var result))
                return result;

            return null;
        }

        public Clan GetClanByAccountName(string clanName)
        {
            clanName = clanName.ToLower();
            if (_clanNameToClan.TryGetValue(clanName, out var result))
                return result;

            return null;
        }

        public void AddClan(Clan clan)
        {
            _clanNameToClan.Add(clan.Name.ToLower(), clan);
            _clanIdToClan.Add(clan.Id, clan);
        }

        #endregion

        #region Tick

        public async Task Tick()
        {
            await TickClients();

            await TickChannels();

            await TickGames();
        }

        private async Task TickChannels()
        {
            Queue<Channel> channelsToRemove = new Queue<Channel>();

            // Tick channels
            foreach (var channel in _channels)
            {
                if (channel.ReadyToDestroy)
                {
                    Logger.Info($"Destroying Channel {channel}");
                    channelsToRemove.Enqueue(channel);
                }
                else
                {
                    await channel.Tick();
                }
            }

            // Remove channels
            while (channelsToRemove.TryDequeue(out var channel))
                _channels.Remove(channel);
        }

        private async Task TickGames()
        {
            Queue<int> gamesToRemove = new Queue<int>();

            // Tick games
            foreach (var gameKeyPair in _gameIdToGame)
            {
                if (gameKeyPair.Value.ReadyToDestroy)
                {
                    Logger.Info($"Destroying Game {gameKeyPair.Value}");
                    await gameKeyPair.Value.EndGame();
                    gamesToRemove.Enqueue(gameKeyPair.Key);
                }
                else
                {
                    await gameKeyPair.Value.Tick();
                }
            }

            // Remove games
            while (gamesToRemove.TryDequeue(out var gameId))
                _gameIdToGame.Remove(gameId);
        }

        private async Task TickClients()
        {
            Queue<string> clientsToRemove = new Queue<string>();

            while (_addQueue.TryDequeue(out var newClient))
            {
                try
                {
                    _accountIdToClient.Add(newClient.AccountId, newClient);
                    _accountNameToClient.Add(newClient.AccountName.ToLower(), newClient);
                    _accessTokenToClient.Add(newClient.Token, newClient);
                    _sessionKeyToClient.Add(newClient.SessionKey, newClient);
                }
                catch (Exception e)
                {
                    // clean up
                    if (newClient != null)
                    {
                        _accountIdToClient.Remove(newClient.AccountId);

                        if (newClient.AccountName != null)
                            _accountNameToClient.Remove(newClient.AccountName.ToLower());

                        if (newClient.Token != null)
                            _accessTokenToClient.Remove(newClient.Token);

                        if (newClient.SessionKey != null)
                            _sessionKeyToClient.Remove(newClient.SessionKey);
                    }

                    Logger.Error(e);
                    //throw e;
                }
            }

            foreach (var clientKeyPair in _sessionKeyToClient)
            {
                if (!clientKeyPair.Value.IsConnected)
                {
                    if (clientKeyPair.Value.Timedout)
                        Logger.Warn($"Timing out client {clientKeyPair.Value}");
                    else
                        Logger.Info($"Destroying Client {clientKeyPair.Value}");

                    // Logout and end session
                    await clientKeyPair.Value.Logout();
                    clientKeyPair.Value.EndSession();

                    clientsToRemove.Enqueue(clientKeyPair.Key);
                }
            }

            // Remove
            while (clientsToRemove.TryDequeue(out var sessionKey))
            {
                if (_sessionKeyToClient.Remove(sessionKey, out var clientObject))
                {
                    _accountIdToClient.Remove(clientObject.AccountId);
                    _accessTokenToClient.Remove(clientObject.Token);
                    _accountNameToClient.Remove(clientObject.AccountName.ToLower());
                }
            }
        }

        private void TickDme()
        {
            Queue<string> dmeToRemove = new Queue<string>();

            foreach (var dmeKeyPair in _sessionKeyToDmeClient)
            {
                if (!dmeKeyPair.Value.IsConnected)
                {
                    Logger.Info($"Destroying DME Client {dmeKeyPair.Value}");

                    // Logout and end session
                    dmeKeyPair.Value?.Logout();
                    dmeKeyPair.Value?.EndSession();

                    dmeToRemove.Enqueue(dmeKeyPair.Key);
                }
            }

            // Remove
            while (dmeToRemove.TryDequeue(out var sessionKey))
            {
                if (_sessionKeyToDmeClient.Remove(sessionKey, out var clientObject))
                {
                    _accessTokenToDmeClient.Remove(clientObject.Token);
                }
            }
        }

        #endregion

    }
}
