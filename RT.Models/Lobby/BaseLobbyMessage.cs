using RT.Common;
using RT.Cryptography;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace RT.Models
{
    public abstract class BaseLobbyMessage : BaseMediusMessage
    {
        public override NetMessageTypes PacketClass => NetMessageTypes.MessageClassLobby;

        public BaseLobbyMessage()
        {

        }

    }
}
