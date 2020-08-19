using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Deadlocked.Server
{
    public class ClientSocket
    {
        private TcpClient _client = null;

        public string Token { get; protected set; }

        public ClientObject Client { get; protected set; }

        public int ComponentState { get; set; } = 0;
        public int ApplicationId { get; set; } = 0;

        public ClientSocket(TcpClient client)
        {
            _client = client;
        }

        public void SetToken(string token)
        {
            Token = token;
            Client = Program.Clients.FirstOrDefault(x => x.Token == token);
        }

        #region Sockets

        public bool Connected => _client?.Connected ?? false;

        /// <summary>
        /// Socket remote endpoint.
        /// </summary>
        public EndPoint RemoteEndPoint
        {
            get
            {
                // This can crash the server when the socket is closed abruptly
                try
                {
                    return _client?.Client.RemoteEndPoint;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                return null;
            }
        }

        /// <summary>
        /// Socket local endpoint.
        /// </summary>
        public EndPoint LocalEndPoint
        {
            get
            {
                // This can crash the server when the socket is closed abruptly
                try
                {
                    return _client?.Client.LocalEndPoint;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                return null;
            }
        }

        public void Close()
        {
            _client?.Close();
        }

        public void Disconnect()
        {
            try
            {
                if (Connected)
                    _client?.Client.Disconnect(true);
            }
            catch (SocketException e)
            {
                Console.WriteLine(e);
            }
        }

        public int Receive(byte[] buffer)
        {
            return _client?.GetStream().Read(buffer) ?? 0;
        }

        public int ReadAvailable(byte[] buffer)
        {
            if (_client.Available == 0)
                return 0;

            return _client.GetStream().Read(buffer, 0, _client.Available);
        }

        public void Send(byte[] buffer)
        {
            if (!Connected || buffer == null || buffer.Length == 0)
                return;

            try
            {
                _client?.Client.Send(buffer);
            }
            catch (SocketException e)
            {
                Console.WriteLine(e);
                Console.WriteLine(e.InnerException);
                Console.WriteLine($"SEND TO {ToString()}: {BitConverter.ToString(buffer)}");
            }
        }

        #endregion

        public override string ToString()
        {
            if (Client?.ClientAccount != null)
                return Client.ClientAccount.AccountName;
            else
                return RemoteEndPoint?.ToString();
        }

    }
}
