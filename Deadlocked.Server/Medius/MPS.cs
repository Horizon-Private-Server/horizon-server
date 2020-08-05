using Deadlocked.Server.Messages;
using Deadlocked.Server.Messages.Lobby;
using Deadlocked.Server.Messages.RTIME;
using Medius.Crypto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Deadlocked.Server.Medius
{
    public class MPS : BaseMediusComponent
    {
        public override int Port => 10079;

        public MPS()
        {
            _sessionCipher = new PS2_RC4(Utils.FromString(Program.KEY), CipherContext.RC_CLIENT_SESSION);
        }

        protected override void Tick(ClientSocket client)
        {
            List<BaseMessage> recv = new List<BaseMessage>();
            List<BaseMessage> responses = new List<BaseMessage>();

            lock (_queue)
            {
                while (_queue.Count > 0)
                    recv.Add(_queue.Dequeue());
            }

            foreach (var msg in recv)
                HandleCommand(msg, client, ref responses);

            // 
            var targetMsgs = client.Client?.PullProxyMessages();
            if (targetMsgs != null && targetMsgs.Count > 0)
                responses.AddRange(targetMsgs);

            responses.Send(client);
        }

        protected override int HandleCommand(BaseMessage message, ClientSocket client, ref List<BaseMessage> responses)
        {
            List<BaseMessage> msgs = null;

            // 
            if (message.Id != RT_MSG_TYPE.RT_MSG_CLIENT_ECHO)
                Console.WriteLine(message.ToString());

            // 
            switch (message.Id)
            {
                case RT_MSG_TYPE.RT_MSG_CLIENT_HELLO:

                    responses.Add(new RT_MSG_SERVER_HELLO());

                    break;
                case RT_MSG_TYPE.RT_MSG_CLIENT_CRYPTKEY_PUBLIC:
                    {
                        responses.Add(new RT_MSG_SERVER_CRYPTKEY_PEER() { Key = Utils.FromString(Program.KEY) });
                        break;
                    }
                case RT_MSG_TYPE.RT_MSG_CLIENT_CONNECT_TCP:
                    {
                        responses.Add(new RT_MSG_SERVER_CONNECT_REQUIRE() { ARG1 = 0x02, ARG2 = 0x48, ARG3 = 0x02 });
                        break;
                    }
                case RT_MSG_TYPE.RT_MSG_CLIENT_CONNECT_READY_REQUIRE:
                    {
                        responses.Add(new RT_MSG_SERVER_CRYPTKEY_GAME() { Key = Utils.FromString(Program.KEY) });
                        responses.Add(new RT_MSG_SERVER_CONNECT_ACCEPT_TCP() { UNK_02 = 0x3326, IP = (client.RemoteEndPoint as IPEndPoint).Address });
                        break;
                    }
                case RT_MSG_TYPE.RT_MSG_CLIENT_CONNECT_READY_TCP:
                    {
                        responses.Add(new RT_MSG_SERVER_CONNECT_COMPLETE() { ARG1 = 0x0001 });
                        responses.Add(new RT_MSG_SERVER_ECHO());
                        break;
                    }
                case RT_MSG_TYPE.RT_MSG_SERVER_ECHO:
                    {

                        break;
                    }
                case RT_MSG_TYPE.RT_MSG_CLIENT_SET_RECV_FLAG:
                    {

                        break;
                    }
                case RT_MSG_TYPE.RT_MSG_CLIENT_APP_BROADCAST:
                    {
                        var msg = message as RT_MSG_CLIENT_APP_BROADCAST;

                        var game = Program.Games.FirstOrDefault(x => x.Clients.Contains(client.Client));
                        if (game != null)
                        {
                            foreach (var peer in game.Clients)
                            {
                                if (peer != client.Client)
                                {
                                    peer.AddProxyMessage(new RawMessage(RT_MSG_TYPE.RT_MSG_SERVER_APP)
                                    {
                                        Contents = msg.Contents
                                    });
                                }
                            }
                        }
                        break;
                    }
                case RT_MSG_TYPE.RT_MSG_CLIENT_TIMEBASE_QUERY:
                    {
                        var msg = message as RT_MSG_CLIENT_TIMEBASE_QUERY;
                        var game = Program.Games.FirstOrDefault(x => x.Clients.Contains(client.Client));

                        responses.Add(new RT_MSG_SERVER_TIMEBASE_QUERY_NOTIFY()
                        {
                            ClientTime = msg.Timestamp,
                            ServerTime = game?.Time ?? 0
                        });

                        break;
                    }
                case RT_MSG_TYPE.RT_MSG_CLIENT_APP_TOSERVER:
                    {

                        var appMsg = (message as RT_MSG_CLIENT_APP_TOSERVER).AppMessage;

                        switch (appMsg.Id)
                        {
                            case MediusAppPacketIds.ExtendedSessionBeginRequest:
                                {
                                    var sessionBeginMsg = appMsg as MediusExtendedSessionBeginRequest;

                                    responses.Add(new RT_MSG_SERVER_APP()
                                    {
                                        AppMessage = new MediusSessionBeginResponse()
                                        {
                                            MessageID = sessionBeginMsg.MessageID,
                                            SessionKey = "13088",
                                            StatusCode = MediusCallbackStatus.MediusSuccess
                                        }
                                    });
                                    break;
                                }
                            default:
                                {
                                    Console.WriteLine($"MPS Unhandled App Message: {appMsg.Id} {appMsg}");
                                    break;
                                }
                        }
                        break;
                    }

                case RT_MSG_TYPE.RT_MSG_CLIENT_ECHO:
                    {
                        responses.Add(new RT_MSG_CLIENT_ECHO() { Value = (message as RT_MSG_CLIENT_ECHO).Value });
                        break;
                    }
                case RT_MSG_TYPE.RT_MSG_CLIENT_DISCONNECT:
                case RT_MSG_TYPE.RT_MSG_CLIENT_DISCONNECT_WITH_REASON:
                    {
                        client.Disconnect();
                        break;
                    }
                default:
                    {
                        Console.WriteLine($"MPS Unhandled Medius Command: {message.Id} {message}");
                        break;
                    }
            }

            return 0;
        }
    }
}
