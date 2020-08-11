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
    public class DMEServer : IMediusComponent
    {
        public class UdpClientObject
        {
            public IPEndPoint EndPoint;
            public ClientObject Client;
        }

        private static ushort PortCounter = 0;

        private int _port = 0;
        public int Port => _port;

        protected Queue<BaseMessage> _queue = new Queue<BaseMessage>();
        protected PS2_RC4 _sessionCipher = null;
        protected List<UdpClientObject> _clients = new List<UdpClientObject>();

        private Game _game = null;
        private UDPSocket _udpServer = new UDPSocket();

        public DMEServer(Game game)
        {
            _sessionCipher = new PS2_RC4(Utils.FromString(Program.KEY), CipherContext.RC_CLIENT_SESSION);
            _game = game;
            _port = Program.Settings.DmeServerPortStart + PortCounter++;
        }


        public void OnReceive(IPEndPoint source, byte[] buffer)
        {
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
                    msg.Source = source;
                    _queue.Enqueue(msg);
                }
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
            List<BaseMessage> recv = new List<BaseMessage>();
            List<BaseMessage> responses = new List<BaseMessage>();

            // 
            _udpServer.ReadAvailable();

            lock (_queue)
            {
                while (_queue.Count > 0)
                    recv.Add(_queue.Dequeue());
            }

            // 
            foreach (var msg in recv)
                HandleCommand(msg, ref responses);

            // 
            foreach (var msg in responses)
            {
                msg.Serialize(out var sMsgs);
                foreach (var sMsg in sMsgs)
                    foreach (var target in msg.Targets)
                        _udpServer.Send(target, sMsg);
            }
        }

        private void HandleCommand(BaseMessage message, ref List<BaseMessage> responses)
        {
            // 
            var client = _clients.FirstOrDefault(x => x.EndPoint.ToString() == message.Source.ToString());

            // 
            if (message.Id != RT_MSG_TYPE.RT_MSG_CLIENT_ECHO)
                Console.WriteLine($"DME {client?.Client?.ClientAccount?.AccountName}: " + message.ToString());


            switch (message.Id)
            {
                case RT_MSG_TYPE.RT_MSG_CLIENT_APP_BROADCAST:
                    {

                        break;
                    }
                case RT_MSG_TYPE.RT_MSG_CLIENT_APP_LIST:
                    {
                        break;
                    }
                case RT_MSG_TYPE.RT_MSG_CLIENT_APP_SINGLE:
                    {

                        break;
                    }
                case RT_MSG_TYPE.RT_MSG_CLIENT_CONNECT_AUX_UDP:
                    {
                        if (client != null)
                            break;

                        var msg = message as RT_MSG_CLIENT_CONNECT_AUX_UDP;

                        _clients.Add(client = new UdpClientObject()
                        {
                            Client = _game.Clients.FirstOrDefault(x => x.GameId == msg.UNK_25)?.Client,
                            EndPoint = message.Source
                        });

                        responses.Add(new RT_MSG_SERVER_CONNECT_ACCEPT_AUX_UDP()
                        {
                            Targets = new IPEndPoint[] { message.Source },
                            UNK_00 = msg.UNK_22,
                            UNK_01 = msg.UNK_23,
                            UNK_02 = msg.UNK_24,
                            UNK_03 = msg.UNK_25,
                            UNK_04 = msg.UNK_26,
                            UNK_05 = msg.UNK_27,
                            UNK_06 = 0x0001,
                            EndPoint = msg.Source
                        });

                        //responses.Add(new RawMessage(RT_MSG_TYPE.RT_MSG_CLIENT_APP_SINGLE)
                        //{
                        //    Targets = new IPEndPoint[] { msg.Source },
                        //    Contents = Utils.FromString("00 00 00 01 D7 71 09 00 00 01 00 00 00 01 D7 71 09 00 01 01 00 00 00 01 D7 71 09 00 02 01 00 00".Replace(" ", ""))
                        //});

                        //client.Client.AddProxyMessage(new RT_MSG_SERVER_CONNECT_COMPLETE()
                        //{
                        //    Targets = new IPEndPoint[] { message.Source },
                        //    ARG1 = 0x0001
                        //});
                        break;
                    }
                case RT_MSG_TYPE.RT_MSG_CLIENT_ECHO:
                    {
                        responses.Add(new RT_MSG_CLIENT_ECHO()
                        {
                            Targets = new IPEndPoint[] { message.Source },
                            Value = (message as RT_MSG_CLIENT_ECHO).Value
                        });
                        break;
                    }
                default:
                    {
                        Console.WriteLine($"DME Unhandled Medius Command: {message.Id} {message}");
                        break;
                    }
            }
        }
    }
}
