using DotNetty.Common.Internal.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Deadlocked.Server.Config
{
    public class DbSettings
    {
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
