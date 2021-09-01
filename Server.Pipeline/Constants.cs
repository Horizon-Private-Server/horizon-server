using DotNetty.Common.Utilities;
using Server.Pipeline.Attribute;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Pipeline
{
    public static class Constants
    {
        public static readonly AttributeKey<ScertClientAttribute> SCERT_CLIENT = AttributeKey<ScertClientAttribute>.ValueOf("SCERT_CLIENT");
    }
}
