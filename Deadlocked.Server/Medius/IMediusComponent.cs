using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Deadlocked.Server.Medius
{
    public interface IMediusComponent
    {
        string Name { get; }
        int Port { get; }

        void Start();
        Task Stop();

        Task Tick();
    }
}
