using RT.Models;
using Server.Medius.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Medius.PluginArgs
{
    public class OnUpdateClanStatsRequestArgs
    {
        /// <summary>
        /// Player making request.
        /// </summary>
        public ClientObject Player { get; set; }

        /// <summary>
        /// MediusUpdateClanStatsRequest request.
        /// </summary>
        public IMediusRequest Request { get; set; }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"Player:{Player} " +
                $"Request:{Request}";
        }
    }
}
