using DotNetty.Common.Internal.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Medius.Models
{
    public class Clan
    {
        static readonly IInternalLogger _logger = InternalLoggerFactory.GetInstance<Clan>();
        protected virtual IInternalLogger Logger => _logger;

        public int Id { get; set; } = 0;
        public string Name { get; set; } = "";


        public Clan()
        {

        }
    }
}
