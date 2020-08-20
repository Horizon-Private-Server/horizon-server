using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.MGCL
{
	[MediusMessage(NetMessageTypes.MessageClassLobbyReport, MediusMGCLMessageIds.ServerSessionEndRequest)]
    public class MediusServerSessionEndRequest : BaseMGCLMessage
    {
		public override byte PacketType => (byte)MediusMGCLMessageIds.ServerSessionEndRequest;


        public override string ToString()
        {
            return base.ToString();
        }
    }
}
