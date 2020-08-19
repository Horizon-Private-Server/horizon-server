using Deadlocked.Server.Accounts;
using Deadlocked.Server.Messages;
using Deadlocked.Server.Messages.Lobby;
using Deadlocked.Server.Messages.MGCL;
using Deadlocked.Server.Messages.RTIME;
using Medius.Crypto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;

namespace Deadlocked.Server.Medius
{
    public class MAS : BaseMediusComponent
    {
        public override int Port => Program.Settings.MASPort;

        public override PS2_RSA AuthKey => Program.GlobalAuthKey;

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
            // Log if id is set
            if (Program.Settings.IsLog(message.Id))
                Console.WriteLine($"MAS {client}: {message}");

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
                        var msg = message as RT_MSG_CLIENT_CONNECT_TCP;

                        // Set app id of client
                        client.ApplicationId = msg.AppId;

                        responses.Add(new RT_MSG_SERVER_CONNECT_REQUIRE() { Contents = Utils.FromString("024802") });
                        break;
                    }
                case RT_MSG_TYPE.RT_MSG_CLIENT_CONNECT_READY_REQUIRE:
                    {
                        responses.Add(new RT_MSG_SERVER_CRYPTKEY_GAME() { Key = Utils.FromString(Program.KEY) });
                        responses.Add(new RT_MSG_SERVER_CONNECT_ACCEPT_TCP()
                        {
                            UNK_02 = 0x00,
                            UNK_03 = 0x00,
                            IP = (client.RemoteEndPoint as IPEndPoint)?.Address
                        });
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
                            // 
                            case MediusAppPacketIds.MediusServerSessionBeginRequest:
                                {
                                    var msg = appMsg as MediusServerSessionBeginRequest;

                                    // Create DME object
                                    var clientObject = new DMEObject(msg);
                                    Program.Clients.Add(clientObject);
                                    client.SetToken(clientObject.Token);
                                    client.ApplicationId = msg.ApplicationID;

                                    responses.Add(new RT_MSG_SERVER_APP()
                                    {
                                        AppMessage = new MediusServerSessionBeginResponse()
                                        {
                                            MessageID = msg.MessageID,
                                            Confirmation = MGCL_ERROR_CODE.MGCL_SUCCESS,
                                            ConnectInfo = new NetConnectionInfo()
                                            {
                                                AccessKey = clientObject.Token,
                                                SessionKey = clientObject.SessionKey,
                                                WorldID = 0,
                                                ServerKey = Program.GlobalAuthPublic,
                                                AddressList = new NetAddressList()
                                                {
                                                    AddressList = new NetAddress[MediusConstants.NET_ADDRESS_LIST_COUNT]
                                                        {
                                                            new NetAddress() {Address = Program.SERVER_IP.ToString(), Port = (uint)Program.ProxyServer.Port, AddressType = NetAddressType.NetAddressTypeExternal},
                                                            //new NetAddress() { AddressType = NetAddressType.NetAddressNone }
                                                            new NetAddress() {Address = Program.SERVER_IP.ToString(), Port = (uint)Program.NATServer.Port, AddressType = NetAddressType.NetAddressTypeNATService},
                                                        }
                                                },
                                                Type = NetConnectionType.NetConnectionTypeClientServerTCP
                                            }
                                        }
                                    });
                                    break;
                                }
                            case MediusAppPacketIds.MediusServerAuthenticationRequest:
                                {
                                    var msg = appMsg as MediusServerAuthenticationRequest;


                                    if (client.Client is DMEObject dmeObject)
                                    {
                                        // 
                                        dmeObject.SetIp(msg.AddressList.AddressList[0].Address);

                                        // Override the dme server ip
                                        if (!string.IsNullOrEmpty(Program.Settings.DmeIpOverride))
                                            dmeObject.SetIp(Program.Settings.DmeIpOverride);
                                    }

                                    responses.Add(new RT_MSG_SERVER_APP()
                                    {
                                        AppMessage = new MediusServerAuthenticationResponse()
                                        {
                                            MessageID = msg.MessageID,
                                            Confirmation = MGCL_ERROR_CODE.MGCL_SUCCESS,
                                            ConnectInfo = new NetConnectionInfo()
                                            {
                                                AccessKey = client.Client.Token,
                                                SessionKey = client.Client.SessionKey,
                                                WorldID = 0,
                                                ServerKey = Program.GlobalAuthPublic,
                                                AddressList = new NetAddressList()
                                                {
                                                    AddressList = new NetAddress[MediusConstants.NET_ADDRESS_LIST_COUNT]
                                                        {
                                                            new NetAddress() {Address = Program.SERVER_IP.ToString(), Port = (uint)Program.ProxyServer.Port, AddressType = NetAddressType.NetAddressTypeExternal},
                                                            //new NetAddress() { AddressType = NetAddressType.NetAddressNone }
                                                            new NetAddress() {Address = Program.SERVER_IP.ToString(), Port = (uint)Program.NATServer.Port, AddressType = NetAddressType.NetAddressTypeNATService},
                                                        }
                                                },
                                                Type = NetConnectionType.NetConnectionTypeClientServerTCP
                                            }
                                        }
                                    });

                                    break;
                                }
                            case MediusAppPacketIds.MediusServerSetAttributesRequest:
                                {
                                    var msg = appMsg as MediusServerSetAttributesRequest;
                                    
                                    responses.Add(new RT_MSG_SERVER_APP()
                                    {
                                        AppMessage = new MediusServerSetAttributesResponse()
                                        {
                                            MessageID = msg.MessageID,
                                            Confirmation = MGCL_ERROR_CODE.MGCL_SUCCESS
                                        }
                                    });

                                    break;
                                }


                            // 
                            case MediusAppPacketIds.SessionBegin:
                                {
                                    var msg = appMsg as MediusSessionBeginRequest;

                                    responses.Add(new RT_MSG_SERVER_APP()
                                    {
                                        AppMessage = new MediusSessionBeginResponse()
                                        {
                                            MessageID = msg.MessageID,
                                            SessionKey = Program.GenerateSessionKey(),
                                            StatusCode = MediusCallbackStatus.MediusSuccess
                                        }
                                    });
                                    break;
                                }
                            case MediusAppPacketIds.SessionEnd:
                                {
                                    var msg = appMsg as MediusSessionEndRequest;

                                    responses.Add(new RT_MSG_SERVER_APP()
                                    {
                                        AppMessage = new MediusSessionEndResponse()
                                        {
                                            MessageID = msg.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusSuccess,
                                        }
                                    });
                                    break;
                                }
                            case MediusAppPacketIds.ExtendedSessionBeginRequest:
                                {
                                    var sessionBeginMsg = appMsg as MediusExtendedSessionBeginRequest;

                                    responses.Add(new RT_MSG_SERVER_APP()
                                    {
                                        AppMessage = new MediusSessionBeginResponse()
                                        {
                                            MessageID = sessionBeginMsg.MessageID,
                                            SessionKey = Program.GenerateSessionKey(),
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
                            case MediusAppPacketIds.DnasSignaturePost:
                                {
                                    var msg = appMsg as MediusDnasSignaturePost;
                                    break;
                                }
                            case MediusAppPacketIds.AccountRegistration:
                                {
                                    var msg = appMsg as MediusAccountRegistrationRequest;

                                    // Ensure account doesn't already exist
                                    if (Program.Database.TryGetAccountByName(msg.AccountName, out var account))
                                    {
                                        responses.Add(new RT_MSG_SERVER_APP()
                                        {
                                            AppMessage = new MediusAccountRegistrationResponse()
                                            {
                                                MessageID = msg.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusAccountAlreadyExists
                                            }
                                        });
                                    }
                                    else
                                    {
                                        // Create new account
                                        account = new Account()
                                        {
                                            AccountName = msg.AccountName,
                                            AccountPassword = msg.Password
                                        };

                                        // Add to collection
                                        Program.Database.AddAccount(account);

                                        // Reply with account id
                                        responses.Add(new RT_MSG_SERVER_APP()
                                        {
                                            AppMessage = new MediusAccountRegistrationResponse()
                                            {
                                                MessageID = msg.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                                AccountID = account.AccountId
                                            }
                                        });
                                    }
                                    break;
                                }
                            case MediusAppPacketIds.AccountGetID:
                                {
                                    var msg = appMsg as MediusAccountGetIDRequest;
                                    int? accountId = null;

                                    // Try to grab account id
                                    if (Program.Database.TryGetAccountByName(msg.AccountName, out var account))
                                        accountId = account.AccountId;

                                    // Return id
                                    responses.Add(new RT_MSG_SERVER_APP()
                                    {
                                        AppMessage = new MediusAccountGetIDResponse()
                                        {
                                            MessageID = msg.MessageID,
                                            AccountID = accountId ?? 0,
                                            StatusCode = accountId.HasValue ? MediusCallbackStatus.MediusSuccess : MediusCallbackStatus.MediusAccountNotFound
                                        }
                                    });

                                    break;
                                }
                            case MediusAppPacketIds.AccountDelete:
                                {
                                    var msg = appMsg as MediusAccountDeleteRequest;
                                    var status = MediusCallbackStatus.MediusFail;

                                    // 
                                    var account = client.Client?.ClientAccount;

                                    // Ensure logged in
                                    if (account != null)
                                    {
                                        // Double check password
                                        if (account.AccountPassword == msg.MasterPassword)
                                        {
                                            // Delete
                                            Program.Database.DeleteAccount(account);
                                            client.Client.Logout();
                                            status = MediusCallbackStatus.MediusSuccess;
                                        }
                                    }

                                    responses.Add(new RT_MSG_SERVER_APP()
                                    {
                                        AppMessage = new MediusAccountDeleteResponse()
                                        {
                                            MessageID = msg.MessageID,
                                            StatusCode = status
                                        }
                                    });

                                    Console.WriteLine($"Delete account {account?.AccountName} {status}");

                                    break;
                                }
                            case MediusAppPacketIds.AnonymousLogin:
                                {
                                    var msg = appMsg as MediusAnonymousLoginRequest;

                                    responses.Add(new RT_MSG_SERVER_APP()
                                    {
                                        AppMessage = new MediusAnonymousLoginResponse()
                                        {
                                            MessageID = msg.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusSuccess,
                                            AccountID = -1,
                                            AccountType = MediusAccountType.MediusMasterAccount,
                                            MediusWorldID = Program.Settings.DefaultChannelId,
                                            ConnectInfo = new NetConnectionInfo()
                                            {
                                                SessionKey = msg.SessionKey,
                                                WorldID = 0,
                                                ServerKey = Program.GlobalAuthPublic,
                                                AddressList = new NetAddressList()
                                                {
                                                    AddressList = new NetAddress[MediusConstants.NET_ADDRESS_LIST_COUNT]
                                                        {
                                                            new NetAddress() {Address = Program.SERVER_IP.ToString(), Port = (uint)Program.LobbyServer.Port, AddressType = NetAddressType.NetAddressTypeExternal},
                                                            //new NetAddress() { AddressType = NetAddressType.NetAddressNone}
                                                            new NetAddress() {Address = Program.SERVER_IP.ToString(), Port = (uint)Program.NATServer.Port, AddressType = NetAddressType.NetAddressTypeNATService},
                                                        }
                                                },
                                                Type = NetConnectionType.NetConnectionTypeClientServerTCP
                                            }
                                        }
                                    });

                                    break;
                                }
                            case MediusAppPacketIds.AccountLogin:
                                {
                                    var loginMsg = appMsg as MediusAccountLoginRequest;
                                    Console.WriteLine($"LOGIN REQUEST: {loginMsg}");

                                    // Find account
                                    if (!Program.Database.TryGetAccountByName(loginMsg.Username, out var account))
                                    {
                                        responses.Add(new RT_MSG_SERVER_APP()
                                        {
                                            AppMessage = new MediusAccountLoginResponse()
                                            {
                                                MessageID = loginMsg.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusAccountNotFound,
                                            }
                                        });
                                    }

                                    // Check client isn't already logged in
                                    else if (account.IsLoggedIn)
                                    {
                                        responses.Add(new RT_MSG_SERVER_APP()
                                        {
                                            AppMessage = new MediusAccountLoginResponse()
                                            {
                                                MessageID = loginMsg.MessageID,
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
                                                MessageID = loginMsg.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusInvalidPassword
                                            }
                                        });
                                    }
                                    else
                                    {
                                        // 
                                        var clientObject = new ClientObject(account, client.ApplicationId, loginMsg.SessionKey);
                                        clientObject.Status = MediusPlayerStatus.MediusPlayerInAuthWorld;

                                        // 
                                        Program.Clients.Add(clientObject);

                                        // 
                                        client.SetToken(clientObject.Token);

                                        // 
                                        Console.WriteLine($"LOGGING IN AS {account.AccountName} with access token {clientObject.Token}");

                                        // Send cheats
                                        if (Program.Settings.Patches != null)
                                            foreach (var patch in Program.Settings.Patches)
                                                if (patch.Enabled && patch.ApplicationId == client.ApplicationId)
                                                    responses.AddRange(patch.Serialize());

                                        // Tell client
                                        responses.Add(new RT_MSG_SERVER_APP() 
                                        {
                                            AppMessage = new MediusAccountLoginResponse()
                                            {
                                                MessageID = loginMsg.MessageID,
                                                AccountID = account.AccountId,
                                                AccountType = MediusAccountType.MediusMasterAccount,
                                                ConnectInfo = new NetConnectionInfo()
                                                {
                                                    AccessKey = clientObject.Token,
                                                    SessionKey = loginMsg.SessionKey,
                                                    WorldID = Program.Settings.DefaultChannelId,
                                                    ServerKey = Program.GlobalAuthPublic,
                                                    AddressList = new NetAddressList()
                                                    {
                                                        AddressList = new NetAddress[MediusConstants.NET_ADDRESS_LIST_COUNT]
                                                        {
                                                            new NetAddress() {Address = Program.SERVER_IP.ToString(), Port = (uint)Program.LobbyServer.Port, AddressType = NetAddressType.NetAddressTypeExternal},
                                                            //new NetAddress() { AddressType = NetAddressType.NetAddressNone}
                                                            new NetAddress() {Address = Program.SERVER_IP.ToString(), Port = (uint)Program.NATServer.Port, AddressType = NetAddressType.NetAddressTypeNATService},
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
                            case MediusAppPacketIds.AccountLogout:
                                {
                                    var msg = appMsg as MediusAccountLogoutRequest;
                                    bool success = false;

                                    // Check token
                                    if (client.Client != null && client.Client.ClientAccount != null && msg.SessionKey == client.Client.SessionKey)
                                    {
                                        success = true;

                                        // Logout
                                        client.Client.Logout();
                                    }

                                    responses.Add(new RT_MSG_SERVER_APP()
                                    {
                                        AppMessage = new MediusAccountLogoutResponse()
                                        {
                                            StatusCode = success ? MediusCallbackStatus.MediusSuccess : MediusCallbackStatus.MediusFail
                                        }
                                    });
                                    break;
                                }
                            case MediusAppPacketIds.TextFilter:
                                {
                                    var msg = appMsg as MediusTextFilterRequest;

                                    // Accept everything
                                    // No filter
                                    responses.Add(new RT_MSG_SERVER_APP()
                                    {
                                        AppMessage = new MediusTextFilterResponse()
                                        {
                                            MessageID = msg.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusSuccess,
                                            Text = msg.Text
                                        }
                                    });

                                    break;
                                }
                            case MediusAppPacketIds.GetBuddyList_ExtraInfo:
                                {
                                    var msg = appMsg as MediusGetBuddyList_ExtraInfoRequest;

                                    responses.Add(new RT_MSG_SERVER_APP()
                                    {
                                        AppMessage = new MediusGetBuddyList_ExtraInfoResponse()
                                        {
                                            MessageID = msg.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusNoResult,
                                            EndOfList = true
                                        }
                                    });

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
