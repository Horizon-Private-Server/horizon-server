using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;
using RT.Common;
using RT.Cryptography;
using RT.Models;
using Server.Common;
using Server.Database;
using Server.Medius.Config;
using Server.Medius.Models;
using Server.Medius.PluginArgs;
using Server.Plugins;
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

        public static ServerSettings Settings = new ServerSettings();

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
                        data.ApplicationId = clientConnectTcp.AppId;

                        #region Check if AppId from Client matches Server
                        if (!Program.Settings.IsCompatAppId(clientConnectTcp.AppId))
                        {
                            Logger.Error($"Client {clientChannel.RemoteAddress} attempting to authenticate with incompatible app id {clientConnectTcp.AppId}");
                            await clientChannel.CloseAsync();
                            return;
                        }
                        #endregion

                        if (scertClient.IsPS3Client)
                        {
                            Queue(new RT_MSG_SERVER_CONNECT_REQUIRE() { ReqServerPassword = 0x00, Contents = Utils.FromString("4802") }, clientChannel);
                        }
                        else if (scertClient.MediusVersion > 109 && scertClient.MediusVersion != null)
                        {
                            Queue(new RT_MSG_SERVER_CONNECT_REQUIRE() { ReqServerPassword = 0x02, Contents = Utils.FromString("4802") }, clientChannel);
                        }
                        else
                        {
                            #region If Frequency, TMBO, Socom 1 or ATV Offroad Fury 2 then
                            //
                            if (data.ApplicationId == 10010 || data.ApplicationId == 10031 || data.ApplicationId == 10274 || data.ApplicationId == 10284 || data.ApplicationId == 10984)
                            {
                                //Do NOT send hereCryptKey Game
                                Queue(new RT_MSG_SERVER_CONNECT_ACCEPT_TCP()
                                {
                                    UNK_00 = 0,
                                    UNK_02 = GenerateNewScertClientId(),
                                    UNK_06 = 0x0001,
                                    IP = (clientChannel.RemoteAddress as IPEndPoint)?.Address
                                }, clientChannel);

                                //If ATV Offroad Fury 2, complete connection
                                if (data.ApplicationId == 10284)
                                {
                                    Queue(new RT_MSG_SERVER_CONNECT_COMPLETE() { ClientCountAtConnect = 0x0001 }, clientChannel);
                                }
                            }
                            #endregion
                            else
                            {
                                //Older Medius titles do NOT use CRYPTKEY_GAME, newer ones have this.
                                Queue(new RT_MSG_SERVER_CRYPTKEY_GAME() { Key = scertClient.CipherService.GetPublicKey(CipherContext.RC_CLIENT_SESSION) }, clientChannel);
                                Queue(new RT_MSG_SERVER_CONNECT_ACCEPT_TCP()
                                {
                                    UNK_00 = 0,
                                    UNK_02 = GenerateNewScertClientId(),
                                    UNK_06 = 0x0001,
                                    IP = (clientChannel.RemoteAddress as IPEndPoint)?.Address
                                }, clientChannel);
                                Queue(new RT_MSG_SERVER_CONNECT_COMPLETE() { ClientCountAtConnect = 0x0001 }, clientChannel);
                            }
                        }
                        break;
                    }
                case RT_MSG_CLIENT_CONNECT_READY_REQUIRE clientConnectReadyRequire:
                    {
                        if (!scertClient.IsPS3Client && scertClient.RsaAuthKey != null)
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
                        Queue(new RT_MSG_SERVER_CONNECT_COMPLETE() { ClientCountAtConnect = 0x0001 }, clientChannel);
                        Queue(new RT_MSG_SERVER_ECHO(), clientChannel);
                        break;
                    }
                #region Echos
                case RT_MSG_SERVER_ECHO serverEchoReply:
                    {

                        break;
                    }
                case RT_MSG_CLIENT_ECHO clientEcho:
                    {
                        Queue(new RT_MSG_CLIENT_ECHO() { Value = clientEcho.Value }, clientChannel);
                        break;
                    }
                #endregion

                case RT_MSG_CLIENT_APP_TOSERVER clientAppToServer:
                    {
                        await ProcessMediusMessage(clientAppToServer.Message, clientChannel, data);
                        break;
                    }

                #region Client Disconnect
                case RT_MSG_CLIENT_DISCONNECT _:
                case RT_MSG_CLIENT_DISCONNECT_WITH_REASON _:
                    {
                        data.State = ClientState.DISCONNECTED;
                        _ = clientChannel.CloseAsync();
                        break;
                    }
                #endregion

                default:
                    {
                        Logger.Warn($"UNHANDLED MESSAGE: {message}");

                        break;
                    }
            }

            return;
        }

        protected virtual async Task ProcessMediusMessage(BaseMediusMessage message, IChannel clientChannel, ChannelData data)
        {
            var scertClient = clientChannel.GetAttribute(Server.Pipeline.Constants.SCERT_CLIENT).Get();
            if (message == null)
                return;


            switch (message)
            {
                #region MGCL - Dme

                case MediusServerSessionBeginRequest mgclSessionBeginRequest:
                    {
                        // Create DME object
                        data.ClientObject = Program.ProxyServer.ReserveDMEObject(mgclSessionBeginRequest);

                        data.ClientObject.ServerType = mgclSessionBeginRequest.ServerType;

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
                                WorldID = Program.Manager.GetDefaultLobbyChannel(data.ApplicationId).Id,
                                ServerKey = Program.GlobalAuthPublic,
                                AddressList = new NetAddressList()
                                {
                                    AddressList = new NetAddress[Constants.NET_ADDRESS_LIST_COUNT]
                                        {
                                            new NetAddress() { Address = Program.Settings.NATIp, Port = (uint)Program.Settings.NATPort, AddressType = NetAddressType.NetAddressTypeNATService },
                                            new NetAddress() { AddressType = NetAddressType.NetAddressNone },
                                        }
                                },
                                Type = NetConnectionType.NetConnectionTypeClientServerUDP
                            }
                        });

                        break;
                    }

                case MediusServerSessionBeginRequest1 serverSessionBeginRequest1:
                    {
                        // Create DME object
                        data.ClientObject = Program.ProxyServer.ReserveDMEObject(serverSessionBeginRequest1);

                        data.ClientObject.ServerType = serverSessionBeginRequest1.ServerType;
                        // 
                        data.ClientObject.OnConnected();

                        data.ClientObject.Queue(new MediusServerSessionBeginResponse()
                        {
                            MessageID = serverSessionBeginRequest1.MessageID,
                            Confirmation = MGCL_ERROR_CODE.MGCL_SUCCESS,
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
                                        new NetAddress() { Address = Program.Settings.NATIp, Port = (uint)Program.Settings.NATPort, AddressType = NetAddressType.NetAddressTypeNATService },
                                        new NetAddress() { AddressType = NetAddressType.NetAddressNone },
                                    }
                                },
                                Type = NetConnectionType.NetConnectionTypeClientServerUDP
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
                        //dmeObject.SetIp(mgclAuthRequest.AddressList.AddressList[0].Address);

                        // Keep the client alive until the dme objects connects to MPS or times out
                        dmeObject.KeepAliveUntilNextConnection();

                        // Arc The Lad - End of Darkness cannot use the ServerKey, so they need to be removed, same with Amplitude and R&C3: Pubeta
                        if (dmeObject.ApplicationId == 10984 || dmeObject.ApplicationId == 10164 || dmeObject.ApplicationId == 10680)
                        {
                            dmeObject.Queue(new MediusServerAuthenticationResponse()
                            {
                                MessageID = mgclAuthRequest.MessageID,
                                Confirmation = MGCL_ERROR_CODE.MGCL_SUCCESS,
                                ConnectInfo = new NetConnectionInfo()
                                {
                                    AccessKey = dmeObject.Token,
                                    SessionKey = dmeObject.SessionKey,
                                    WorldID = Program.Manager.GetDefaultLobbyChannel(data.ApplicationId).Id,
                                    //The ServerKey need to be omitted of the connectInfo, a null serverkey throw a Network Error
                                    //ServerKey = Program.GlobalAuthPublic,
                                    AddressList = new NetAddressList()
                                    {
                                        AddressList = new NetAddress[Constants.NET_ADDRESS_LIST_COUNT]
                                        {
                                            new NetAddress() { Address = Program.ProxyServer.IPAddress.ToString(), Port = (uint)Program.ProxyServer.Port, AddressType = NetAddressType.NetAddressTypeExternal },
                                            new NetAddress() { AddressType = NetAddressType.NetAddressNone },
                                        }
                                    },
                                    Type = NetConnectionType.NetConnectionTypeClientServerTCP
                                }
                            });
                        }
                        else //reply
                        {
                            dmeObject.Queue(new MediusServerAuthenticationResponse()
                            {
                                MessageID = mgclAuthRequest.MessageID,
                                Confirmation = MGCL_ERROR_CODE.MGCL_SUCCESS,
                                ConnectInfo = new NetConnectionInfo()
                                {
                                    AccessKey = dmeObject.Token,
                                    SessionKey = dmeObject.SessionKey,
                                    WorldID = Program.Manager.GetDefaultLobbyChannel(data.ApplicationId).Id,
                                    ServerKey = Program.GlobalAuthPublic,
                                    AddressList = new NetAddressList()
                                    {
                                        AddressList = new NetAddress[Constants.NET_ADDRESS_LIST_COUNT]
                                        {
                                            new NetAddress() { Address = Program.ProxyServer.IPAddress.ToString(), Port = (uint)Program.ProxyServer.Port, AddressType = NetAddressType.NetAddressTypeExternal },
                                            new NetAddress() { AddressType = NetAddressType.NetAddressNone },
                                        }
                                    },
                                    Type = NetConnectionType.NetConnectionTypeClientServerTCP
                                }
                            });
                        }
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

                case MediusServerSessionEndRequest sessionEndRequest:
                    {
                        data?.ClientObject.Queue(new MediusServerSessionEndResponse()
                        {
                            MessageID = sessionEndRequest.MessageID,
                            ErrorCode = MGCL_ERROR_CODE.MGCL_SUCCESS
                        });
                        break;
                    }

                case MediusServerReport serverReport:
                    {
                        (data.ClientObject as DMEObject)?.OnWorldReport(serverReport);

                        break;
                    }

                #endregion


                #region Session

                case MediusExtendedSessionBeginRequest extendedSessionBeginRequest:
                    {
                        // Create client object
                        data.ClientObject = Program.LobbyServer.ReserveClient(extendedSessionBeginRequest);
                        data.ClientObject.ApplicationId = data.ApplicationId;
                        data.ClientObject.MediusVersion = (int)scertClient.MediusVersion;
                        data.ClientObject.OnConnected();

                        if (Settings.SystemMessageSingleTest == true)
                        {
                            QueueBanMessage(data, "MAS.Notification Test: You have been banned from this server.");
                        } else {
                            // Reply
                            data.ClientObject.Queue(new MediusSessionBeginResponse()
                            {
                                MessageID = extendedSessionBeginRequest.MessageID,
                                SessionKey = data.ClientObject.SessionKey,
                                StatusCode = MediusCallbackStatus.MediusSuccess
                            });
                        }
                        break;
                    }
                case MediusSessionBeginRequest sessionBeginRequest:
                    {
                        // Create client object
                        data.ClientObject = Program.LobbyServer.ReserveClient(sessionBeginRequest);
                        data.ClientObject.ApplicationId = data.ApplicationId;
                        data.ClientObject.MediusVersion = (int)scertClient.MediusVersion;
                        data.ClientObject.OnConnected();

                        if (Settings.SystemMessageSingleTest == true)
                        {
                            QueueBanMessage(data, "MAS.Notification Test: You have been banned from this server.");
                        } else {
                            // Reply
                            data.ClientObject.Queue(new MediusSessionBeginResponse()
                            {
                                MessageID = sessionBeginRequest.MessageID,
                                SessionKey = data.ClientObject.SessionKey,
                                StatusCode = MediusCallbackStatus.MediusSuccess
                            });
                        }
                        break;
                    }

                case MediusSessionBeginRequest1 SessionBeginRequest1:
                    {
                        // Create client object
                        data.ClientObject = Program.LobbyServer.ReserveClient1(SessionBeginRequest1);
                        data.ClientObject.ApplicationId = data.ApplicationId;
                        data.ClientObject.MediusVersion = (int)scertClient.MediusVersion;
                        data.ClientObject.OnConnected();

                        if (Settings.SystemMessageSingleTest == true)
                        {
                            QueueBanMessage(data, "MAS.Notification Test: You have been banned from this server.");
                        } else {
                            // Reply
                            data.ClientObject.Queue(new MediusSessionBeginResponse()
                            {
                                MessageID = SessionBeginRequest1.MessageID,
                                SessionKey = data.ClientObject.SessionKey,
                                StatusCode = MediusCallbackStatus.MediusSuccess
                            });
                        }
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

                #region Localization

                case MediusSetLocalizationParamsRequest setLocalizationParamsRequest:
                    {
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {setLocalizationParamsRequest} without a session.");

                        data.ClientObject.Queue(new MediusStatusResponse0()
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

                        data.ClientObject.Queue(new MediusStatusResponse0()
                        {
                            MessageID = setLocalizationParamsRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess
                        });
                        break;
                    }
                case MediusSetLocalizationParamsRequest2 setLocalizationParamsRequest:
                    {
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {setLocalizationParamsRequest} without a session.");

                        data.ClientObject.Queue(new MediusStatusResponse0()
                        {
                            MessageID = setLocalizationParamsRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess
                        });
                        break;
                    }

                #endregion

                #region DNAS CID Check
                case MediusDnasSignaturePost dnasSignaturePost:
                    {
                        if(Settings.DnasEnablePost == true)
                        {
                            //If DNAS Signature Post is the PS2/PSP/PS3 Console ID then continue
                            if (dnasSignaturePost.DnasSignatureType == MediusDnasCategory.DnasConsoleID)
                            {
                                data.MachineId = BitConverter.ToString(dnasSignaturePost.DnasSignature);

                                // Then post to the Database if logged in
                                if (data.ClientObject?.IsLoggedIn ?? false)
                                    _ = Program.Database.PostMachineId(data.ClientObject.AccountId, data.MachineId);
                            }
                        } else {
                            //DnasEnablePost set to false;
                        }

                        break;
                    }
                #endregion

                #region AccessLevel (PS3)

                case MediusGetAccessLevelInfoRequest getAccessLevelInfoRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getAccessLevelInfoRequest} without a session.");

                        data.ClientObject.Queue(new MediusGetAccessLevelInfoResponse()
                        {
                            MessageID = getAccessLevelInfoRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                            AccessLevel = MediusAccessLevelType.MEDIUS_ACCESSLEVEL_MODERATOR,
                        });
                        break;
                    }

                #endregion 

                #region NpId (PS3) WIP
                /*
                case MediusNpIdsGetByAccountNamesRequest getNpIdsGetByAccountNamesRequest:
                {
                    // ERROR - Need a session
                    if (data.ClientObject == null)
                        throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getNpIdsGetByAccountNamesRequest} without a session.");

                    Program.Database.GetNpIdByAccountNames(data.ApplicationId).ContinueWith((r) =>
                    {
                        if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                            return;

                        if (r.IsCompletedSuccessfully && r.Result != null && r.Result.Length > 0)
                        {
                            List<MediusNpIdsGetByAccountNamesResponse> responses = new List<MediusNpIdsGetByAccountNamesResponse>();
                            foreach (var result in r.Result)
                            {
                                responses.Add(new MediusNpIdsGetByAccountNamesResponse()
                                {
                                    MessageID = getNpIdsGetByAccountNamesRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                    AccountName = result.AccountName,
                                    SceNpId = result.SceNpId + 1,
                                    EndOfList = false
                                });
                            }

                            responses[responses.Count - 1].EndOfList = true;
                            data.ClientObject.Queue(responses);
                        }
                        else
                        {
                            data.ClientObject.Queue(new MediusNpIdsGetByAccountNamesResponse()
                            {
                                MessageID = getNpIdsGetByAccountNamesRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusFeatureNotEnabled,
                                AccountName = "",
                                SceNpId = "",
                                EndOfList = true
                            });
                        }
                    });
                    break;
                }
                */
                #endregion

                #region Version Server

                case MediusVersionServerRequest mediusVersionServerRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {mediusVersionServerRequest} without a session.");
						
						if(Settings.MediusServerVersionOverride == true)
                        {
                            #region F1 2005 PAL
                            // F1 2005 PAL SCES / F1 2005 PAL TCES
                            if (data.ApplicationId == 10954 || data.ApplicationId == 10952)
                            {
                                data.ClientObject.Queue(new MediusVersionServerResponse()
                                {
                                    MessageID = mediusVersionServerRequest.MessageID,
                                    VersionServer = "Medius Authentication Server Version: 2.10.0009 2.10.00.",
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                });
                            } else
                            #endregion
                            {
                                data.ClientObject.Queue(new MediusVersionServerResponse()
                                {
                                    MessageID = mediusVersionServerRequest.MessageID,
                                    VersionServer = "Medius Authentication Server Version 3.09",
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                });
                            }
                        } else {
                            // If MediusServerVersionOverride is false, we send our own Version String
                            data.ClientObject.Queue(new MediusVersionServerResponse()
                            {
                                MessageID = mediusVersionServerRequest.MessageID,
                                VersionServer = Settings.MASVersion,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                            });
                        }

                        break;
                    }

                #endregion

                #region Locations

                case MediusGetLocationsRequest getLocationsRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getLocationsRequest} without a session.");

                        var locations = Program.Settings?.Locations
                            ?.Where(x => x.AppIds == null || x.AppIds.Contains(data.ClientObject.ApplicationId))?.ToList();

                        if (locations == null || locations.Count == 0)
                        {
                            data.ClientObject.Queue(new MediusGetLocationsResponse()
                            {
                                MessageID = getLocationsRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusFail,
                                EndOfList = true
                            });
                        }
                        else
                        {
                            var responses = locations.Select(x => new MediusGetLocationsResponse()
                            {
                                MessageID = getLocationsRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                LocationId = x.Id,
                                LocationName = x.Name
                            }).ToList();

                            responses[responses.Count - 1].EndOfList = true;
                            data.ClientObject.Queue(responses);
                        }
                        break;
                    }

                case MediusPickLocationRequest pickLocationRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {pickLocationRequest} without a session.");


                        data.ClientObject.LocationId = pickLocationRequest.LocationID;
                        data.ClientObject.Queue(new MediusPickLocationResponse()
                        {
                            MessageID = pickLocationRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess
                        });

                        break;
                    }

                #endregion

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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {accountDeleteRequest} without being logged in.");

                        _ = Program.Database.DeleteAccount(data.ClientObject.AccountName, data.ClientObject.ApplicationId).ContinueWith((r) =>
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
                                //AccessKey = anonymousLoginRequest.SessionKey,
                                SessionKey = anonymousLoginRequest.SessionKey,
                                WorldID = 0,
                                //ServerKey = Program.GlobalAuthPublic,
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
                            _ = Program.Database.GetAccountByName(accountLoginRequest.Username, data.ClientObject.ApplicationId).ContinueWith(async (r) =>
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
                                        await Login(accountLoginRequest.MessageID, clientChannel, data, r.Result, false);
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
                                    }).ContinueWith(async (r) =>
                                    {
                                        if (r.IsCompletedSuccessfully && r.Result != null)
                                        {
                                            await Login(accountLoginRequest.MessageID, clientChannel, data, r.Result, false);
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
                            await data.ClientObject.Logout();
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
                                        // Failed due to special characters
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
                        if (Program.Manager.GetClientByAccountName(ticketLoginRequest.AccountName)?.IsLoggedIn ?? false)
                        {
                            data.ClientObject.Queue(new MediusTicketLoginResponse()
                            {
                                MessageID = ticketLoginRequest.MessageID,
                                StatusCodeTicketLogin = MediusCallbackStatus.MediusAccountLoggedIn
                            });
                        }
                        else
                        {   //Check if their MacBanned
                            _ = Program.Database.GetIsMacBanned(data.MachineId).ContinueWith((r) => 
                            {
                                if (r.IsCompletedSuccessfully && data != null && data.ClientObject != null && data.ClientObject.IsConnected)
                                {

                                    #region isBanned?
                                    Logger.Info(msg: $"Is Connected User MAC Banned: {r.Result}");

                                    if (r.Result)
                                    {

                                        // Account is banned
                                        // Temporary solution is to tell the client the login failed
                                        data?.ClientObject?.Queue(new MediusTicketLoginResponse()
                                        {
                                            MessageID = ticketLoginRequest.MessageID,
                                            StatusCodeTicketLogin = MediusCallbackStatus.MediusAccountBanned
                                        });

                                        // Send ban message
                                        QueueBanMessage(data);
                                    }
                                    #endregion

                                    Logger.Info($"Account found for AppId from Client: {data.ClientObject.ApplicationId}");

                                    _ = Program.Database.GetAccountByName(ticketLoginRequest.AccountName, data.ClientObject.ApplicationId).ContinueWith(async (r) =>
                                    {

                                        if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                            return;

                                        if (r.IsCompletedSuccessfully && r.Result != null && data != null && data.ClientObject != null && data.ClientObject.IsConnected)
                                        {

                                            if (r.Result.IsBanned == true)
                                            {
                                                // Account is banned
                                                // Respond with Statuscode MediusAccountBanned
                                                data?.ClientObject?.Queue(new MediusTicketLoginResponse()
                                                {
                                                    MessageID = ticketLoginRequest.MessageID,
                                                    StatusCodeTicketLogin = MediusCallbackStatus.MediusAccountBanned
                                                });

                                                // Then queue send ban message
                                                QueueBanMessage(data, "Your CID has been banned");
                                            }

                                            #region Beta Check 
                                            if (Program.Settings.Beta != null && Program.Settings.Beta.Enabled && Program.Settings.Beta.RestrictSignin && !Program.Settings.Beta.PermittedAccounts.Contains(r.Result.AccountName))
                                            {
                                                // Account not allowed to sign in
                                                data?.ClientObject?.Queue(new MediusTicketLoginResponse()
                                                {
                                                    MessageID = ticketLoginRequest.MessageID,
                                                    StatusCodeTicketLogin = MediusCallbackStatus.MediusFail
                                                });
                                            }
                                            #endregion

                                            await Login(ticketLoginRequest.MessageID, clientChannel, data, r.Result, true);
                                        }
                                        else
                                        {
                                            // Account not found, create new and login

                                            #region Beta Check
                                            // Check that account creation is enabled
                                            if (Program.Settings.Beta != null && Program.Settings.Beta.Enabled && !Program.Settings.Beta.AllowAccountCreation)
                                            {
                                                // Reply error
                                                data.ClientObject.Queue(new MediusTicketLoginResponse()
                                                {
                                                    MessageID = ticketLoginRequest.MessageID,
                                                    StatusCodeTicketLogin = MediusCallbackStatus.MediusFail,
                                                });
                                                return;
                                            }
                                            #endregion

                                            Logger.Info($"Account Not Found for AppId from Client: {data.ClientObject.ApplicationId}");

                                            _ = Program.Database.CreateAccount(new Database.Models.CreateAccountDTO()
                                            {
                                                AccountName = ticketLoginRequest.AccountName,
                                                AccountPassword = Utils.ComputeSHA256(ticketLoginRequest.Password),
                                                MachineId = data.MachineId,
                                                MediusStats = Convert.ToBase64String(new byte[Constants.ACCOUNTSTATS_MAXLEN]),
                                                AppId = data.ClientObject.ApplicationId
                                            }).ContinueWith(async (r) => {

                                                Logger.Info($"Creating New Account for user {ticketLoginRequest.AccountName}!");
                                                
                                                if (r.IsCompletedSuccessfully && r.Result != null)
                                                {
                                                    await Login(ticketLoginRequest.MessageID, clientChannel, data, r.Result, true);
                                                }
                                                else
                                                {
                                                    // Reply error
                                                    data.ClientObject.Queue(new MediusTicketLoginResponse()
                                                    {
                                                        MessageID = ticketLoginRequest.MessageID,
                                                        StatusCodeTicketLogin = MediusCallbackStatus.MediusInvalidPassword
                                                    });
                                                }
                                                
                                            });
                                        }
                                    });

                                }
                                else
                                {

                                    Logger.Info($"AppId from Client: {data.ClientObject.ApplicationId}");

                                    _ = Program.Database.GetAccountByName(ticketLoginRequest.AccountName, data.ClientObject.ApplicationId).ContinueWith(async (r) =>
                                    {

                                        if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                            return;

                                        if (r.IsCompletedSuccessfully && r.Result != null && data != null && data.ClientObject != null && data.ClientObject.IsConnected)
                                        {

                                            if (r.Result.IsBanned == true)
                                            {
                                                // Send ban message
                                                QueueBanMessage(data, "Your CID has been banned");

                                                // Account is banned
                                                // Temporary solution is to tell the client the login failed
                                                data?.ClientObject?.Queue(new MediusTicketLoginResponse()
                                                {
                                                    MessageID = ticketLoginRequest.MessageID,
                                                    StatusCodeTicketLogin = MediusCallbackStatus.MediusAccountBanned
                                                });

                                            }

                                            if (Program.Settings.Beta != null && Program.Settings.Beta.Enabled && Program.Settings.Beta.RestrictSignin && !Program.Settings.Beta.PermittedAccounts.Contains(r.Result.AccountName))
                                            {
                                                // Account not allowed to sign in
                                                data?.ClientObject?.Queue(new MediusTicketLoginResponse()
                                                {
                                                    MessageID = ticketLoginRequest.MessageID,
                                                    StatusCodeTicketLogin = MediusCallbackStatus.MediusFail
                                                });
                                            }

                                            await Login(ticketLoginRequest.MessageID, clientChannel, data, r.Result, true);
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
                                                    StatusCodeTicketLogin = MediusCallbackStatus.MediusFail,
                                                });
                                                return;
                                            }
                                            Logger.Info($"Account Not Found for AppId from Client: {data.ClientObject.ApplicationId}");

                                            _ = Program.Database.CreateAccount(new Database.Models.CreateAccountDTO()
                                            {
                                                AccountName = ticketLoginRequest.AccountName,
                                                AccountPassword = Utils.ComputeSHA256(ticketLoginRequest.Password),
                                                MachineId = data.MachineId,
                                                MediusStats = Convert.ToBase64String(new byte[Constants.ACCOUNTSTATS_MAXLEN]),
                                                AppId = data.ClientObject.ApplicationId
                                            }).ContinueWith(async (r) =>
                                            {
                                                Logger.Info($"Creating New Account for user {ticketLoginRequest.AccountName}!");

                                                await Login(ticketLoginRequest.MessageID, clientChannel, data, r.Result, true);

                                                /*
                                                if (r.IsCompletedSuccessfully && r.Result != null)
                                                {

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
                                                */
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
                        await Program.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_ON_GET_ALL_ANNOUNCEMENTS, new OnPlayerRequestArgs()
                        {
                            Player = data.ClientObject,
                            Request = getAllAnnouncementsRequest
                        });

                        _ = Program.Database.GetLatestAnnouncements(data.ApplicationId).ContinueWith((r) =>
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
                                        AnnouncementID = result.Id++,
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
                        await Program.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_ON_GET_ANNOUNCEMENTS, new OnPlayerRequestArgs()
                        {
                            Player = data.ClientObject,
                            Request = getAnnouncementsRequest
                        });

                        _ = Program.Database.GetLatestAnnouncement(data.ApplicationId).ContinueWith((r) =>
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
                                    AnnouncementID = r.Result.Id++,
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
                        await Program.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_ON_GET_POLICY, new OnPlayerRequestArgs()
                        {
                            Player = data.ClientObject,
                            Request = getPolicyRequest
                        });

                        switch (getPolicyRequest.Policy)
                        {
                            case MediusPolicyType.Privacy:
                                {
                                    _ = Program.Database.GetUsagePolicy().ContinueWith((r) =>
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
                                    _ = Program.Database.GetUsagePolicy().ContinueWith((r) =>
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

                #region Ladders

                case MediusGetLadderStatsRequest getLadderStatsRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getLadderStatsRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getLadderStatsRequest} without being logged in.");

                        switch (getLadderStatsRequest.LadderType)
                        {
                            case MediusLadderType.MediusLadderTypePlayer:
                                {
                                    _ = Program.Database.GetAccountById(getLadderStatsRequest.AccountID_or_ClanID).ContinueWith((r) =>
                                    {
                                        if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                            return;

                                        if (r.IsCompletedSuccessfully && r.Result != null)
                                        {
                                            data.ClientObject.Queue(new MediusGetLadderStatsResponse()
                                            {
                                                MessageID = getLadderStatsRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                                Stats = r.Result.AccountStats
                                            });
                                        }
                                        else
                                        {
                                            data.ClientObject.Queue(new MediusGetLadderStatsResponse()
                                            {
                                                MessageID = getLadderStatsRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusDBError
                                            });
                                        }
                                    });
                                    break;
                                }
                            case MediusLadderType.MediusLadderTypeClan:
                                {
                                    _ = Program.Database.GetClanById(getLadderStatsRequest.AccountID_or_ClanID).ContinueWith((r) =>
                                    {
                                        if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                            return;

                                        if (r.IsCompletedSuccessfully && r.Result != null)
                                        {
                                            data.ClientObject.Queue(new MediusGetLadderStatsResponse()
                                            {
                                                MessageID = getLadderStatsRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                                Stats = r.Result.ClanStats
                                            });
                                        }
                                        else
                                        {
                                            data.ClientObject.Queue(new MediusGetLadderStatsResponse()
                                            {
                                                MessageID = getLadderStatsRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusDBError
                                            });
                                        }
                                    });
                                    break;
                                }
                            default:
                                {
                                    Logger.Warn($"Unhandled MediusGetLadderStatsRequest {getLadderStatsRequest}");
                                    break;
                                }
                        }
                        break;
                    }

                case MediusGetLadderStatsWideRequest getLadderStatsWideRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getLadderStatsWideRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getLadderStatsWideRequest} without being logged in.");

                        switch (getLadderStatsWideRequest.LadderType)
                        {
                            case MediusLadderType.MediusLadderTypePlayer:
                                {
                                    _ = Program.Database.GetAccountById(getLadderStatsWideRequest.AccountID_or_ClanID).ContinueWith((r) =>
                                    {
                                        if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                            return;

                                        if (r.IsCompletedSuccessfully && r.Result != null)
                                        {
                                            data.ClientObject.Queue(new MediusGetLadderStatsWideResponse()
                                            {
                                                MessageID = getLadderStatsWideRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                                AccountID_or_ClanID = r.Result.AccountId,
                                                Stats = r.Result.AccountWideStats
                                            });
                                        }
                                        else
                                        {
                                            data.ClientObject.Queue(new MediusGetLadderStatsWideResponse()
                                            {
                                                MessageID = getLadderStatsWideRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusDBError
                                            });
                                        }
                                    });
                                    break;
                                }
                            case MediusLadderType.MediusLadderTypeClan:
                                {
                                    _ = Program.Database.GetClanById(getLadderStatsWideRequest.AccountID_or_ClanID).ContinueWith((r) =>
                                    {
                                        if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                            return;

                                        if (r.IsCompletedSuccessfully && r.Result != null)
                                        {
                                            data.ClientObject.Queue(new MediusGetLadderStatsWideResponse()
                                            {
                                                MessageID = getLadderStatsWideRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                                AccountID_or_ClanID = r.Result.ClanId,
                                                Stats = r.Result.ClanWideStats
                                            });
                                        }
                                        else
                                        {
                                            data.ClientObject.Queue(new MediusGetLadderStatsWideResponse()
                                            {
                                                MessageID = getLadderStatsWideRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusDBError
                                            });
                                        }
                                    });
                                    break;
                                }
                            default:
                                {
                                    Logger.Warn($"Unhandled MediusGetLadderStatsWideRequest {getLadderStatsWideRequest}");
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

                #region Time
                case MediusGetServerTimeRequest getServerTimeRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getServerTimeRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getServerTimeRequest} without being logged in.");



                        TimeZoneInfo localZone = TimeZoneInfo.Local;

                        if (localZone != null)
                        {
                            if ("(UTC - 12:00) International Date Line West" == localZone.DisplayName) {
                                //Send International Date Line West
                                data.ClientObject.Queue(new MediusGetServerTimeResponse()
                                {
                                    MessageID = getServerTimeRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                    Local_server_timezone = MediusTimeZone.MediusTimeZone_IDLW
                                });
                            }

                            if ("(GMT - 6:00) Central Standard Time" == localZone.DisplayName) {

                                //Send Central Standard Time
                                data.ClientObject.Queue(new MediusGetServerTimeResponse()
                                {
                                    MessageID = getServerTimeRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                    Local_server_timezone = MediusTimeZone.MediusTimeZone_CST
                                });
                            }
                        } else {

                            //default
                            data.ClientObject.Queue(new MediusGetServerTimeResponse()
                            {
                                MessageID = getServerTimeRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                Local_server_timezone = MediusTimeZone.MediusTimeZone_GMT
                            });
                        }

                        break;
                    }
                #endregion

                #region GetMyIP
                //Syphon Filter - The Omega Strain Beta

                case MediusGetMyIPRequest getMyIpRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getMyIpRequest} without a session.");

                        data.ClientObject.Queue(new MediusGetMyIPResponse()
                        {
                            MessageID = getMyIpRequest.MessageID,
                            IP = (clientChannel.RemoteAddress as IPEndPoint)?.Address,
                            StatusCode = MediusCallbackStatus.MediusSuccess
                        });
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

        #region Login

        private async Task Login(MessageId messageId, IChannel clientChannel, ChannelData data, Database.Models.AccountDTO accountDto, bool ticket)
        {
            var fac = new PS2CipherFactory();
            var rsa = fac.CreateNew(CipherContext.RSA_AUTH) as PS2_RSA;

            //
            await data.ClientObject.Login(accountDto);

            #region Update DB IP and CID
            _ = Program.Database.PostAccountIp(accountDto.AccountId, (clientChannel.RemoteAddress as IPEndPoint).Address.MapToIPv4().ToString());
            if (!string.IsNullOrEmpty(data.MachineId))
                _ = Program.Database.PostMachineId(data.ClientObject.AccountId, data.MachineId);
            #endregion

            // Add to logged in clients
            Program.Manager.AddClient(data.ClientObject);

            // 
            Logger.Info($"LOGGING IN AS {data.ClientObject.AccountName} with access token {data.ClientObject.Token}");

            // Put client in default channel
            data.ClientObject.JoinChannel(Program.Manager.GetDefaultLobbyChannel(data.ApplicationId));

            // Tell client
            if (ticket == true)
            {
                #region IF PS3 Client
                data.ClientObject.Queue(new MediusTicketLoginResponse()
                {
                    //TicketLoginResponse
                    MessageID = messageId,
                    StatusCodeTicketLogin = MediusCallbackStatus.MediusSuccess,
                    PasswordType = MediusPasswordType.MediusPasswordNotSet,

                    //AccountLoginResponse Wrapped
                    MessageID2 = messageId,
                    StatusCodeAccountLogin = MediusCallbackStatus.MediusSuccess,
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
                });
                // Prepare for transition to lobby server
                data.ClientObject.KeepAliveUntilNextConnection();
                #endregion
            }
            else
            {
                #region If PS2/PSP
                data.ClientObject.Queue(new MediusAccountLoginResponse()
                {
                    MessageID = messageId,
                    StatusCode = MediusCallbackStatus.MediusSuccess,
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
                });
                // Prepare for transition to lobby server
                data.ClientObject.KeepAliveUntilNextConnection();
                #endregion
            }
        }

        #endregion 

    }
}
