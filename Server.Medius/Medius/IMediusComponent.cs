using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server.Medius
{
    public interface IMediusComponent
    {
        int Port { get; }

        void Start();
        Task Stop();

        Task Tick();
    }
}
