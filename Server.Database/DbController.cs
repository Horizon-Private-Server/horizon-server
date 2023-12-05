﻿using DotNetty.Common.Internal.Logging;
using Newtonsoft.Json;
using RT.Common;
using Server.Common;
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
using System.Web;

namespace Server.Database
{
    public class DbController
    {
        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<DbController>();

        private DbSettings _settings = new DbSettings();

        private int _simulatedAccountIdCounter = 1;
        private int _simulatedClanIdCounter = 1;
        private int _simulatedClanMessageIdCounter = 1;
        private int _simulatedClanInvitationIdCounter = 1;
        private string _dbAccessToken = null;
        private string _dbAccountName = null;
        private List<AccountDTO> _simulatedAccounts = new List<AccountDTO>();
        private List<ClanDTO> _simulatedClans = new List<ClanDTO>();

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
            ClearAuthToken();
            var response = await Authenticate(_settings.DatabaseUsername, _settings.DatabasePassword);

            // Validate
            if (response == null || response.Roles == null || !response.Roles.Contains("database"))
                return false;

            // 
            _dbAccountName = response.AccountName;
            _dbAccessToken = response.Token;

            // 
            return !string.IsNullOrEmpty(_dbAccessToken);
        }

        public async Task<bool> AmIAuthenticated()
        {
            if (_settings.SimulatedMode)
                return true;

            return !String.IsNullOrEmpty(_dbAccessToken);
        }

        public void ClearAuthToken()
        {
            _dbAccessToken = null;
        }

        public string GetUsername()
        {
            if (_settings.SimulatedMode)
                return _settings.DatabaseUsername;

            return _dbAccountName;
        }

        #region Account

