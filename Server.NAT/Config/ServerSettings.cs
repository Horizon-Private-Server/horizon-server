using System;
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

    }
}
