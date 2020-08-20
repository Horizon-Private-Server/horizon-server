using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.MGCL
{
    [MediusMessage(TypesAAA.MediusServerSessionEndRequest)]
    public class MediusServerSessionEndRequest : BaseMGCLMessage
    {
        public override TypesAAA MessageType => TypesAAA.MediusServerSessionEndRequest;


        public override string ToString()
        {
            return base.ToString();
        }
    }
}