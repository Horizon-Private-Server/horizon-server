using DotNetty.Common.Internal.Logging;
using Newtonsoft.Json;
using RT.Common;
using Server.Database.Config;
using Server.Database.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Server.Database
{
    public class DbController
    {
        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<DbController>();

        private DbSettings _settings = new DbSettings();

        private int _simulatedAccountIdCounter = 0;
        private string _dbAccessToken = null;
        private List<AccountDTO> _simulatedAccounts = new List<AccountDTO>();

        #region Cache

        private class GetDbCache
        {
            static ConcurrentDictionary<string, GetDbCache> _getCache = new ConcurrentDictionary<string, GetDbCache>();

            public DateTime LastUpdate;

            public int Lifetime = 0;

            public object Value;

            public bool IsValid => Value != null && (DateTime.UtcNow - LastUpdate).TotalSeconds < Lifetime;


            public GetDbCache(int lifetime)
            {
                this.Lifetime = lifetime;
            }

            public static bool TryGetCache<T>(string route, out T value)
            {
                if (_getCache.TryGetValue(route, out var cache) && cache != null && cache.IsValid && cache.Value is T cacheAsT)
                {
                    value = cacheAsT;
                    return true;
                }

                value = default(T);
                return false;
            }

            public static void UpdateCache(string route, object value, int lifetime)
            {
                if (_getCache.TryGetValue(route, out var cache))
                {
                    cache.Value = value;
                    cache.LastUpdate = DateTime.UtcNow;
                }
                else
                {
                    _getCache.TryAdd(route, new GetDbCache(lifetime)
                    {
                        LastUpdate = DateTime.UtcNow,
                        Value = value
                    });
                }
            }
        }

        #endregion


        public DbController(string configPath)
        {
            // Load db settings
            if (File.Exists(configPath))
            {
                // Populate existing object
                try { JsonConvert.PopulateObject(File.ReadAllText(configPath), _settings); }
                catch (Exception e) { Logger.Error(e); }
            }
            else
            {
                // Save default db config
                File.WriteAllText(configPath, JsonConvert.SerializeObject(_settings, Formatting.Indented));
            }
        }

        /// <summary>
        /// Authenticate with middleware.
        /// </summary>
        public async Task<bool> Authenticate()
        {
            // Succeed in simulated mode
            if (_settings.SimulatedMode)
                return true;

            //
            var response = await Authenticate(_settings.DatabaseUsername, _settings.DatabasePassword);

            // Validate
            if (response == null || response.Roles == null || !response.Roles.Contains("database"))
                return false;

            // 
            _dbAccessToken = response.Token;

            // 
            return !string.IsNullOrEmpty(_dbAccessToken);
        }


        #region Account

        /// <summary>
        /// Get account by name.
        /// </summary>
        /// <param name="name">Case insensitive name of player.</param>
        /// <returns>Returns account.</returns>
        public async Task<AccountDTO> GetAccountByName(string name)
        {
            AccountDTO result = null;

            try
            {
                if (_settings.SimulatedMode)
                {
                    result = _simulatedAccounts.FirstOrDefault(x => x.AccountName.ToLower() == name.ToLower());
                }
                else
                {
                    result = await GetDbAsync<AccountDTO>($"Account/searchAccountByName?AccountName={name}");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// Get account by id.
        /// </summary>
        /// <param name="id">Id of player.</param>
        /// <returns>Returns account.</returns>
        public async Task<AccountDTO> GetAccountById(int id)
        {
            AccountDTO result = null;

            try
            {
                if (_settings.SimulatedMode)
                {
                    result = _simulatedAccounts.FirstOrDefault(x => x.AccountId == id);
                }
                else
                {
                    result = await GetDbAsync<AccountDTO>($"Account/getAccount?AccountId={id}");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// Creates an account.
        /// </summary>
        /// <param name="createAccount">Account creation parameters.</param>
        /// <returns>Returns created account.</returns>
        public async Task<AccountDTO> CreateAccount(CreateAccountDTO createAccount)
        {
            AccountDTO result = null;

            try
            {
                if (_settings.SimulatedMode)
                {
                    var checkExisting = await GetAccountByName(createAccount.AccountName);
                    if (checkExisting == null)
                    {
                        _simulatedAccounts.Add(result = new AccountDTO()
                        {
                            AccountId = _simulatedAccountIdCounter++,
                            AccountName = createAccount.AccountName,
                            AccountPassword = createAccount.AccountPassword,
                            AccountWideStats = new int[Constants.LADDERSTATSWIDE_MAXLEN],
                            MediusStats = createAccount.MediusStats,
                            Friends = new AccountRelationDTO[0],
                            Ignored = new AccountRelationDTO[0],
                            IsBanned = false
                        });
                    }
                    else
                    {
                        throw new Exception($"Account creation failed account name already exists!");
                    }
                }
                else
                {
                    var response = await PostDbAsync($"Account/createAccount", JsonConvert.SerializeObject(createAccount));

                    // Deserialize on success
                    if (response.IsSuccessStatusCode)
                        result = JsonConvert.DeserializeObject<AccountDTO>(await response.Content.ReadAsStringAsync());
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// Delete account by name.
        /// </summary>
        /// <param name="accountName">Case insensitive name of account.</param>
        /// <returns>Success or failure.</returns>
        public async Task<bool> DeleteAccount(string accountName)
        {
            bool result = false;

            try
            {
                if (_settings.SimulatedMode)
                {
                    result = _simulatedAccounts.RemoveAll(x => x.AccountName.ToLower() == accountName.ToLower()) > 0;
                }
                else
                {
                    result = (await GetDbAsync($"Account/deleteAccount?AccountName={accountName}")).IsSuccessStatusCode;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// Posts the account sign in date to the database.
        /// </summary>
        /// <param name="accountId">Id to post login date to.</param>
        /// <param name="time">Time logged in.</param>
        /// <returns>Success or failure.</returns>
        public async Task<bool> PostAccountSignInDate(int accountId, DateTime time)
        {
            bool result = false;

            try
            {
                if (_settings.SimulatedMode)
                {
                    result = true;
                }
                else
                {
                    result = (await PostDbAsync($"Account/postAccountSignInDate?AccountId={accountId}", $"\"{time.ToUniversalTime()}\"")).IsSuccessStatusCode;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// Get account status by account id.
        /// </summary>
        /// <param name="accountId">Unique id of account.</param>
        /// <returns>Account status.</returns>
        public async Task<AccountStatusDTO> GetAccountStatus(int accountId)
        {
            AccountStatusDTO result = null;

            try
            {
                if (_settings.SimulatedMode)
                {
                    result = null;
                }
                else
                {
                    result = await GetDbAsync<AccountStatusDTO>($"Account/getAccountStatus?AccountId={accountId}");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// Posts the current account status.
        /// </summary>
        /// <param name="status">Account status.</param>
        /// <returns>Success or failure.</returns>
        public async Task<bool> PostAccountStatus(AccountStatusDTO status)
        {
            bool result = false;

            try
            {
                if (_settings.SimulatedMode)
                {
                    result = false;
                }
                else
                {
                    result = (await PostDbAsync($"Account/postAccountStatusUpdates", JsonConvert.SerializeObject(status))).IsSuccessStatusCode;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        public async Task<bool> ClearAccountStatuses()
        {
            bool result = false;

            try
            {
                if (_settings.SimulatedMode)
                {
                    result = true;
                }
                else
                {
                    result = (await PostDbAsync($"Account/clearAccountStatuses", null)).IsSuccessStatusCode;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// Get account metadata by account id.
        /// </summary>
        /// <param name="accountId">Unique id of account.</param>
        /// <returns>Account metadata.</returns>
        public async Task<string> GetAccountMetadata(int accountId)
        {
            string result = null;

            try
            {
                if (_settings.SimulatedMode)
                {
                    result = null;
                }
                else
                {
                    result = await GetDbAsync<string>($"Account/getAccountMetadata?accountId={accountId}");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// Posts the given metadata to the given account.
        /// </summary>
        /// <param name="accountId">Id of account.</param>
        /// <param name="metadata">Metadata to post.</param>
        /// <returns>True on success.</returns>
        public async Task<bool> PostAccountMetadata(int accountId, string metadata)
        {
            bool result = false;

            try
            {
                if (_settings.SimulatedMode)
                {
                    result = false;
                }
                else
                {
                    result = (await PostDbAsync($"Account/postAccountMetadata?accountId={accountId}", JsonConvert.SerializeObject(metadata))).IsSuccessStatusCode;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// Posts ip to account.
        /// </summary>
        public async Task<bool> PostAccountIp(int accountId, string ip)
        {
            bool result = false;

            try
            {
                if (_settings.SimulatedMode)
                {
                    result = true;
                }
                else
                {
                    result = (await PostDbAsync($"Account/postAccountIp?AccountId={accountId}", $"\"{ip}\"")).IsSuccessStatusCode;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// Gets the total number of active players by app id.
        /// </summary>
        /// <param name="appId">App Id to filter total active accounts by.</param>
        /// <returns>Number of active accounts or null.</returns>
        public async Task<int?> GetActiveAccountCountByAppId(int appId)
        {
            int? result = null;

            try
            {
                if (_settings.SimulatedMode)
                {
                    result = _simulatedAccounts.Count;
                }
                else
                {
                    var response = await GetDbAsync($"Account/getActiveAccountCountByAppId?AppId={appId}");

                    // Deserialize on success
                    if (response.IsSuccessStatusCode && int.TryParse(await response.Content.ReadAsStringAsync(), out int r))
                        result = r;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// Gets whether or not the ip is banned.
        /// </summary>
        public async Task<bool> GetIsIpBanned(string ip)
        {
            bool result = false;

            try
            {
                if (_settings.SimulatedMode)
                {
                    result = false;
                }
                else
                {
                    result = await PostDbAsync<bool>($"Account/getIpIsBanned", $"\"{ip}\"");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// Gets whether the mac address is banned.
        /// </summary>
        /// <param name="mac">MAC Address as a Base64 string</param>
        public async Task<bool> GetIsMacBanned(string mac)
        {
            bool result = false;

            try
            {
                if (_settings.SimulatedMode)
                {
                    result = false;
                }
                else
                {
                    result = await PostDbAsync<bool>($"Account/getMacIsBanned", $"\"{mac}\"");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// Posts the given machine id to the database account with the given account id.
        /// </summary>
        /// <param name="accountId">Account id.</param>
        /// <param name="machineId">Machine id.</param>
        public async Task<bool> PostMachineId(int accountId, string machineId)
        {
            bool result = false;

            try
            {
                if (_settings.SimulatedMode)
                {
                    result = false;
                }
                else
                {
                    result = (await PostDbAsync($"Account/postMachineId?AccountId={accountId}", $"\"{machineId}\"")).IsSuccessStatusCode;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        #endregion

        #region Buddy / Ignored

        /// <summary>
        /// Add buddy to buddy list.
        /// </summary>
        /// <param name="addBuddy">Add buddy parameters.</param>
        /// <returns>Success or failure.</returns>
        public async Task<bool> AddBuddy(BuddyDTO addBuddy)
        {
            bool result = false;

            try
            {
                if (_settings.SimulatedMode)
                {
                    var account = await GetAccountById(addBuddy.AccountId);
                    var buddyAccount = await GetAccountById(addBuddy.BuddyAccountId);
                    if (account != null && buddyAccount != null)
                    {
                        var friends = account.Friends;
                        Array.Resize(ref friends, account.Friends.Length + 1);
                        friends[friends.Length - 1] = new AccountRelationDTO()
                        {
                            AccountId = buddyAccount.AccountId,
                            AccountName = buddyAccount.AccountName
                        };
                        account.Friends = friends;
                        result = true;
                    }
                }
                else
                {
                    result = (await PostDbAsync($"Buddy/addBuddy", JsonConvert.SerializeObject(addBuddy))).IsSuccessStatusCode;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// Remove buddy from buddy list.
        /// </summary>
        /// <param name="removeBuddy">Remove buddy parameters.</param>
        /// <returns>Success or failure.</returns>
        public async Task<bool> RemoveBuddy(BuddyDTO removeBuddy)
        {
            bool result = false;

            try
            {
                if (_settings.SimulatedMode)
                {
                    var account = await GetAccountById(removeBuddy.AccountId);
                    var buddyAccount = await GetAccountById(removeBuddy.BuddyAccountId);
                    if (account != null && buddyAccount != null)
                    {
                        var newFriends = new List<AccountRelationDTO>();
                        foreach (var friend in account.Friends)
                        {
                            if (friend.AccountId == buddyAccount.AccountId)
                                continue;

                            newFriends.Add(friend);
                        }
                        account.Friends = newFriends.ToArray();
                        result = true;
                    }
                }
                else
                {
                    result = (await PostDbAsync($"Buddy/removeBuddy", JsonConvert.SerializeObject(removeBuddy))).IsSuccessStatusCode;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// Add player to ignored list.
        /// </summary>
        /// <param name="addIgnored">Add ignored parameters.</param>
        /// <returns>Success or failure.</returns>
        public async Task<bool> AddIgnored(IgnoredDTO addIgnored)
        {
            bool result = false;

            try
            {
                if (_settings.SimulatedMode)
                {
                    var account = await GetAccountById(addIgnored.AccountId);
                    var ignoreAccount = await GetAccountById(addIgnored.IgnoredAccountId);
                    if (account != null && ignoreAccount != null)
                    {
                        var ignored = account.Ignored;
                        Array.Resize(ref ignored, account.Ignored.Length + 1);
                        ignored[ignored.Length - 1] = new AccountRelationDTO()
                        {
                            AccountId = ignoreAccount.AccountId,
                            AccountName = ignoreAccount.AccountName
                        };
                        account.Ignored = ignored;
                        result = true;
                    }
                }
                else
                {
                    result = (await PostDbAsync($"Buddy/addIgnored", JsonConvert.SerializeObject(addIgnored))).IsSuccessStatusCode;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// Remove player from ignored list.
        /// </summary>
        /// <param name="removeIgnored">Remove ignored parameters.</param>
        /// <returns>Success or failure.</returns>
        public async Task<bool> RemoveIgnored(IgnoredDTO removeIgnored)
        {
            bool result = false;

            try
            {
                if (_settings.SimulatedMode)
                {
                    var account = await GetAccountById(removeIgnored.AccountId);
                    var ignoreAccount = await GetAccountById(removeIgnored.IgnoredAccountId);
                    if (account != null && ignoreAccount != null)
                    {
                        var newIgnored = new List<AccountRelationDTO>();
                        foreach (var ignored in account.Ignored)
                        {
                            if (ignored.AccountId == ignoreAccount.AccountId)
                                continue;

                            newIgnored.Add(ignored);
                        }
                        account.Ignored = newIgnored.ToArray();
                        result = true;
                    }
                }
                else
                {
                    result = (await PostDbAsync($"Buddy/removeIgnored", JsonConvert.SerializeObject(removeIgnored))).IsSuccessStatusCode;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }


        #endregion

        #region Stats

        /// <summary>
        /// Get player ranking in a given leaderboard.
        /// </summary>
        /// <param name="accountId">Account id of player.</param>
        /// <param name="statId">Index of stat. Starts at 1.</param>
        /// <returns>Leaderboard result for player.</returns>
        public async Task<LeaderboardDTO> GetPlayerLeaderboardIndex(int accountId, int statId)
        {
            LeaderboardDTO result = null;

            try
            {
                if (_settings.SimulatedMode)
                {
                    var account = await GetAccountById(accountId);
                    if (account == null)
                        return null;

                    return new LeaderboardDTO()
                    {
                        AccountId = accountId,
                        AccountName = account.AccountName,
                        Index = 1,
                        MediusStats = account.MediusStats,
                        StatValue = account.AccountWideStats[statId],
                        TotalRankedAccounts = 1
                    };
                }
                else
                {
                    result = await GetDbAsync<LeaderboardDTO>($"Stats/getPlayerLeaderboardIndex?AccountId={accountId}&StatId={statId}");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }


        /// <summary>
        /// Get leaderboard for a given stat by page and size.
        /// </summary>
        /// <param name="statId">Stat id. Starts at 1.</param>
        /// <param name="startIndex">Position to start gathering results from. Starts at 0.</param>
        /// <param name="size">Max number of items to retrieve.</param>
        /// <returns>Collection of leaderboard results for each player in page.</returns>
        public async Task<LeaderboardDTO[]> GetLeaderboard(int statId, int startIndex, int size)
        {
            LeaderboardDTO[] result = null;

            try
            {
                if (_settings.SimulatedMode)
                {
                    var ordered = _simulatedAccounts.OrderBy(x => x.AccountWideStats[statId]).Skip(startIndex).Take(size).ToList();
                    result = ordered.Select(x => new LeaderboardDTO()
                    {
                        AccountId = x.AccountId,
                        AccountName = x.AccountName,
                        MediusStats = x.MediusStats,
                        StatValue = x.AccountWideStats[statId],
                        TotalRankedAccounts = 0,
                        Index = startIndex + ordered.IndexOf(x)
                    }).ToArray();
                }
                else
                {
                    result = await GetDbAsync<LeaderboardDTO[]>($"Stats/getLeaderboard?StatId={statId}&StartIndex={startIndex}&Size={size}");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// Posts ladder stats to account id.
        /// </summary>
        /// <param name="statPost">Model containing account id and ladder stats collection.</param>
        /// <returns>Success or failure.</returns>
        public async Task<bool> PostLadderStats(StatPostDTO statPost)
        {
            bool result = false;

            try
            {
                if (_settings.SimulatedMode)
                {
                    var account = await GetAccountById(statPost.AccountId);
                    if (account == null)
                        return false;

                    account.AccountWideStats = statPost.Stats;
                    result = true;
                }
                else
                {
                    result = (await PostDbAsync($"Stats/postStats", JsonConvert.SerializeObject(statPost))).IsSuccessStatusCode;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// Post medius stats to account.
        /// </summary>
        /// <param name="accountId">Account id to post stats to.</param>
        /// <param name="stats">Stats to post encoded as a Base64 string.</param>
        /// <returns>Success or failure.</returns>
        public async Task<bool> PostMediusStats(int accountId, string stats)
        {
            bool result = false;

            try
            {
                if (_settings.SimulatedMode)
                {
                    var account = await GetAccountById(accountId);
                    if (account == null)
                        return false;

                    account.MediusStats = stats;
                    result = true;
                }
                else
                {
                    result = (await PostDbAsync($"Account/postMediusStats?AccountId={accountId}", $"\"{stats}\""))?.IsSuccessStatusCode ?? false;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        #endregion

        #region Announcements / Policy

        /// <summary>
        /// Gets the latest announcement.
        /// </summary>
        public async Task<DimAnnouncements> GetLatestAnnouncement()
        {
            DimAnnouncements result = null;

            try
            {
                if (_settings.SimulatedMode)
                {
                    return new DimAnnouncements()
                    {
                        AnnouncementTitle = "Title",
                        AnnouncementBody = "Body",
                        CreateDt = DateTime.UtcNow
                    };
                }
                else
                {
                    result = await GetDbAsync<DimAnnouncements>($"api/Keys/getAnnouncements?fromDt={DateTime.UtcNow}");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// Gets the latest announcements.
        /// </summary>
        public async Task<DimAnnouncements[]> GetLatestAnnouncements(int size = 10)
        {
            DimAnnouncements[] result = null;

            try
            {
                if (_settings.SimulatedMode)
                {
                    return new DimAnnouncements[]
                    {
                        new DimAnnouncements()
                        {
                            AnnouncementTitle = "Title",
                            AnnouncementBody = "Body",
                            CreateDt = DateTime.UtcNow
                        }
                    };
                }
                else
                {
                    result = await GetDbAsync<DimAnnouncements[]>($"api/Keys/getAnnouncementsList?Dt={DateTime.UtcNow}&TakeSize={size}");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// Gets the usage policy.
        /// </summary>
        public async Task<DimEula> GetUsagePolicy()
        {
            DimEula result = null;

            try
            {
                if (_settings.SimulatedMode)
                {
                    return new DimEula()
                    {
                        EulaTitle = "Title",
                        EulaBody = "Body"
                    };
                }
                else
                {
                    result = await GetDbAsync<DimEula>($"api/Keys/getEULA?fromDt={DateTime.UtcNow}");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// [TODO]
        /// Gets the privacy policy.
        /// </summary>
        public async Task<DimEula> GetPrivacyPolicy()
        {
            DimEula result = null;

            try
            {
                if (_settings.SimulatedMode)
                {
                    return new DimEula()
                    {
                        EulaTitle = "Title",
                        EulaBody = "Body"
                    };
                }
                else
                {
                    result = await GetDbAsync<DimEula>($"api/getEULA?fromDt={DateTime.UtcNow.AddDays(-1)}");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        #endregion

        #region Game

        /// <summary>
        /// 
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        public async Task<bool> CreateGame(GameDTO game)
        {
            bool result = false;
            try
            {
                if (_settings.SimulatedMode)
                {
                    result = true;
                }
                else
                {
                    result = (await PostDbAsync($"api/Game/create", JsonConvert.SerializeObject(game))).IsSuccessStatusCode;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        public async Task<bool> UpdateGame(GameDTO game)
        {
            bool result = false;
            try
            {
                if (_settings.SimulatedMode)
                {
                    result = true;
                }
                else
                {
                    result = (await PutDbAsync($"api/Game/update/{game.GameId}", JsonConvert.SerializeObject(game))).IsSuccessStatusCode;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gameId"></param>
        /// <param name="metadata"></param>
        /// <returns></returns>
        public async Task<bool> UpdateGameMetadata(int gameId, string metadata)
        {
            bool result = false;
            try
            {
                if (_settings.SimulatedMode)
                {
                    result = true;
                }
                else
                {
                    result = (await PutDbAsync($"api/Game/updateMetaData/{gameId}", JsonConvert.SerializeObject(metadata))).IsSuccessStatusCode;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// Delete game by game id.
        /// </summary>
        /// <param name="gameId">Game id.</param>
        /// <returns>Success or failure.</returns>
        public async Task<bool> DeleteGame(int gameId)
        {
            bool result = false;

            try
            {
                if (_settings.SimulatedMode)
                {
                    result = true;
                }
                else
                {
                    result = (await DeleteDbAsync($"api/Game/delete/{gameId}")).IsSuccessStatusCode;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// Clear the active games table.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> ClearActiveGames()
        {
            bool result = false;

            try
            {
                if (_settings.SimulatedMode)
                {
                    result = true;
                }
                else
                {
                    result = (await DeleteDbAsync($"api/Game/clear")).IsSuccessStatusCode;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        #endregion

        #region Key

        public async Task<ServerFlagsDTO> GetServerFlags()
        {
            ServerFlagsDTO result = null;

            try
            {
                if (_settings.SimulatedMode)
                {
                    return new ServerFlagsDTO()
                    {
                        MaintenanceMode = new MaintenanceDTO()
                        {
                            IsActive = false,
                            FromDt = DateTime.UtcNow - TimeSpan.FromSeconds(10),
                            ToDt = DateTime.UtcNow + TimeSpan.FromSeconds(1)
                        }
                    };
                }
                else
                {
                    result = await GetDbAsync<ServerFlagsDTO>($"api/Keys/getServerFlags");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        #endregion

        #region Auth

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private async Task<AuthenticationResponse> Authenticate(string username, string password)
        {
            AuthenticationResponse result = null;

            try
            {
                result = await PostDbAsync<AuthenticationResponse>($"Account/authenticate", new AuthenticationRequest()
                {
                    AccountName = username,
                    Password = password
                });
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        #endregion

        #region Http

        private async Task<HttpResponseMessage> DeleteDbAsync(string route)
        {
            // 
            HttpResponseMessage result = null;

            using (var handler = new HttpClientHandler())
            {
                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                handler.ServerCertificateCustomValidationCallback =
                    (httpRequestMessage, cert, cetChain, policyErrors) =>
                    {
                        return true;
                    };

                using (var client = new HttpClient(handler))
                {
                    if (!string.IsNullOrEmpty(_dbAccessToken))
                        client.DefaultRequestHeaders.Add("Authorization", _dbAccessToken);
                    client.DefaultRequestHeaders.Add("Accept", "application/json");

                    try
                    {
                        result = await client.DeleteAsync($"{_settings.DatabaseUrl}/{route}");
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                        result = null;
                    }
                }
            }

            return result;
        }

        private async Task<HttpResponseMessage> GetDbAsync(string route)
        {
            // Try to get a cached result first
            if (GetDbCache.TryGetCache(route, out HttpResponseMessage value))
                return value;

            // 
            HttpResponseMessage result = null;

            using (var handler = new HttpClientHandler())
            {
                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                handler.ServerCertificateCustomValidationCallback =
                    (httpRequestMessage, cert, cetChain, policyErrors) =>
                    {
                        return true;
                    };

                using (var client = new HttpClient(handler))
                {
                    if (!string.IsNullOrEmpty(_dbAccessToken))
                        client.DefaultRequestHeaders.Add("Authorization", _dbAccessToken);
                    client.DefaultRequestHeaders.Add("Accept", "application/json");

                    try
                    {
                        result = await client.GetAsync($"{_settings.DatabaseUrl}/{route}");

                        // Update cached value
                        GetDbCache.UpdateCache(route, result, _settings.CacheDuration);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                        result = null;
                    }
                }
            }

            return result;
        }

        private async Task<T> GetDbAsync<T>(string route)
        {
            // Try to get a cached result first
            if (GetDbCache.TryGetCache(route, out T value))
                return value;

            // 
            T result = default(T);

            using (var handler = new HttpClientHandler())
            {
                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                handler.ServerCertificateCustomValidationCallback =
                    (httpRequestMessage, cert, cetChain, policyErrors) =>
                    {
                        return true;
                    };

                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Add("Accept", "application/json");

                    try
                    {
                        if (!string.IsNullOrEmpty(_dbAccessToken))
                            client.DefaultRequestHeaders.Add("Authorization", _dbAccessToken);
                        var response = await client.GetAsync($"{_settings.DatabaseUrl}/{route}");

                        // Deserialize on success
                        if (response.IsSuccessStatusCode)
                            result = JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());

                        // Update cached value
                        GetDbCache.UpdateCache(route, result, _settings.CacheDuration);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                        result = default(T);
                    }
                }
            }

            return result;
        }

        private async Task<HttpResponseMessage> PostDbAsync(string route, string body)
        {
            // 
            HttpResponseMessage result = null;

            using (var handler = new HttpClientHandler())
            {
                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                handler.ServerCertificateCustomValidationCallback =
                    (httpRequestMessage, cert, cetChain, policyErrors) =>
                    {
                        return true;
                    };

                using (var client = new HttpClient(handler))
                {
                    if (!string.IsNullOrEmpty(_dbAccessToken))
                        client.DefaultRequestHeaders.Add("Authorization", _dbAccessToken);
                    client.DefaultRequestHeaders.Add("Accept", "application/json");

                    try
                    {
                        result = await client.PostAsync($"{_settings.DatabaseUrl}/{route}", String.IsNullOrEmpty(body) ? null : new StringContent(body, Encoding.UTF8, "application/json"));
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                        result = null;
                    }
                }
            }

            return result;
        }

        private async Task<HttpResponseMessage> PostDbAsync(string route, object body)
        {
            // 
            HttpResponseMessage result = null;

            using (var handler = new HttpClientHandler())
            {
                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                handler.ServerCertificateCustomValidationCallback =
                    (httpRequestMessage, cert, cetChain, policyErrors) =>
                    {
                        return true;
                    };

                using (var client = new HttpClient(handler))
                {
                    if (!string.IsNullOrEmpty(_dbAccessToken))
                        client.DefaultRequestHeaders.Add("Authorization", _dbAccessToken);
                    client.DefaultRequestHeaders.Add("Accept", "application/json");

                    try
                    {
                        result = await client.PostAsync($"{_settings.DatabaseUrl}/{route}", new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json"));
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                        result = null;
                    }
                }
            }

            return result;
        }

        private async Task<T> PostDbAsync<T>(string route, object body)
        {
            // 
            T result = default(T);

            using (var handler = new HttpClientHandler())
            {
                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                handler.ServerCertificateCustomValidationCallback =
                    (httpRequestMessage, cert, cetChain, policyErrors) =>
                    {
                        return true;
                    };

                using (var client = new HttpClient(handler))
                {
                    if (!string.IsNullOrEmpty(_dbAccessToken))
                        client.DefaultRequestHeaders.Add("Authorization", _dbAccessToken);
                    client.DefaultRequestHeaders.Add("Accept", "application/json");

                    try
                    {
                        var response = await client.PostAsync($"{_settings.DatabaseUrl}/{route}", new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json"));

                        // Deserialize on success
                        if (response.IsSuccessStatusCode)
                            result = JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());

                        // Update cached value
                        GetDbCache.UpdateCache(route, result, _settings.CacheDuration);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                        result = default(T);
                    }
                }
            }

            return result;
        }

        private async Task<HttpResponseMessage> PutDbAsync(string route, string body)
        {
            // 
            HttpResponseMessage result = null;

            using (var handler = new HttpClientHandler())
            {
                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                handler.ServerCertificateCustomValidationCallback =
                    (httpRequestMessage, cert, cetChain, policyErrors) =>
                    {
                        return true;
                    };

                using (var client = new HttpClient(handler))
                {
                    if (!string.IsNullOrEmpty(_dbAccessToken))
                        client.DefaultRequestHeaders.Add("Authorization", _dbAccessToken);
                    client.DefaultRequestHeaders.Add("Accept", "application/json");

                    try
                    {
                        result = await client.PutAsync($"{_settings.DatabaseUrl}/{route}", String.IsNullOrEmpty(body) ? null : new StringContent(body, Encoding.UTF8, "application/json"));
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                        result = null;
                    }
                }
            }

            return result;
        }

        private async Task<HttpResponseMessage> PutDbAsync(string route, object body)
        {
            // 
            HttpResponseMessage result = null;

            using (var handler = new HttpClientHandler())
            {
                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                handler.ServerCertificateCustomValidationCallback =
                    (httpRequestMessage, cert, cetChain, policyErrors) =>
                    {
                        return true;
                    };

                using (var client = new HttpClient(handler))
                {
                    if (!string.IsNullOrEmpty(_dbAccessToken))
                        client.DefaultRequestHeaders.Add("Authorization", _dbAccessToken);
                    client.DefaultRequestHeaders.Add("Accept", "application/json");

                    try
                    {
                        result = await client.PutAsync($"{_settings.DatabaseUrl}/{route}", new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json"));
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                        result = null;
                    }
                }
            }

            return result;
        }

        private async Task<T> PutDbAsync<T>(string route, object body)
        {
            // 
            T result = default(T);

            using (var handler = new HttpClientHandler())
            {
                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                handler.ServerCertificateCustomValidationCallback =
                    (httpRequestMessage, cert, cetChain, policyErrors) =>
                    {
                        return true;
                    };

                using (var client = new HttpClient(handler))
                {
                    if (!string.IsNullOrEmpty(_dbAccessToken))
                        client.DefaultRequestHeaders.Add("Authorization", _dbAccessToken);
                    client.DefaultRequestHeaders.Add("Accept", "application/json");

                    try
                    {
                        var response = await client.PutAsync($"{_settings.DatabaseUrl}/{route}", new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json"));

                        // Deserialize on success
                        if (response.IsSuccessStatusCode)
                            result = JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());

                        // Update cached value
                        GetDbCache.UpdateCache(route, result, _settings.CacheDuration);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                        result = default(T);
                    }
                }
            }

            return result;
        }


        #endregion

    }
}
