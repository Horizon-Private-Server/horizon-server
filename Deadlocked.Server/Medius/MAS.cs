using Deadlocked.Server.Accounts;
using Deadlocked.Server.Medius.Models;
using Deadlocked.Server.Medius.Models.Packets;
using Deadlocked.Server.Medius.Models.Packets.Lobby;
using Deadlocked.Server.Medius.Models.Packets.MGCL;
using Deadlocked.Server.SCERT;
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

        protected override async Task ProcessMessage(BaseScertMessage message, IChannel clientChannel, ChannelData data)
        {
            // 
            switch (message)
            {
                case RT_MSG_CLIENT_HELLO clientHello:
                    {
                        Queue(new RT_MSG_SERVER_HELLO(), clientChannel);
                        break;
                    }
                case RT_MSG_CLIENT_CRYPTKEY_PUBLIC clientCryptKeyPublic:
                    {
                        Queue(new RT_MSG_SERVER_CRYPTKEY_PEER() { Key = Utils.FromString(Program.KEY) }, clientChannel);
                        break;
                    }
                case RT_MSG_CLIENT_CONNECT_TCP clientConnectTcp:
                    {
                        data.ApplicationId = clientConnectTcp.AppId;
                        Queue(new RT_MSG_SERVER_CONNECT_REQUIRE() { Contents = Utils.FromString("024802") }, clientChannel);
                        break;
                    }
                case RT_MSG_CLIENT_CONNECT_READY_REQUIRE clientConnectReadyRequire:
                    {
                        Queue(new RT_MSG_SERVER_CRYPTKEY_GAME() { Key = Utils.FromString(Program.KEY) }, clientChannel);
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
                        }, clientChannel);
                        break;
                    }
                case RT_MSG_CLIENT_CONNECT_READY_TCP clientConnectReadyTcp:
                    {
                        Queue(new RT_MSG_SERVER_CONNECT_COMPLETE() { ARG1 = 0x0001 }, clientChannel);
                        Queue(new RT_MSG_SERVER_ECHO(), clientChannel);
                        break;
                    }
                case RT_MSG_SERVER_ECHO serverEchoReply:
                    {

                        break;
                    }
                case RT_MSG_CLIENT_ECHO clientEcho:
                    {
                        Queue(new RT_MSG_CLIENT_ECHO() { Value = clientEcho.Value }, clientChannel);
                        break;
                    }
                case RT_MSG_CLIENT_APP_TOSERVER clientAppToServer:
                    {
                        ProcessMediusMessage(clientAppToServer.Message, clientChannel, data);
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

        protected virtual void ProcessMediusMessage(BaseMediusMessage message, IChannel clientChannel, ChannelData data)
        {
            if (message == null)
                return;


            switch (message)
            {
                case MediusServerSessionBeginRequest mgclSessionBeginRequest:
                    {
                        // Create DME object
                        data.ClientObject = Program.ProxyServer.ReserveDMEObject(mgclSessionBeginRequest);

                        // Reply
                        data.ClientObject.Queue(new MediusServerSessionBeginResponse()
                        {
                            MessageID = mgclSessionBeginRequest.MessageID,
                            Confirmation = MGCL_ERROR_CODE.MGCL_SUCCESS,
                            ConnectInfo = new NetConnectionInfo()
                            {
                                AccessKey = data.ClientObject.Token,
                                SessionKey = data.ClientObject.SessionKey,
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
                        });

                        break;
                    }
                case MediusServerAuthenticationRequest mgclAuthRequest:
                    {
                        var dmeObject = data.ClientObject as DMEObject;
                        if (dmeObject == null)
                            throw new InvalidOperationException($"Non-DME Client sending MGCL messages.");

                        // 
                        dmeObject.SetIp(mgclAuthRequest.AddressList.AddressList[0].Address);

                        // Override the dme server ip
                        if (!string.IsNullOrEmpty(Program.Settings.DmeIpOverride))
                            dmeObject.SetIp(Program.Settings.DmeIpOverride);

                        // Reply
                        dmeObject.Queue(new MediusServerAuthenticationResponse()
                        {
                            MessageID = mgclAuthRequest.MessageID,
                            Confirmation = MGCL_ERROR_CODE.MGCL_SUCCESS,
                            ConnectInfo = new NetConnectionInfo()
                            {
                                AccessKey = dmeObject.Token,
                                SessionKey = dmeObject.SessionKey,
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
                        });
                        break;
                    }
                case MediusServerSetAttributesRequest mgclSetAttrRequest:
                    {
                        var dmeObject = data.ClientObject as DMEObject;
                        if (dmeObject == null)
                            throw new InvalidOperationException($"Non-DME Client sending MGCL messages.");

                        // Reply with success
                        dmeObject.Queue(new MediusServerSetAttributesResponse()
                        {
                            MessageID = mgclSetAttrRequest.MessageID,
                            Confirmation = MGCL_ERROR_CODE.MGCL_SUCCESS
                        });
                        break;
                    }

                case MediusExtendedSessionBeginRequest extendedSessionBeginRequest:
                    {
                        // Create client object
                        data.ClientObject = Program.LobbyServer.ReserveClient(extendedSessionBeginRequest);
                        data.ClientObject.ApplicationId = data.ApplicationId;

                        // Reply
                        data.ClientObject.Queue(new MediusSessionBeginResponse()
                        {
                            MessageID = extendedSessionBeginRequest.MessageID,
                            SessionKey = data.ClientObject.SessionKey,
                            StatusCode = MediusCallbackStatus.MediusSuccess
                        });
                        break;
                    }
                case MediusSessionBeginRequest sessionBeginRequest:
                    {
                        // Create client object
                        data.ClientObject = Program.LobbyServer.ReserveClient(sessionBeginRequest);
                        data.ClientObject.ApplicationId = data.ApplicationId;

                        // Reply
                        data.ClientObject.Queue(new MediusSessionBeginResponse()
                        {
                            MessageID = sessionBeginRequest.MessageID,
                            SessionKey = data.ClientObject.SessionKey,
                            StatusCode = MediusCallbackStatus.MediusSuccess
                        });
                        break;
                    }
                case MediusSessionEndRequest sessionEndRequest:
                    {
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} is trying to end session without an Client Object");

                        // Remove
                        Program.Clients.Remove(data.ClientObject);

                        // 
                        data.ClientObject = null;

                        Queue(new RT_MSG_SERVER_APP()
                        {
                            Message = new MediusSessionEndResponse()
                            {
                                MessageID = sessionEndRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                            }
                        }, clientChannel);
                        break;
                    }
                case MediusSetLocalizationParamsRequest setLocalizationParamsRequest:
                    {
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {setLocalizationParamsRequest} without a session.");

                        data.ClientObject.Queue(new MediusSetLocalizationParamsResponse()
                        {
                            MessageID = setLocalizationParamsRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess
                        });
                        break;
                    }
                case MediusDnasSignaturePost dnasSignaturePost:
                    {

                        break;
                    }
                case MediusAccountRegistrationRequest accountRegRequest:
                    {
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {accountRegRequest} without a session.");

                        // Ensure account doesn't already exist
                        if (Program.Database.TryGetAccountByName(accountRegRequest.AccountName, out var account))
                        {
                            data.ClientObject.Queue(new MediusAccountRegistrationResponse()
                            {
                                MessageID = accountRegRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusAccountAlreadyExists
                            });
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
                            data.ClientObject.Queue(new MediusAccountRegistrationResponse()
                            {
                                MessageID = accountRegRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                AccountID = account.AccountId
                            });
                        }
                        break;
                    }
                case MediusAccountGetIDRequest accountGetIdRequest:
                    {
                        int? accountId = null;

                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {accountGetIdRequest} without a session.");

                        // Try to grab account id
                        if (Program.Database.TryGetAccountByName(accountGetIdRequest.AccountName, out var account))
                            accountId = account.AccountId;

                        // Return id
                        data.ClientObject.Queue(new MediusAccountGetIDResponse()
                        {
                            MessageID = accountGetIdRequest.MessageID,
                            AccountID = accountId ?? 0,
                            StatusCode = accountId.HasValue ? MediusCallbackStatus.MediusSuccess : MediusCallbackStatus.MediusAccountNotFound
                        });

                        break;
                    }
                case MediusAccountDeleteRequest accountDeleteRequest:
                    {
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {accountDeleteRequest} without a session.");

                        // 
                        var status = MediusCallbackStatus.MediusFail;

                        // 
                        var account = data.ClientObject.ClientAccount;

                        // Ensure logged in
                        if (account != null)
                        {
                            // Double check password
                            if (account.AccountPassword == accountDeleteRequest.MasterPassword)
                            {
                                // Delete
                                Program.Database.DeleteAccount(account);
                                data.ClientObject.Logout();
                                status = MediusCallbackStatus.MediusSuccess;
                            }
                        }

                        data.ClientObject.Queue(new MediusAccountDeleteResponse()
                        {
                            MessageID = accountDeleteRequest.MessageID,
                            StatusCode = status
                        });

                        Logger.Info($"Delete account {account?.AccountName} {status}");

                        break;
                    }
                case MediusAnonymousLoginRequest anonymousLoginRequest:
                    {
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {anonymousLoginRequest} without a session.");

                        data.ClientObject.Queue(new MediusAnonymousLoginResponse()
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
                        });

                        break;
                    }
                case MediusAccountLoginRequest accountLoginRequest:
                    {
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {accountLoginRequest} without a session.");

                        // Find account
                        if (!Program.Database.TryGetAccountByName(accountLoginRequest.Username, out var account))
                        {
                            data.ClientObject.Queue(new MediusAccountLoginResponse()
                            {
                                MessageID = accountLoginRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusAccountNotFound,
                            });
                        }

                        // Check client isn't already logged in
                        else if (account.IsLoggedIn)
                        {
                            data.ClientObject.Queue(new MediusAccountLoginResponse()
                            {
                                MessageID = accountLoginRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusAccountLoggedIn
                            });
                        }
                        else if (account.AccountPassword != accountLoginRequest.Password)
                        {
                            data.ClientObject.Queue(new MediusAccountLoginResponse()
                            {
                                MessageID = accountLoginRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusInvalidPassword
                            });
                        }
                        else
                        {
                            //
                            data.ClientObject.Login(account);
                            data.ClientObject.Status = MediusPlayerStatus.MediusPlayerInAuthWorld;

                            // Send patches
                            if (Program.Settings.Patches != null)
                                foreach (var patch in Program.Settings.Patches)
                                    if (patch.Enabled && patch.ApplicationId == data.ClientObject.ApplicationId)
                                        data.ClientObject.Queue(patch.Serialize());

                            // 
                            Logger.Info($"LOGGING IN AS {account.AccountName} with access token {data.ClientObject.Token}");

                            // Tell client
                            data.ClientObject.Queue(new MediusAccountLoginResponse()
                            {
                                MessageID = accountLoginRequest.MessageID,
                                AccountID = account.AccountId,
                                AccountType = MediusAccountType.MediusMasterAccount,
                                ConnectInfo = new NetConnectionInfo()
                                {
                                    AccessKey = data.ClientObject.Token,
                                    SessionKey = data.ClientObject.SessionKey,
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
                            });
                        }
                        break;
                    }
                case MediusAccountLogoutRequest accountLogoutRequest:
                    {
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {accountLogoutRequest} without a session.");

                        MediusCallbackStatus result = MediusCallbackStatus.MediusFail;

                        // Check token
                        if (data.ClientObject.ClientAccount != null && accountLogoutRequest.SessionKey == data.ClientObject.SessionKey)
                        {
                            // 
                            result = MediusCallbackStatus.MediusSuccess;

                            // Logout
                            data.ClientObject.Logout();
                        }

                        data.ClientObject.Queue(new MediusAccountLogoutResponse()
                        {
                            MessageID = accountLogoutRequest.MessageID,
                            StatusCode = result
                        });
                        break;
                    }
                case MediusTextFilterRequest textFilterRequest:
                    {
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {textFilterRequest} without a session.");

                        // Accept everything
                        // No filter
                        data.ClientObject.Queue(new MediusTextFilterResponse()
                        {
                            MessageID = textFilterRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                            Text = textFilterRequest.Text
                        });

                        break;
                    }

                #region Deadlocked No-op Messages (MAS)

                case MediusGetBuddyList_ExtraInfoRequest getBuddyList_ExtraInfoRequest:
                    {
                        Queue(new RT_MSG_SERVER_APP()
                        {
                            Message = new MediusGetBuddyList_ExtraInfoResponse()
                            {
                                MessageID = getBuddyList_ExtraInfoRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNoResult,
                                EndOfList = true
                            }
                        }, clientChannel);
                        break;
                    }
                case MediusGetIgnoreListRequest getIgnoreListRequest:
                    {
                        Queue(new RT_MSG_SERVER_APP()
                        {
                            Message = new MediusGetIgnoreListResponse()
                            {
                                MessageID = getIgnoreListRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNoResult,
                                EndOfList = true
                            }
                        }, clientChannel);
                        break;
                    }
                case MediusGetMyClansRequest getMyClansRequest:
                    {
                        Queue(new RT_MSG_SERVER_APP()
                        {
                            Message = new MediusGetMyClansResponse()
                            {
                                MessageID = getMyClansRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNoResult,
                                EndOfList = true
                            }
                        }, clientChannel);
                        break;
                    }

                #endregion

                default:
                    {
                        Logger.Warn($"{Name} Unhandled Medius Message: {message}");
                        break;
                    }
            }
        }

    }
}
