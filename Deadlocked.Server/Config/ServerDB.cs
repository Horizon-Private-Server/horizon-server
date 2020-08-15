using Deadlocked.Server.Accounts;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Deadlocked.Server.Config
{
    public class ServerDB
    {
        #region Accounts

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty]
        private List<Account> Accounts = new List<Account>();

        /// <summary>
        /// Grabs the first account with a matching id.
        /// Returns null if no account found.
        /// </summary>
        public Account GetAccountById(int id)
        {
            return Accounts.FirstOrDefault(x => x.AccountId == id);
        }

        /// <summary>
        /// Try get account by id.
        /// </summary>
        public bool TryGetAccountById(int id, out Account account)
        {
            account = Accounts.FirstOrDefault(x => x.AccountId == id);
            return account != null;
        }

        /// <summary>
        /// Get account by name. Not case sensitive.
        /// </summary>
        public bool TryGetAccountByName(string name, out Account account)
        {
            account = Accounts.FirstOrDefault(x => x.AccountName.ToLower() == name.ToLower());
            return account != null;
        }

        /// <summary>
        /// Add account to collection.
        /// AccountId is set internally.
        /// </summary>
        /// <param name="account"></param>
        public void AddAccount(Account account)
        {
            account.AccountId = (Accounts.Count > 0 ? Accounts.Max(x => x.AccountId) : 0) + 1;
            Accounts.Add(account);

            Save();
        }

        /// <summary>
        /// Deletes an account.
        /// </summary>
        public void DeleteAccount(Account account)
        {
            Accounts.Remove(account);
            Save();
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Save the database.
        /// </summary>
        public void Save()
        {
            // Save accounts
            File.WriteAllText(Program.DB_FILE, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        #endregion

    }
}
