using RT.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Server.Common;

namespace RT.Models
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_CLIENT_APP_TO_PLUGIN)]
    public class RT_MSG_CLIENT_APP_TO_PLUGIN : BaseScertMessage
    {
        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_CLIENT_APP_TO_PLUGIN;

        public BaseMediusMessage Message { get; set; } = null;

        public int plugInHeader;
        public int UNK1;

        public override void Deserialize(Server.Common.Stream.MessageReader reader) { 


            plugInHeader = reader.ReadInt16();
            UNK1 = reader.ReadInt16();
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer) { 

            
            writer.Write(plugInHeader);
            writer.Write(UNK1);
            
           
        }
        public override string ToString()
        {
            return base.ToString() + " " +
                $"plugInHeader: {plugInHeader} " +
                $"UNK1: {UNK1} ";
        }
    }
}