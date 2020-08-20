using Deadlocked.Server.Accounts;
using Deadlocked.Server.Medius.Models.Packets;
using Deadlocked.Server.Medius.Models.Packets.Lobby;
using Deadlocked.Server.Medius.Models.Packets.MGCL;
using Deadlocked.Server.SCERT.Models;
using Deadlocked.Server.SCERT.Models.Packets;
using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;
using Medius.Crypto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Deadlocked.Server.Medius
{
    public class MAS : BaseMediusComponent
    {
        static readonly IInternalLogger _logger = InternalLoggerFactory.GetInstance<MAS>();

        protected override IInternalLogger Logger => _logger;
        public override string Name => "MAS";
        public override int Port => Program.Settings.MASPort;

        public override PS2_RSA AuthKey => Program.GlobalAuthKey;

        public MAS()
        {
            _sessionCipher = new PS2_RC4(Utils.FromString(Program.KEY), CipherContext.RC_CLIENT_SESSION);
        }

        protected override async Task ProcessMessage(BaseScertMessage message, IChannel clientChannel, ClientObject clientObject)
        {
            // 
            switch (message)
            {
                case RT_MSG_CLIENT_HELLO clientHello:
                    {
                        Queue(new RT_MSG_SERVER_HELLO(), clientObject);
                        break;
                    }
                case RT_MSG_CLIENT_CRYPTKEY_PUBLIC clientCryptKeyPublic:
                    {
                        Queue(new RT_MSG_SERVER_CRYPTKEY_PEER() { Key = Utils.FromString(Program.KEY) }, clientObject);
                        break;
                    }
                case RT_MSG_CLIENT_CONNECT_TCP clientConnectTcp:
                    {
                        Queue(new RT_MSG_SERVER_CONNECT_REQUIRE() { Contents = Utils.FromString("024802") }, clientObject);
                        break;
                    }
                case RT_MSG_CLIENT_CONNECT_READY_REQUIRE clientConnectReadyRequire:
                    {
                        Queue(new RT_MSG_SERVER_CRYPTKEY_GAME() { Key = Utils.FromString(Program.KEY) }, clientObject);
                        Queue(new RT_MSG_SERVER_CONNECT_ACCEPT_TCP()
                        {
                            UNK_00 = 0,
                            UNK_01 = 0,
                            UNK_02 = 0,
                            UNK_03 = 0,
                            UNK_04 = 0,
                            UNK_05 = 0,
                            UNK_06 = 0x0001,
                            IP = (clientChannel.RemoteAddress as IPEndPoint)?.Address
                        }, clientObject);
                        break;
                    }
                case RT_MSG_CLIENT_CONNECT_READY_TCP clientConnectReadyTcp:
                    {
                        Queue(new RT_MSG_SERVER_CONNECT_COMPLETE() { ARG1 = 0x0001 }, clientObject);
                        Queue(new RT_MSG_SERVER_ECHO(), clientObject);
                        break;
                    }
                case RT_MSG_SERVER_ECHO serverEchoReply:
                    {

                        break;
                    }
                case RT_MSG_CLIENT_ECHO clientEcho:
                    {
                        Queue(new RT_MSG_CLIENT_ECHO() { Value = clientEcho.Value }, clientObject);
                        break;
                    }
                case RT_MSG_CLIENT_APP_TOSERVER clientAppToServer:
                    {
                        ProcessMediusMessage(clientAppToServer.Message, clientChannel, clientObject);
                        break;
                    }

                case RT_MSG_CLIENT_DISCONNECT_WITH_REASON clientDisconnectWithReason:
                    {
                        await clientChannel.DisconnectAsync();
                        break;
                    }
                default:
                    {
                        Logger.Warn($"{Name} UNHANDLED MESSAGE: {message}");

                        break;
                    }
            }

            return;
        }

        protected virtual void ProcessMediusMessage(BaseMediusMessage message, IChannel clientChannel, ClientObject clientObject)
        {
            if (message == null)
                return;


            switch (message)
            {
                case MediusServerSessionBeginRequest mgclSessionBeginRequest:
                    {
                        // Create DME object
                        var dmeObject = new DMEObject(mgclSessionBeginRequest);
                        dmeObject.DotNettyId = clientChannel.Id.AsLongText();

                        // Replace client object with dme object
                        _nettyToClientObject.AddOrUpdate(dmeObject.DotNettyId, dmeObject, (key, oldValue) => dmeObject);

                        // Reply
                        Queue(new RT_MSG_SERVER_APP()
                        {
                            Message = new MediusServerSessionBeginResponse()
                            {
                                MessageID = mgclSessionBeginRequest.MessageID,
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
                                            new NetAddress() {Address = Program.SERVER_IP.ToString(), Port = (uint)Program.NATServer.Port, AddressType = NetAddressType.NetAddressTypeNATService},
                                        }
                                    },
                                    Type = NetConnectionType.NetConnectionTypeClientServerTCP
                                }
                            }
                        }, dmeObject);

                        break;
                    }
                case MediusServerAuthenticationRequest mgclAuthRequest:
                    {
                        if (clientObject is DMEObject dmeObject)
                        {
                            // 
                            dmeObject.SetIp(mgclAuthRequest.AddressList.AddressList[0].Address);

                            // Override the dme server ip
                            if (!string.IsNullOrEmpty(Program.Settings.DmeIpOverride))
                                dmeObject.SetIp(Program.Settings.DmeIpOverride);
                        }

                        // Reply
                        Queue(new MediusServerAuthenticationResponse()
                        {
                            MessageID = mgclAuthRequest.MessageID,
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
                                        new NetAddress() {Address = Program.SERVER_IP.ToString(), Port = (uint)Program.NATServer.Port, AddressType = NetAddressType.NetAddressTypeNATService},
                                    }
                                },
                                Type = NetConnectionType.NetConnectionTypeClientServerTCP
                            }
                        }, clientObject);
                        break;
                    }
                case MediusServerSetAttributesRequest mgclSetAttrRequest:
                    {
                        // Reply with success
                        Queue(new MediusServerSetAttributesResponse()
                        {
                            MessageID = mgclSetAttrRequest.MessageID,
                            Confirmation = MGCL_ERROR_CODE.MGCL_SUCCESS
                        }, clientObject);
                        break;
                    }


                case MediusExtendedSessionBeginRequest extendedSessionBeginRequest:
                    {
                        Queue(new MediusSessionBeginResponse()
                        {
                            MessageID = extendedSessionBeginRequest.MessageID,
                            SessionKey = Program.GenerateSessionKey(),
                            StatusCode = MediusCallbackStatus.MediusSuccess
                        }, clientObject);
                        break;
                    }
                case MediusSessionBeginRequest sessionBeginRequest:
                    {
                        Queue(new MediusSessionBeginResponse()
                        {
                            MessageID = sessionBeginRequest.MessageID,
                            SessionKey = Program.GenerateSessionKey(),
                            StatusCode = MediusCallbackStatus.MediusSuccess
                        }, clientObject);
                        break;
                    }
                case MediusSessionEndRequest sessionEndRequest:
                    {
                        Queue(new MediusSessionEndResponse()
                        {
                            MessageID = sessionEndRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                        }, clientObject);
                        break;
                    }
                case MediusSetLocalizationParamsRequest setLocalizationParamsRequest:
                    {
                        Queue(new MediusSetLocalizationParamsResponse()
                        {
                            MessageID = setLocalizationParamsRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess
                        }, clientObject);
                        break;
                    }
                case MediusDnasSignaturePost dnasSignaturePost:
                    {

                        break;
                    }
                case MediusAccountRegistrationRequest accountRegRequest:
                    {
                        // Ensure account doesn't already exist
                        if (Program.Database.TryGetAccountByName(accountRegRequest.AccountName, out var account))
                        {
                            Queue(new MediusAccountRegistrationResponse()
                            {
                                MessageID = accountRegRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusAccountAlreadyExists
                            }, clientObject);
                        }
                        else
                        {
                            // Create new account
                            account = new Account()
                            {
                                AccountName = accountRegRequest.AccountName,
                                AccountPassword = accountRegRequest.Password
                            };

                            // Add to collection
                            Program.Database.AddAccount(account);

                            // Reply with account id
                            Queue(new MediusAccountRegistrationResponse()
                            {
                                MessageID = accountRegRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                AccountID = account.AccountId
                            }, clientObject);
                        }
                        break;
                    }
                case MediusAccountGetIDRequest accountGetIdRequest:
                    {
                        int? accountId = null;

                        // Try to grab account id
                        if (Program.Database.TryGetAccountByName(accountGetIdRequest.AccountName, out var account))
                            accountId = account.AccountId;

                        // Return id
                        Queue(new MediusAccountGetIDResponse()
                        {
                            MessageID = accountGetIdRequest.MessageID,
                            AccountID = accountId ?? 0,
                            StatusCode = accountId.HasValue ? MediusCallbackStatus.MediusSuccess : MediusCallbackStatus.MediusAccountNotFound
                        }, clientObject);

                        break;
                    }
                case MediusAccountDeleteRequest accountDeleteRequest:
                    {
                        // 
                        var status = MediusCallbackStatus.MediusFail;

                        // 
                        var account = clientObject.ClientAccount;

                        // Ensure logged in
                        if (account != null)
                        {
                            // Double check password
                            if (account.AccountPassword == accountDeleteRequest.MasterPassword)
                            {
                                // Delete
                                Program.Database.DeleteAccount(account);
                                clientObject.Logout();
                                status = MediusCallbackStatus.MediusSuccess;
                            }
                        }

                        Queue(new MediusAccountDeleteResponse()
                        {
                            MessageID = accountDeleteRequest.MessageID,
                            StatusCode = status
                        }, clientObject);

                        Console.WriteLine($"Delete account {account?.AccountName} {status}");

                        break;
                    }
                case MediusAnonymousLoginRequest anonymousLoginRequest:
                    {
                        Queue(new MediusAnonymousLoginResponse()
                        {
                            MessageID = anonymousLoginRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                            AccountID = -1,
                            AccountType = MediusAccountType.MediusMasterAccount,
                            MediusWorldID = Program.Settings.DefaultChannelId,
                            ConnectInfo = new NetConnectionInfo()
                            {
                                SessionKey = anonymousLoginRequest.SessionKey,
                                WorldID = 0,
                                ServerKey = Program.GlobalAuthPublic,
                                AddressList = new NetAddressList()
                                {
                                    AddressList = new NetAddress[MediusConstants.NET_ADDRESS_LIST_COUNT]
                                    {
                                        new NetAddress() {Address = Program.SERVER_IP.ToString(), Port = (uint)Program.LobbyServer.Port, AddressType = NetAddressType.NetAddressTypeExternal},
                                        new NetAddress() {Address = Program.SERVER_IP.ToString(), Port = (uint)Program.NATServer.Port, AddressType = NetAddressType.NetAddressTypeNATService},
                                    }
                                },
                                Type = NetConnectionType.NetConnectionTypeClientServerTCP
                            }
                        }, clientObject);

                        break;
                    }
                case MediusAccountLoginRequest accountLoginRequest:
                    {
                        Console.WriteLine($"LOGIN REQUEST: {accountLoginRequest}");

                        // Find account
                        if (!Program.Database.TryGetAccountByName(accountLoginRequest.Username, out var account))
                        {
                            Queue(new MediusAccountLoginResponse()
                            {
                                MessageID = accountLoginRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusAccountNotFound,
                            }, clientObject);
                        }

                        // Check client isn't already logged in
                        else if (account.IsLoggedIn)
                        {
                            Queue(new MediusAccountLoginResponse()
                            {
                                MessageID = accountLoginRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusAccountLoggedIn
                            }, clientObject);
                        }
                        else if (account.AccountPassword != accountLoginRequest.Password)
                        {
                            Queue(new MediusAccountLoginResponse()
                            {
                                MessageID = accountLoginRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusInvalidPassword
                            }, clientObject);
                        }
                        else
                        {
                            //
                            clientObject.Login(account);
                            clientObject.Status = MediusPlayerStatus.MediusPlayerInAuthWorld;

                            // 
                            Console.WriteLine($"LOGGING IN AS {account.AccountName} with access token {clientObject.Token}");

                            // Send patches
                            if (Program.Settings.Patches != null)
                                foreach (var patch in Program.Settings.Patches)
                                    if (patch.Enabled && patch.ApplicationId == clientObject.ApplicationId)
                                        Queue(patch.Serialize(), clientObject);

                            // Tell client
                            Queue(new MediusAccountLoginResponse()
                            {
                                MessageID = accountLoginRequest.MessageID,
                                AccountID = account.AccountId,
                                AccountType = MediusAccountType.MediusMasterAccount,
                                ConnectInfo = new NetConnectionInfo()
                                {
                                    AccessKey = clientObject.Token,
                                    SessionKey = accountLoginRequest.SessionKey,
                                    WorldID = Program.Settings.DefaultChannelId,
                                    ServerKey = Program.GlobalAuthPublic,
                                    AddressList = new NetAddressList()
                                    {
                                        AddressList = new NetAddress[MediusConstants.NET_ADDRESS_LIST_COUNT]
                                        {
                                            new NetAddress() {Address = Program.SERVER_IP.ToString(), Port = (uint)Program.LobbyServer.Port, AddressType = NetAddressType.NetAddressTypeExternal},
                                            new NetAddress() {Address = Program.SERVER_IP.ToString(), Port = (uint)Program.NATServer.Port, AddressType = NetAddressType.NetAddressTypeNATService},
                                        }
                                    },
                                    Type = NetConnectionType.NetConnectionTypeClientServerTCP
                                },
                                MediusWorldID = Program.Settings.DefaultChannelId,
                                StatusCode = MediusCallbackStatus.MediusSuccess
                            }, clientObject);
                        }
                        break;
                    }
                case MediusAccountLogoutRequest accountLogoutRequest:
                    {
                        MediusCallbackStatus result = MediusCallbackStatus.MediusFail;

                        // Check token
                        if (clientObject.ClientAccount != null && accountLogoutRequest.SessionKey == clientObject.SessionKey)
                        {
                            // 
                            result = MediusCallbackStatus.MediusSuccess;

                            // Logout
                            clientObject.Logout();
                        }

                        Queue(new MediusAccountLogoutResponse()
                        {
                            MessageID = accountLogoutRequest.MessageID,
                            StatusCode = result
                        }, clientObject);
                        break;
                    }
                case MediusTextFilterRequest textFilterRequest:
                    {
                        // Accept everything
                        // No filter
                        Queue(new MediusTextFilterResponse()
                        {
                            MessageID = textFilterRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                            Text = textFilterRequest.Text
                        }, clientObject);

                        break;
                    }
                case MediusGetBuddyList_ExtraInfoRequest getBuddyList_ExtraInfoRequest:
                    {
                        Queue(new MediusGetBuddyList_ExtraInfoResponse()
                        {
                            MessageID = getBuddyList_ExtraInfoRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusNoResult,
                            EndOfList = true
                        }, clientObject);

                        break;
                    }

                default:
                    {
                        Logger.Warn($"{Name} Unhandled Medius Message: {message}");
                        break;
                    }
            }
        }

    }
}
