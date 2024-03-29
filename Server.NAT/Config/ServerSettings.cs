﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Server.NAT.Config
{
    public class ServerSettings
    {
        /// <summary>
        /// Port of the NAT server.
        /// </summary>
        public int Port { get; set; } = 10070;

        /// <summary>
        /// When set, all nat ip requests will be receive the server's ip and this port.
        /// </summary>
        public int? OverridePort { get; set; } = null;
    }
}
