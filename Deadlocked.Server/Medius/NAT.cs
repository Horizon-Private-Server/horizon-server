using RT.Cryptography;
using RT.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Deadlocked.Server.Medius
{
    /// <summary>
    /// Unimplemented NAT.
    /// </summary>
    public class NAT
    {
        public int Port => Program.Settings.NATPort;

        public NAT()
        {

        }

        private void ReplyIpPort(IPEndPoint target)
        {
            byte[] response = new byte[6];
            Array.Copy(target.Address.GetAddressBytes(), 0, response, 0, 4);
            Array.Copy(BitConverter.GetBytes((ushort)target.Port).Reverse().ToArray(), 0, response, 4, 2);
        }

        public void Start()
        {

        }

        public void Stop()
        {

        }

        public void Tick()
        {

        }
    }
}
