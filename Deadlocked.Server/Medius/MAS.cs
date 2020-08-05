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
    public class MAS : BaseMediusComponent
    {
        public override int Port => 10075;

        public MAS()
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

            // 
            foreach (var msg in recv)
                HandleCommand(msg, client, ref responses);

            // 
            if (shouldEcho)
                Echo(client, ref responses);

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
                        client.Client?.OnEcho(DateTime.UtcNow);
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
                            case MediusAppPacketIds.SetLocalizationParams:
                                {
                                    var localizationMsg = appMsg as MediusSetLocalizationParamsRequest;

                                    responses.Add(new RT_MSG_SERVER_APP()
                                    {
                                        AppMessage = new MediusSetLocalizationParamsResponse()
                                        {
                                            MessageID = localizationMsg.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusSuccess
                                        }
                                    });
                                    break;
                                }
                            case MediusAppPacketIds.AccountLogin:
                                {
                                    var loginMsg = appMsg as MediusAccountLoginRequest;
                                    Console.WriteLine($"LOGIN REQUEST: {loginMsg}");

                                    // Find account or create new
                                    if (!Program.Database.GetAccountByName(loginMsg.Username, out var account))
                                    {
                                        account = new Accounts.Account()
                                        {
                                            AccountName = loginMsg.Username,
                                            AccountPassword = loginMsg.Password
                                        };

                                        Program.Database.AddAccount(account);
                                    }

                                    // Check client isn't already logged in
                                    if (account.IsLoggedIn)
                                    {
                                        responses.Add(new RT_MSG_SERVER_APP()
                                        {
                                            AppMessage = new MediusAccountLoginResponse()
                                            {
                                                StatusCode = MediusCallbackStatus.MediusAccountLoggedIn
                                            }
                                        });
                                    }
                                    else if (account.AccountPassword != loginMsg.Password)
                                    {
                                        responses.Add(new RT_MSG_SERVER_APP()
                                        {
                                            AppMessage = new MediusAccountLoginResponse()
                                            {
                                                StatusCode = MediusCallbackStatus.MediusInvalidPassword
                                            }
                                        });
                                    }
                                    else
                                    {

                                        var clientObject = new ClientObject(account, loginMsg.SessionKey);
                                        Program.Clients.Add(clientObject);

                                        Console.WriteLine($"LOGGING IN AS {account.AccountName} with access token {clientObject.Token}");

                                        // Tell client
                                        responses.Add(new RT_MSG_SERVER_APP()
                                        {
                                            AppMessage = new MediusAccountLoginResponse()
                                            {
                                                AccountID = account.AccountId,
                                                AccountType = MediusAccountType.MediusMasterAccount,
                                                ConnectInfo = new NetConnectionInfo()
                                                {
                                                    AccessKey = clientObject.Token,
                                                    SessionKey = loginMsg.SessionKey,
                                                    WorldID = 0,
                                                    ServerKey = new RSA_KEY(Program.GlobalAuthKey.N.ToByteArrayUnsigned().Reverse().ToArray()),
                                                    AddressList = new NetAddressList()
                                                    {
                                                        AddressList = new  NetAddress[MediusConstants.NET_ADDRESS_LIST_COUNT]
                                                        {
                                                            new NetAddress() {Address = Program.SERVER_IP.ToString(), Port = (uint)Program.LobbyServer.Port, AddressType = NetAddressType.NetAddressTypeExternal},
                                                            new NetAddress() {Address = Program.SERVER_IP.ToString(), Port = (uint)10070, AddressType = NetAddressType.NetAddressTypeNATService},
                                                        }
                                                    },
                                                    Type = NetConnectionType.NetConnectionTypeClientServerTCP
                                                },
                                                MediusWorldID = Program.Settings.DefaultChannelId,
                                                StatusCode = MediusCallbackStatus.MediusSuccess
                                            }
                                        });
                                    }
                                    break;
                                }
                            default:
                                {
                                    Console.WriteLine($"MAS Unhandled App Message: {appMsg.Id} {appMsg}");
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
