using Server.Common.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.UnivereInformation.Config
{
    public class ServerSettings
    {
        /// <summary>
        /// How many milliseconds before refreshing the config.
        /// </summary>
        public int RefreshConfigInterval = 5000;

        /// <summary>
        /// Port of the MUIS server.
        /// </summary>
        public int[] Ports { get; set; } = new int[] { 10071 };

        /// <summary>
        /// Universes.
        /// </summary>
        public Dictionary<int, UniverseInfo> Universes { get; set; } = new Dictionary<int, UniverseInfo>();

        /// <summary>
        /// Logging settings.
        /// </summary>
        public LogSettings Logging { get; set; } = new LogSettings();
    }

    public class UniverseInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Endpoint { get; set; }
        public int Port { get; set; }
        public uint UniverseId { get; set; }
    }
}
