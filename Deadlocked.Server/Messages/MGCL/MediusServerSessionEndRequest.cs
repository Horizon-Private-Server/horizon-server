using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.MGCL
{
    [MediusApp(MediusAppPacketIds.MediusServerSessionEndRequest)]
    public class MediusServerSessionEndRequest : BaseMGCLMessage
    {
        public override MediusAppPacketIds Id => MediusAppPacketIds.MediusServerSessionEndRequest;


        public override string ToString()
        {
            return base.ToString();
        }
    }
}