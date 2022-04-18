using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;
using RT.Common;
using RT.Cryptography;
using RT.Models;
using Server.Common;
using Server.Database;
using Server.Medius.Models;
using Server.Medius.PluginArgs;
using Server.Plugins;
using Server.Plugins.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Server.Medius
{
    public class MAS : BaseMediusComponent
    {
        static readonly IInternalLogger _logger = InternalLoggerFactory.GetInstance<MAS>();

        protected override IInternalLogger Logger => _logger;
        public override int Port => Program.Settings.MASPort;

        public MAS()
        {

        }

        protected override async Task ProcessMessage(BaseScertMessage message, IChannel clientChannel, ChannelData data)
        {
            // Get ScertClient data
            var scertClient = clientChannel.GetAttribute(Server.Pipeline.Constants.SCERT_CLIENT).Get();
            scertClient.CipherService.EnableEncryption = Program.Settings.EncryptMessages;

            // 
            switch (message)
            {
                case RT_MSG_CLIENT_HELLO clientHello:
                    {
                        // send hello
                        Queue(new RT_MSG_SERVER_HELLO() { RsaPublicKey = Program.Settings.EncryptMessages ? Program.Settings.DefaultKey.N : Org.BouncyCastle.Math.BigInteger.Zero }, clientChannel);
                        break;
                    }
                case RT_MSG_CLIENT_CRYPTKEY_PUBLIC clientCryptKeyPublic:
                    {
                        // generate new client session key
                        scertClient.CipherService.GenerateCipher(CipherContext.RSA_AUTH, clientCryptKeyPublic.Key.Reverse().ToArray());
                        scertClient.CipherService.GenerateCipher(CipherContext.RC_CLIENT_SESSION);

                        Queue(new RT_MSG_SERVER_CRYPTKEY_PEER() { Key = scertClient.CipherService.GetPublicKey(CipherContext.RC_CLIENT_SESSION) }, clientChannel);
                        break;
                    }
                case RT_MSG_CLIENT_CONNECT_TCP clientConnectTcp:
                    {
                        if (!Program.Settings.IsCompatAppId(clientConnectTcp.AppId))
                        {
                            Logger.Error($"Client {clientChannel.RemoteAddress} attempting to authenticate with incompatible app id {clientConnectTcp.AppId}");
                            await clientChannel.CloseAsync();
                            return;
                        }

                        data.ApplicationId = clientConnectTcp.AppId;
                        if (!scertClient.IsPS3Client)
                        {
                            Queue(new RT_MSG_SERVER_CONNECT_REQUIRE() { ReqServerPassword = 0x02, Contents = Utils.FromString("4802") }, clientChannel);
                        }
                        else
                        {
                            Queue(new RT_MSG_SERVER_CONNECT_REQUIRE() { ReqServerPassword = 0x00, Contents = Utils.FromString("4802") }, clientChannel);
                        }
                        break;
                    }
                case RT_MSG_CLIENT_CONNECT_READY_REQUIRE clientConnectReadyRequire:
                    {
                        if (!scertClient.IsPS3Client)
                        {
                            Queue(new RT_MSG_SERVER_CRYPTKEY_GAME() { Key = scertClient.CipherService.GetPublicKey(CipherContext.RC_CLIENT_SESSION) }, clientChannel);
                        }
                        Queue(new RT_MSG_SERVER_CONNECT_ACCEPT_TCP()
                        {
                            UNK_00 = 0,
                            UNK_02 = GenerateNewScertClientId(),
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
                        data.State = ClientState.DISCONNECTED;
                        _ = clientChannel.CloseAsync();
                        break;
                    }
                default:
                    {
                        Logger.Warn($"UNHANDLED MESSAGE: {message}");

                        break;
                    }
            }

            return;
        }

        protected virtual void ProcessMediusMessage(BaseMediusMessage message, IChannel clientChannel, ChannelData data)
        {
            var scertClient = clientChannel.GetAttribute(Server.Pipeline.Constants.SCERT_CLIENT).Get();
            if (message == null)
                return;


            switch (message)
            {
#if DEBUG
                #region Dme

                case MediusServerSessionBeginRequest mgclSessionBeginRequest:
                    {
                        // Create DME object
                        data.ClientObject = Program.ProxyServer.ReserveDMEObject(mgclSessionBeginRequest);

                        // 
                        data.ClientObject.OnConnected();

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
                                    AddressList = new NetAddress[Constants.NET_ADDRESS_LIST_COUNT]
                                        {
                                            new NetAddress() {Address = Program.LobbyServer.IPAddress.ToString(), Port = (uint)Program.ProxyServer.Port, AddressType = NetAddressType.NetAddressTypeExternal},
                                            new NetAddress() {Address = Program.Settings.NATIp, Port = (uint)Program.Settings.NATPort, AddressType = NetAddressType.NetAddressTypeNATService},
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

                        // Keep the client alive until the dme objects connects to MPS or times out
                        dmeObject.KeepAliveUntilNextConnection();

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
                                    AddressList = new NetAddress[Constants.NET_ADDRESS_LIST_COUNT]
                                    {
                                        new NetAddress() {Address = Program.ProxyServer.IPAddress.ToString(), Port = (uint)Program.ProxyServer.Port, AddressType = NetAddressType.NetAddressTypeExternal},
                                        new NetAddress() {Address = Program.Settings.NATIp, Port = (uint)Program.Settings.NATPort, AddressType = NetAddressType.NetAddressTypeNATService},
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

                #endregion
#endif

                #region Session

                case MediusExtendedSessionBeginRequest extendedSessionBeginRequest:
                    {
                        // Create client object
                        data.ClientObject = Program.LobbyServer.ReserveClient(extendedSessionBeginRequest);
                        data.ClientObject.ApplicationId = data.ApplicationId;
                        data.ClientObject.MediusVersion = scertClient.MediusVersion;
                        data.ClientObject.OnConnected();

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
                        data.ClientObject.MediusVersion = scertClient.MediusVersion;
                        data.ClientObject.OnConnected();

                        // Reply
                        data.ClientObject.Queue(new MediusSessionBeginResponse()
                        {
                            MessageID = sessionBeginRequest.MessageID,
                            SessionKey = data.ClientObject.SessionKey,
                            StatusCode = MediusCallbackStatus.MediusSuccess
                        });
                        break;
                    }

                case MediusSessionBegin1Request SessionBegin1Request:
                    {
                        // Create client object
                        data.ClientObject = Program.LobbyServer.ReserveClient1(SessionBegin1Request);
                        data.ClientObject.ApplicationId = data.ApplicationId;
                        data.ClientObject.MediusVersion = scertClient.MediusVersion;
                        data.ClientObject.OnConnected();

                        // Reply
                        data.ClientObject.Queue(new MediusSessionBeginResponse()
                        {
                            MessageID = SessionBegin1Request.MessageID,
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
                        data.ClientObject.EndSession();

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

                #endregion

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
                case MediusSetLocalizationParamsRequest1 setLocalizationParamsRequest:
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
                        if (dnasSignaturePost.DnasSignatureType == MediusDnasCategory.DnasConsoleID)
                        {
                            data.MachineId = BitConverter.ToString(dnasSignaturePost.DnasSignature);

                            // post if logged in
                            if (data.ClientObject?.IsLoggedIn ?? false)
                                _ = Program.Database.PostMachineId(data.ClientObject.AccountId, data.MachineId);
                        }
                        break;
                    }

                #region Account

                case MediusAccountRegistrationRequest accountRegRequest:
                    {
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {accountRegRequest} without a session.");

                        // Check that account creation is enabled
                        if (Program.Settings.Beta != null && Program.Settings.Beta.Enabled && !Program.Settings.Beta.AllowAccountCreation)
                        {
                            // Reply error
                            data.ClientObject.Queue(new MediusAccountRegistrationResponse()
                            {
                                MessageID = accountRegRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusFail
                            });

                            return;
                        }

                        // validate name
                        if (!Program.PassTextFilter(Config.TextFilterContext.ACCOUNT_NAME, accountRegRequest.AccountName))
                        {
                            data.ClientObject.Queue(new MediusAccountRegistrationResponse()
                            {
                                MessageID = accountRegRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusFail,
                            });
                            return;
                        }

                        _ = Program.Database.CreateAccount(new Database.Models.CreateAccountDTO()
                        {
                            AccountName = accountRegRequest.AccountName,
                            AccountPassword = Utils.ComputeSHA256(accountRegRequest.Password),
                            MachineId = data.MachineId,
                            MediusStats = Convert.ToBase64String(new byte[Constants.ACCOUNTSTATS_MAXLEN]),
                            AppId = data.ClientObject.ApplicationId
                        }).ContinueWith((r) =>
                        {
                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                // Reply with account id
                                data.ClientObject.Queue(new MediusAccountRegistrationResponse()
                                {
                                    MessageID = accountRegRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                    AccountID = r.Result.AccountId
                                });
                            }
                            else
                            {
                                // Reply error
                                data.ClientObject.Queue(new MediusAccountRegistrationResponse()
                                {
                                    MessageID = accountRegRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusAccountAlreadyExists
                                });
                            }
                        });
                        break;
                    }
                case MediusAccountGetIDRequest accountGetIdRequest:
                    {
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {accountGetIdRequest} without a session.");

                        _ = Program.Database.GetAccountByName(accountGetIdRequest.AccountName, data.ClientObject.ApplicationId).ContinueWith((r) =>
                        {
                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                // Success
                                data?.ClientObject?.Queue(new MediusAccountGetIDResponse()
                                {
                                    MessageID = accountGetIdRequest.MessageID,
                                    AccountID = r.Result.AccountId,
                                    StatusCode = MediusCallbackStatus.MediusSuccess
                                });
                            }
                            else
                            {
                                // Fail
                                data?.ClientObject?.Queue(new MediusAccountGetIDResponse()
                                {
                                    MessageID = accountGetIdRequest.MessageID,
                                    AccountID = -1,
                                    StatusCode = MediusCallbackStatus.MediusAccountNotFound
                                });
                            }
                        });

                        break;
                    }
                case MediusAccountDeleteRequest accountDeleteRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {accountDeleteRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {accountDeleteRequest} without a being logged in.");

                        _ = Program.Database.DeleteAccount(data.ClientObject.AccountName).ContinueWith((r) =>
                        {
                            if (r.IsCompletedSuccessfully && r.Result)
                            {
                                Logger.Info($"Delete account {data?.ClientObject?.AccountName}");

                                data?.ClientObject?.Logout();

                                data?.ClientObject?.Queue(new MediusAccountDeleteResponse()
                                {
                                    MessageID = accountDeleteRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess
                                });
                            }
                            else
                            {
                                data?.ClientObject?.Queue(new MediusAccountDeleteResponse()
                                {
                                    MessageID = accountDeleteRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusDBError
                                });
                            }
                        });
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
                            MediusWorldID = Program.Manager.GetDefaultLobbyChannel(data.ApplicationId).Id,
                            ConnectInfo = new NetConnectionInfo()
                            {
                                SessionKey = anonymousLoginRequest.SessionKey,
                                WorldID = 0,
                                ServerKey = Program.GlobalAuthPublic,
                                AddressList = new NetAddressList()
                                {
                                    AddressList = new NetAddress[Constants.NET_ADDRESS_LIST_COUNT]
                                    {
                                        new NetAddress() {Address = Program.LobbyServer.IPAddress.ToString(), Port = (uint)Program.LobbyServer.Port, AddressType = NetAddressType.NetAddressTypeExternal},
                                        new NetAddress() {Address = Program.Settings.NATIp, Port = (uint)Program.Settings.NATPort, AddressType = NetAddressType.NetAddressTypeNATService},
                                    }
                                },
                                Type = NetConnectionType.NetConnectionTypeClientServerTCP
                            }
                        });

                        break;
                    }
                case MediusAccountLoginRequest accountLoginRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {accountLoginRequest} without a session.");

                        // Check the client isn't already logged in
                        if (Program.Manager.GetClientByAccountName(accountLoginRequest.Username)?.IsLoggedIn ?? false)
                        {
                            data.ClientObject.Queue(new MediusAccountLoginResponse()
                            {
                                MessageID = accountLoginRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusAccountLoggedIn
                            });
                        }
                        else
                        {
                            Program.Database.GetAccountByName(accountLoginRequest.Username, data.ClientObject.ApplicationId).ContinueWith((r) =>
                            {
                                if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                    return;

                                if (r.IsCompletedSuccessfully && r.Result != null && data != null && data.ClientObject != null && data.ClientObject.IsConnected)
                                {
                                    if (r.Result.IsBanned)
                                    {
                                        // Send ban message
                                        QueueBanMessage(data);

                                        // Account is banned
                                        // Temporary solution is to tell the client the login failed
                                        data?.ClientObject?.Queue(new MediusAccountLoginResponse()
                                        {
                                            MessageID = accountLoginRequest.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusAccountBanned
                                        });

                                    }
                                    else if (Program.Settings.Beta != null && Program.Settings.Beta.Enabled && Program.Settings.Beta.RestrictSignin && !Program.Settings.Beta.PermittedAccounts.Contains(r.Result.AccountName))
                                    {
                                        // Account not allowed to sign in
                                        data?.ClientObject?.Queue(new MediusAccountLoginResponse()
                                        {
                                            MessageID = accountLoginRequest.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusFail
                                        });
                                    }
                                    else if (Program.Manager.GetClientByAccountName(accountLoginRequest.Username)?.IsLoggedIn ?? false)
                                    {
                                        data.ClientObject.Queue(new MediusAccountLoginResponse()
                                        {
                                            MessageID = accountLoginRequest.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusAccountLoggedIn
                                        });
                                    }
                                    else if (Utils.ComputeSHA256(accountLoginRequest.Password) == r.Result.AccountPassword)
                                    {
                                        Login(accountLoginRequest.MessageID, clientChannel, data, r.Result, false);
                                    }
                                    else
                                    {
                                        // Incorrect password
                                        data?.ClientObject?.Queue(new MediusAccountLoginResponse()
                                        {
                                            MessageID = accountLoginRequest.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusInvalidPassword
                                        });
                                    }
                                }
                                else if (Program.Settings.CreateAccountOnNotFound)
                                {
                                    // Account not found, create new and login
                                    // Check that account creation is enabled
                                    if (Program.Settings.Beta != null && Program.Settings.Beta.Enabled && !Program.Settings.Beta.AllowAccountCreation)
                                    {
                                        // Reply error
                                        data.ClientObject.Queue(new MediusAccountLoginResponse()
                                        {
                                            MessageID = accountLoginRequest.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusFail,
                                        });
                                        return;
                                    }

                                    // validate name
                                    if (!Program.PassTextFilter(Config.TextFilterContext.ACCOUNT_NAME, accountLoginRequest.Username))
                                    {
                                        data.ClientObject.Queue(new MediusAccountLoginResponse()
                                        {
                                            MessageID = accountLoginRequest.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusFail,
                                        });
                                        return;
                                    }

                                    _ = Program.Database.CreateAccount(new Database.Models.CreateAccountDTO()
                                    {
                                        AccountName = accountLoginRequest.Username,
                                        AccountPassword = Utils.ComputeSHA256(accountLoginRequest.Password),
                                        MachineId = data.MachineId,
                                        MediusStats = Convert.ToBase64String(new byte[Constants.ACCOUNTSTATS_MAXLEN]),
                                        AppId = data.ClientObject.ApplicationId
                                    }).ContinueWith((r) =>
                                    {
                                        if (r.IsCompletedSuccessfully && r.Result != null)
                                        {
                                            Login(accountLoginRequest.MessageID, clientChannel, data, r.Result, false);
                                        }
                                        else
                                        {
                                            // Reply error
                                            data.ClientObject.Queue(new MediusAccountLoginResponse()
                                            {
                                                MessageID = accountLoginRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusInvalidPassword
                                            });
                                        }
                                    });
                                }
                                else
                                {
                                    // Account not found
                                    data.ClientObject.Queue(new MediusAccountLoginResponse()
                                    {
                                        MessageID = accountLoginRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusAccountNotFound,
                                    });
                                }
                            });
                        }
                        break;
                    }
                case MediusAccountLogoutRequest accountLogoutRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {accountLogoutRequest} without a session.");

                        MediusCallbackStatus result = MediusCallbackStatus.MediusFail;

                        // Check token
                        if (data.ClientObject.IsLoggedIn && accountLogoutRequest.SessionKey == data.ClientObject.SessionKey)
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

                        // Deny special characters
                        // Also trim any whitespace
                        switch (textFilterRequest.TextFilterType)
                        {
                            case MediusTextFilterType.MediusTextFilterPassFail:
                                {
                                    // validate name
                                    if (!Program.PassTextFilter(Config.TextFilterContext.ACCOUNT_NAME, textFilterRequest.Text))
                                    {
                                        data.ClientObject.Queue(new MediusTextFilterResponse()
                                        {
                                            MessageID = textFilterRequest.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusFail
                                        });
                                        return;
                                    }
                                    else
                                    {
                                        //
                                        data.ClientObject.Queue(new MediusTextFilterResponse()
                                        {
                                            MessageID = textFilterRequest.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusSuccess,
                                            Text = textFilterRequest.Text.Trim()
                                        });
                                    }
                                    break;
                                }
                            case MediusTextFilterType.MediusTextFilterReplace:
                                {
                                    data.ClientObject.Queue(new MediusTextFilterResponse()
                                    {
                                        MessageID = textFilterRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        Text = Program.FilterTextFilter(Config.TextFilterContext.ACCOUNT_NAME, textFilterRequest.Text).Trim()
                                    });
                                    break;
                                }
                        }
                        break;
                    }

                case MediusTicketLoginRequest ticketLoginRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {ticketLoginRequest} without a session.");

                        // Check the client isn't already logged in
                        if (Program.Manager.GetClientByAccountName(ticketLoginRequest.Username)?.IsLoggedIn ?? false)
                        {
                            data.ClientObject.Queue(new MediusTicketLoginResponse()
                            {
                                MessageID = ticketLoginRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusAccountLoggedIn
                            });
                        }
                        else
                        {
                            Program.Database.GetAccountByName(ticketLoginRequest.Username, data.ClientObject.ApplicationId).ContinueWith((r) =>
                            {
                                if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                    return;

                                if (r.IsCompletedSuccessfully && r.Result != null && data != null && data.ClientObject != null && data.ClientObject.IsConnected)
                                {
                                    if (r.Result.IsBanned)
                                    {
                                        // Send ban message
                                        QueueBanMessage(data);

                                        // Account is banned
                                        // Temporary solution is to tell the client the login failed
                                        data?.ClientObject?.Queue(new MediusTicketLoginResponse()
                                        {
                                            MessageID = ticketLoginRequest.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusAccountBanned
                                        });

                                    }
                                    else if (Program.Settings.Beta != null && Program.Settings.Beta.Enabled && Program.Settings.Beta.RestrictSignin && !Program.Settings.Beta.PermittedAccounts.Contains(r.Result.AccountName))
                                    {
                                        // Account not allowed to sign in
                                        data?.ClientObject?.Queue(new MediusTicketLoginResponse()
                                        {
                                            MessageID = ticketLoginRequest.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusFail
                                        });
                                    }
                                    else
                                    {
                                        Login(ticketLoginRequest.MessageID, clientChannel, data, r.Result, true);
                                    }
                                }
                                else
                                {
                                    // Account not found, create new and login
                                    // Check that account creation is enabled
                                    if (Program.Settings.Beta != null && Program.Settings.Beta.Enabled && !Program.Settings.Beta.AllowAccountCreation)
                                    {
                                        // Reply error
                                        data.ClientObject.Queue(new MediusTicketLoginResponse()
                                        {
                                            MessageID = ticketLoginRequest.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusFail,
                                        });
                                        return;
                                    }

                                    _ = Program.Database.CreateAccount(new Database.Models.CreateAccountDTO()
                                    {
                                        AccountName = ticketLoginRequest.Username,
                                        AccountPassword = Utils.ComputeSHA256(ticketLoginRequest.Password),
                                        MachineId = data.MachineId,
                                        MediusStats = Convert.ToBase64String(new byte[Constants.ACCOUNTSTATS_MAXLEN]),
                                        AppId = data.ClientObject.ApplicationId
                                    }).ContinueWith((r) =>
                                    {
                                        if (r.IsCompletedSuccessfully && r.Result != null)
                                        {
                                            Login(ticketLoginRequest.MessageID, clientChannel, data, r.Result, true);
                                        }
                                        else
                                        {
                                            // Reply error
                                            data.ClientObject.Queue(new MediusTicketLoginResponse()
                                            {
                                                MessageID = ticketLoginRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusInvalidPassword
                                            });
                                        }
                                    });
                                }
                            });
                        }
                        break;
                    }

                #endregion

                #region Policy / Announcements

                case MediusGetAllAnnouncementsRequest getAllAnnouncementsRequest:
                    {
                        // Send to plugins
                        Program.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_ON_GET_ALL_ANNOUNCEMENTS, new OnPlayerRequestArgs()
                        {
                            Player = data.ClientObject,
                            Request = getAllAnnouncementsRequest
                        });

                        Program.Database.GetLatestAnnouncements(data.ApplicationId).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result != null && r.Result.Length > 0)
                            {
                                List<MediusGetAnnouncementsResponse> responses = new List<MediusGetAnnouncementsResponse>();
                                foreach (var result in r.Result)
                                {
                                    responses.Add(new MediusGetAnnouncementsResponse()
                                    {
                                        MessageID = getAllAnnouncementsRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        Announcement = string.IsNullOrEmpty(result.AnnouncementTitle) ? $"{result.AnnouncementBody}" : $"{result.AnnouncementTitle}\n{result.AnnouncementBody}\n",
                                        AnnouncementID = result.Id,
                                        EndOfList = false
                                    });
                                }

                                responses[responses.Count - 1].EndOfList = true;
                                data.ClientObject.Queue(responses);
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusGetAnnouncementsResponse()
                                {
                                    MessageID = getAllAnnouncementsRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                    Announcement = "",
                                    AnnouncementID = 0,
                                    EndOfList = true
                                });
                            }
                        });
                        break;
                    }

                case MediusGetAnnouncementsRequest getAnnouncementsRequest:
                    {
                        // Send to plugins
                        Program.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_ON_GET_ANNOUNCEMENTS, new OnPlayerRequestArgs()
                        {
                            Player = data.ClientObject,
                            Request = getAnnouncementsRequest
                        });

                        Program.Database.GetLatestAnnouncement(data.ApplicationId).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                data.ClientObject.Queue(new MediusGetAnnouncementsResponse()
                                {
                                    MessageID = getAnnouncementsRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                    Announcement = string.IsNullOrEmpty(r.Result.AnnouncementTitle) ? $"{r.Result.AnnouncementBody}" : $"{r.Result.AnnouncementTitle}\n{r.Result.AnnouncementBody}\n",
                                    AnnouncementID = r.Result.Id,
                                    EndOfList = true
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusGetAnnouncementsResponse()
                                {
                                    MessageID = getAnnouncementsRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                    Announcement = "",
                                    AnnouncementID = 0,
                                    EndOfList = true
                                });
                            }
                        });
                        break;
                    }

                case MediusGetPolicyRequest getPolicyRequest:
                    {
                        // Send to plugins
                        Program.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_ON_GET_POLICY, new OnPlayerRequestArgs()
                        {
                            Player = data.ClientObject,
                            Request = getPolicyRequest
                        });

                        switch (getPolicyRequest.Policy)
                        {
                            case MediusPolicyType.Privacy:
                                {
                                    Program.Database.GetUsagePolicy().ContinueWith((r) =>
                                    {
                                        if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                            return;

                                        if (r.IsCompletedSuccessfully && r.Result != null)
                                        {
                                            string txt = r.Result.EulaBody;
                                            if (!string.IsNullOrEmpty(r.Result.EulaTitle))
                                                txt = r.Result.EulaTitle + "\n" + txt;
                                            data.ClientObject.Queue(MediusGetPolicyResponse.FromText(getPolicyRequest.MessageID, txt));
                                        }
                                        else
                                        {
                                            data.ClientObject.Queue(new MediusGetPolicyResponse() { MessageID = getPolicyRequest.MessageID, StatusCode = MediusCallbackStatus.MediusSuccess, Policy = "", EndOfText = true });
                                        }
                                    });
                                    break;
                                }
                            case MediusPolicyType.Usage:
                                {
                                    Program.Database.GetUsagePolicy().ContinueWith((r) =>
                                    {
                                        if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                            return;

                                        if (r.IsCompletedSuccessfully && r.Result != null)
                                        {
                                            string txt = r.Result.EulaBody;
                                            if (!string.IsNullOrEmpty(r.Result.EulaTitle))
                                                txt = r.Result.EulaTitle + "\n" + txt;
                                            data.ClientObject.Queue(MediusGetPolicyResponse.FromText(getPolicyRequest.MessageID, txt));
                                        }
                                        else
                                        {
                                            data.ClientObject.Queue(new MediusGetPolicyResponse() { MessageID = getPolicyRequest.MessageID, StatusCode = MediusCallbackStatus.MediusSuccess, Policy = "", EndOfText = true });
                                        }
                                    });

                                    break;
                                }
                        }
                        break;
                    }

                #endregion

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
                        Logger.Warn($"Unhandled Medius Message: {message}");
                        break;
                    }
            }
        }

        private void Login(MessageId messageId, IChannel clientChannel, ChannelData data, Database.Models.AccountDTO accountDto, bool ticket)
        {
            var fac = new PS2CipherFactory();
            var rsa = fac.CreateNew(CipherContext.RSA_AUTH) as PS2_RSA;

            //
            data.ClientObject.Login(accountDto);

            // Update db ip
            _ = Program.Database.PostAccountIp(accountDto.AccountId, (clientChannel.RemoteAddress as IPEndPoint).Address.MapToIPv4().ToString());
            if (!String.IsNullOrEmpty(data.MachineId))
                _ = Program.Database.PostMachineId(data.ClientObject.AccountId, data.MachineId);

            // Add to logged in clients
            Program.Manager.AddClient(data.ClientObject);

            // 
            Logger.Info($"LOGGING IN AS {data.ClientObject.AccountName} with access token {data.ClientObject.Token}");

            // Put client in default channel
            data.ClientObject.JoinChannel(Program.Manager.GetDefaultLobbyChannel(data.ApplicationId));


            // Tell client
            if (ticket == true)
            {
                data.ClientObject.Queue(new MediusTicketLoginResponse()
                {
                    MessageID = messageId,
                    AccountID = data.ClientObject.AccountId,
                    AccountType = MediusAccountType.MediusMasterAccount,
                    ConnectInfo = new NetConnectionInfo()
                    {
                        AccessKey = data.ClientObject.Token,
                        SessionKey = data.ClientObject.SessionKey,
                        WorldID = Program.Manager.GetDefaultLobbyChannel(data.ApplicationId).Id,
                        ServerKey = new RSA_KEY(), //Program.GlobalAuthPublic,
                        AddressList = new NetAddressList()
                        {
                            AddressList = new NetAddress[Constants.NET_ADDRESS_LIST_COUNT]
                            {
                                new NetAddress() {Address = Program.LobbyServer.IPAddress.ToString(), Port = (uint)Program.LobbyServer.Port, AddressType = NetAddressType.NetAddressTypeExternal},
                                new NetAddress() {Address = Program.Settings.NATIp, Port = (uint)Program.Settings.NATPort, AddressType = NetAddressType.NetAddressTypeNATService},
                            }
                        },
                        Type = NetConnectionType.NetConnectionTypeClientServerTCP
                    },
                    MediusWorldID = Program.Manager.GetDefaultLobbyChannel(data.ApplicationId).Id,
                    StatusCode = MediusCallbackStatus.MediusSuccess
                });

                // Prepare for transition to lobby server
                data.ClientObject.KeepAliveUntilNextConnection();
            }
            else
            {
                data.ClientObject.Queue(new MediusAccountLoginResponse()
                {
                    MessageID = messageId,
                    AccountID = data.ClientObject.AccountId,
                    AccountType = MediusAccountType.MediusMasterAccount,
                    ConnectInfo = new NetConnectionInfo()
                    {
                        AccessKey = data.ClientObject.Token,
                        SessionKey = data.ClientObject.SessionKey,
                        WorldID = Program.Manager.GetDefaultLobbyChannel(data.ApplicationId).Id,
                        ServerKey = Program.GlobalAuthPublic,
                        AddressList = new NetAddressList()
                        {
                            AddressList = new NetAddress[Constants.NET_ADDRESS_LIST_COUNT]
                            {
                                new NetAddress() {Address = Program.LobbyServer.IPAddress.ToString(), Port = (uint)Program.LobbyServer.Port, AddressType = NetAddressType.NetAddressTypeExternal},
                                new NetAddress() {Address = Program.Settings.NATIp, Port = (uint)Program.Settings.NATPort, AddressType = NetAddressType.NetAddressTypeNATService},
                            }
                        },
                        Type = NetConnectionType.NetConnectionTypeClientServerTCP
                    },
                    MediusWorldID = Program.Manager.GetDefaultLobbyChannel(data.ApplicationId).Id,
                    StatusCode = MediusCallbackStatus.MediusSuccess
                });

                // Prepare for transition to lobby server
                data.ClientObject.KeepAliveUntilNextConnection();
            }
        }
    }
}
