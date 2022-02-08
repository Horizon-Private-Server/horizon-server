using DotNetty.Transport.Channels;
using RT.Models;
using Server.Dme.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Dme.PluginArgs
{
    public class OnMessageArgs
    {
        public ClientObject Player { get; set; } = null;

        public IChannel Channel { get; set; } = null;

        public BaseScertMessage Message { get; set; } = null;

        public bool IsIncoming { get; }

        public bool Ignore { get; set; } = false;


        public OnMessageArgs(bool isIncoming)
        {
            IsIncoming = isIncoming;
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"Player:{Player} " +
                $"Channel:{Channel} " +
                $"Message:{Message} " +
                $"Ignore:{Ignore}";
        }
    }
}
