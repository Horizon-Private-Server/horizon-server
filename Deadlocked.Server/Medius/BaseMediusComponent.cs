using Deadlocked.Server.Messages;
using Deadlocked.Server.Messages.RTIME;
using Medius.Crypto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Deadlocked.Server.Medius
{
    public abstract class BaseMediusComponent : IMediusComponent
    {

        public static Random RNG = new Random();
        public abstract int Port { get; }


        public abstract PS2_RSA AuthKey { get; }

        protected Queue<BaseMessage> _queue = new Queue<BaseMessage>();
        protected PS2_RC4 _sessionCipher = null;
        protected TcpListener Listener = null;

        public List<ClientSocket> Clients = new List<ClientSocket>();

        protected DateTime timeLastEcho = DateTime.UtcNow;
        protected byte[] readBuffer = new byte[1024 * 10];


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

                        lock (Clients)
                        {
                            Clients.Add(client);
                        }

                        
                        // Console.WriteLine($"Connection accepted on port {Port}.");

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
            lock (Clients)
            {
                foreach (var client in Clients)
                    client.Close();

                //
                Clients.Clear();
            }

            //
            Listener.Stop();
        }

        protected void OnRead(ClientSocket client, byte[] buffer, int index, int count)
        {
            try
            {
                // Check for PING-PONG message
                if (count == 4 && Encoding.UTF8.GetString(buffer, index, count) == "PING")
                {
                    client.Send(Encoding.UTF8.GetBytes("PONG"));
                    client.Close();
                    return;
                }

                var msgs = BaseMessage.Instantiate(buffer, index, count, (id, context) =>
                {
                    switch (context)
                    {
                        case CipherContext.RC_CLIENT_SESSION: return _sessionCipher;
                        case CipherContext.RSA_AUTH: return AuthKey;
                        default: return null;
                    }
                });

                lock (_queue)
                {
                    foreach (var msg in msgs)
                    {
                        msg.Source = (IPEndPoint)client.RemoteEndPoint;
                        _queue.Enqueue(msg);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine($"RECV BUFFER FROM {client}: {BitConverter.ToString(buffer, index, count)}");
                throw e;
            }
        }

        protected void Read(ClientSocket client)
        {
            int size = client.ReadAvailable(readBuffer);

            if (size > 0)
                OnRead(client, readBuffer, 0, size);
        }

        public void Tick()
        {
            // Collection of clients that have DC'd
            Queue<ClientSocket> removeQueue = new Queue<ClientSocket>();

            // Iterate through each
            // Run tick on each unless client disconnected
            lock (Clients)
            {
                foreach (var client in Clients)
                {
                    if (client == null || !client.Connected)
                    {
                        Console.WriteLine($"SOCKET CLOSED: Removing {client} from {GetType().Name} on port {Port}. SocketConnected:{client?.Connected} ClientObjectConnected:{client.ClientObject?.IsConnected} TimeSinceLastEcho:{(DateTime.UtcNow - client.ClientObject?.UtcLastEcho)}");
                        removeQueue.Enqueue(client);
                    }
                    else if (client.ClientObject != null && client.ClientObject.Timedout)
                    {
                        Console.WriteLine($"TIMEOUT: Removing {client} from {GetType().Name} on port {Port}. SocketConnected:{client?.Connected} ClientObjectConnected:{client.ClientObject?.IsConnected} TimeSinceLastEcho:{(DateTime.UtcNow - client.ClientObject?.UtcLastEcho)}");
                        removeQueue.Enqueue(client);
                    }
                    else
                    {
                        try
                        {
                            // Receive
                            Read(client);

                            // Tick
                            Tick(client);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Unhandled exception, closing client socket {client}.\n{e}");

                            // close the socket
                            client.Close();

                            // Add to remove queue
                            removeQueue.Enqueue(client);
                        }
                    }
                }
            }


            // Remove disconnected clients from collection
            lock (Clients)
            {
                while (removeQueue.Count > 0)
                {
                    var client = removeQueue.Dequeue();
                    client.Close();
                    Clients.Remove(client);
                }
            }
        }

        protected virtual void Tick(ClientSocket client)
        {

        }

        protected virtual void Echo(ClientSocket client, ref List<BaseMessage> responses)
        {
            responses.Add(new RT_MSG_SERVER_ECHO() { });
        }

        protected virtual int HandleCommand(BaseMessage message, ClientSocket client, ref List<BaseMessage> responses)
        {
            return 0;
        }
    }
}
