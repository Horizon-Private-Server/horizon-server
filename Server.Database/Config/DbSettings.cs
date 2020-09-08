using DotNetty.Common.Internal.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Server.Database.Config
{
    public class DbSettings
    {
        /// <summary>
        /// When true, the controller will simulate a database.
        /// Data is not persistent in simulated mode.
        /// </summary>
        public bool SimulatedMode { get; set; } = true;

        /// <summary>
        /// Database url.
        /// </summary>
        public string DatabaseUrl { get; set; } = "http://localhost:80";

        /// <summary>
        /// Database username.
        /// </summary>
        public string DatabaseUsername { get; set; } = null;


        /// <summary>
        /// Database password.
        /// </summary>
        public string DatabasePassword { get; set; } = null;

        /// <summary>
        /// Number of seconds that a given cached get request will remain valid.
        /// </summary>
        public int CacheDuration { get; set; } = 15;
    }
}
