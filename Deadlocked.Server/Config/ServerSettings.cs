using Deadlocked.Server.Mods;
using Deadlocked.Server.SCERT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Deadlocked.Server.Config
{
    public class ServerSettings
    {
        /// <summary>
        /// How many milliseconds before refreshing the config.
        /// </summary>
        public int RefreshConfigInterval = 5000;

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
        public int ClientTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Time since game created and host never connected to close the game world.
        /// </summary>
        public int GameTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Number of seconds before the server should send an echo to the client.
        /// </summary>
        public int ServerEchoInterval { get; set; } = 10;

        /// <summary>
        /// Number of ticks per second.
        /// </summary>
        public int TickRate { get; set; } = 10;

        /// <summary>
        /// Port of the MUIS server.
        /// </summary>
        public int MUISPort { get; set; } = 10071;

        /// <summary>
        /// Port of the MAS server.
        /// </summary>
        public int MASPort { get; set; } = 10075;

        /// <summary>
        /// Port of the MLS server.
        /// </summary>
        public int MLSPort { get; set; } = 10078;

        /// <summary>
        /// Port of the MPS server.
        /// </summary>
        public int MPSPort { get; set; } = 10077;

        /// <summary>
        /// Port of the NAT server.
        /// </summary>
        public int NATPort { get; set; } = 10070;

        /// <summary>
        /// When set, all DME servers will receive this ip. This bypasses how the DME handles local network servers.
        /// </summary>
        public string DmeIpOverride { get; set; } = null;

        /// <summary>
        /// Path to the dme binary.
        /// </summary>
        public string DmeStartPath { get; set; } = null;

        /// <summary>
        /// Whether or not to restart the DME server when it is no longer running.
        /// </summary>
        public bool DmeRestartOnCrash { get; set; } = false;

        /// <summary>
        /// Collection of patches to apply to logged in clients.
        /// </summary>
        public List<Patch> Patches { get; set; } = new List<Patch>();

        /// <summary>
        /// Collection of custom game modes.
        /// </summary>
        public List<Gamemode> Gamemodes { get; set; } = new List<Gamemode>();

        /// <summary>
        /// Collection of RT messages to print out
        /// </summary>
        public string[] RtLogFilter { get; set; } = Enum.GetNames(typeof(RT_MSG_TYPE));


        private Dictionary<RT_MSG_TYPE, bool> _rtLogFilters = new Dictionary<RT_MSG_TYPE, bool>();


        /// <summary>
        /// Whether or not the given rt message id should be logged
        /// </summary>
        public bool IsLog(RT_MSG_TYPE msgId)
        {
            if (_rtLogFilters.TryGetValue(msgId, out var result))
                return result;

            return false;
        }

        /// <summary>
        /// Does some post processing on the deserialized model.
        /// </summary>
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            // Load rt log filters in dictionary
            _rtLogFilters.Clear();
            if (RtLogFilter != null)
            {
                foreach (var filter in RtLogFilter)
                    _rtLogFilters.Add((RT_MSG_TYPE)Enum.Parse(typeof(RT_MSG_TYPE), filter), true);
            }
        }
    }
}
