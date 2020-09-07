using Server.Medius.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Medius.PluginArgs
{
    public class OnPlayerGameArgs
    {
        /// <summary>
        /// Player.
        /// </summary>
        public ClientObject Player { get; set; }

        /// <summary>
        /// Game.
        /// </summary>
        public Game Game { get; set; }
    }
}
