using Deadlocked.Server.Messages;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Deadlocked.Server.Medius
{
    public interface IMediusComponent
    {
        int Port { get; }

        void Start();
        void Stop();

        void Tick();
    }
}
