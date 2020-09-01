using RT.Common;
using RT.Cryptography;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace RT.Models
{
    public abstract class BaseMGCLMessage : BaseMediusMessage
    {
        public override NetMessageTypes PacketClass => NetMessageTypes.MessageClassLobbyReport;

        public BaseMGCLMessage()
        {

        }

    }
}
