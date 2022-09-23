using RT.Models;
using Server.Dme.Models;
using Server.Pipeline.Udp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Dme.PluginArgs
{
    public class OnTcpMsg
    {
        public ClientObject Player { get; set; }

        public BaseScertMessage Packet { get; set; }

        public bool Ignore { get; set; }
    }
}
