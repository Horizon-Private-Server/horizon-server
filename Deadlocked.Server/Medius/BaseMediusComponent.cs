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

        public static Random RNG = new Random();
        public abstract int Port { get; }


        public abstract PS2_RSA AuthKey { get; }

        protected Queue<BaseMessage> _queue = new Queue<BaseMessage>();
        protected PS2_RC4 _sessionCipher = null;
        protected TcpListener Listener = null;

        public List<ClientSocket> Clients = new List<ClientSocket>();

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
                        // Console.WriteLine($"Connection accepted on port {Port}.");
                        lock (Clients)
                        {
                            Clients.Add(client);
                        }
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

        protected void Read(ClientSocket client)
        {
            byte[] data = new byte[4096];
            int size = client.ReadAvailable(data);

            if (size > 0)
            {
                byte[] buffer = new byte[size];
                Array.Copy(data, 0, buffer, 0, size);

                try
                {
                    // Check for PING-PONG message
                    if (size == 4 && Encoding.UTF8.GetString(buffer) == "PING")
                    {
                        client.Send(Encoding.UTF8.GetBytes("PONG"));
                        client.Close();
                        return;
                    }

                    var msgs = BaseMessage.Instantiate(buffer, (id, context) =>
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
                    Console.WriteLine($"RECV BUFFER FROM {client}: {(buffer == null ? "<null>" : BitConverter.ToString(buffer))}");
                    throw e;
                }
            }
        }

        public void Tick()
        {
            // Determine if should echo
            if ((DateTime.UtcNow - timeLastEcho).TotalSeconds > Program.Settings.ServerEchoInterval)
                shouldEcho = true;

            // Collection of clients that have DC'd
            Queue<ClientSocket> removeQueue = new Queue<ClientSocket>();

            // Iterate through each
            // Run tick on each unless client disconnected
            lock (Clients)
            {
                foreach (var client in Clients)
                {
                    if (client == null || !client.Connected || (client.Client != null && !client.Client.IsConnected))
                    {
                        Console.WriteLine($"Removing {client} from {GetType().Name} on port {Port}. SocketConnected:{client?.Connected} ClientObjectConnected:{client.Client?.IsConnected} TimeSinceLastEcho:{(DateTime.UtcNow - client.Client?.UtcLastEcho)}");
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
