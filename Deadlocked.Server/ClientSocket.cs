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
        private Socket _client = null;

        public string Token { get; protected set; }

        public ClientObject Client { get; protected set; }

        public int ComponentState { get; set; } = 0;

        public ClientSocket(Socket client)
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

        public EndPoint RemoteEndPoint => _client?.RemoteEndPoint;

        public EndPoint LocalEndPoint => _client?.LocalEndPoint;

        public void Close()
        {
            _client?.Close();
        }

        public void Disconnect()
        {
            if (Connected)
                _client?.Disconnect(true);
        }

        public int Receive(byte[] buffer)
        {
            return _client?.Receive(buffer) ?? 0;
        }

        public void Send(byte[] buffer)
        {
            if (!Connected)
                return;

            _client?.Send(buffer);
        }

        #endregion

    }
}