        public async Task<string> GetPlayerList()
        {
            string results = null;

            try
            {
                if (_settings.SimulatedMode) // Deprecated
                {
                    return "[]";
                }
                else
                {
                    HttpResponseMessage Resp = await GetDbAsync($"Account/getOnlineAccounts");
                    results = await Resp.Content.ReadAsStringAsync();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return results;
        }


        /// <summary>
        /// Get account by name.
        /// </summary>
        /// <param name="name">Case insensitive name of player.</param>
        /// <returns>Returns account.</returns>
        public async Task<AccountDTO> GetAccountByName(string name, int appId)
        {
            AccountDTO result = null;

            try
            {
                if (_settings.SimulatedMode)
                {
                    result = _simulatedAccounts.FirstOrDefault(x => x.AppId == appId && x.AccountName.ToLower() == name.ToLower());
                }
                else
                {
                    name = HttpUtility.UrlEncode(name);
                    string route = $"Account/searchAccountByName?AccountName={name}&AppId={appId}";
                    result = await GetDbAsync<AccountDTO>(route);
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
                    var checkExisting = await GetAccountByName(createAccount.AccountName, createAccount.AppId);
                    if (checkExisting == null)
                    {
                        _simulatedAccounts.Add(result = new AccountDTO()
                        {
                            AccountId = _simulatedAccountIdCounter++,
                            AccountName = createAccount.AccountName,
                            AccountPassword = createAccount.AccountPassword,
                            AccountWideStats = new int[Constants.LADDERSTATSWIDE_MAXLEN],
                            AccountCustomWideStats = new int[1000],
                            AppId = createAccount.AppId,
                            MachineId = createAccount.MachineId,
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
        /// Changes the given accounts password.
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="oldPassword"></param>
        /// <param name="newPassword"></param>
        /// <returns></returns>
        public async Task<bool> ChangeAccountPassword(int accountId, string oldPassword, string newPassword)
        {
            bool result = false;
            AccountPasswordRequest request = new AccountPasswordRequest()
            {
                AccountId = accountId,
                OldPassword = oldPassword,
                NewPassword = newPassword,
                ConfirmNewPassword = newPassword
            };

            try
            {
                if (_settings.SimulatedMode)
                {
                    var checkExisting = await GetAccountById(accountId);
                    if (checkExisting != null)
                    {
                        checkExisting.AccountPassword = Utils.ComputeSHA256(newPassword);
                    }
                    else
                    {
                        throw new Exception($"Account creation failed account name already exists!");
                    }
                }
                else
                {
                    var response = await PostDbAsync($"Account/changeAccountPassword", JsonConvert.SerializeObject(request));

                    // Deserialize on success
                    if (response.IsSuccessStatusCode)
                        result = true;
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
        public async Task<bool> DeleteAccount(string accountName, int appId)
        {
            bool result = false;

            try
            {
                if (_settings.SimulatedMode)
                {
                    result = _simulatedAccounts.RemoveAll(x => x.AccountName.ToLower() == accountName.ToLower() && x.AppId == appId) > 0;
                }
                else
                {
                    result = (await GetDbAsync($"Account/deleteAccount?AccountName={accountName}&AppId={appId}")).IsSuccessStatusCode;
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
                    var account = _simulatedAccounts.FirstOrDefault(x => x.AccountId == accountId);
                    result = account?.Metadata;
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
                    var account = _simulatedAccounts.FirstOrDefault(x => x.AccountId == accountId);
                    if (account != null)
                    {
                        account.Metadata = metadata;
                        result = true;
                    }
                    else
                    {
                        result = false;
                    }
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
        /// Gets the total number of active clans by app id.
        /// </summary>
        /// <param name="appId">App Id to filter total active clans by.</param>
        /// <returns>Number of active clans or null.</returns>
        public async Task<int?> GetActiveClanCountByAppId(int appId)
        {
            int? result = null;

            try
            {
                if (_settings.SimulatedMode)
                {
                    result = _simulatedClans.Count(x=>!x.IsDisbanded);
                }
                else
                {
                    var response = await GetDbAsync($"Clan/getActiveClanCountByAppId?AppId={appId}");

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
                    result = await PostDbAsync<bool>($"Account/getIpIsBanned", $"{ip}");
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
                    result = await PostDbAsync<bool>($"Account/getMacIsBanned", $"{mac}");
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
        /// Get player wide stats.
        /// </summary>
        /// <param name="accountId">Account id of player.</param>
        /// <returns></returns>
        public async Task<StatPostDTO> GetPlayerWideStats(int accountId)
        {
            StatPostDTO result = null;

            try
            {
                if (_settings.SimulatedMode)
                {
                    var stats = _simulatedAccounts.FirstOrDefault(x => x.AccountId == accountId)?.AccountWideStats;
                    if (stats != null)
                    {
                        result = new StatPostDTO()
                        {
                            AccountId = accountId,
                            Stats = stats
                        };
                    }
                }
                else
                {
                    result = await GetDbAsync<StatPostDTO>($"Stats/getStats?AccountId={accountId}");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// Get player wide stats.
        /// </summary>
        /// <param name="accountId">Account id of player.</param>
        /// <returns></returns>
        public async Task<ClanStatPostDTO> GetClanWideStats(int clanId)
        {
            ClanStatPostDTO result = null;

            try
            {
                if (_settings.SimulatedMode)
                {
                    var stats = _simulatedClans.FirstOrDefault(x => x.ClanId == clanId)?.ClanWideStats;
                    if (stats != null)
                    {
                        result = new ClanStatPostDTO()
                        {
                            ClanId = clanId,
                            Stats = stats
                        };
                    }
                }
                else
                {
                    result = await GetDbAsync<ClanStatPostDTO>($"Stats/getClanStats?ClanId={clanId}");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// Get player ranking in a given leaderboard.
        /// </summary>
        /// <param name="accountId">Account id of player.</param>
        /// <param name="statId">Index of stat. Starts at 1.</param>
        /// <returns>Leaderboard result for player.</returns>
        public async Task<LeaderboardDTO> GetPlayerLeaderboardIndex(int accountId, int statId, int appId)
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
                        StatValue = account.AccountWideStats[statId-1],
                        TotalRankedAccounts = 1
                    };
                }
                else
                {
                    result = await GetDbAsync<LeaderboardDTO>($"Stats/getPlayerLeaderboardIndex?AccountId={accountId}&StatId={statId}&AppId={appId}");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// Get clan ranking in a given leaderboard.
        /// </summary>
        /// <param name="clanId">Clan id of clan.</param>
        /// <param name="statId">Index of stat. Starts at 1.</param>
        /// <returns>Leaderboard result for clan.</returns>
        public async Task<ClanLeaderboardDTO> GetClanLeaderboardIndex(int clanId, int statId, int appId)
        {
            ClanLeaderboardDTO result = null;

            try
            {
                if (_settings.SimulatedMode)
                {
                    var clan = await GetClanById(clanId);
                    if (clan == null)
                        return null;

                    var ordered = _simulatedClans.Where(x => !x.IsDisbanded).OrderByDescending(x => x.ClanWideStats[statId]).ToList();
                    return new ClanLeaderboardDTO()
                    {
                        ClanId = clan.ClanId,
                        ClanName = clan.ClanName,
                        Index = ordered.FindIndex(0, ordered.Count, x => x.ClanId == clanId),
                        MediusStats = clan.ClanMediusStats,
                        StatValue = clan.ClanWideStats[statId],
                        TotalRankedClans = _simulatedClans.Count(x => !x.IsDisbanded)
                    };
                }
                else
                {
                    result = await GetDbAsync<ClanLeaderboardDTO>($"Stats/getClanLeaderboardIndex?ClanId={clanId}&StatId={statId+1}&AppId={appId}");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// Get clan leaderboard for a given stat by page and size.
        /// </summary>
        /// <param name="statId">Stat id. Starts at 1.</param>
        /// <param name="startIndex">Position to start gathering results from. Starts at 0.</param>
        /// <param name="size">Max number of items to retrieve.</param>
        /// <returns>Collection of leaderboard results for each player in page.</returns>
        public async Task<ClanLeaderboardDTO[]> GetClanLeaderboard(int statId, int startIndex, int size, int appId)
        {
            ClanLeaderboardDTO[] result = null;

            try
            {
                if (_settings.SimulatedMode)
                {
                    var ordered = _simulatedClans.Where(x => x.AppId == appId).Where(x=>!x.IsDisbanded).OrderByDescending(x => x.ClanWideStats[statId]).Skip(startIndex).Take(size).ToList();
                    result = ordered.Select(x => new ClanLeaderboardDTO()
                    {
                        ClanId = x.ClanId,
                        ClanName = x.ClanName,
                        MediusStats = x.ClanMediusStats,
                        StatValue = x.ClanWideStats[statId],
                        TotalRankedClans = _simulatedClans.Count(y => !y.IsDisbanded),
                        Index = startIndex + ordered.IndexOf(x)
                    }).ToArray();
                }
                else
                {
                    result = await GetDbAsync<ClanLeaderboardDTO[]>($"Stats/getClanLeaderboard?StatId={statId+1}&StartIndex={startIndex}&Size={size}&AppId={appId}");
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
        public async Task<LeaderboardDTO[]> GetLeaderboard(int statId, int startIndex, int size, int appId)
        {
            LeaderboardDTO[] result = null;

            try
            {
                if (_settings.SimulatedMode)
                {
                    var ordered = _simulatedAccounts.Where(x=>x.AppId == appId).OrderByDescending(x => x.AccountWideStats[statId]).Skip(startIndex).Take(size).ToList();
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
                    result = await GetDbAsync<LeaderboardDTO[]>($"Stats/getLeaderboard?StatId={statId}&StartIndex={startIndex}&Size={size}&AppId={appId}");
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
        public async Task<bool> PostAccountLadderStats(StatPostDTO statPost)
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
        /// Posts custom ladder stats to account id.
        /// </summary>
        /// <param name="statPost">Model containing account id and ladder stats collection.</param>
        /// <returns>Success or failure.</returns>
        public async Task<bool> PostAccountLadderCustomStats(StatPostDTO statPost)
        {
            bool result = false;

            try
            {
                if (_settings.SimulatedMode)
                {
                    var account = await GetAccountById(statPost.AccountId);
                    if (account == null)
                        return false;

                    account.AccountCustomWideStats = statPost.Stats;
                    result = true;
                }
                else
                {
                    result = (await PostDbAsync($"Stats/postStatsCustom", JsonConvert.SerializeObject(statPost))).IsSuccessStatusCode;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// Posts ladder stats to clan id.
        /// </summary>
        /// <param name="statPost">Model containing clan id and ladder stats collection.</param>
        /// <returns>Success or failure.</returns>
        public async Task<bool> PostClanLadderStats(int accountId, int? clanId, int[] stats)
        {
            bool result = false;
            if (!clanId.HasValue)
                return false;

            try
            {
                if (_settings.SimulatedMode)
                {
                    var account = await GetAccountById(accountId);
                    if (account.ClanId != clanId)
                        return false;

                    var clan = await GetClanById(account.ClanId.Value);
                    if (clan == null)
                        return false;

                    clan.ClanWideStats = stats;
                    result = true;
                }
                else
                {
                    result = (await PostDbAsync($"Stats/postClanStats", JsonConvert.SerializeObject(new ClanStatPostDTO()
                    {
                        ClanId = clanId.Value,
                        Stats = stats
                    }))).IsSuccessStatusCode;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// Posts custom ladder stats to clan id.
        /// </summary>
        /// <returns>Success or failure.</returns>
        public async Task<bool> PostClanLadderCustomStats(int accountId, int? clanId, int[] stats)
        {
            bool result = false;
            if (!clanId.HasValue)
                return false;

            try
            {
                if (_settings.SimulatedMode)
                {
                    var account = await GetAccountById(accountId);
                    if (account.ClanId != clanId)
                        return false;

                    var clan = await GetClanById(account.ClanId.Value);
                    if (clan == null)
                        return false;

                    clan.ClanCustomWideStats = stats;
                    result = true;
                }
                else
                {
                    result = (await PostDbAsync($"Stats/postClanStatsCustom", JsonConvert.SerializeObject(new ClanStatPostDTO()
                    {
                        ClanId = clanId.Value,
                        Stats = stats
                    }))).IsSuccessStatusCode;
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

        /// <summary>
        /// Post medius stats to clan.
        /// </summary>
        /// <param name="clanId">Clan id to post stats to.</param>
        /// <param name="stats">Stats to post encoded as a Base64 string.</param>
        /// <returns>Success or failure.</returns>
        public async Task<bool> PostClanMediusStats(int clanId, string stats)
        {
            bool result = false;

            try
            {
                if (_settings.SimulatedMode)
                {
                    var clan = await GetClanById(clanId);
                    if (clan == null)
                        return false;

                    clan.ClanMediusStats = stats;
                    result = true;
                }
                else
                {
                    result = (await PostDbAsync($"Clan/postClanMediusStats?ClanId={clanId}", $"\"{stats}\""))?.IsSuccessStatusCode ?? false;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        #endregion

        #region Clan

        /// <summary>
        /// Get clan by name.
        /// </summary>
        /// <param name="name">Case insensitive name of clan.</param>
        /// <returns>Returns clan.</returns>
        public async Task<ClanDTO> GetClanByName(string name, int appId)
        {
            ClanDTO result = null;

            try
            {
                if (_settings.SimulatedMode)
                {
                    result = _simulatedClans.FirstOrDefault(x => x.AppId == appId && x.ClanName.ToLower() == name.ToLower());
                }
                else
                {
                    result = await GetDbAsync<ClanDTO>($"Clan/searchClanByName?clanName={name}&appId={appId}");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// Get clan by id.
        /// </summary>
        /// <param name="id">Id of clan.</param>
        /// <returns>Returns clan.</returns>
        public async Task<ClanDTO> GetClanById(int id)
        {
            ClanDTO result = null;

            try
            {
                if (_settings.SimulatedMode)
                {
                    result = _simulatedClans.FirstOrDefault(x => x.ClanId == id);
                }
                else
                {
                    result = await GetDbAsync<ClanDTO>($"Clan/getClan?clanId={id}");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// Creates a clan.
        /// </summary>
        /// <param name="createClan">Clan creation parameters.</param>
        /// <returns>Returns created clan.</returns>
        public async Task<ClanDTO> CreateClan(int creatorAccountId, string clanName, int appId, string mediusStats)
        {
            ClanDTO result = null;

            try
            {
                if (_settings.SimulatedMode)
                {
                    var checkExisting = await GetClanByName(clanName, appId);
                    if (checkExisting == null)
                    {
                        var creatorAccount = await GetAccountById(creatorAccountId);
                        _simulatedClans.Add(result = new ClanDTO()
                        {
                            ClanId = _simulatedClanIdCounter++,
                            ClanName = clanName,
                            ClanLeaderAccount = creatorAccount,
                            ClanMemberAccounts = new List<AccountDTO>(new AccountDTO[] { creatorAccount }),
                            ClanMemberInvitations = new List<ClanInvitationDTO>(),
                            ClanMessages = new List<ClanMessageDTO>(),
                            ClanMediusStats = Convert.ToBase64String(new byte[Constants.CLANSTATS_MAXLEN]),
                            ClanWideStats = new int[Constants.LADDERSTATSWIDE_MAXLEN],
                            AppId = appId
                        });

                        creatorAccount.ClanId = result.ClanId;
                    }
                    else
                    {
                        throw new Exception($"Clan creation failed clan name already exists!");
                    }
                }
                else
                {
                    var response = await PostDbAsync($"Clan/createClan?accountId={creatorAccountId}&clanName={clanName}&appId={appId}&mediusStats={mediusStats}", null);

                    // Deserialize on success
                    if (response.IsSuccessStatusCode)
                        result = JsonConvert.DeserializeObject<ClanDTO>(await response.Content.ReadAsStringAsync());
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// Delete clan by id.
        /// </summary>
        /// <param name="clanId">Id of clan.</param>
        /// <returns>Success or failure.</returns>
        public async Task<bool> DeleteClan(int accountId, int clanId)
        {
            bool result = false;

            try
            {
                if (_settings.SimulatedMode)
                {
                    // 
                    var clan = await GetClanById(clanId);
                    if (clan == null || clan.ClanLeaderAccount.AccountId != accountId)
                        return false;

                    // remove members
                    foreach (var member in clan.ClanMemberAccounts)
                        member.ClanId = null;

                    // revoke invitations
                    foreach (var inv in clan.ClanMemberInvitations)
                        inv.ResponseStatus = 3;

                    // remove
                    return _simulatedClans.Remove(clan);
                }
                else
                {
                    result = (await GetDbAsync($"Clan/deleteClan?accountId={accountId}&clanId={clanId}")).IsSuccessStatusCode;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// Transfers leadership of a clan to a new leader.
        /// </summary>
        /// <param name="leaderAccountId">Account id of leader.</param>
        /// <param name="clanId">Id of clan.</param>
        /// <param name="newLeaderAccountId">Account id of new leader.</param>
        /// <returns>Returns created clan.</returns>
        public async Task<bool> ClanTransferLeadership(int leaderAccountId, int clanId, int newLeaderAccountId)
        {
            bool result = false;

            try
            {
                if (_settings.SimulatedMode)
                {
                    var clan = await GetClanById(clanId);
                    if (clan == null || clan.ClanLeaderAccount.AccountId != leaderAccountId)
                        return false;

                    var newLeaderAccount = await GetAccountById(newLeaderAccountId);
                    if (newLeaderAccount == null)
                        return false;

                    // must be a member
                    if (newLeaderAccount.ClanId != clanId)
                        return false;

                    clan.ClanLeaderAccount = newLeaderAccount;
                    result = true;
                }
                else
                {
                    result = (await PostDbAsync($"Clan/transferLeadership", JsonConvert.SerializeObject(new ClanTransferLeadershipDTO()
                    {
                        AccountId = leaderAccountId,
                        ClanId = clanId,
                        NewLeaderAccountId = newLeaderAccountId
                    }))).IsSuccessStatusCode;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// Transfers leadership of a clan to a new leader.
        /// </summary>
        /// <param name="leaderAccountId">Account id of leader.</param>
        /// <param name="clanId">Id of clan.</param>
        /// <param name="newLeaderAccountId">Account id of new leader.</param>
        /// <returns>Returns created clan.</returns>
        public async Task<bool> ClanLeave(int fromAccountId, int clanId, int accountId)
        {
            bool result = false;

            try
            {
                if (_settings.SimulatedMode)
                {
                    var clan = await GetClanById(clanId);
                    if (clan == null)
                        return false;

                    // only allow leader or player remove player
                    if (fromAccountId != accountId && clan.ClanLeaderAccount.AccountId != fromAccountId)
                        return false;
                    
                    // prevent leader from leaving -- must transfer or disband
                    if (clan.ClanLeaderAccount.AccountId == accountId)
                        return false;

                    var account = clan.ClanMemberAccounts.FirstOrDefault(x => x.AccountId == accountId);
                    if (account != null)
                    {
                        account.ClanId = null;
                        clan.ClanMemberAccounts.Remove(account);
                    }

                    result = true;
                }
                else
                {
                    result = (await PostDbAsync($"Clan/leaveClan?fromAccountId={fromAccountId}&clanId={clanId}&accountId={accountId}", null))?.IsSuccessStatusCode ?? false;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// Creates a new clan invitation for the given player.
        /// </summary>
        /// <param name="fromAccountId">Id of account sending invite.</param>
        /// <param name="clanId">Id of clan.</param>
        /// <param name="accountId">Id of target player.</param>
        /// <param name="message">Invite message.</param>
        /// <returns>Success or failure.</returns>
        public async Task<bool> CreateClanInvitation(int fromAccountId, int clanId, int accountId, string message)
        {
            bool result = false;

            try
            {
                if (_settings.SimulatedMode)
                {
                    // get clan
                    var clan = _simulatedClans.FirstOrDefault(x => x.ClanId == clanId);
                    if (clan == null)
                        return false;

                    // validate from is leader
                    if (clan.ClanLeaderAccount.AccountId != fromAccountId)
                        return false;

                    // get target account
                    var account = _simulatedAccounts.FirstOrDefault(x => x.AccountId == accountId);
                    if (account == null)
                        return false;

                    // check if invitations already made
                    if (clan.ClanMemberInvitations.Any(x => x.TargetAccountId == accountId && x.ResponseStatus == 0))
                        return false;

                    // add
                    clan.ClanMemberInvitations.Add(new ClanInvitationDTO()
                    {
                        InvitationId = _simulatedClanInvitationIdCounter++,
                        AppId = clan.AppId,
                        ClanId = clanId,
                        ClanName = clan.ClanName,
                        TargetAccountId = accountId,
                        TargetAccountName = account.AccountName,
                        Message = message
                    });

                    return true;
                }
                else
                {
                    result = (await PostDbAsync($"Clan/createInvitation?accountId={fromAccountId}", new ClanInvitationDTO()
                    {
                        ClanId = clanId,
                        TargetAccountId = accountId,
                        Message = message
                    })).IsSuccessStatusCode;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }


        /// <summary>
        /// Returns all clan invitations for the given player.
        /// </summary>
        /// <param name="accountId">Id of target player.</param>
        /// <returns>Success or failure.</returns>
        public async Task<List<AccountClanInvitationDTO>> GetClanInvitationsByAccount(int accountId)
        {
            List<AccountClanInvitationDTO> result = null;

            try
            {
                if (_settings.SimulatedMode)
                {
                    // get clans
                    var clans = _simulatedClans.Where(x => x.ClanMemberInvitations.Any(y => y.TargetAccountId == accountId));

                    // 
                    result = clans
                        .Select(x => new AccountClanInvitationDTO()
                        {
                            LeaderAccountId = x.ClanLeaderAccount.AccountId,
                            LeaderAccountName = x.ClanLeaderAccount.AccountName,
                            Invitation = x.ClanMemberInvitations.FirstOrDefault(y => y.TargetAccountId == accountId)
                        })
                        .Where(x => x.Invitation != null)
                        .ToList();
                }
                else
                {
                    result = (await GetDbAsync<List<AccountClanInvitationDTO>>($"Clan/invitations?accountId={accountId}"));
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// Sets the response to the given clan invitation.
        /// </summary>
        /// <param name="accountId">Id of account.</param>
        /// <param name="inviteId">Id of clan invitation.</param>
        /// <param name="message">Response message to record.</param>
        /// <param name="responseStatus">Response to invitation.</param>
        /// <returns>Success or failure.</returns>
        public async Task<bool> RespondToClanInvitation(int accountId, int inviteId, string message, int responseStatus)
        {
            bool result = false;

            try
            {
                if (_settings.SimulatedMode)
                {
                    // find invitation
                    var invite = _simulatedClans.Select(x => x.ClanMemberInvitations.FirstOrDefault(y => y.InvitationId == inviteId)).FirstOrDefault(x => x != null);
                    if (invite == null)
                        return false;

                    // get clan
                    var clan = _simulatedClans.FirstOrDefault(x => x.ClanMemberInvitations.Contains(invite));
                    if (clan == null)
                        return false;

                    // get account
                    var account = _simulatedAccounts.FirstOrDefault(x => x.AccountId == accountId);
                    if (account == null)
                        return false;

                    // validate its for user
                    if (invite.TargetAccountId != accountId)
                        return false;

                    // validate it's not already been decided
                    if (invite.ResponseStatus != 0)
                        return false;

                    // 
                    invite.ResponseMessage = message;
                    invite.ResponseStatus = responseStatus;
                    invite.ResponseTime = (int)Utils.GetUnixTime();
                    
                    // handle accept
                    if (responseStatus == 1)
                    {
                        account.ClanId = invite.ClanId;
                        clan.ClanMemberAccounts.Add(account);
                    }

                    result = true;
                }
                else
                {
                    result = (await PostDbAsync($"Clan/respondInvitation", new ClanInvitationResponseDTO()
                    {
                        AccountId = accountId,
                        InvitationId = inviteId,
                        Response = responseStatus,
                        ResponseMessage = message,
                        ResponseTime = (int)Utils.GetUnixTime()
                    })).IsSuccessStatusCode;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// Revokes a given clan's invitation to the target account.
        /// </summary>
        /// <param name="fromAccountId">Id of account requesting revoke.</param>
        /// <param name="clanId">Id of clan.</param>
        /// <param name="targetAccountId">Target account to revoke invitation to.</param>
        /// <returns>Success or failure.</returns>
        public async Task<bool> RevokeClanInvitation(int fromAccountId, int clanId, int targetAccountId)
        {
            bool result = false;

            try
            {
                if (_settings.SimulatedMode)
                {
                    // get clan
                    var clan = _simulatedClans.FirstOrDefault(x => x.ClanId == clanId);
                    if (clan == null)
                        return false;

                    // validate leader is fromAccount
                    if (clan.ClanLeaderAccount.AccountId != fromAccountId)
                        return false;

                    // find invitation
                    var invite = clan.ClanMemberInvitations.FirstOrDefault(x => x.TargetAccountId == targetAccountId);
                    if (invite == null)
                        return false;

                    // validate it's not already been decided
                    if (invite.ResponseStatus != 0)
                        return false;

                    // 
                    invite.ResponseStatus = 3;

                    result = true;
                }
                else
                {
                    result = (await PostDbAsync($"Clan/revokeInvitation?fromAccountId={fromAccountId}&clanId={clanId}&targetAccountId={targetAccountId}", null)).IsSuccessStatusCode;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// Returns all clan invitations for the given player.
        /// </summary>
        /// <param name="accountId">Id of target player.</param>
        /// <returns>Success or failure.</returns>
        public async Task<List<ClanMessageDTO>> GetClanMessages(int accountId, int clanId, int startIndex, int pageSize)
        {
            List<ClanMessageDTO> result = null;

            try
            {
                if (_settings.SimulatedMode)
                {
                    // get clan
                    var clan = await GetClanById(clanId);
                    if (clan != null)
                    {
                        // 
                        result = clan.ClanMessages
                            .Skip(startIndex * pageSize)
                            .Take(pageSize)
                            .ToList();
                    }
                }
                else
                {
                    result = (await GetDbAsync<List<ClanMessageDTO>>($"Clan/messages?accountId={accountId}&clanId={clanId}&start={startIndex}&pageSize={pageSize}"));
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// Adds a clan message to the clan.
        /// </summary>
        /// <param name="accountId">Id of sender account.</param>
        /// <param name="clanId">Id of clan.</param>
        /// <param name="message">Message to add.</param>
        /// <returns>Success or failure.</returns>
        public async Task<bool> ClanAddMessage(int accountId, int clanId, string message)
        {
            bool result = false;

            try
            {
                if (_settings.SimulatedMode)
                {
                    // get clan
                    var clan = await GetClanById(clanId);
                    if (clan == null)
                        return false;

                    // validate leader
                    if (clan.ClanLeaderAccount.AccountId != accountId)
                        return false;

                    //
                    clan.ClanMessages.Add(new ClanMessageDTO()
                    {
                        Id = _simulatedClanMessageIdCounter++,
                        Message = message
                    });

                    result = true;
                }
                else
                {
                    result = (await PostDbAsync($"Clan/addMessage?accountId={accountId}&clanId={clanId}", new ClanMessageDTO()
                    {
                        Message = message
                    })).IsSuccessStatusCode;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        /// <summary>
        /// Edits an existing clan message.
        /// </summary>
        /// <param name="accountId">Id of sender account.</param>
        /// <param name="clanId">Id of clan.</param>
        /// <param name="messageId">Id of clan message to edit.</param>
        /// <param name="message">Message to add.</param>
        /// <returns>Success or failure.</returns>
        public async Task<bool> ClanEditMessage(int accountId, int clanId, int messageId, string message)
        {
            bool result = false;

            try
            {
                if (_settings.SimulatedMode)
                {
                    // get clan
                    var clan = await GetClanById(clanId);
                    if (clan == null)
                        return false;

                    // validate leader
                    if (clan.ClanLeaderAccount.AccountId != accountId)
                        return false;

                    // find message
                    var clanMessage = clan.ClanMessages.FirstOrDefault(x => x.Id == messageId);
                    if (clanMessage == null)
                        return false;

                    //
                    clanMessage.Message = message;

                    result = true;
                }
                else
                {
                    result = (await PutDbAsync($"Clan/editMessage?accountId={accountId}&clanId={clanId}", new ClanMessageDTO()
                    {
                        Id = messageId,
                        Message = message
                    })).IsSuccessStatusCode;
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
        public async Task<DimAnnouncements> GetLatestAnnouncement(int appId)
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
                    result = await GetDbAsync<DimAnnouncements>($"api/Keys/getAnnouncements?fromDt={DateTime.UtcNow}&AppId={appId}");
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
        public async Task<DimAnnouncements[]> GetLatestAnnouncements(int appId, int size = 10)
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
                    result = await GetDbAsync<DimAnnouncements[]>($"api/Keys/getAnnouncementsList?Dt={DateTime.UtcNow}&TakeSize={size}&AppId={appId}");
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


        public async Task<string> GetGameList()
        {
            string results = null;

            HttpResponseMessage Resp = null;
            try
            {
                if (_settings.SimulatedMode) // Deprecated
                {
                    return "[]";
                }
                else
                {
                    Resp = await GetDbAsync($"api/Game/list");
                    results = await Resp.Content.ReadAsStringAsync();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return results;
        }




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

        #region World

        public async Task<ChannelDTO[]> GetChannels()
        {
            ChannelDTO[] results = null;

            try
            {
                if (_settings.SimulatedMode)
                {
                    return new ChannelDTO[]
                    {
                        new ChannelDTO()
                        {
                            AppId = 0,
                            Id = 0,
                            Name = "Channel 1",
                            MaxPlayers = 256,
                            GenericFieldFilter = 32
                        }
                    };
                }
                else
                {
                    results = await GetDbAsync<ChannelDTO[]>($"api/World/getChannels");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return results;
        }

        public async Task<LocationDTO[]> GetLocations()
        {
            LocationDTO[] results = null;

            try
            {
                if (_settings.SimulatedMode)
                {
                    return new LocationDTO[]
                    {
                        new LocationDTO()
                        {
                            AppId = 0,
                            Id = 0,
                            Name = "Location 1"
                        }
                    };
                }
                else
                {
                    results = await GetDbAsync<LocationDTO[]>($"api/World/getLocations");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return results;
        }

        public async Task<LocationDTO[]> GetLocations(int appId)
        {
            LocationDTO[] results = null;

            try
            {
                if (_settings.SimulatedMode)
                {
                    return new LocationDTO[]
                    {
                        new LocationDTO()
                        {
                            AppId = appId,
                            Id = 0,
                            Name = "Location 1"
                        }
                    };
                }
                else
                {
                    results = await GetDbAsync<LocationDTO[]>($"api/World/getLocations/{appId}");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return results;
        }


        #endregion

        #region Logs


        /// <summary>
        /// 
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        public async Task<bool> Log(int? accountId, string methodName, string logTitle, string logMsg, string logStacktrace, string payload)
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
                    result = (await PostDbAsync($"api/Logs/submitLog", JsonConvert.SerializeObject(new LogDTO()
                    {
                        AccountId = accountId,
                        MethodName = methodName,
                        LogTitle = logTitle,
                        LogMsg = logMsg,
                        LogStacktrace = logStacktrace,
                        Payload = payload
                    }))).IsSuccessStatusCode;
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

        public async Task<AppIdDTO[]> GetAppIds()
        {
            AppIdDTO[] results = null;

            try
            {
                if (_settings.SimulatedMode)
                {
                    return new AppIdDTO[]
                    {
                        new AppIdDTO()
                        {
                            Name = "All",
                            AppIds = Enumerable.Range(0, 100000).ToList()
                        }
                    };
                }
                else
                {
                    results = await GetDbAsync<AppIdDTO[]>($"api/Keys/getAppIds");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return results;
        }

        public async Task<Dictionary<string, string>> GetServerSettings(int appId)
        {
            Dictionary<string, string> result = null;

            try
            {
                if (_settings.SimulatedMode)
                {
                    return new Dictionary<string, string>();
                }
                else
                {
                    result = await GetDbAsync<Dictionary<string, string>>($"api/Keys/getSettings?appId={appId}");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        public async Task SetServerSettings(int appId, Dictionary<string, string> settings)
        {
            try
            {
                if (_settings.SimulatedMode)
                {

                }
                else
                {
                    await PostDbAsync($"api/Keys/setSettings?appId={appId}", settings);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

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
                    catch (HttpRequestException e)
                    {
                        Logger.Error(e);
                        ClearAuthToken();
                        result = null;
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
                    }
                    catch (HttpRequestException e)
                    {
                        Logger.Error(e);
                        ClearAuthToken();
                        result = null;
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
                    }
                    catch (HttpRequestException e)
                    {
                        Logger.Error(e);
                        ClearAuthToken();
                        result = default(T);
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
                    catch (HttpRequestException e)
                    {
                        Logger.Error(e);
                        ClearAuthToken();
                        result = null;
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
                    catch (HttpRequestException e)
                    {
                        Logger.Error(e);
                        ClearAuthToken();
                        result = null;
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
                    }
                    catch (HttpRequestException e)
                    {
                        Logger.Error(e);
                        ClearAuthToken();
                        result = default(T);
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
                    catch (HttpRequestException e)
                    {
                        Logger.Error(e);
                        ClearAuthToken();
                        result = null;
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
                    catch (HttpRequestException e)
                    {
                        Logger.Error(e);
                        ClearAuthToken();
                        result = null;
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
                    }
                    catch (HttpRequestException e)
                    {
                        Logger.Error(e);
                        ClearAuthToken();
                        result = default(T);
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
