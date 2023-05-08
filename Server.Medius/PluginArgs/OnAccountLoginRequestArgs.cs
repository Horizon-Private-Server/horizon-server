using RT.Models;
using Server.Medius.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Medius.PluginArgs
{
    public class OnAccountLoginRequestArgs
    {
        /// <summary>
        /// Player making request.
        /// </summary>
        public ClientObject Player { get; set; }

        /// <summary>
        /// AccountLogin request.
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
