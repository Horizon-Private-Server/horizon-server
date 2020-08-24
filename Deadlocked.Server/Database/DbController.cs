using Deadlocked.Server.Database.Models;
using DotNetty.Common.Internal.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Deadlocked.Server.Database
{
    public class DbController
    {
        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<DbController>();

        #region Cache

        private class GetDbCache
        {
            static Dictionary<string, GetDbCache> _getCache = new Dictionary<string, GetDbCache>();

            public DateTime LastUpdate;

            public object Value;

            public bool IsValid => Value != null && (DateTime.UtcNow - LastUpdate).TotalSeconds < Program.DbSettings.CacheDuration;


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

            public static void UpdateCache(string route, object value)
            {
                if (_getCache.TryGetValue(route, out var cache))
                {
                    cache.Value = value;
                    cache.LastUpdate = DateTime.UtcNow;
                }
                else
                {
                    _getCache.Add(route, new GetDbCache()
                    {
                        LastUpdate = DateTime.UtcNow,
                        Value = value
                    });
                }
            }
        }

        #endregion

        #region Account

        /// <summary>
        /// Get account by name.
        /// </summary>
        /// <param name="name">Case insensitive name of player.</param>
        /// <returns>Returns account.</returns>
        public static async Task<AccountDTO> GetAccountByName(string name)
        {
            AccountDTO result = null;

            try
            {
                result = await GetDbAsync<AccountDTO>($"Account/searchAccountByName?AccountName={name}");
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
        public static async Task<AccountDTO> GetAccountById(int id)
        {
            AccountDTO result = null;

            try
            {
                result = await GetDbAsync<AccountDTO>($"Account/getAccount?AccountId={id}");
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
        public static async Task<AccountDTO> CreateAccount(CreateAccountDTO createAccount)
        {
            AccountDTO result = null;

            try
            {
                var response = await PostDbAsync($"Account/createAccount", JsonConvert.SerializeObject(createAccount));

                // Deserialize on success
                if (response.IsSuccessStatusCode)
                    result = JsonConvert.DeserializeObject<AccountDTO>(await response.Content.ReadAsStringAsync());
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
        public static async Task<bool> DeleteAccount(string accountName)
        {
            bool result = false;

            try
            {
                result = (await GetDbAsync($"Account/deleteAccount?AccountName={accountName}")).IsSuccessStatusCode;
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
        public static async Task<bool> PostAccountSignInDate(int accountId, DateTime time)
        {
            bool result = false;

            try
            {
                result = (await PostDbAsync($"Account/postAccountSignInDate?AccountId={accountId}", time.ToUniversalTime().ToString())).IsSuccessStatusCode;
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
        public static async Task<AccountStatusDTO> GetAccountStatus(int accountId)
        {
            AccountStatusDTO result = null;

            try
            {
                result = await GetDbAsync<AccountStatusDTO>($"Account/getAccountStatus?AccountId={accountId}");
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
        public static async Task<bool> PostAccountStatus(AccountStatusDTO status)
        {
            bool result = false;

            try
            {
                var response = (await PostDbAsync($"Account/postAccountStatusUpdates", JsonConvert.SerializeObject(status))).IsSuccessStatusCode;
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
        public static async Task<int?> GetActiveAccountCountByAppId(int appId)
        {
            int? result = null;

            try
            {
                var response = await GetDbAsync($"Account/getActiveAccountCountByAppId?AppId={appId}");

                // Deserialize on success
                if (response.IsSuccessStatusCode && int.TryParse(await response.Content.ReadAsStringAsync(), out int r))
                    result = r;
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
        public static async Task<bool> AddBuddy(BuddyDTO addBuddy)
        {
            bool result = false;

            try
            {
                result = (await PostDbAsync($"Buddy/addBuddy", JsonConvert.SerializeObject(addBuddy))).IsSuccessStatusCode;
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
        public static async Task<bool> RemoveBuddy(BuddyDTO removeBuddy)
        {
            bool result = false;

            try
            {
                result = (await PostDbAsync($"Buddy/removeBuddy", JsonConvert.SerializeObject(removeBuddy))).IsSuccessStatusCode;
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
        public static async Task<bool> AddIgnored(IgnoredDTO addIgnored)
        {
            bool result = false;

            try
            {
                result = (await PostDbAsync($"Buddy/addIgnored", JsonConvert.SerializeObject(addIgnored))).IsSuccessStatusCode;
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
        public static async Task<bool> RemoveIgnored(IgnoredDTO removeIgnored)
        {
            bool result = false;

            try
            {
                var response = (await PostDbAsync($"Buddy/removeIgnored", JsonConvert.SerializeObject(removeIgnored))).IsSuccessStatusCode;
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
        public static async Task<LeaderboardDTO> GetPlayerLeaderboardIndex(int accountId, int statId)
        {
            LeaderboardDTO result = null;

            try
            {
                result = await GetDbAsync<LeaderboardDTO>($"Stats/getPlayerLeaderboardIndex?AccountId={accountId}&StatId={statId}");
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
        public static async Task<LeaderboardDTO[]> GetLeaderboard(int statId, int startIndex, int size)
        {
            LeaderboardDTO[] result = null;

            try
            {
                result = await GetDbAsync<LeaderboardDTO[]>($"Stats/getLeaderboard?StatId={statId}&StartIndex={startIndex}&Size={size}");
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
        public static async Task<bool> PostLadderStats(StatPostDTO statPost)
        {
            bool result = false;

            try
            {
                var response = (await PostDbAsync($"Stats/postStats", JsonConvert.SerializeObject(statPost))).IsSuccessStatusCode;
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
        public static async Task<bool> PostMediusStats(int accountId, string stats)
        {
            bool result = false;

            try
            {
                var response = (await PostDbAsync($"Account/postMediusStats?AccountId={accountId}", stats)).IsSuccessStatusCode;
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        #endregion

        #region Http

        private static async Task<HttpResponseMessage> GetDbAsync(string route)
        {
            // Try to get a cached result first
            if (GetDbCache.TryGetCache(route, out HttpResponseMessage value))
                return value;

            // 
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            HttpResponseMessage result = null;

            try
            {
                result = await client.GetAsync($"{Program.DbSettings.DatabaseUrl}/{route}");

                // Update cached value
                GetDbCache.UpdateCache(route, result);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                result = null;
            }
            finally
            {
                client.Dispose();
            }

            return result;
        }

        private static async Task<T> GetDbAsync<T>(string route)
        {
            // Try to get a cached result first
            if (GetDbCache.TryGetCache(route, out T value))
                return value;

            // 
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            T result = default(T);

            try
            {
                var response = await client.GetAsync($"{Program.DbSettings.DatabaseUrl}/{route}");

                // Deserialize on success
                if (response.IsSuccessStatusCode)
                    result = JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());

                // Update cached value
                GetDbCache.UpdateCache(route, result);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                result = default(T);
            }
            finally
            {
                client.Dispose();
            }

            return result;
        }

        private static async Task<HttpResponseMessage> PostDbAsync(string route, string body)
        {
            var handler = new HttpClientHandler()
            {
                AllowAutoRedirect = false
            };

            HttpClient client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            HttpResponseMessage result = null;

            try
            {
                result = await client.PostAsync($"{Program.DbSettings.DatabaseUrl}/{route}", new StringContent(body, Encoding.UTF8, "application/json"));
            }
            catch (Exception e)
            {
                Logger.Error(e);
                result = null;
            }
            finally
            {
                client.Dispose();
            }

            return result;
        }

        private static async Task<HttpResponseMessage> PostDbAsync(string route, object body)
        {
            var handler = new HttpClientHandler()
            {
                AllowAutoRedirect = false
            };

            HttpClient client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            HttpResponseMessage result = null;

            try
            {
                result = await client.PostAsync($"{Program.DbSettings.DatabaseUrl}/{route}", new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json"));
            }
            catch (Exception e)
            {
                Logger.Error(e);
                result = null;
            }
            finally
            {
                client.Dispose();
            }

            return result;
        }

        #endregion

    }
}
