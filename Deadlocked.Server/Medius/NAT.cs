using Deadlocked.Server.SCERT.Models;
using Deadlocked.Server.SCERT.Models.Packets;
using Medius.Crypto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Deadlocked.Server.Medius
{
    public class NAT
    {
        public class UdpClientObject
        {
            public IPEndPoint EndPoint;
            public DateTime LastPing;
        }

        public string Name => "NAT";
        public int Port => Program.Settings.NATPort;

        protected Queue<BaseScertMessage> _queue = new Queue<BaseScertMessage>();
        protected List<UdpClientObject> _clients = new List<UdpClientObject>();

        private UDPSocket _udpServer = new UDPSocket();

        public NAT()
        {

        }


        public void OnReceive(IPEndPoint source, byte[] buffer)
        {
            // Log if id is set
            Console.WriteLine($"{Name} {source}: {BitConverter.ToString(buffer)}");

            var client = _clients.FirstOrDefault(x => x.EndPoint.Equals(source));
            if (client == null)
            {
                client = new UdpClientObject()
                {
                    EndPoint = source,
                    LastPing = DateTime.UtcNow
                };

                _clients.Add(client);

                byte[] response = new byte[6];
                Array.Copy(source.Address.GetAddressBytes(), 0, response, 0, 4);
                Array.Copy(BitConverter.GetBytes((ushort)source.Port).Reverse().ToArray(), 0, response, 4, 2);

                _udpServer.Send(source, response);
            }
            else
            {
                
                client.LastPing = DateTime.UtcNow;
            }
        }

        public void Start()
        {
            _udpServer.Server(Port);
            _udpServer.OnReceive += OnReceive;
        }

        public void Stop()
        {
            _udpServer.Stop();
        }

        public void Tick()
        {
            // 
            _udpServer.ReadAvailable();
        }
    }
}
