using System;
using System.Collections.Generic;
using System.Text;

namespace Deadlocked.Server.Config
{
    public class ServerSettings
    {
        /// <summary>
        /// Application id.
        /// </summary>
        public int ApplicationId { get; set; } = 0;

        /// <summary>
        /// Announcement.
        /// </summary>
        public string Announcement { get; set; } = "";


        /// <summary>
        /// Usage policy.
        /// </summary>
        public string UsagePolicy { get; set; } = "";


        /// <summary>
        /// Privacy policy.
        /// </summary>
        public string PrivacyPolicy { get; set; } = "";

        /// <summary>
        /// Default channel for a client to connect to on login.
        /// </summary>
        public int DefaultChannelId { get; set; } = 0;

        /// <summary>
        /// By default the server will grab its public ip.
        /// If this is set, it will use the ip provided here instead.
        /// </summary>
        public string ServerIpOverride { get; set; } = null;

        /// <summary>
        /// Time since last echo response before timing the client out.
        /// </summary>
        public int ClientTimeoutSeconds { get; set; } = 15;

        /// <summary>
        /// Time since game created and host never connected to close the game world.
        /// </summary>
        public int GameTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Number of ticks per second.
        /// </summary>
        public int TickRate { get; set; } = 10;
    }
}
