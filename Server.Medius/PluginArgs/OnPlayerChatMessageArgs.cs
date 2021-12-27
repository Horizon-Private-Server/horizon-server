using RT.Models;
using RT.Models.Misc;
using Server.Medius.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Medius.PluginArgs
{
    public class OnPlayerChatMessageArgs
    {
        /// <summary>
        /// Source player.
        /// </summary>
        public ClientObject Player { get; set; }

        /// <summary>
        /// Message.
        /// </summary>
        public IMediusChatMessage Message { get; set; }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"Player:{Player} " +
                $"Message:{Message}";
        }

    }
}
