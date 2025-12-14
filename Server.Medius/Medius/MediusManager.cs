using DotNetty.Common.Internal.Logging;
using RT.Common;
using RT.Models;
using Server.Medius.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Medius
{
    public class MediusManager
    {
        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<MediusManager>();

        class QuickLookup
        {
            public Dictionary<int, ClientObject> AccountIdToClient = new Dictionary<int, ClientObject>();
            public Dictionary<string, ClientObject> AccountNameToClient = new Dictionary<string, ClientObject>();
            public Dictionary<string, ClientObject> AccessTokenToClient = new Dictionary<string, ClientObject>();
            public Dictionary<string, ClientObject> SessionKeyToClient = new Dictionary<string, ClientObject>();

            public Dictionary<string, DMEObject> AccessTokenToDmeClient = new Dictionary<string, DMEObject>();
            public Dictionary<string, DMEObject> SessionKeyToDmeClient = new Dictionary<string, DMEObject>();

            public Dictionary<int, Channel> ChannelIdToChannel = new Dictionary<int, Channel>();
            public Dictionary<int, Game> GameIdToGame = new Dictionary<int, Game>();

            public Dictionary<int, Clan> ClanIdToClan = new Dictionary<int, Clan>();
            public Dictionary<string, Clan> ClanNameToClan = new Dictionary<string, Clan>();
        }

        private Dictionary<string, int[]> _appIdGroups = new Dictionary<string, int[]>();
        private Dictionary<int, QuickLookup> _lookupsByAppId = new Dictionary<int, QuickLookup>();

        private ConcurrentQueue<ClientObject> _addQueue = new ConcurrentQueue<ClientObject>();

        #region Clients

        public List<ClientObject> GetClients(int appId)
        {
            var appIdsInGroup = GetAppIdsInGroup(appId);

            return _lookupsByAppId.Where(x => appIdsInGroup.Contains(x.Key)).SelectMany(x => x.Value.AccountIdToClient.Select(x => x.Value)).ToList();
        }

        public ClientObject GetClientByAccountId(int accountId)
        {
            foreach (var lookupByAppId in _lookupsByAppId)
            {
                if (lookupByAppId.Value.AccountIdToClient.TryGetValue(accountId, out var result))
                    return result;
            }

            return null;
        }

        public ClientObject GetClientByAccountName(string accountName, int appId)
        {
            var appIdsInGroup = GetAppIdsInGroup(appId);
            accountName = accountName.ToLower();

            foreach (var appIdInGroup in appIdsInGroup)
            {
                if (_lookupsByAppId.TryGetValue(appIdInGroup, out var quickLookup))
                {
                    if (quickLookup.AccountNameToClient.TryGetValue(accountName, out var result))
                        return result;
                }
            }

            return null;
        }

        public ClientObject GetClientByAccessToken(string accessToken, int appId)
        {
            var appIdsInGroup = GetAppIdsInGroup(appId);

            foreach (var appIdInGroup in appIdsInGroup)
            {
                if (_lookupsByAppId.TryGetValue(appIdInGroup, out var quickLookup))
                {
                    if (quickLookup.AccessTokenToClient.TryGetValue(accessToken, out var result))
                        return result;
                }
            }

            return null;
        }

        public DMEObject GetDmeByAccessToken(string accessToken, int appId)
        {
            var appIdsInGroup = GetAppIdsInGroup(appId);

            foreach (var appIdInGroup in appIdsInGroup)
            {
                if (_lookupsByAppId.TryGetValue(appIdInGroup, out var quickLookup))
                {
                    if (quickLookup.AccessTokenToDmeClient.TryGetValue(accessToken, out var result))
                        return result;
                }
            }

            return null;
        }

        public DMEObject GetDmeBySessionKey(string sessionKey, int appId)
        {
            var appIdsInGroup = GetAppIdsInGroup(appId);

            foreach (var appIdInGroup in appIdsInGroup)
            {
                if (_lookupsByAppId.TryGetValue(appIdInGroup, out var quickLookup))
                {
                    if (quickLookup.SessionKeyToDmeClient.TryGetValue(sessionKey, out var result))
                        return result;
                }
            }

            return null;
        }

        public void AddDmeClient(DMEObject dmeClient)
        {
            if (!dmeClient.IsLoggedIn)
                throw new InvalidOperationException($"Attempting to add DME client {dmeClient} to MediusManager but client has not yet logged in.");

            if (!_lookupsByAppId.TryGetValue(dmeClient.ApplicationId, out var quickLookup))
                _lookupsByAppId.Add(dmeClient.ApplicationId, quickLookup = new QuickLookup());


            try
            {
                quickLookup.AccessTokenToDmeClient.Add(dmeClient.Token, dmeClient);
                quickLookup.SessionKeyToDmeClient.Add(dmeClient.SessionKey, dmeClient);
            }
            catch (Exception e)
            {
                // clean up
                if (dmeClient != null)
                {
                    if (dmeClient.Token != null)
                        quickLookup.AccessTokenToDmeClient.Remove(dmeClient.Token);

                    if (dmeClient.SessionKey != null)
                        quickLookup.SessionKeyToDmeClient.Remove(dmeClient.SessionKey);
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

        public Game GetGameByDmeWorldId(string dmeSessionKey, int dmeWorldId)
        {
            foreach (var lookupByAppId in _lookupsByAppId)
            {
                lock (lookupByAppId.Value.GameIdToGame)
                {
                    var game = lookupByAppId.Value.GameIdToGame.FirstOrDefault(x => x.Value?.DMEServer?.SessionKey == dmeSessionKey && x.Value?.DMEWorldId == dmeWorldId).Value;
                    if (game != null)
                        return game;
                }
            }

            return null;
        }

        public Game GetGameByGameId(int gameId)
        {
            foreach (var lookupByAppId in _lookupsByAppId)
            {
                lock (lookupByAppId.Value.GameIdToGame)
                {
                    if (lookupByAppId.Value.GameIdToGame.TryGetValue(gameId, out var game))
                        return game;
                }
            }

            return null;
        }

        public void AddGame(Game game)
        {
            if (!_lookupsByAppId.TryGetValue(game.ApplicationId, out var quickLookup))
                _lookupsByAppId.Add(game.ApplicationId, quickLookup = new QuickLookup());

            quickLookup.GameIdToGame.Add(game.Id, game);
            _ = Program.Database.CreateGame(game.ToGameDTO());
        }

        public IEnumerable<Game> GetGameList(int appId, int pageIndex, int pageSize, IEnumerable<GameListFilter> filters)
        {
            var appIdsInGroup = GetAppIdsInGroup(appId);

            return _lookupsByAppId.Where(x => appIdsInGroup.Contains(x.Key))
                            .SelectMany(x => x.Value.GameIdToGame.Select(x => x.Value))
                            .Where(x => (x.WorldStatus == MediusWorldStatus.WorldActive || x.WorldStatus == MediusWorldStatus.WorldStaging) &&
                                        (filters.Count() == 0 || filters.Any(y => y.IsMatch(x))))
                            .Skip((pageIndex - 1) * pageSize)
                            .Take(pageSize);
        }

        public void CreateGame(ClientObject client, IMediusRequest request)
        {
            if (!_lookupsByAppId.TryGetValue(client.ApplicationId, out var quickLookup))
                _lookupsByAppId.Add(client.ApplicationId, quickLookup = new QuickLookup());

            var appIdsInGroup = GetAppIdsInGroup(client.ApplicationId);
            string gameName = null;
            if (request is MediusCreateGameRequest r)
                gameName = r.GameName;
            else if (request is MediusCreateGameRequest1 r1)
                gameName = r1.GameName;

            Logger.Info($"CreateGame request {request.MessageID} from {client} app:{client.ApplicationId} name:\"{gameName}\"");

            var existingGames = _lookupsByAppId.Where(x => appIdsInGroup.Contains(client.ApplicationId)).SelectMany(x => x.Value.GameIdToGame.Select(g => g.Value));
            
            // Ensure the name is unique
            // If the host leaves then we unreserve the name
            if (existingGames.Any(x => x.WorldStatus != MediusWorldStatus.WorldClosed && x.WorldStatus != MediusWorldStatus.WorldInactive && x.GameName == gameName && x.Host != null && x.Host.IsConnected))
            {
                Logger.Warn($"CreateGame rejected duplicate name \"{gameName}\" for {client} app:{client.ApplicationId}.");
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
            // If none exist, return error to clist
            var dme = Program.ProxyServer.GetFreeDme(client.ApplicationId, client.Location);
            if (dme == null)
            {
                Logger.Warn($"CreateGame failed for {client} app:{client.ApplicationId} name:\"{gameName}\": no free DME available.");
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

                var createMessageId = new MessageId($"{game.Id}-{client.AccountId}-{request.MessageID}");

                Logger.Info($"Routing create game {game.Id}:{game.GameName} for host {client} to DME {dme?.IP}:{dme?.Port} msg:{createMessageId} app:{client.ApplicationId}");

                // Send create game request to dme server
                dme.Queue(new MediusServerCreateGameWithAttributesRequest()
                {
                    MessageID = createMessageId,
                    MediusWorldUID = (uint)game.Id,
                    Attributes = game.Attributes,
                    ApplicationID = client.ApplicationId,
                    MaxClients = game.MaxPlayers
                });

                // Log if the DME does not respond in a timely manner
                _ = LogCreateGamePendingAsync(game, createMessageId);
            }
            catch (Exception e)
            {
                // 
                Logger.Error($"Exception creating game \"{gameName}\" for {client} app:{client.ApplicationId}", e);

                // Failure adding game for some reason
                client.Queue(new MediusCreateGameResponse()
                {
                    MessageID = request.MessageID,
                    MediusWorldID = -1,
                    StatusCode = MediusCallbackStatus.MediusFail
                });
            }
        }

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

        private async Task LogCreateGamePendingAsync(Game game, MessageId messageId)
        {
            await Task.Delay(TimeSpan.FromSeconds(5));

            if (game != null && game.DMEWorldId < 0 && game.WorldStatus == MediusWorldStatus.WorldPendingCreation)
            {
                Logger.Warn($"Still waiting on DME create response for game {game.Id}:{game.GameName} msg:{messageId} host:{game.Host} app:{game.ApplicationId} created:{game.UtcTimeCreated:O}");
            }
        }

        #endregion

        #region Channels

        public Channel GetChannelByChannelId(int channelId, int appId)
        {
            var appIdsInGroup = GetAppIdsInGroup(appId);

            foreach (var appIdInGroup in appIdsInGroup)
            {
                if (_lookupsByAppId.TryGetValue(appIdInGroup, out var quickLookup))
                {
                    lock (quickLookup.ChannelIdToChannel)
                    {
                        if (quickLookup.ChannelIdToChannel.TryGetValue(channelId, out var result))
                            return result;
                    }
                }
            }

            return null;
        }

        public Channel GetChannelByChannelName(string channelName, int appId)
        {
            var appIdsInGroup = GetAppIdsInGroup(appId);

            foreach (var appIdInGroup in appIdsInGroup)
            {
                if (_lookupsByAppId.TryGetValue(appIdInGroup, out var quickLookup))
                {
                    lock (quickLookup.ChannelIdToChannel)
                    {
                        return quickLookup.ChannelIdToChannel.FirstOrDefault(x => x.Value.Name == channelName && appIdsInGroup.Contains(x.Value.ApplicationId)).Value;
                    }
                }
            }

            return null;
        }

        public uint GetChannelCount(ChannelType type, int appId)
        {
            var appIdsInGroup = GetAppIdsInGroup(appId);
            uint count = 0;

            foreach (var appIdInGroup in appIdsInGroup)
            {
                if (_lookupsByAppId.TryGetValue(appIdInGroup, out var quickLookup))
                {
                    lock (quickLookup.ChannelIdToChannel)
                    {
                        count += (uint)quickLookup.ChannelIdToChannel.Count(x => x.Value.Type == type);
                    }
                }
            }

            return count;
        }

        public Channel GetOrCreateDefaultLobbyChannel(int appId)
        {
            Channel channel = null;
            var appIdsInGroup = GetAppIdsInGroup(appId);

            foreach (var appIdInGroup in appIdsInGroup)
            {
                if (_lookupsByAppId.TryGetValue(appIdInGroup, out var quickLookup))
                {
                    lock (quickLookup.ChannelIdToChannel)
                    {
                        channel = quickLookup.ChannelIdToChannel.FirstOrDefault(x => x.Value.Type == ChannelType.Lobby).Value;
                        if (channel != null)
                            return channel;
                    }
                }
            }

            // create default
            channel = new Channel()
            {
                ApplicationId = appId,
                Name = "Default",
                Type = ChannelType.Lobby
            };
            AddChannel(channel);

            return channel;
        }

        public void AddChannel(Channel channel)
        {
            if (!_lookupsByAppId.TryGetValue(channel.ApplicationId, out var quickLookup))
                _lookupsByAppId.Add(channel.ApplicationId, quickLookup = new QuickLookup());

            lock (quickLookup.ChannelIdToChannel)
            {
                quickLookup.ChannelIdToChannel.Add(channel.Id, channel);
            }
        }

        public IEnumerable<Channel> GetChannelList(int appId, int pageIndex, int pageSize, ChannelType type)
        {
            var appIdsInGroup = GetAppIdsInGroup(appId);

            return _lookupsByAppId
                .Where(x => appIdsInGroup.Contains(x.Key))
                .SelectMany(x => x.Value.ChannelIdToChannel.Select(x => x.Value))
                .Where(x => x.Type == type)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize);
        }

        #endregion

        #region Clans

        //public Clan GetClanByAccountId(int clanId, int appId)
        //{
        //    if (_clanIdToClan.TryGetValue(clanId, out var result))
        //        return result;

        //    return null;
        //}

        //public Clan GetClanByAccountName(string clanName, int appId)
        //{
        //    clanName = clanName.ToLower();
        //    if (_clanNameToClan.TryGetValue(clanName, out var result))
        //        return result;

        //    return null;
        //}

        //public void AddClan(Clan clan)
        //{
        //    if (!_lookupsByAppId.TryGetValue(clan.ApplicationId, out var quickLookup))
        //        _lookupsByAppId.Add(dmeClient.ApplicationId, quickLookup = new QuickLookup());

        //    _clanNameToClan.Add(clan.Name.ToLower(), clan);
        //    _clanIdToClan.Add(clan.Id, clan);
        //}

        #endregion

        #region Tick

        public async Task Tick()
        {
            await TickClients();

            await TickChannels ();

            await TickGames();
        }

        private async Task TickChannels()
        {
            Queue<(QuickLookup, int)> channelsToRemove = new Queue<(QuickLookup, int)>();

            // Tick channels
            foreach (var quickLookup in _lookupsByAppId)
            {
                foreach (var channelKeyPair in quickLookup.Value.ChannelIdToChannel)
                {
                    if (channelKeyPair.Value.ReadyToDestroy)
                    {
                        Logger.Info($"Destroying Channel {channelKeyPair.Value}");
                        channelsToRemove.Enqueue((quickLookup.Value, channelKeyPair.Key));
                    }
                    else
                    {
                        await channelKeyPair.Value.Tick();
                    }
                }
            }

            // Remove channels
            while (channelsToRemove.TryDequeue(out var lookupAndChannelId))
                lookupAndChannelId.Item1.ChannelIdToChannel.Remove(lookupAndChannelId.Item2);
        }

        private async Task TickGames()
        {
            Queue<(QuickLookup, int)> gamesToRemove = new Queue<(QuickLookup, int)>();

            // Tick games
            foreach (var quickLookup in _lookupsByAppId)
            {
                foreach (var gameKeyPair in quickLookup.Value.GameIdToGame)
                {
                    if (gameKeyPair.Value.ReadyToDestroy)
                    {
                        Logger.Info($"Destroying Game {gameKeyPair.Value}");
                        await gameKeyPair.Value.EndGame();
                        gamesToRemove.Enqueue((quickLookup.Value, gameKeyPair.Key));
                    }
                    else
                    {
                        await gameKeyPair.Value.Tick();
                    }
                }
            }

            // Remove games
            while (gamesToRemove.TryDequeue(out var lookupAndGameId))
                lookupAndGameId.Item1.GameIdToGame.Remove(lookupAndGameId.Item2);
        }

        private async Task TickClients()
        {
            Queue<(int, string)> clientsToRemove = new Queue<(int, string)>();

            while (_addQueue.TryDequeue(out var newClient))
            {
                if (!_lookupsByAppId.TryGetValue(newClient.ApplicationId, out var quickLookup))
                    _lookupsByAppId.Add(newClient.ApplicationId, quickLookup = new QuickLookup());

                try
                {
                    quickLookup.AccountIdToClient.Add(newClient.AccountId, newClient);
                    quickLookup.AccountNameToClient.Add(newClient.AccountName.ToLower(), newClient);
                    quickLookup.AccessTokenToClient.Add(newClient.Token, newClient);
                    quickLookup.SessionKeyToClient.Add(newClient.SessionKey, newClient);
                }
                catch (Exception e)
                {
                    // clean up
                    if (newClient != null)
                    {
                        quickLookup.AccountIdToClient.Remove(newClient.AccountId);

                        if (newClient.AccountName != null)
                            quickLookup.AccountNameToClient.Remove(newClient.AccountName.ToLower());

                        if (newClient.Token != null)
                            quickLookup.AccessTokenToClient.Remove(newClient.Token);

                        if (newClient.SessionKey != null)
                            quickLookup.SessionKeyToClient.Remove(newClient.SessionKey);
                    }

                    Logger.Error(e);
                    //throw e;
                }
            }

            foreach (var quickLookup in _lookupsByAppId)
            {
                foreach (var clientKeyPair in quickLookup.Value.SessionKeyToClient)
                {
                    if (!clientKeyPair.Value.IsConnected)
                    {
                        Logger.Info($"Destroying Client {clientKeyPair.Value}");

                        // Logout and end session
                        await clientKeyPair.Value.Logout();
                        clientKeyPair.Value.EndSession();

                        clientsToRemove.Enqueue((quickLookup.Key, clientKeyPair.Key));
                    }
                    else if (clientKeyPair.Value.Timedout)
                    {
                        clientKeyPair.Value.ForceDisconnect();
                    }
                }
            }

            // Remove
            while (clientsToRemove.TryDequeue(out var appIdAndSessionKey))
            {
                if (_lookupsByAppId.TryGetValue(appIdAndSessionKey.Item1, out var quickLookup))
                {
                    if (quickLookup.SessionKeyToClient.Remove(appIdAndSessionKey.Item2, out var clientObject))
                    {
                        quickLookup.AccountIdToClient.Remove(clientObject.AccountId);
                        quickLookup.AccessTokenToClient.Remove(clientObject.Token);
                        quickLookup.AccountNameToClient.Remove(clientObject.AccountName.ToLower());
                    }
                }
            }
        }

        private void TickDme()
        {
            Queue<(int, string)> dmeToRemove = new Queue<(int, string)>();

            foreach (var quickLookup in _lookupsByAppId)
            {
                foreach (var dmeKeyPair in quickLookup.Value.SessionKeyToDmeClient)
                {
                    if (!dmeKeyPair.Value.IsConnected)
                    {
                        Logger.Info($"Destroying DME Client {dmeKeyPair.Value}");

                        // Logout and end session
                        dmeKeyPair.Value?.Logout();
                        dmeKeyPair.Value?.EndSession();

                        dmeToRemove.Enqueue((quickLookup.Key, dmeKeyPair.Key));
                    }
                }
            }

            // Remove
            while (dmeToRemove.TryDequeue(out var appIdAndSessionKey))
            {
                if (_lookupsByAppId.TryGetValue(appIdAndSessionKey.Item1, out var quickLookup))
                {
                    if (quickLookup.SessionKeyToDmeClient.Remove(appIdAndSessionKey.Item2, out var clientObject))
                    {
                        quickLookup.AccessTokenToDmeClient.Remove(clientObject.Token);
                    }
                }
            }
        }

        #endregion

        #region App Ids

        public async Task OnDatabaseAuthenticated()
        {
            // get supported app ids
            var appids = await Program.Database.GetAppIds();

            // build dictionary of app ids from response
            _appIdGroups = appids.ToDictionary(x => x.Name, x => x.AppIds.ToArray());
        }

        public bool IsAppIdSupported(int appId)
        {
            return _appIdGroups.Any(x => x.Value.Contains(appId));
        }

        public int[] GetAppIdsInGroup(int appId)
        {
            return _appIdGroups.FirstOrDefault(x => x.Value.Contains(appId)).Value ?? new int[0];
        }

        #endregion

    }
}
