using Server.Dme.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Dme.PluginArgs
{
    public class OnPlayerArgs
    {
        public ClientObject Player { get; set; }

        public World Game { get; set; }
    }
}
