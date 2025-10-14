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
        /// Data is not persistent in simulated mode unless SimulatedEncryptionKey is set.
        /// </summary>
        public bool SimulatedMode { get; set; } = true;

        /// <summary>
        /// If set, will be used to encrypt/decrypt persistent data in simulated mode.
        /// </summary>
        public string SimulatedEncryptionKey { get; set; } = null;

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
    }
}
