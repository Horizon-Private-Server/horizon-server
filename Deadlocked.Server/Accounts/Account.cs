using Deadlocked.Server.Messages;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Deadlocked.Server.Accounts
{
    public class Account
    {

        /// <summary>
        /// Unique account identifier.
        /// </summary>
        public int AccountId { get; set; } = 0;

        /// <summary>
        /// Account name.
        /// </summary>
        public string AccountName { get; set; } = "";

        /// <summary>
        /// Account password.
        /// </summary>
        public string AccountPassword { get; set; } = "";



        /// <summary>
        /// Current client.
        /// </summary>
        [JsonIgnore]
        public ClientObject Client { get; set; } = null;

        /// <summary>
        /// Whether the client is logged in.
        /// </summary>
        [JsonIgnore]
        public bool IsLoggedIn => Client != null && Client.IsConnected;
    }
}
