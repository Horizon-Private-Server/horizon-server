using Deadlocked.Server.Messages;
using Deadlocked.Server.Messages.DME;
using Deadlocked.Server.Messages.RTIME;
using Medius.Crypto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Deadlocked.Server.Medius
{
    public class NAT : IMediusComponent
    {
        public class UdpClientObject
        {
            public IPEndPoint EndPoint;
            public DateTime LastPing;
        }

        public int Port => 10070;

        protected Queue<BaseMessage> _queue = new Queue<BaseMessage>();
        protected List<UdpClientObject> _clients = new List<UdpClientObject>();

        private UDPSocket _udpServer = new UDPSocket();

        public NAT()
        {

        }


        public void OnReceive(IPEndPoint source, byte[] buffer)
        {
            Console.WriteLine($"!!! NAT !!! NEW MSG {source} {BitConverter.ToString(buffer)}");

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
