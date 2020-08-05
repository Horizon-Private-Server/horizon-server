using Deadlocked.Server.Messages;
using Deadlocked.Server.Messages.App;
using Deadlocked.Server.Messages.RTIME;
using Medius.Crypto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Deadlocked.Server.Medius
{
    public class MUIS : BaseMediusComponent
    {
        public override int Port => 10071;

        public MUIS()
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

            responses.Send(client);
        }

        protected override int HandleCommand(BaseMessage message, ClientSocket client, ref List<BaseMessage> responses)
        {
            List<BaseMessage> msgs = null;

            // 
            Console.WriteLine(message.ToString());

            // 
            switch (message.Id)
            {
                case RT_MSG_TYPE.RT_MSG_CLIENT_HELLO: //Connecting 1

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
                        responses.Add(new RT_MSG_SERVER_CONNECT_ACCEPT_TCP() { IP = (client.RemoteEndPoint as IPEndPoint).Address });
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
                case RT_MSG_TYPE.RT_MSG_CLIENT_APP_TOSERVER:
                    {
                        var appMsg = (message as RT_MSG_CLIENT_APP_TOSERVER).AppMessage;

                        switch (appMsg.Id)
                        {
                            case MediusAppPacketIds.GetUniverseInformation:
                                {
                                    responses.Add(new RT_MSG_SERVER_APP() { AppMessage = new MediusUniverseVariableSvoURLResponse() { Result = 1 } });
                                    responses.Add(new RT_MSG_SERVER_APP() { AppMessage = new RawAppMessage(0x0200) { Contents = Utils.FromString("0411E8010200000000000000A602000000000000310000000000000000000000000000000000000000000000003C03000001000000526174636865743A20446561646C6F636B65642050726F64756374696F6E000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000072617463686574646C2D70726F642E70646F6E6C696E652E736365612E636F6D0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000005B270000526174636865743A20446561646C6F636B65642050726F64756374696F6E000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000") } });
                                    responses.Add(new RT_MSG_SERVER_APP() { AppMessage = new RawAppMessage(0x0200) { Contents = Utils.FromString("0411BE000200010000000000A6020000E801000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000001") } });
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
            }

            return 0;
        }
    }
}
