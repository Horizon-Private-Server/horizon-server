using Deadlocked.Server.Messages;
using Deadlocked.Server.Messages.RTIME;
using Medius.Crypto;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Deadlocked.Server.Medius
{
    public abstract class BaseMediusComponent : IMediusComponent
    {
        public abstract int Port { get; }

        protected Queue<BaseMessage> _queue = new Queue<BaseMessage>();
        protected PS2_RC4 _sessionCipher = null;
        protected TcpListener Listener = null;

        protected List<ClientSocket> _clients = new List<ClientSocket>();

        protected DateTime timeLastEcho = DateTime.UtcNow;

        protected bool shouldEcho = false;


        public virtual void Start()
        {
            Listener = new TcpListener(IPAddress.Any, Port);
            Listener.ExclusiveAddressUse = false;
            Listener.Start();

            // Handle new connections
            new Thread(() =>
            {
                try
                {
                    while (true)
                    {
                        var client = new ClientSocket(Listener.AcceptTcpClient());
                        Console.WriteLine($"Connection accepted on port {Port}.");
                        _clients.Add(client);

                        new Thread(() =>
                        {
                            OnClientConnected(client);
                        }).Start();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }).Start();
        }

        public virtual void Stop()
        {
            // 
            foreach (var client in _clients)
                client.Close();

            //
            _clients.Clear();

            //
            Listener.Stop();
        }

        protected virtual void OnClientConnected(ClientSocket client)
        {
            byte[] data = new byte[4096];
            try
            {
                while (true)
                {
                    if (client == null || !client.Connected)
                        break;

                    int size = client.Receive(data);
                    if (!client.Connected)
                        break;

                    if (size <= 0)
                        break;

                    if (size > 0)
                    {
                        int ret = 0;
                        byte[] buffer = new byte[size];
                        Array.Copy(data, 0, buffer, 0, size);

                        var msgs = BaseMessage.Instantiate(buffer, (id, context) =>
                        {
                            switch (context)
                            {
                                case CipherContext.RC_CLIENT_SESSION: return _sessionCipher;
                                case CipherContext.RSA_AUTH: return Program.GlobalAuthKey;
                                default: return null;
                            }
                        });

                        lock (_queue)
                        {
                            foreach (var msg in msgs)
                            {
                                _queue.Enqueue(msg);
                            }
                        }

                        if (ret == 1)
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            //_clients.Remove(client);
            client.Disconnect();
            client.Close();
        }

        public void Tick()
        {
            // Determine if should echo
            if ((DateTime.UtcNow - timeLastEcho).TotalSeconds > 5)
                shouldEcho = true;

            // Collection of clients that have DC'd
            Queue<ClientSocket> removeQueue = new Queue<ClientSocket>();

            // Iterate through each
            // Run tick on each unless client disconnected
            foreach (var client in _clients)
            {
                if (client == null || !client.Connected)
                    removeQueue.Enqueue(client);
                else
                    Tick(client);
            }

            // Remove disconnected clients from collection
            while (removeQueue.Count > 0)
            {
                var client = removeQueue.Dequeue();
                client.Close();
                _clients.Remove(client);
            }

            // Reset echo timer
            if (shouldEcho)
            {
                shouldEcho = false;
                timeLastEcho = DateTime.UtcNow;
            }
        }

        protected virtual void Tick(ClientSocket client)
        {

        }

        protected void Echo(ClientSocket client, ref List<BaseMessage> responses)
        {
            responses.Add(new RT_MSG_SERVER_ECHO() { });
        }

        protected virtual int HandleCommand(BaseMessage message, ClientSocket client, ref List<BaseMessage> responses)
        {
            return 0;
        }
    }
}
