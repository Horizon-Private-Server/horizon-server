using Deadlocked.Server.Medius.Models;
using Deadlocked.Server.Medius.Models.Packets;
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
        /// Array of friends by account id.
        /// </summary>
        public List<int> Friends { get; set; } = new List<int>();

        /// <summary>
        /// Array of ignored by account id.
        /// </summary>
        public List<int> Ignored { get; set; } = new List<int>();

        /// <summary>
        /// Wide stats.
        /// </summary>
        public int[] AccountWideStats { get; set; } = new int[MediusConstants.LADDERSTATSWIDE_MAXLEN];

        /// <summary>
        /// Stats.
        /// </summary>
        public byte[] Stats { get; set; } = new byte[MediusConstants.ACCOUNTSTATS_MAXLEN];

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
