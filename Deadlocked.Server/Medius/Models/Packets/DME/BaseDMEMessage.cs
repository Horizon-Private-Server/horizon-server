using Deadlocked.Server.Messages.Lobby;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.DME
{
    public abstract class BaseDMEMessage : BaseMediusMessage
    {
        public override NetMessageTypes MessageClass => NetMessageTypes.MessageClassDME;

        public BaseDMEMessage()
        {

        }
    }
}
