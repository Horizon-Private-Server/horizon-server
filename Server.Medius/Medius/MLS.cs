using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;
using RT.Common;
using RT.Cryptography;
using RT.Models;
using RT.Models.Misc;
using Server.Common;
using Server.Database.Models;
using Server.Medius.Config;
using Server.Medius.Models;
using Server.Medius.PluginArgs;
using Server.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Server.Medius
{
    public class MLS : BaseMediusComponent
    {
        static readonly IInternalLogger _logger = InternalLoggerFactory.GetInstance<MLS>();

        protected override IInternalLogger Logger => _logger;
        public override int Port => Program.Settings.MLSPort;

        public static ServerSettings Settings = new ServerSettings();

        public MLS()
        {

        }

        public ClientObject ReserveClient(MediusSessionBeginRequest request)
        {
            var client = new ClientObject();
            client.BeginSession();
            return client;
        }

        public ClientObject ReserveClient1(MediusSessionBeginRequest1 request)
        {
            var client = new ClientObject();
            client.BeginSession();
            return client;
        }

        public ClientObject ReserveClient(MediusExtendedSessionBeginRequest request)
        {
            var client = new ClientObject();
            client.BeginSession();
            return client;
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
                        // 
                        data.ApplicationId = clientConnectTcp.AppId;

                        data.ClientObject = Program.Manager.GetClientByAccessToken(clientConnectTcp.AccessToken);

                        data.ClientObject = Program.Manager.GetClientByAccountName(data.ClientObject.AccountName);

                        //If Client Object is null, then ignore
                        if (data.ClientObject == null)
                        {
                            Logger.Error($"IGNORING CLIENT 1 {data} || {data.ClientObject}");
                            data.Ignore = true;
                        }
                        else
                        {
                            // 
                            data.ClientObject.OnConnected();

                            // Update our client object to use existing one
                            data.ClientObject.ApplicationId = clientConnectTcp.AppId;


                            if (scertClient.IsPS3Client)
                            {
                                //CAC & Warhawk
                                if (data.ClientObject.ApplicationId == 20623 || data.ClientObject.ApplicationId == 20624 || data.ClientObject.ApplicationId == 20043 || data.ClientObject.ApplicationId == 20044)
                                {
                                    Queue(new RT_MSG_SERVER_CONNECT_ACCEPT_TCP()
                                    {
                                        UNK_00 = 0,
                                        UNK_02 = GenerateNewScertClientId(),
                                        UNK_06 = 0x0001,
                                        IP = (clientChannel.RemoteAddress as IPEndPoint)?.Address
                                    }, clientChannel);
                                } else {
                                    Queue(new RT_MSG_SERVER_CONNECT_REQUIRE() { ReqServerPassword = 0x00, Contents = Utils.FromString("4802") }, clientChannel);
                                }
                            }
                            else if (scertClient.MediusVersion > 108)
                            {
                                Queue(new RT_MSG_SERVER_CONNECT_REQUIRE() { ReqServerPassword = 0x02, Contents = Utils.FromString("4802") }, clientChannel);
                            }

                            //If Frequency, TMBO, Socom 1, or ATV Offroad Fury 2 then
                            if (data.ApplicationId == 10010 || data.ApplicationId == 10031 || data.ApplicationId == 10274 || data.ApplicationId == 10284)
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
                        if (!scertClient.IsPS3Client)
                        {
                            Queue(new RT_MSG_SERVER_CRYPTKEY_GAME() { Key = scertClient.CipherService.GetPublicKey(CipherContext.RC_CLIENT_SESSION) }, clientChannel);
                        }
                        Queue(new RT_MSG_SERVER_CONNECT_ACCEPT_TCP()
                        {
                            UNK_00 = 0x0019,
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
                case RT_MSG_SERVER_ECHO serverEchoReply:
                    {

                        break;
                    }
                case RT_MSG_CLIENT_ECHO clientEcho:
                    {
                        if (data.ClientObject == null || !data.ClientObject.IsLoggedIn)
                            break;

                        Queue(new RT_MSG_CLIENT_ECHO() { Value = clientEcho.Value }, clientChannel);
                        break;
                    }
                case RT_MSG_CLIENT_APP_TOSERVER clientAppToServer:
                    {
                        await ProcessMediusMessage(clientAppToServer.Message, clientChannel, data);
                        break;
                    }

                case RT_MSG_CLIENT_DISCONNECT _:
                    {
                        //Medius 1.08 (Used on WRC 4) haven't a state
                        if (scertClient.MediusVersion <= 108)
                            _ = clientChannel.CloseAsync();
                        else
                            data.State = ClientState.DISCONNECTED;
                        _ = clientChannel.CloseAsync();
                        break;
                    }

                case RT_MSG_CLIENT_DISCONNECT_WITH_REASON _:
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

        protected virtual async Task ProcessMediusMessage(BaseMediusMessage message, IChannel clientChannel, ChannelData data)
        {
            if (message == null)
                return;

            switch (message)
            {
                #region Session End

                case MediusSessionEndRequest sessionEndRequest:
                    {
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} is trying to end session without an Client Object");

                        // End session
                        data.ClientObject.EndSession();

                        // 
                        data.ClientObject = null;

                        Queue(new RT_MSG_SERVER_APP()
                        {
                            Message = new MediusSessionEndResponse()
                            {
                                MessageID = sessionEndRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess
                            }
                        }, clientChannel);
                        break;
                    }

                #endregion

                #region Logout

                case MediusAccountLogoutRequest accountLogoutRequest:
                    {
                        MediusCallbackStatus status = MediusCallbackStatus.MediusFail;

                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {accountLogoutRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {accountLogoutRequest} without being logged in.");

                        // Check token
                        if (accountLogoutRequest.SessionKey == data.ClientObject.SessionKey)
                        {
                            // 
                            Logger.Info($"{data.ClientObject.AccountName} has logged out.");

                            // 
                            status = MediusCallbackStatus.MediusSuccess;

                            // Logout
                            await data.ClientObject.Logout();
                        }

                        // Reply
                        data.ClientObject.Queue(new MediusAccountLogoutResponse()
                        {
                            MessageID = accountLogoutRequest.MessageID,
                            StatusCode = status
                        });
                        break;
                    }

                #endregion

                #region Announcements / Policy

                case MediusGetAllAnnouncementsRequest getAllAnnouncementsRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getAllAnnouncementsRequest} without a session.");

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
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getAnnouncementsRequest} without a session.");

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
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getPolicyRequest} without a session.");

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

                #region NpIdPost

                //Makes a PostRequest to the Medius Lobby Server on Connect sending the SCE_NPID_MAXLEN data blob
                case MediusNpIdPostRequest mediusNpIdPostRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {mediusNpIdPostRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {mediusNpIdPostRequest} without being logged in.");

                        _ = Program.Database.PostNpId(mediusNpIdPostRequest.data, data.ApplicationId);
                        
                        data.ClientObject.Queue(new MediusStatusResponse0()
                        {
                            MessageID = mediusNpIdPostRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess
                        });
                        break;
                    }

                #endregion

                #region Match
                //Unimplemented
                case MediusMatchGetSupersetListRequest matchGetSupersetListRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {matchGetSupersetListRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {matchGetSupersetListRequest} without being logged in.");



                        // Default - No Result
                        data.ClientObject.Queue(new MediusMatchGetSupersetListResponse()
                        {
                            MessageID = matchGetSupersetListRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                            EndOfList = true,
                            SupersetID = 1,
                            SupersetName = "Co-Op",
                            SupersetDescription = "Test",
                            SupersetExtraInfo = null,
                        });

                        break;
                    }
                    //Unimplemented
                case MediusMatchFindGameRequest matchFindGameRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {matchFindGameRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {matchFindGameRequest} without being logged in.");



                        // Success
                        data.ClientObject.Queue(new MediusMatchFindGameStatusResponse()
                        {
                            MessageID = matchFindGameRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                        });

                        break;
                    }

                #endregion

                #region Version Server

                case MediusVersionServerRequest mediusVersionServerRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {mediusVersionServerRequest} without a session.");
                        
                        data.ClientObject.Queue(new MediusVersionServerResponse()
                        {
                            MessageID = mediusVersionServerRequest.MessageID,
                            VersionServer = "Medius Lobby Server 1.40.PRE8",
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                        });

                        break;
                    }

                #endregion

                #region Account

                case MediusAccountGetIDRequest accountGetIdRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {accountGetIdRequest} without a session.");

                        _ = Program.Database.GetAccountByName(accountGetIdRequest.AccountName, data.ClientObject.ApplicationId).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                data.ClientObject.Queue(new MediusAccountGetIDResponse()
                                {
                                    MessageID = accountGetIdRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                    AccountID = r.Result.AccountId
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusAccountGetIDResponse()
                                {
                                    MessageID = accountGetIdRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusAccountNotFound
                                });
                            }
                        });
                        break;
                    }

                case MediusAccountUpdatePasswordRequest accountUpdatePasswordRequest:
                    {

                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {accountUpdatePasswordRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {accountUpdatePasswordRequest} without being logged in.");


                        // Post
                        _ = Program.Database.PostAccountUpdatePassword(data.ClientObject.AccountId, accountUpdatePasswordRequest.OldPassword, accountUpdatePasswordRequest.NewPassword).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result)
                            {
                                data.ClientObject.Queue(new MediusAccountUpdatePasswordStatusResponse()
                                {
                                    MessageID = accountUpdatePasswordRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusAccountUpdatePasswordStatusResponse()
                                {
                                    MessageID = accountUpdatePasswordRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusDBError
                                });
                            }
                        });


                        break;
                    }

                case MediusAccountUpdateStatsRequest accountUpdateStatsRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {accountUpdateStatsRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {accountUpdateStatsRequest} without being logged in.");

                        // Post
                        _ = Program.Database.PostMediusStats(data.ClientObject.AccountId, Convert.ToBase64String(accountUpdateStatsRequest.Stats)).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result)
                            {
                                data.ClientObject.Queue(new MediusAccountUpdateStatsResponse()
                                {
                                    MessageID = accountUpdateStatsRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusAccountUpdateStatsResponse()
                                {
                                    MessageID = accountUpdateStatsRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusDBError
                                });
                            }
                        });
                        break;
                    }

                #endregion

                #region Buddy List

                case MediusBuddySetListRequest buddySetListRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {buddySetListRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {buddySetListRequest} without being logged in.");

                        //Fetch PS3 Buddy List from the current connected client
                        data.ClientObject.FriendsListPS3 = buddySetListRequest.List;
                        //If FriendsList on PS3 is null, return No Result.
                        if (data.ClientObject.FriendsListPS3 == null)
                        {
                            //If Friends list from NP is actually null, send No Result
                            data.ClientObject.Queue(new MediusBuddySetListResponse()
                            {
                                MessageID = buddySetListRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNoResult,
                            });
                        } else {
                            // Success NP Buddy List is NOT NULL - Return Success!
                            data.ClientObject.Queue(new MediusBuddySetListResponse()
                            {
                                MessageID = buddySetListRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                            });
                        }

                        break;
                    }

                case MediusIgnoreSetListRequest ignoreSetListRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {ignoreSetListRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {ignoreSetListRequest} without being logged in.");

                        //Fetch PS3 Buddy List from the current connected client
                        data.ClientObject.FriendsListPS3 = ignoreSetListRequest.List;
                        //If FriendsList on PS3 is null, return No Result.
                        if (data.ClientObject.FriendsListPS3 == null)
                        {
                            //If Friends list from NP is actually null, send No Result
                            data.ClientObject.Queue(new MediusBuddySetListResponse()
                            {
                                MessageID = ignoreSetListRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNoResult,
                            });
                        } else {
                            // Success NP Buddy List is NOT NULL - Return Success!
                            data.ClientObject.Queue(new MediusBuddySetListResponse()
                            {
                                MessageID = ignoreSetListRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                            });
                        }
                        break;
                    }

                case MediusAddToBuddyListRequest addToBuddyListRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {addToBuddyListRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {addToBuddyListRequest} without being logged in.");

                        // Add
                        _ = Program.Database.AddBuddy(new BuddyDTO()
                        {
                            AccountId = data.ClientObject.AccountId,
                            BuddyAccountId = addToBuddyListRequest.AccountID
                        }).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result)
                            {
                                data.ClientObject.Queue(new MediusAddToBuddyListResponse()
                                {
                                    MessageID = addToBuddyListRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess
                                });

                                _ = data.ClientObject.RefreshAccount();
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusAddToBuddyListResponse()
                                {
                                    MessageID = addToBuddyListRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusDBError
                                });
                            }
                        });
                        break;
                    }

                case MediusAddToBuddyListConfirmationRequest addToBuddyListConfirmationRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {addToBuddyListConfirmationRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {addToBuddyListConfirmationRequest} without being logged in.");

                        //
                        _ = Program.Database.GetAccountById(data.ClientObject.AccountId).ContinueWith((r) => {

                            if (data == null || data.ClientObject == null || data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result != null) { 
                                
                                foreach(var player in r.Result.Friends)
                                {
                                    var buddyPlayer = Program.Manager.GetClientByAccountId(player.AccountId);

                                    data.ClientObject.Queue(new MediusAddToBuddyListConfirmationResponse()
                                    {
                                        MessageID = addToBuddyListConfirmationRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        TargetAccountID = addToBuddyListConfirmationRequest.TargetAccountID,
                                        TargetAccountName = player.AccountName,
                                    });
                                }

                            }
                        });

                        break;
                    }

                case MediusGetBuddyInvitationsRequest getBuddyInvitationsRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getBuddyInvitationsRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getBuddyInvitationsRequest} without being logged in.");


                        //
                        _ = Program.Database.GetAccountById(data.ClientObject.AccountId).ContinueWith((r) => {

                            if (data == null || data.ClientObject == null || data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {

                                // Responses
                                List<MediusGetBuddyInvitationsResponse> buddyInvitationsResponses = new List<MediusGetBuddyInvitationsResponse>();

                                foreach (var player in r.Result.Friends)
                                {
                                    var buddyPlayer = Program.Manager.GetClientByAccountId(player.AccountId);

                                    data.ClientObject.Queue(new MediusGetBuddyInvitationsResponse()
                                    {
                                        MessageID = getBuddyInvitationsRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        AccountID = buddyPlayer.AccountId,
                                        AccountName = buddyPlayer.AccountName,
                                        AddType = MediusBuddyAddType.AddSingle,
                                        EndOfList = false
                                    });
                                }

                                // If we have any responses then send them
                                if (buddyInvitationsResponses.Count > 0)
                                {
                                    // Ensure AccountId is incremented per friend 
                                    buddyInvitationsResponses[buddyInvitationsResponses.Count - 1].AccountID++;
                                    // Ensure the last response is tagged as EndOfList
                                    buddyInvitationsResponses[buddyInvitationsResponses.Count - 1].EndOfList = true;
                                    // Send friends
                                    data.ClientObject.Queue(buddyInvitationsResponses);
                                }
                                else
                                {
                                    // No Invitations
                                    data.ClientObject.Queue(new MediusGetBuddyInvitationsResponse()
                                    {
                                        MessageID = getBuddyInvitationsRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusNoResult,
                                        EndOfList = true
                                    });
                                }

                            }
                        });


                        break;
                    }

                case MediusRemoveFromBuddyListRequest removeFromBuddyListRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {removeFromBuddyListRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {removeFromBuddyListRequest} without being logged in.");

                        // Remove
                        _ = Program.Database.RemoveBuddy(new BuddyDTO()
                        {
                            AccountId = data.ClientObject.AccountId,
                            BuddyAccountId = removeFromBuddyListRequest.AccountID
                        }).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result)
                            {
                                data.ClientObject.Queue(new MediusRemoveFromBuddyListResponse()
                                {
                                    MessageID = removeFromBuddyListRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess
                                });

                                _ = data.ClientObject.RefreshAccount();
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusRemoveFromBuddyListResponse()
                                {
                                    MessageID = removeFromBuddyListRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusDBError
                                });
                            }
                        });
                        break;
                    }

                case MediusGetBuddyList_ExtraInfoRequest getBuddyList_ExtraInfoRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getBuddyList_ExtraInfoRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel},{data.ClientObject} sent {getBuddyList_ExtraInfoRequest} without being logged in.");

                        /*
                        // Responses
                        List<MediusGetBuddyList_ExtraInfoResponse> friendListResponses = new List<MediusGetBuddyList_ExtraInfoResponse>();

                        // Iterate through friends and build a response for each
                        foreach (var friend in data.ClientObject.FriendsList)
                        {
                            var friendClient = Program.Manager.GetClientByAccountId(friend.Key);
                            friendListResponses.Add(new MediusGetBuddyList_ExtraInfoResponse()
                            {
                                MessageID = getBuddyList_ExtraInfoRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                AccountID = friend.Key,
                                AccountName = friendClient.AccountName,
                                OnlineState = new MediusPlayerOnlineState()
                                {
                                    ConnectStatus = (friendClient != null && friendClient.IsLoggedIn) ? friendClient.PlayerStatus : MediusPlayerStatus.MediusPlayerDisconnected,
                                    MediusLobbyWorldID = friendClient?.CurrentChannel?.Id ?? Program.Manager.GetDefaultLobbyChannel(data.ApplicationId).Id,
                                    MediusGameWorldID = friendClient?.CurrentGame?.Id ?? -1,
                                    GameName = friendClient?.CurrentGame?.GameName,
                                    LobbyName = friendClient?.CurrentChannel?.Name ?? ""
                                },
                                EndOfList = false
                            });
                        }
                        */


                        /*
                        // Iterate through friends and build a response for each
                        foreach (var friend in data.ClientObject.FriendsListPS3)
                        {
                            var friendClientPS3 = Program.Manager.GetClientByAccountId(friend.);
                            friendListResponses.Add(new MediusGetBuddyList_ExtraInfoResponse()
                            {
                                MessageID = getBuddyList_ExtraInfoRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                AccountID = friendClientPS3.AccountId,
                                AccountName = Convert.ToString(friend),
                                OnlineState = new MediusPlayerOnlineState()
                                {
                                    ConnectStatus = (friendClientPS3 != null && friendClientPS3.IsLoggedIn) ? friendClientPS3.PlayerStatus : MediusPlayerStatus.MediusPlayerDisconnected,
                                    MediusLobbyWorldID = friendClientPS3?.CurrentChannel?.Id ?? Program.Manager.GetDefaultLobbyChannel(data.ApplicationId).Id,
                                    MediusGameWorldID = friendClientPS3?.CurrentGame?.Id ?? -1,
                                    GameName = friendClientPS3?.CurrentGame?.GameName,
                                    LobbyName = friendClientPS3?.CurrentChannel?.Name ?? ""
                                },
                                EndOfList = false
                            });
                        }
                        */
                        
                        _ = Program.Database.GetAccountById(data.ClientObject.AccountId).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                // Responses
                                List<MediusGetBuddyList_ExtraInfoResponse> friendListResponses = new List<MediusGetBuddyList_ExtraInfoResponse>();

                                // Iterate through friends and build a response for each
                                foreach (var friend in r.Result.Friends)
                                {
                                    var friendClient = Program.Manager.GetClientByAccountId(friend.AccountId);
                                    friendListResponses.Add(new MediusGetBuddyList_ExtraInfoResponse()
                                    {
                                        MessageID = getBuddyList_ExtraInfoRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        AccountID = friend.AccountId,
                                        AccountName = friend.AccountName,
                                        OnlineState = new MediusPlayerOnlineState()
                                        {
                                            ConnectStatus = (friendClient != null && friendClient.IsLoggedIn) ? friendClient.PlayerStatus : MediusPlayerStatus.MediusPlayerDisconnected,
                                            MediusLobbyWorldID = friendClient?.CurrentChannel?.Id ?? Program.Manager.GetDefaultLobbyChannel(data.ApplicationId).Id,
                                            MediusGameWorldID = friendClient?.CurrentGame?.Id ?? -1,
                                            GameName = friendClient?.CurrentGame?.GameName ?? "",
                                            LobbyName = friendClient?.CurrentChannel?.Name ?? ""
                                        },
                                        EndOfList = false
                                    });
                                }

                                // If we have any responses then send them
                                if (friendListResponses.Count > 0)
                                {
                                    // Ensure the last response is tagged as EndOfList
                                    friendListResponses[friendListResponses.Count - 1].EndOfList = true;

                                    // Send friends
                                    data.ClientObject.Queue(friendListResponses);
                                }
                                else
                                {
                                    // No friends
                                    data.ClientObject.Queue(new MediusGetBuddyList_ExtraInfoResponse()
                                    {
                                        MessageID = getBuddyList_ExtraInfoRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusNoResult,
                                        EndOfList = true
                                    });
                                }
                            }
                            else
                            {
                                // DB error
                                data.ClientObject.Queue(new MediusGetBuddyList_ExtraInfoResponse()
                                {
                                    MessageID = getBuddyList_ExtraInfoRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusDBError,
                                    EndOfList = true
                                });
                            }
                        });
                        break;
                    }

                #endregion

                #region Ignore List

                case MediusGetIgnoreListRequest getIgnoreListRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getIgnoreListRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getIgnoreListRequest} without being logged in.");

                        // 
                        _ = Program.Database.GetAccountById(data.ClientObject.AccountId).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                // Responses
                                List<MediusGetIgnoreListResponse> ignoredListResponses = new List<MediusGetIgnoreListResponse>();

                                // Iterate and send to client
                                foreach (var player in r.Result.Ignored)
                                {
                                    var playerClient = Program.Manager.GetClientByAccountId(player.AccountId);
                                    ignoredListResponses.Add(new MediusGetIgnoreListResponse()
                                    {
                                        MessageID = getIgnoreListRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        IgnoreAccountID = player.AccountId,
                                        IgnoreAccountName = player.AccountName,
                                        PlayerStatus = playerClient?.PlayerStatus ?? MediusPlayerStatus.MediusPlayerDisconnected,
                                        EndOfList = false
                                    });
                                }

                                // If we have any responses then send them
                                if (ignoredListResponses.Count > 0)
                                {
                                    // Ensure the last response is tagged as EndOfList
                                    ignoredListResponses[ignoredListResponses.Count - 1].EndOfList = true;

                                    // Send friends
                                    data.ClientObject.Queue(ignoredListResponses);
                                }
                                else
                                {
                                    // No ignored
                                    data.ClientObject.Queue(new MediusGetIgnoreListResponse()
                                    {
                                        MessageID = getIgnoreListRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusNoResult,
                                        EndOfList = true
                                    });
                                }
                            }
                            else
                            {
                                // DB error
                                data.ClientObject.Queue(new MediusGetIgnoreListResponse()
                                {
                                    MessageID = getIgnoreListRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusDBError,
                                    EndOfList = true
                                });
                            }
                        });
                        break;
                    }

                case MediusAddToIgnoreListRequest addToIgnoreList:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {addToIgnoreList} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {addToIgnoreList} without being logged in.");

                        // Add
                        _ = Program.Database.AddIgnored(new IgnoredDTO()
                        {
                            AccountId = data.ClientObject.AccountId,
                            IgnoredAccountId = addToIgnoreList.IgnoreAccountID
                        }).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result)
                            {
                                data.ClientObject.Queue(new MediusAddToIgnoreListResponse()
                                {
                                    MessageID = addToIgnoreList.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusAddToIgnoreListResponse()
                                {
                                    MessageID = addToIgnoreList.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusDBError
                                });
                            }
                        });
                        break;
                    }

                case MediusRemoveFromIgnoreListRequest removeFromIgnoreListRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {removeFromIgnoreListRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {removeFromIgnoreListRequest} without being logged in.");

                        // Remove
                        _ = Program.Database.RemoveIgnored(new IgnoredDTO()
                        {
                            AccountId = data.ClientObject.AccountId,
                            IgnoredAccountId = removeFromIgnoreListRequest.IgnoreAccountID
                        }).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result)
                            {
                                data.ClientObject.Queue(new MediusRemoveFromIgnoreListResponse()
                                {
                                    MessageID = removeFromIgnoreListRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusRemoveFromIgnoreListResponse()
                                {
                                    MessageID = removeFromIgnoreListRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusDBError
                                });
                            }
                        });
                        break;
                    }

                #endregion

                #region Ladder Stats

                case MediusUpdateLadderStatsRequest updateLadderStatsRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {updateLadderStatsRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {updateLadderStatsRequest} without being logged in.");

                        // pass to plugins
                        var pluginMessage = new OnPlayerWideStatsArgs()
                        {
                            Game = data.ClientObject.CurrentGame,
                            Player = data.ClientObject,
                            IsClan = updateLadderStatsRequest.LadderType == MediusLadderType.MediusLadderTypeClan,
                            WideStats = updateLadderStatsRequest.Stats
                        };
                        await Program.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_POST_WIDE_STATS, pluginMessage);

                        // reject
                        if (pluginMessage.Reject)
                        {
                            data.ClientObject.Queue(new MediusUpdateLadderStatsResponse()
                            {
                                MessageID = updateLadderStatsRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusFail
                            });
                            break;
                        }

                        switch (updateLadderStatsRequest.LadderType)
                        {
                            case MediusLadderType.MediusLadderTypePlayer:
                                {

                                    _ = Program.Database.PostAccountLadderStats(new StatPostDTO()
                                    {
                                        AccountId = data.ClientObject.AccountId,
                                        Stats = pluginMessage.WideStats
                                    }).ContinueWith((r) =>
                                    {
                                        if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                            return;

                                        if (r.IsCompletedSuccessfully && r.Result)
                                        {
                                            data.ClientObject.WideStats = pluginMessage.WideStats;
                                            data.ClientObject.Queue(new MediusUpdateLadderStatsResponse()
                                            {
                                                MessageID = updateLadderStatsRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusSuccess
                                            });
                                        }
                                        else
                                        {
                                            data.ClientObject.Queue(new MediusUpdateLadderStatsResponse()
                                            {
                                                MessageID = updateLadderStatsRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusDBError
                                            });
                                        }
                                    });
                                    break;
                                }
                            case MediusLadderType.MediusLadderTypeClan:
                                {
                                    _ = Program.Database.PostClanLadderStats(data.ClientObject.AccountId, data.ClientObject.ClanId, pluginMessage.WideStats).ContinueWith((r) =>
                                    {
                                        if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                            return;

                                        if (r.IsCompletedSuccessfully && r.Result)
                                        {
                                            data.ClientObject.WideStats = pluginMessage.WideStats;
                                            data.ClientObject.Queue(new MediusUpdateLadderStatsResponse()
                                            {
                                                MessageID = updateLadderStatsRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusSuccess
                                            });
                                        }
                                        else
                                        {
                                            data.ClientObject.Queue(new MediusUpdateLadderStatsResponse()
                                            {
                                                MessageID = updateLadderStatsRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusDBError
                                            });
                                        }
                                    });
                                    break;
                                }
                            default:
                                {
                                    Logger.Warn($"Unhandled MediusUpdateLadderStatsRequest {updateLadderStatsRequest}");
                                    break;
                                }
                        }
                        break;
                    }

                case MediusUpdateLadderStatsWideRequest updateLadderStatsWideRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {updateLadderStatsWideRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {updateLadderStatsWideRequest} without being logged in.");

                        // pass to plugins
                        var pluginMessage = new OnPlayerWideStatsArgs()
                        {
                            Game = data.ClientObject.CurrentGame,
                            Player = data.ClientObject,
                            IsClan = updateLadderStatsWideRequest.LadderType == MediusLadderType.MediusLadderTypeClan,
                            WideStats = updateLadderStatsWideRequest.Stats
                        };
                        await Program.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_POST_WIDE_STATS, pluginMessage);

                        // reject
                        if (pluginMessage.Reject)
                        {
                            data.ClientObject.Queue(new MediusUpdateLadderStatsWideResponse()
                            {
                                MessageID = updateLadderStatsWideRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusFail
                            });
                            break;
                        }

                        switch (updateLadderStatsWideRequest.LadderType)
                        {
                            case MediusLadderType.MediusLadderTypePlayer:
                                {

                                    _ = Program.Database.PostAccountLadderStats(new StatPostDTO()
                                    {
                                        AccountId = data.ClientObject.AccountId,
                                        Stats = pluginMessage.WideStats
                                    }).ContinueWith((r) =>
                                    {
                                        if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                            return;

                                        if (r.IsCompletedSuccessfully && r.Result)
                                        {
                                            data.ClientObject.WideStats = pluginMessage.WideStats;
                                            data.ClientObject.Queue(new MediusUpdateLadderStatsWideResponse()
                                            {
                                                MessageID = updateLadderStatsWideRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusSuccess
                                            });
                                        }
                                        else
                                        {
                                            data.ClientObject.Queue(new MediusUpdateLadderStatsWideResponse()
                                            {
                                                MessageID = updateLadderStatsWideRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusDBError
                                            });
                                        }
                                    });


                                    break;
                                }
                            case MediusLadderType.MediusLadderTypeClan:
                                {
                                    _ = Program.Database.PostClanLadderStats(data.ClientObject.AccountId, data.ClientObject.ClanId, pluginMessage.WideStats).ContinueWith((r) =>
                                    {
                                        if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                            return;

                                        if (r.IsCompletedSuccessfully && r.Result)
                                        {
                                            data.ClientObject.WideStats = pluginMessage.WideStats;
                                            data.ClientObject.Queue(new MediusUpdateLadderStatsWideResponse()
                                            {
                                                MessageID = updateLadderStatsWideRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusSuccess
                                            });
                                        }
                                        else
                                        {
                                            data.ClientObject.Queue(new MediusUpdateLadderStatsWideResponse()
                                            {
                                                MessageID = updateLadderStatsWideRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusDBError
                                            });
                                        }
                                    });
                                    break;
                                }
                            default:
                                {
                                    Logger.Warn($"Unhandled MediusUpdateLadderStatsWideRequest {updateLadderStatsWideRequest}");
                                    break;
                                }
                        }

                        break;
                    }


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
                                                StatusCode = MediusCallbackStatus.MediusNoResult
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
                                                Stats = r.Result.ClanWideStats
                                            });
                                        }
                                        else
                                        {
                                            data.ClientObject.Queue(new MediusGetLadderStatsResponse()
                                            {
                                                MessageID = getLadderStatsRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusNoResult
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

                case MediusLadderListRequest ladderListRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {ladderListRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {ladderListRequest} without being logged in.");

                        //
                        await Program.Database.GetLeaderboardList(ladderListRequest.StartPosition - 1, ladderListRequest.PageSize, data.ApplicationId).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                var responses = new List<MediusLadderListResponse>(r.Result.Length);
                                foreach (var ladderEntry in r.Result)
                                {
                                    byte[] mediusStats = new byte[Constants.ACCOUNTSTATS_MAXLEN];
                                    try { var dbAccStats = Convert.FromBase64String(ladderEntry.MediusStats ?? ""); mediusStats = dbAccStats; } catch (Exception) { }
                                    responses.Add(new MediusLadderListResponse()
                                    {
                                        MessageID = ladderListRequest.MessageID,
                                        AccountID = ladderEntry.AccountId,
                                        AccountName = ladderEntry.AccountName,
                                        LadderPosition = (uint)(ladderEntry.Index + 1),
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        EndOfList = false
                                    });
                                }

                                if (responses.Count > 0)
                                {
                                    // Flag last item as EndOfList
                                    responses[responses.Count - 1].EndOfList = true;

                                    //
                                    data.ClientObject.Queue(responses);
                                }
                                else
                                {
                                    data.ClientObject.Queue(new MediusLadderListResponse()
                                    {
                                        MessageID = ladderListRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusNoResult,
                                        EndOfList = true
                                    });
                                }
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusLadderListResponse()
                                {
                                    MessageID = ladderListRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusDBError,
                                    EndOfList = true
                                });
                            }
                        });
                        break;
                    }

                case MediusLadderList_ExtraInfoRequest ladderList_ExtraInfoRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {ladderList_ExtraInfoRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {ladderList_ExtraInfoRequest} without being logged in.");

                        //
                        _ = Program.Database.GetLeaderboard(ladderList_ExtraInfoRequest.LadderStatIndex + 1, (int)ladderList_ExtraInfoRequest.StartPosition - 1, (int)ladderList_ExtraInfoRequest.PageSize, data.ApplicationId).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                var responses = new List<MediusLadderList_ExtraInfoResponse>(r.Result.Length);
                                foreach (var ladderEntry in r.Result)
                                {
                                    byte[] mediusStats = new byte[Constants.ACCOUNTSTATS_MAXLEN];
                                    try { var dbAccStats = Convert.FromBase64String(ladderEntry.MediusStats ?? ""); mediusStats = dbAccStats; } catch (Exception) { }
                                    responses.Add(new MediusLadderList_ExtraInfoResponse()
                                    {
                                        MessageID = ladderList_ExtraInfoRequest.MessageID,
                                        AccountID = ladderEntry.AccountId,
                                        AccountName = ladderEntry.AccountName,
                                        AccountStats = mediusStats,
                                        LadderPosition = (uint)(ladderEntry.Index + 1),
                                        LadderStat = ladderEntry.StatValue,
                                        OnlineState = new MediusPlayerOnlineState()
                                        {

                                        },
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        EndOfList = false
                                    });
                                }

                                if (responses.Count > 0)
                                {
                                    // Flag last item as EndOfList
                                    responses[responses.Count - 1].EndOfList = true;

                                    //
                                    data.ClientObject.Queue(responses);
                                }
                                else
                                {
                                    Logger.Info("GetLadderListRequest_ExtraInfo - no result");
                                    data.ClientObject.Queue(new MediusLadderList_ExtraInfoResponse()
                                    {
                                        MessageID = ladderList_ExtraInfoRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusNoResult,
                                        EndOfList = true
                                    });
                                }
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusLadderList_ExtraInfoResponse()
                                {
                                    MessageID = ladderList_ExtraInfoRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusDBError,
                                    EndOfList = true
                                });
                            }
                        });
                        break;
                    }
                    
                case MediusGetTotalUsersRequest getTotalUsersRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getTotalUsersRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getTotalUsersRequest} without being logged in.");

                        var game = data.ClientObject.CurrentGame;
                        var channel = data.ClientObject.CurrentChannel;

                        if (channel == null)
                        {
                            data.ClientObject.Queue(new MediusGetTotalUsersResponse()
                            {
                                MessageID = getTotalUsersRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusWMError
                            });
                        }
                        else
                        {
                            // Success
                            data.ClientObject.Queue(new MediusGetTotalUsersResponse()
                            {
                                MessageID = getTotalUsersRequest.MessageID,
                                TotalInSystem = (uint)channel.Clients.Count(),
                                TotalInGame = 0,
                                StatusCode = MediusCallbackStatus.MediusSuccess
                            });
                        }
                        break;
                    }
                    
                case MediusGetTotalRankingsRequest getTotalRankingsRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getTotalRankingsRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getTotalRankingsRequest} without being logged in.");

                        // Process
                        switch (getTotalRankingsRequest.LadderType)
                        {
                            case MediusLadderType.MediusLadderTypeClan:
                                {
                                    // 
                                    _ = Program.Database.GetActiveClanCountByAppId(data.ClientObject.ApplicationId).ContinueWith((r) =>
                                    {
                                        if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                            return;

                                        if (r.IsCompletedSuccessfully && r.Result.HasValue)
                                        {
                                            //
                                            data.ClientObject.Queue(new MediusGetTotalRankingsResponse()
                                            {
                                                MessageID = getTotalRankingsRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                                TotalRankings = (uint)r.Result.Value
                                            });
                                        }
                                        else
                                        {
                                            //
                                            data.ClientObject.Queue(new MediusGetTotalRankingsResponse()
                                            {
                                                MessageID = getTotalRankingsRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusDBError
                                            });
                                        }
                                    });
                                    break;
                                }
                            case MediusLadderType.MediusLadderTypePlayer:
                                {

                                    // 
                                    _ = Program.Database.GetActiveAccountCountByAppId(data.ClientObject.ApplicationId).ContinueWith((r) =>
                                    {
                                        if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                            return;

                                        if (r.IsCompletedSuccessfully && r.Result.HasValue)
                                        {
                                            //
                                            data.ClientObject.Queue(new MediusGetTotalRankingsResponse()
                                            {
                                                MessageID = getTotalRankingsRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                                TotalRankings = (uint)r.Result.Value
                                            });
                                        }
                                        else
                                        {
                                            //
                                            data.ClientObject.Queue(new MediusGetTotalRankingsResponse()
                                            {
                                                MessageID = getTotalRankingsRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusDBError
                                            });
                                        }
                                    });
                                    break;
                                }
                        }
                        break;
                    }

                // For Legacy titles
                case MediusLadderPositionRequest ladderPositionRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {ladderPositionRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {ladderPositionRequest} without being logged in.");


                        _ = Program.Database.GetPlayerLeaderboard(ladderPositionRequest.AccountID).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                data.ClientObject.Queue(new MediusLadderPositionResponse()
                                {
                                    MessageID = ladderPositionRequest.MessageID,
                                    LadderPosition = (uint)r.Result.Index + 1,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusLadderPositionResponse()
                                {
                                    MessageID = ladderPositionRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult
                                });
                            }
                        });
                        break;
                    }

                case MediusLadderPosition_ExtraInfoRequest ladderPosition_ExtraInfoRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {ladderPosition_ExtraInfoRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {ladderPosition_ExtraInfoRequest} without being logged in.");


                        _ = Program.Database.GetPlayerLeaderboardIndex(ladderPosition_ExtraInfoRequest.AccountID, ladderPosition_ExtraInfoRequest.LadderStatIndex + 1).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                data.ClientObject.Queue(new MediusLadderPosition_ExtraInfoResponse()
                                {
                                    MessageID = ladderPosition_ExtraInfoRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                    LadderPosition = (uint)r.Result.Index + 1,
                                    TotalRankings = (uint)r.Result.TotalRankedAccounts,
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusLadderPosition_ExtraInfoResponse()
                                {
                                    MessageID = ladderPosition_ExtraInfoRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult
                                });
                            }
                        });
                        break;
                    }

                case MediusClanLadderListRequest clanLadderListRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {clanLadderListRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {clanLadderListRequest} without being logged in.");

                        //
                        _ = Program.Database.GetClanLeaderboard(clanLadderListRequest.ClanLadderStatIndex, (int)clanLadderListRequest.StartPosition - 1, (int)clanLadderListRequest.PageSize, data.ApplicationId).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                var responses = new List<MediusClanLadderListResponse>(r.Result.Length);
                                foreach (var ladderEntry in r.Result)
                                {
                                    byte[] mediusStats = new byte[Constants.ACCOUNTSTATS_MAXLEN];
                                    try { var dbAccStats = Convert.FromBase64String(ladderEntry.MediusStats ?? ""); mediusStats = dbAccStats; } catch (Exception) { }
                                    responses.Add(new MediusClanLadderListResponse()
                                    {
                                        MessageID = clanLadderListRequest.MessageID,
                                        ClanID = ladderEntry.ClanId,
                                        ClanName = ladderEntry.ClanName,
                                        LadderPosition = (uint)(ladderEntry.Index + 1),
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        EndOfList = false
                                    });
                                }

                                if (responses.Count > 0)
                                {
                                    // Flag last item as EndOfList
                                    responses[responses.Count - 1].EndOfList = true;

                                    //
                                    data.ClientObject.Queue(responses);
                                }
                                else
                                {
                                    data.ClientObject.Queue(new MediusClanLadderListResponse()
                                    {
                                        MessageID = clanLadderListRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusNoResult,
                                        EndOfList = true
                                    });
                                }
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusClanLadderListResponse()
                                {
                                    MessageID = clanLadderListRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusDBError,
                                    EndOfList = true
                                });
                            }
                        });

                        break;
                    }

                #endregion

                #region Player Info

                case MediusFindPlayerRequest findPlayerRequest:
                    {
                        ClientObject foundPlayer = null;

                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {findPlayerRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {findPlayerRequest} without being logged in.");

                        if (findPlayerRequest.SearchType == MediusPlayerSearchType.PlayerAccountID)
                        {
                            foundPlayer = Program.Manager.GetClientByAccountId(findPlayerRequest.ID);
                        }
                        else if (findPlayerRequest.SearchType == MediusPlayerSearchType.PlayerAccountName)
                        {
                            foundPlayer = Program.Manager.GetClientByAccountName(findPlayerRequest.Name);
                        }

                        if (foundPlayer == null || !foundPlayer.IsLoggedIn)
                        {
                            data.ClientObject.Queue(new MediusFindPlayerResponse()
                            {
                                MessageID = findPlayerRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusAccountNotFound,
                                AccountID = findPlayerRequest.ID,
                                AccountName = findPlayerRequest.Name,
                                EndOfList = true
                            });
                        }
                        else
                        {
                            data.ClientObject.Queue(new MediusFindPlayerResponse()
                            {
                                MessageID = findPlayerRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                ApplicationID = data.ApplicationId,
                                AccountID = foundPlayer.AccountId,
                                AccountName = foundPlayer.AccountName,
                                ApplicationType = (foundPlayer.PlayerStatus == MediusPlayerStatus.MediusPlayerInGameWorld) ? MediusApplicationType.MediusAppTypeGame : MediusApplicationType.LobbyChatChannel,
                                ApplicationName = "?????", //Needs work to pull from a list.
                                MediusWorldID = (foundPlayer.PlayerStatus == MediusPlayerStatus.MediusPlayerInGameWorld) ? foundPlayer.CurrentGame?.Id ?? -1 : foundPlayer.CurrentChannel?.Id ?? -1,
                                EndOfList = true
                            });
                        }
                        break;
                    }

                case MediusPlayerInfoRequest playerInfoRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {playerInfoRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {playerInfoRequest} without being logged in.");

                        _ = Program.Database.GetAccountById(playerInfoRequest.AccountID).ContinueWith((r) =>
                        {
                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                byte[] mediusStats = new byte[Constants.ACCOUNTSTATS_MAXLEN];
                                try { var dbAccStats = Convert.FromBase64String(r.Result.MediusStats ?? ""); mediusStats = dbAccStats; } catch (Exception) { }
                                var playerClientObject = Program.Manager.GetClientByAccountId(r.Result.AccountId);
                                data?.ClientObject?.Queue(new MediusPlayerInfoResponse()
                                {
                                    MessageID = playerInfoRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                    AccountName = r.Result.AccountName,
                                    ApplicationID = data.ApplicationId,
                                    PlayerStatus = (playerClientObject != null && playerClientObject.IsLoggedIn) ? playerClientObject.PlayerStatus : MediusPlayerStatus.MediusPlayerDisconnected,
                                    ConnectionClass = MediusConnectionType.Ethernet,
                                    Stats = mediusStats
                                });
                            }
                            else
                            {
                                data?.ClientObject?.Queue(new MediusPlayerInfoResponse()
                                {
                                    MessageID = playerInfoRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess
                                });
                            }
                        });
                        break;
                    }

                case MediusUpdateUserState updateUserState:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {updateUserState} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {updateUserState} without being logged in.");

                        switch (updateUserState.UserAction)
                        {
                            case MediusUserAction.LeftGameWorld:
                                {
                                    await data.ClientObject.LeaveGame(data.ClientObject.CurrentGame);
                                    break;
                                }
                            case MediusUserAction.KeepAlive:
                                {
                                    data.ClientObject.KeepAliveUntilNextConnection();
                                    break;
                                }
                        }

                        break;
                    }

                #endregion

                #region Clan

                case MediusCreateClanRequest createClanRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {createClanRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {createClanRequest} without being logged in.");

                        // validate name
                        if (!Program.PassTextFilter(Config.TextFilterContext.CLAN_NAME, createClanRequest.ClanName))
                        {
                            data.ClientObject.Queue(new MediusCreateClanResponse()
                            {
                                MessageID = createClanRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusFail
                            });
                            return;
                        }

                        _ = Program.Database.CreateClan(data.ClientObject.AccountId, createClanRequest.ClanName, data.ClientObject.ApplicationId, Convert.ToBase64String(new byte[Constants.CLANSTATS_MAXLEN])).ContinueWith((r) =>
                        {
                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                // Reply with account id
                                data.ClientObject.Queue(new MediusCreateClanResponse()
                                {
                                    MessageID = createClanRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                    ClanID = r.Result.ClanId
                                });
                                data.ClientObject.ClanId = r.Result.ClanId;
                            }
                            else
                            {
                                // Reply error
                                data.ClientObject.Queue(new MediusCreateClanResponse()
                                {
                                    MessageID = createClanRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusClanNameInUse
                                });
                            }
                        });

                        break;
                    }

                case MediusCheckMyClanInvitationsRequest checkMyClanInvitationsRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {checkMyClanInvitationsRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {checkMyClanInvitationsRequest} without being logged in.");

                        _ = Program.Database.GetClanInvitationsByAccount(data.ClientObject.AccountId).ContinueWith(r =>
                        {
                            List<MediusCheckMyClanInvitationsResponse> responses = new List<MediusCheckMyClanInvitationsResponse>();
                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                responses.AddRange(r.Result
                                    .Where(x => x.Invitation.ResponseStatus == 0) // only return undecided
                                    .Skip((checkMyClanInvitationsRequest.Start - 1) * checkMyClanInvitationsRequest.PageSize)
                                    .Take(checkMyClanInvitationsRequest.PageSize)
                                    .Select(x => new MediusCheckMyClanInvitationsResponse()
                                {
                                    MessageID = checkMyClanInvitationsRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                    ClanID = x.Invitation.ClanId,
                                    ClanInvitationID = x.Invitation.InvitationId,
                                    LeaderAccountID = x.LeaderAccountId,
                                    LeaderAccountName = x.LeaderAccountName,
                                    Message = x.Invitation.Message,
                                    ResponseStatus = (MediusClanInvitationsResponseStatus)x.Invitation.ResponseStatus
                                }));
                            }

                            if (responses.Count == 0)
                            {
                                responses.Add(new MediusCheckMyClanInvitationsResponse()
                                {
                                    MessageID = checkMyClanInvitationsRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult,
                                    EndOfList = true
                                });
                            }

                            responses[responses.Count - 1].EndOfList = true;
                            data.ClientObject.Queue(responses);
                        });

                        break;
                    }

                case MediusRemovePlayerFromClanRequest removePlayerFromClanRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {removePlayerFromClanRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {removePlayerFromClanRequest} without being logged in.");

                        if (!data.ClientObject.ClanId.HasValue)
                        {
                            data.ClientObject.Queue(new MediusRemovePlayerFromClanResponse()
                            {
                                MessageID = removePlayerFromClanRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNotClanMember
                            });
                        }
                        else
                        {
                            _ = Program.Database.ClanLeave(data.ClientObject.AccountId, removePlayerFromClanRequest.ClanID, removePlayerFromClanRequest.PlayerAccountID).ContinueWith(r =>
                            {
                                if (r.IsCompletedSuccessfully && r.Result)
                                {
                                    data.ClientObject.Queue(new MediusRemovePlayerFromClanResponse()
                                    {
                                        MessageID = removePlayerFromClanRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                    });
                                }
                                else
                                {
                                    data.ClientObject.Queue(new MediusRemovePlayerFromClanResponse()
                                    {
                                        MessageID = removePlayerFromClanRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusFail,
                                    });
                                }
                            });
                        }

                        break;
                    }

                case MediusGetMyClansRequest getMyClansRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getMyClansRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getMyClansRequest} without being logged in.");

                        //
                        _ = data.ClientObject.RefreshAccount().ContinueWith(t =>
                        {
                            if (!data.ClientObject.ClanId.HasValue)
                            {
                                data.ClientObject.Queue(new MediusGetMyClansResponse()
                                {
                                    MessageID = getMyClansRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult,
                                    EndOfList = true
                                });
                            }
                            else
                            {
                                _ = Program.Database.GetClanById(data.ClientObject.ClanId.Value).ContinueWith(r =>
                                {
                                    if (r.IsCompletedSuccessfully && r.Result != null)
                                    {
                                        data.ClientObject.Queue(new MediusGetMyClansResponse()
                                        {
                                            MessageID = getMyClansRequest.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusSuccess,
                                            ClanID = r.Result.ClanId,
                                            ApplicationID = r.Result.AppId,
                                            ClanName = r.Result.ClanName,
                                            LeaderAccountID = r.Result.ClanLeaderAccount.AccountId,
                                            LeaderAccountName = r.Result.ClanLeaderAccount.AccountName,
                                            Stats = Convert.FromBase64String(r.Result.ClanMediusStats),
                                            Status = r.Result.IsDisbanded ? MediusClanStatus.ClanDisbanded : MediusClanStatus.ClanActive,
                                            EndOfList = true
                                        });
                                    }
                                    else
                                    {
                                        data.ClientObject.Queue(new MediusGetMyClansResponse()
                                        {
                                            MessageID = getMyClansRequest.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusNoResult,
                                            ClanID = -1,
                                            ApplicationID = data.ApplicationId,
                                            EndOfList = true
                                        });
                                    }
                                });
                            }
                        });

                        break;
                    }

                case MediusGetClanMemberListRequest getClanMemberListRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getClanMemberListRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getClanMemberListRequest} without being logged in.");

                        _ = Program.Database.GetClanById(getClanMemberListRequest.ClanID).ContinueWith(r =>
                        {
                            List<MediusGetClanMemberListResponse> responses = new List<MediusGetClanMemberListResponse>();
                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                responses.AddRange(r.Result.ClanMemberAccounts.Select(x =>
                                {
                                    var account = Program.Manager.GetClientByAccountId(x.AccountId);
                                    return new MediusGetClanMemberListResponse()
                                    {
                                        MessageID = getClanMemberListRequest.MessageID,
                                        AccountID = x.AccountId,
                                        AccountName = x.AccountName,
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        EndOfList = false
                                    };
                                }));
                            }

                            if (responses.Count == 0)
                            {
                                responses.Add(new MediusGetClanMemberListResponse()
                                {
                                    MessageID = getClanMemberListRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusClanNotFound,
                                    EndOfList = true
                                });
                            }

                            responses[responses.Count - 1].EndOfList = true;
                            data.ClientObject.Queue(responses);
                        });

                        break;
                    }

                case MediusGetClanMemberList_ExtraInfoRequest getClanMemberList_ExtraInfoRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getClanMemberList_ExtraInfoRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getClanMemberList_ExtraInfoRequest} without being logged in.");

                        _ = Program.Database.GetClanById(getClanMemberList_ExtraInfoRequest.ClanID).ContinueWith(r =>
                        {
                            List<MediusGetClanMemberList_ExtraInfoResponse> responses = new List<MediusGetClanMemberList_ExtraInfoResponse>();
                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                responses.AddRange(r.Result.ClanMemberAccounts.Select(x =>
                                {
                                    var account = Program.Manager.GetClientByAccountId(x.AccountId);
                                    return new MediusGetClanMemberList_ExtraInfoResponse()
                                    {
                                        MessageID = getClanMemberList_ExtraInfoRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        AccountID = x.AccountId,
                                        AccountName = x.AccountName,
                                        LadderPosition = 1,
                                        LadderStat = getClanMemberList_ExtraInfoRequest.LadderStatIndex,
                                        OnlineState = new MediusPlayerOnlineState()
                                        {
                                            ConnectStatus = account?.PlayerStatus ?? MediusPlayerStatus.MediusPlayerDisconnected,
                                            GameName = account?.CurrentGame?.GameName,
                                            LobbyName = account?.CurrentChannel?.Name ?? "",
                                            MediusGameWorldID = account?.CurrentGame?.Id ?? -1,
                                            MediusLobbyWorldID = account?.CurrentChannel?.Id ?? -1
                                        },
                                        Stats = Convert.FromBase64String(x.MediusStats),
                                        TotalRankings = 0,
                                        EndOfList = false
                                    };
                                }));
                            }

                            if (responses.Count == 0)
                            {
                                responses.Add(new MediusGetClanMemberList_ExtraInfoResponse()
                                {
                                    MessageID = getClanMemberList_ExtraInfoRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusClanNotFound,
                                    EndOfList = true
                                });
                            }

                            responses[responses.Count - 1].EndOfList = true;
                            data.ClientObject.Queue(responses);
                        });

                        break;
                    }

                case MediusGetClanInvitationsSentRequest getClanInvitiationsSentRequest:
                     {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getClanInvitiationsSentRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getClanInvitiationsSentRequest} without being logged in.");

                        _ = Program.Database.GetClanById(data.ClientObject.ClanId.Value).ContinueWith(r =>
                        {
                            List<MediusGetClanInvitationsSentResponse> responses = new List<MediusGetClanInvitationsSentResponse>();
                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                responses.AddRange(r.Result.ClanMemberInvitations
                                    .Where(x => x.ResponseStatus == 0) // only return undecided
                                    .Skip((getClanInvitiationsSentRequest.Start - 1) * getClanInvitiationsSentRequest.PageSize)
                                    .Take(getClanInvitiationsSentRequest.PageSize)
                                    .Select(x => new MediusGetClanInvitationsSentResponse()
                                    {
                                        AccountID = x.TargetAccountId,
                                        AccountName = x.TargetAccountName,
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        ResponseMsg = x.ResponseMessage,
                                        ResponseStatus = (MediusClanInvitationsResponseStatus)x.ResponseStatus,
                                        ResponseTime = x.ResponseTime,
                                    }))
                                    ;
                            }

                            if (responses.Count == 0)
                            {
                                responses.Add(new MediusGetClanInvitationsSentResponse()
                                {
                                    MessageID = getClanInvitiationsSentRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult,
                                    EndOfList = true
                                });
                            }

                            responses[responses.Count - 1].EndOfList = true;
                            data.ClientObject.Queue(responses);
                        });
                        break;
                    }

                case MediusGetClanByIDRequest getClanByIdRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getClanByIdRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getClanByIdRequest} without being logged in.");

                        _ = Program.Database.GetClanById(getClanByIdRequest.ClanID).ContinueWith(r =>
                        {
                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                data.ClientObject.Queue(new MediusGetClanByIDResponse()
                                {
                                    MessageID = getClanByIdRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                    ApplicationID = r.Result.AppId,
                                    ClanName = r.Result.ClanName,
                                    LeaderAccountID = r.Result.ClanLeaderAccount.AccountId,
                                    LeaderAccountName = r.Result.ClanLeaderAccount.AccountName,
                                    Stats = Convert.FromBase64String(r.Result.ClanMediusStats),
                                    Status = r.Result.IsDisbanded ? MediusClanStatus.ClanDisbanded : MediusClanStatus.ClanActive
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusGetClanByIDResponse()
                                {
                                    MessageID = getClanByIdRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusClanNotFound
                                });
                            }
                        });
                        break;
                    }

                case MediusGetClanByNameRequest getClanByNameRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getClanByNameRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getClanByNameRequest} without being logged in.");

                        _ = Program.Database.GetClanByName(getClanByNameRequest.ClanName, data.ClientObject.ApplicationId).ContinueWith(r =>
                        {
                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                data.ClientObject.Queue(new MediusGetClanByNameResponse()
                                {
                                    MessageID = getClanByNameRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                    ClanID = r.Result.ClanId,
                                    LeaderAccountID = r.Result.ClanLeaderAccount.AccountId,
                                    LeaderAccountName = r.Result.ClanLeaderAccount.AccountName,
                                    Stats = Convert.FromBase64String(r.Result.ClanMediusStats),
                                    Status = r.Result.IsDisbanded ? MediusClanStatus.ClanDisbanded : MediusClanStatus.ClanActive
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusGetClanByNameResponse()
                                {
                                    MessageID = getClanByNameRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusClanNotFound
                                });
                            }
                        });
                        break;
                    }

                case MediusClanLadderPositionRequest getClanLadderPositionRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getClanLadderPositionRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getClanLadderPositionRequest} without being logged in.");

                        _ = Program.Database.GetClanLeaderboardIndex(getClanLadderPositionRequest.ClanID, getClanLadderPositionRequest.ClanLadderStatIndex).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                data.ClientObject.Queue(new MediusClanLadderPositionResponse()
                                {
                                    MessageID = getClanLadderPositionRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                    LadderPosition = (uint)r.Result.Index + 1,
                                    TotalRankings = (uint)r.Result.TotalRankedClans
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusClanLadderPositionResponse()
                                {
                                    MessageID = getClanLadderPositionRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusClanNotFound
                                });
                            }
                        });
                        break;
                    }

                case MediusUpdateClanStatsRequest updateClanStatsRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {updateClanStatsRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {updateClanStatsRequest} without being logged in.");

                        _ = Program.Database.PostClanMediusStats(updateClanStatsRequest.ClanID, Convert.ToBase64String(updateClanStatsRequest.Stats)).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result)
                            {
                                data.ClientObject.Queue(new MediusUpdateClanStatsResponse()
                                {
                                    MessageID = updateClanStatsRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusUpdateClanStatsResponse()
                                {
                                    MessageID = updateClanStatsRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusClanNotFound
                                });
                            }
                        });

                        break;
                    }

                case MediusInvitePlayerToClanRequest invitePlayerToClanRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {invitePlayerToClanRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {invitePlayerToClanRequest} without being logged in.");

                        // ERROR -- Need clan
                        if (!data.ClientObject.ClanId.HasValue)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {invitePlayerToClanRequest} without having a clan.");

                        _ = Program.Database.CreateClanInvitation(data.ClientObject.AccountId, data.ClientObject.ClanId.Value, invitePlayerToClanRequest.PlayerAccountID, invitePlayerToClanRequest.InviteMessage).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result)
                            {
                                data.ClientObject.Queue(new MediusInvitePlayerToClanResponse()
                                {
                                    MessageID = invitePlayerToClanRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusInvitePlayerToClanResponse()
                                {
                                    MessageID = invitePlayerToClanRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusFail
                                });
                            }
                        });
                        break;
                    }

                case MediusRespondToClanInvitationRequest respondToClanInvitationRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {respondToClanInvitationRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {respondToClanInvitationRequest} without being logged in.");

                        _ = Program.Database.RespondToClanInvitation(data.ClientObject.AccountId, respondToClanInvitationRequest.ClanInvitationID, respondToClanInvitationRequest.Message, (int)respondToClanInvitationRequest.Response).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result)
                            {
                                data.ClientObject.Queue(new MediusRespondToClanInvitationResponse()
                                {
                                    MessageID = respondToClanInvitationRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusRespondToClanInvitationResponse()
                                {
                                    MessageID = respondToClanInvitationRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusFail
                                });
                            }
                        });
                        break;
                    }

                case MediusRevokeClanInvitationRequest revokeClanInvitationRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {revokeClanInvitationRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {revokeClanInvitationRequest} without being logged in.");

                        // ERROR -- Need clan
                        if (!data.ClientObject.ClanId.HasValue)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {revokeClanInvitationRequest} without having a clan.");

                        _ = Program.Database.RevokeClanInvitation(data.ClientObject.AccountId, data.ClientObject.ClanId.Value, revokeClanInvitationRequest.PlayerAccountID).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result)
                            {
                                data.ClientObject.Queue(new MediusRevokeClanInvitationResponse()
                                {
                                    MessageID = revokeClanInvitationRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess
                                });
                            } else {
                                data.ClientObject.Queue(new MediusRevokeClanInvitationResponse()
                                {
                                    MessageID = revokeClanInvitationRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusFail
                                });
                            }
                        });
                        break;
                    }

                case MediusDisbandClanRequest disbandClanRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {disbandClanRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {disbandClanRequest} without being logged in.");

                        // ERROR -- Need clan
                        if (!data.ClientObject.ClanId.HasValue)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {disbandClanRequest} without having a clan.");

                        _ = Program.Database.DeleteClan(data.ClientObject.AccountId, disbandClanRequest.ClanID).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result)
                            {
                                data.ClientObject.Queue(new MediusDisbandClanResponse()
                                {
                                    MessageID = disbandClanRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess
                                });
                            } else {
                                data.ClientObject.Queue(new MediusDisbandClanResponse()
                                {
                                    MessageID = disbandClanRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusFail
                                });
                            }
                        });

                        break;
                    }

                case MediusGetMyClanMessagesRequest getMyClanMessagesRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getMyClanMessagesRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getMyClanMessagesRequest} without being logged in.");

                        // ERROR -- Need clan
                        if (!data.ClientObject.ClanId.HasValue)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getMyClanMessagesRequest} without having a clan.");

                        _ = Program.Database.GetClanMessages(data.ClientObject.AccountId, data.ClientObject.ClanId.Value, 0, 1).ContinueWith((r) =>
                        {
                            List<MediusGetMyClanMessagesResponse> responses = new List<MediusGetMyClanMessagesResponse>();
                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                responses.AddRange(r.Result
                                    .Select(x => new MediusGetMyClanMessagesResponse()
                                    {
                                        MessageID = getMyClanMessagesRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        Message = x.Message,
                                        ClanID = data.ClientObject.ClanId.Value
                                    }))
                                    ;
                            }

                            if (responses.Count == 0)
                            {
                                responses.Add(new MediusGetMyClanMessagesResponse()
                                {
                                    MessageID = getMyClanMessagesRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult,
                                    ClanID = data.ClientObject.ClanId.Value,
                                    EndOfList = true
                                });
                            }

                            responses[responses.Count - 1].EndOfList = true;
                            data.ClientObject.Queue(responses);
                        });

                        break;
                    }

                case MediusGetAllClanMessagesRequest getAllClanMessagesRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getAllClanMessagesRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getAllClanMessagesRequest} without being logged in.");

                        // ERROR -- Need clan
                        if (!data.ClientObject.ClanId.HasValue)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getAllClanMessagesRequest} without having a clan.");

                        _ = Program.Database.GetClanMessages(data.ClientObject.AccountId, data.ClientObject.ClanId.Value, getAllClanMessagesRequest.Start - 1, getAllClanMessagesRequest.PageSize).ContinueWith((r) =>
                        {
                            List<MediusGetAllClanMessagesResponse> responses = new List<MediusGetAllClanMessagesResponse>();
                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                responses.AddRange(r.Result
                                    .Select(x => new MediusGetAllClanMessagesResponse()
                                    {
                                        MessageID = getAllClanMessagesRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        ClanMessageID = x.Id,
                                        Message = x.Message,
                                        Status = MediusClanMessageStatus.ClanMessageRead
                                    }))
                                    ;
                            }

                            if (responses.Count == 0)
                            {
                                responses.Add(new MediusGetAllClanMessagesResponse()
                                {
                                    MessageID = getAllClanMessagesRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult,
                                    EndOfList = true
                                });
                            }

                            responses[responses.Count - 1].EndOfList = true;
                            data.ClientObject.Queue(responses);
                        });

                        break;
                    }

                case MediusSendClanMessageRequest sendClanMessageRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {sendClanMessageRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {sendClanMessageRequest} without being logged in.");

                        // ERROR -- Need clan
                        if (!data.ClientObject.ClanId.HasValue)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {sendClanMessageRequest} without having a clan.");

                        // validate message
                        if (!Program.PassTextFilter(Config.TextFilterContext.CLAN_MESSAGE, sendClanMessageRequest.Message))
                        {
                            data.ClientObject.Queue(new MediusSendClanMessageResponse()
                            {
                                MessageID = sendClanMessageRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusFail
                            });
                            return;
                        }

                        _ = Program.Database.ClanAddMessage(data.ClientObject.AccountId, data.ClientObject.ClanId.Value, sendClanMessageRequest.Message).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result)
                            {
                                data.ClientObject.Queue(new MediusSendClanMessageResponse()
                                {
                                    MessageID = sendClanMessageRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusSendClanMessageResponse()
                                {
                                    MessageID = sendClanMessageRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusFail
                                });
                            }
                        });

                        break;
                    }

                case MediusModifyClanMessageRequest modifyClanMessageRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {modifyClanMessageRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {modifyClanMessageRequest} without being logged in.");

                        // ERROR -- Need clan
                        if (!data.ClientObject.ClanId.HasValue)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {modifyClanMessageRequest} without having a clan.");

                        // validate message
                        if (!Program.PassTextFilter(Config.TextFilterContext.CLAN_MESSAGE, modifyClanMessageRequest.NewMessage))
                        {
                            data.ClientObject.Queue(new MediusModifyClanMessageResponse()
                            {
                                MessageID = modifyClanMessageRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusFail
                            });
                            return;
                        }

                        _ = Program.Database.ClanEditMessage(data.ClientObject.AccountId, data.ClientObject.ClanId.Value, modifyClanMessageRequest.ClanMessageID, modifyClanMessageRequest.NewMessage).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result)
                            {
                                data.ClientObject.Queue(new MediusModifyClanMessageResponse()
                                {
                                    MessageID = modifyClanMessageRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusModifyClanMessageResponse()
                                {
                                    MessageID = modifyClanMessageRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusFail
                                });
                            }
                        });

                        break;
                    }

                case MediusTransferClanLeadershipRequest transferClanLeadershipRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {transferClanLeadershipRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {transferClanLeadershipRequest} without being logged in.");

                        // ERROR -- Need clan
                        if (!data.ClientObject.ClanId.HasValue)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {transferClanLeadershipRequest} without having a clan.");

                        _ = Program.Database.ClanTransferLeadership(data.ClientObject.AccountId, data.ClientObject.ClanId.Value, transferClanLeadershipRequest.NewLeaderAccountID).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result)
                            {
                                data.ClientObject.Queue(new MediusTransferClanLeadershipResponse()
                                {
                                    MessageID = transferClanLeadershipRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusTransferClanLeadershipResponse()
                                {
                                    MessageID = transferClanLeadershipRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusFail
                                });
                            }
                        });

                        break;
                    }

                #endregion

                #region Party (PS3)

                case MediusPartyCreateRequest partyCreateRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {partyCreateRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {partyCreateRequest} without being logged in.");


                        //var currentChannel = data.ClientObject.CurrentChannel;

                        data.ClientObject.Queue(new MediusPartyCreateResponse() { 
                            MessageID = partyCreateRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                            MediusWorldID = 1
                        });

                        break;
                    }

                case MediusPartyPlayerReport partyPlayerReport:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {partyPlayerReport} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {partyPlayerReport} without being logged in.");


                        // 
                        if (data.ClientObject.CurrentParty?.Id == partyPlayerReport.MediusWorldID &&
                            data.ClientObject.SessionKey == partyPlayerReport.SessionKey)
                        {
                            data.ClientObject.CurrentParty?.OnPartyPlayerReport(partyPlayerReport);
                        }

                        break;
                    }

                #endregion

                #region Game

                case MediusGetGameListFilterRequest getGameListFilterRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getGameListFilterRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getGameListFilterRequest} without being logged in.");

                        var filters = data.ClientObject.GameListFilters;
                        if (data.ApplicationId == 10984)
                        {
                            if (filters == null || filters.Count == 0)
                            {
                                data.ClientObject.Queue(new MediusGetGameListFilterResponse0()
                                {
                                    MessageID = getGameListFilterRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult,
                                    EndOfList = true
                                });
                            }
                            else
                            {
                                // Generate messages per filter
                                var filterResponses = filters.Select(x => new MediusGetGameListFilterResponse0()
                                {
                                    MessageID = getGameListFilterRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                    FilterField = x.FilterField,
                                    ComparisonOperator = x.ComparisonOperator,
                                    BaselineValue = x.BaselineValue,
                                    EndOfList = false
                                }).ToList();

                                // Set end of list
                                filterResponses[filterResponses.Count - 1].EndOfList = true;

                                // Add to responses
                                data.ClientObject.Queue(filterResponses);
                            }
                        } else {
                            if (filters == null || filters.Count == 0)
                            {
                                data.ClientObject.Queue(new MediusGetGameListFilterResponse()
                                {
                                    MessageID = getGameListFilterRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult,
                                    EndOfList = true
                                });
                            }
                            else
                            {
                                // Generate messages per filter
                                var filterResponses = filters.Select(x => new MediusGetGameListFilterResponse()
                                {
                                    MessageID = getGameListFilterRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                    BaselineValue = x.BaselineValue,
                                    ComparisonOperator = x.ComparisonOperator,
                                    FilterField = x.FilterField,
                                    FilterID = x.FieldID,
                                    Mask = x.Mask,
                                    EndOfList = false
                                }).ToList();

                                // Set end of list
                                filterResponses[filterResponses.Count - 1].EndOfList = true;

                                // Add to responses
                                data.ClientObject.Queue(filterResponses);
                            }
                        }
                        
                        break;
                    }

                case MediusGameListRequest gameListRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {gameListRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {gameListRequest} without being logged in.");

                        /*
                        var gameList = Program.Manager.GetGameList(
                               data.ClientObject.ApplicationId,
                               gameListRequest.PageID,
                               gameListRequest.PageSize,
                               data.ClientObject.GameListFilters)
                            .Select(x => new MediusGameListResponse()
                            {
                                MessageID = gameListRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,

                                MediusWorldID = x.Id,
                                GameName = x.GameName,
                                WorldStatus = x.WorldStatus,
                                GameHostType = x.GameHostType,
                                PlayerCount = (ushort)x.PlayerCount,
                                EndOfList = false
                            }).ToArray();

                        // Make last end of list
                        if (gameList.Length > 0)
                        {
                            gameList[gameList.Length - 1].EndOfList = true;

                            // Add to responses
                            data.ClientObject.Queue(gameList);
                        }
                        else
                        {
                            data.ClientObject.Queue(new MediusGameListResponse()
                            {
                                MessageID = gameListRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNoResult,
                                PlayerCount = 0,
                                EndOfList = true
                            });
                        }
                        */
                        break;
                    }

                case MediusGameList_ExtraInfoRequest gameList_ExtraInfoRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {gameList_ExtraInfoRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {gameList_ExtraInfoRequest} without being logged in.");
                        //By Filter
                        if(data.ClientObject.ApplicationId == 20624 || data.ClientObject.ApplicationId == 20623 || data.ClientObject.ApplicationId == 21924)
                        {
                            var gameList = Program.Manager.GetGameList(
                               data.ClientObject.ApplicationId,
                               gameList_ExtraInfoRequest.PageID,
                               gameList_ExtraInfoRequest.PageSize,
                               data.ClientObject.GameListFilters)
                            .Select(x => new MediusGameList_ExtraInfoResponse()
                            {
                                MessageID = gameList_ExtraInfoRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,

                                GameHostType = x.GameHostType,
                                GameLevel = x.GameLevel,
                                GameName = x.GameName,
                                GameStats = x.GameStats,
                                GenericField1 = x.GenericField1,
                                GenericField2 = x.GenericField2,
                                GenericField3 = x.GenericField3,
                                GenericField4 = x.GenericField4,
                                GenericField5 = x.GenericField5,
                                GenericField6 = x.GenericField6,
                                GenericField7 = x.GenericField7,
                                GenericField8 = x.GenericField8,
                                MaxPlayers = (ushort)x.MaxPlayers,
                                MediusWorldID = x.Id,
                                MinPlayers = (ushort)x.MinPlayers,
                                PlayerCount = (ushort)x.PlayerCount,
                                PlayerSkillLevel = x.PlayerSkillLevel,
                                RulesSet = x.RulesSet,
                                SecurityLevel = (String.IsNullOrEmpty(x.GamePassword) ? MediusWorldSecurityLevelType.WORLD_SECURITY_NONE : MediusWorldSecurityLevelType.WORLD_SECURITY_PLAYER_PASSWORD),
                                WorldStatus = x.WorldStatus,
                                EndOfList = false
                            }).ToArray();

                            // Make last end of list
                            if (gameList.Length > 0)
                            {
                                gameList[gameList.Length - 1].EndOfList = true;

                                // Add to responses
                                data.ClientObject.Queue(gameList);
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusGameList_ExtraInfoResponse()
                                {
                                    MessageID = gameList_ExtraInfoRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult,
                                    EndOfList = true
                                });
                            }
                        }
                            // Size Matters  20770
                            //No Filter
                         else {
                           var gameList = Program.Manager.GetGameList(
                           data.ClientObject.ApplicationId,
                           gameList_ExtraInfoRequest.PageID,
                           gameList_ExtraInfoRequest.PageSize)
                           //data.ClientObject.GameListFilters)
                           .Select(x => new MediusGameList_ExtraInfoResponse()
                           {
                               MessageID = gameList_ExtraInfoRequest.MessageID,
                               StatusCode = MediusCallbackStatus.MediusSuccess,

                               GameHostType = x.GameHostType,
                               GameLevel = x.GameLevel,
                               GameName = x.GameName,
                               GameStats = x.GameStats,
                               GenericField1 = x.GenericField1,
                               GenericField2 = x.GenericField2,
                               GenericField3 = x.GenericField3,
                               GenericField4 = x.GenericField4,
                               GenericField5 = x.GenericField5,
                               GenericField6 = x.GenericField6,
                               GenericField7 = x.GenericField7,
                               GenericField8 = x.GenericField8,
                               MaxPlayers = (ushort)x.MaxPlayers,
                               MediusWorldID = x.Id,
                               MinPlayers = (ushort)x.MinPlayers,
                               PlayerCount = (ushort)x.PlayerCount,
                               PlayerSkillLevel = x.PlayerSkillLevel,
                               RulesSet = x.RulesSet,
                               SecurityLevel = (String.IsNullOrEmpty(x.GamePassword) ? MediusWorldSecurityLevelType.WORLD_SECURITY_NONE : MediusWorldSecurityLevelType.WORLD_SECURITY_PLAYER_PASSWORD),
                               WorldStatus = x.WorldStatus,
                               EndOfList = false
                           }).ToArray();

                            // Make last end of list
                            if (gameList.Length > 0)
                            {
                                gameList[gameList.Length - 1].EndOfList = true;

                                // Add to responses
                                data.ClientObject.Queue(gameList);
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusGameList_ExtraInfoResponse()
                                {
                                    MessageID = gameList_ExtraInfoRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult,
                                    EndOfList = true
                                });
                            }
                        }

                       

                        break;
                    }

                case MediusGameList_ExtraInfoRequest0 gameList_ExtraInfoRequest0:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {gameList_ExtraInfoRequest0} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {gameList_ExtraInfoRequest0} without being logged in.");

                        if(data.ClientObject.ApplicationId == 10952 || data.ClientObject.ApplicationId == 10954)
                        {
                            var gameList = Program.Manager.GetGameList(
                            data.ClientObject.ApplicationId,
                            gameList_ExtraInfoRequest0.PageID,
                            gameList_ExtraInfoRequest0.PageSize)
                            .Select(x => new MediusGameList_ExtraInfoResponse0()
                            {
                                MessageID = gameList_ExtraInfoRequest0.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,

                                GameHostType = x.GameHostType,
                                GameLevel = x.GameLevel,
                                GameName = x.GameName,
                                GameStats = x.GameStats,
                                GenericField1 = x.GenericField1,
                                GenericField2 = x.GenericField2,
                                GenericField3 = x.GenericField3,
                                MaxPlayers = (ushort)x.MaxPlayers,
                                MediusWorldID = x.Id,
                                MinPlayers = (ushort)x.MinPlayers,
                                PlayerCount = (ushort)x.PlayerCount,
                                PlayerSkillLevel = x.PlayerSkillLevel,
                                RulesSet = x.RulesSet,
                                SecurityLevel = (String.IsNullOrEmpty(x.GamePassword) ? MediusWorldSecurityLevelType.WORLD_SECURITY_NONE : MediusWorldSecurityLevelType.WORLD_SECURITY_PLAYER_PASSWORD),
                                WorldStatus = x.WorldStatus,
                                EndOfList = false
                            }).ToArray();

                            // Make last end of list
                            if (gameList.Length > 0)
                            {
                                gameList[gameList.Length - 1].EndOfList = true;

                                // Add to responses
                                data.ClientObject.Queue(gameList);
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusGameList_ExtraInfoResponse0()
                                {
                                    MessageID = gameList_ExtraInfoRequest0.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult,
                                    EndOfList = true
                                });
                            }
                        } 
                        else
                        {
                            var gameList = Program.Manager.GetGameList(
                            data.ClientObject.ApplicationId,
                            gameList_ExtraInfoRequest0.PageID,
                            gameList_ExtraInfoRequest0.PageSize,
                            data.ClientObject.GameListFilters)
                            .Select(x => new MediusGameList_ExtraInfoResponse0()
                            {
                                MessageID = gameList_ExtraInfoRequest0.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,

                                GameHostType = x.GameHostType,
                                GameLevel = x.GameLevel,
                                GameName = x.GameName,
                                GameStats = x.GameStats,
                                GenericField1 = x.GenericField1,
                                GenericField2 = x.GenericField2,
                                GenericField3 = x.GenericField3,
                                MaxPlayers = (ushort)x.MaxPlayers,
                                MediusWorldID = x.Id,
                                MinPlayers = (ushort)x.MinPlayers,
                                PlayerCount = (ushort)x.PlayerCount,
                                PlayerSkillLevel = x.PlayerSkillLevel,
                                RulesSet = x.RulesSet,
                                SecurityLevel = (String.IsNullOrEmpty(x.GamePassword) ? MediusWorldSecurityLevelType.WORLD_SECURITY_NONE : MediusWorldSecurityLevelType.WORLD_SECURITY_PLAYER_PASSWORD),
                                WorldStatus = x.WorldStatus,
                                EndOfList = false
                            }).ToArray();

                            // Make last end of list
                            if (gameList.Length > 0)
                            {
                                gameList[gameList.Length - 1].EndOfList = true;

                                // Add to responses
                                data.ClientObject.Queue(gameList);
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusGameList_ExtraInfoResponse0()
                                {
                                    MessageID = gameList_ExtraInfoRequest0.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult,
                                    EndOfList = true
                                });
                            }
                        }
                        

                        break;
                    }

                case MediusGameInfoRequest gameInfoRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {gameInfoRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {gameInfoRequest} without being logged in.");

                        var game = Program.Manager.GetGameByGameId(gameInfoRequest.MediusWorldID);
                        if (game == null)
                        {
                            data.ClientObject.Queue(new MediusGameInfoResponse()
                            {
                                MessageID = gameInfoRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusGameNotFound
                            });
                        }
                        else
                        {
                            data.ClientObject.Queue(new MediusGameInfoResponse()
                            {
                                MessageID = gameInfoRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,

                                GameHostType = game.GameHostType,
                                GameLevel = game.GameLevel,
                                GameName = game.GameName,
                                GameStats = game.GameStats,
                                GenericField1 = game.GenericField1,
                                GenericField2 = game.GenericField2,
                                GenericField3 = game.GenericField3,
                                GenericField4 = game.GenericField4,
                                GenericField5 = game.GenericField5,
                                GenericField6 = game.GenericField6,
                                GenericField7 = game.GenericField7,
                                GenericField8 = game.GenericField8,
                                MaxPlayers = (ushort)game.MaxPlayers,
                                MinPlayers = (ushort)game.MinPlayers,
                                PlayerCount = (ushort)game.PlayerCount,
                                PlayerSkillLevel = game.PlayerSkillLevel,
                                RulesSet = game.RulesSet,
                                WorldStatus = game.WorldStatus,
                                ApplicationID = data.ApplicationId
                            });
                        }

                        break;
                    }

                case MediusGameInfoRequest0 gameInfoRequest0:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {gameInfoRequest0} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {gameInfoRequest0} without being logged in.");

                        var game = Program.Manager.GetGameByGameId(gameInfoRequest0.MediusWorldID);
                        if (game == null)
                        {
                            data.ClientObject.Queue(new MediusGameInfoResponse0()
                            {
                                MessageID = gameInfoRequest0.MessageID,
                                StatusCode = MediusCallbackStatus.MediusGameNotFound
                            });
                        }
                        else
                        {
                            data.ClientObject.Queue(new MediusGameInfoResponse0()
                            {
                                MessageID = gameInfoRequest0.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,

                                GameHostType = game.GameHostType,
                                GameLevel = game.GameLevel,
                                GameName = game.GameName,
                                GameStats = game.GameStats,
                                GenericField1 = game.GenericField1,
                                GenericField2 = game.GenericField2,
                                GenericField3 = game.GenericField3,
                                MaxPlayers = (ushort)game.MaxPlayers,
                                MinPlayers = (ushort)game.MinPlayers,
                                PlayerCount = (ushort)game.PlayerCount,
                                PlayerSkillLevel = game.PlayerSkillLevel,
                                RulesSet = game.RulesSet,
                                WorldStatus = game.WorldStatus,
                                ApplicationID = data.ApplicationId
                            });
                        }

                        break;
                    }
                    
                case MediusLobbyWorldPlayerListRequest lobbyWorldPlayerListRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {lobbyWorldPlayerListRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                        {
                            data.ClientObject.Queue(new MediusLobbyWorldPlayerListResponse()
                            {
                                MessageID = lobbyWorldPlayerListRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusPlayerNotPrivileged,
                                EndOfList = true
                            });

                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {lobbyWorldPlayerListRequest} without being logged in.");
                        }

                        var channel = data.ClientObject.CurrentChannel;
                        if (channel == null)
                        {
                            data.ClientObject.Queue(new MediusLobbyWorldPlayerListResponse()
                            {
                                MessageID = lobbyWorldPlayerListRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNoResult,
                                EndOfList = true
                            });
                        }
                        else
                        {
                            var results = channel.Clients.Where(x => x.IsConnected).Select(x => new MediusLobbyWorldPlayerListResponse()
                            {
                                MessageID = lobbyWorldPlayerListRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                PlayerStatus = x.PlayerStatus,
                                AccountID = x.AccountId,
                                AccountName = x.AccountName,
                                Stats    = x.AccountStats,
                                ConnectionClass = MediusConnectionType.Ethernet,
                                EndOfList = false
                            }).ToArray();

                            if (results.Length > 0)
                                results[results.Length - 1].EndOfList = true;

                            data.ClientObject.Queue(results);

                        }
                        /*
                        if (data.ClientObject.ApplicationId == 20244) //NBA 07 
                        {
                            
                        } else {
                            var game = Program.Manager.GetGameByGameId(lobbyWorldPlayerListRequest.MediusWorldID);
                            if (game == null)
                            {
                                data.ClientObject.Queue(new MediusLobbyWorldPlayerListResponse()
                                {
                                    MessageID = lobbyWorldPlayerListRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult
                                });
                            }
                            else
                            {
                                var playerList = game.Clients.Where(x => x != null && x.InGame && x.Client.IsConnected).Select(x => new MediusLobbyWorldPlayerListResponse()
                                {
                                    MessageID = lobbyWorldPlayerListRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                    PlayerStatus = x.Client.PlayerStatus,
                                    AccountID = x.Client.AccountId,
                                    AccountName = x.Client.AccountName,
                                    Stats = x.Client.Stats,
                                    ConnectionClass = MediusConnectionType.Ethernet,
                                    EndOfList = false
                                }).ToArray();

                                // Set last end of list
                                if (playerList.Length > 0)
                                    playerList[playerList.Length - 1].EndOfList = true;
                                else
                                {
                                    playerList[playerList.Length - 1].EndOfList = true;
                                }
                                data.ClientObject.Queue(playerList);
                            }
                        }
                        */

                        /*
                         if (client == null)
                        {

                        }
                        else
                        {
                            var playerList = client.Clients.Where(x => x != null && x.InGame && x.Client.IsConnected).Select(x => new MediusLobbyWorldPlayerListResponse()
                            {
                                MessageID = lobbyWorldPlayerListRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                PlayerStatus = x.Client.PlayerStatus,
                                AccountID = x.Client.AccountId,
                                AccountName = x.Client.AccountName,
                                Stats = x.Client.Stats,
                                ConnectionClass = MediusConnectionType.Ethernet,
                                EndOfList = false
                            }).ToArray();

                            // Set last end of list
                            if (playerList.Length > 0)
                                playerList[playerList.Length - 1].EndOfList = true;
                            else { playerList[playerList.Length - 1].EndOfList = true; }
                            data.ClientObject.Queue(playerList);
                        var game1 = Program.Manager.GetGameByGameId(lobbyWorldPlayerListRequest.MediusWorldID);
                        if (game1 == null)
                        {
                            data.ClientObject.Queue(new MediusLobbyWorldPlayerListResponse()
                            {
                                MessageID = lobbyWorldPlayerListRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusGameNotFound
                            });
                        }
                        else
                        {
                            var playerList = game1.Clients.Where(x => x != null && x.InGame && x.Client.IsConnected).Select(x => new MediusLobbyWorldPlayerListResponse()
                            {
                                MessageID = lobbyWorldPlayerListRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                AccountID = x.Client.AccountId,
                                AccountName = x.Client.AccountName,
                                ConnectionClass = MediusConnectionType.Ethernet,
                                EndOfList = false
                            }).ToArray();

                            // Set last end of list
                            if (playerList.Length > 0)
                                playerList[playerList.Length - 1].EndOfList = true;
                            else { playerList[playerList.Length - 1].EndOfList = true; }
                            data.ClientObject.Queue(playerList);
                        }
                        */

                        break;
                    }
                case MediusGameWorldPlayerListRequest gameWorldPlayerListRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {gameWorldPlayerListRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {gameWorldPlayerListRequest} without being logged in.");

                        var game = Program.Manager.GetGameByGameId(gameWorldPlayerListRequest.MediusWorldID);
                        if (game == null)
                        {
                            data.ClientObject.Queue(new MediusGameWorldPlayerListResponse()
                            {
                                MessageID = gameWorldPlayerListRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusGameNotFound
                            });
                        }
                        else
                        {
                            var playerList = game.Clients.Where(x => x != null && x.InGame && x.Client.IsConnected).Select(x => new MediusGameWorldPlayerListResponse()
                            {
                                MessageID = gameWorldPlayerListRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                AccountID = x.Client.AccountId,
                                AccountName = x.Client.AccountName,
                                ConnectionClass = MediusConnectionType.Ethernet,
                                EndOfList = false
                            }).ToArray();

                            // Set last end of list
                            if (playerList.Length > 0)
                                playerList[playerList.Length - 1].EndOfList = true;

                            data.ClientObject.Queue(playerList);
                        }

                        break;
                    }

                case MediusSetGameListFilterRequest setGameListFilterRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {setGameListFilterRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {setGameListFilterRequest} without being logged in.");

                        // Set filter
                        var filter = data.ClientObject.SetGameListFilter(setGameListFilterRequest);

                        // Give reply
                        data.ClientObject.Queue(new MediusSetGameListFilterResponse()
                        {
                            MessageID = setGameListFilterRequest.MessageID,
                            StatusCode = filter == null ? MediusCallbackStatus.MediusFail : MediusCallbackStatus.MediusSuccess,
                            FilterID = filter?.FieldID ?? 0
                        });

                        break;
                    }

                case MediusSetGameListFilterRequest0 setGameListFilterRequest0:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {setGameListFilterRequest0} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {setGameListFilterRequest0} without being logged in.");

                        // Set filter
                        var filter = data.ClientObject.SetGameListFilter(setGameListFilterRequest0);

                        // Give reply
                        data.ClientObject.Queue(new MediusSetGameListFilterResponse0()
                        {
                            MessageID = setGameListFilterRequest0.MessageID,
                            StatusCode = filter == null ? MediusCallbackStatus.MediusFail : MediusCallbackStatus.MediusSuccess,
                        });

                        break;
                    }

                case MediusClearGameListFilterRequest clearGameListFilterRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {clearGameListFilterRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {clearGameListFilterRequest} without being logged in.");

                        // Remove
                        data.ClientObject.ClearGameListFilter(clearGameListFilterRequest.FilterID);

                        // 
                        data.ClientObject.Queue(new MediusClearGameListFilterResponse()
                        {
                            MessageID = clearGameListFilterRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess
                        });

                        break;
                    }

                case MediusClearGameListFilterRequest0 clearGameListFilterRequest0:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {clearGameListFilterRequest0} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {clearGameListFilterRequest0} without being logged in.");

                        // Remove
                        data.ClientObject.ClearGameListFilter(clearGameListFilterRequest0.FilterID);

                        // 
                        data.ClientObject.Queue(new MediusClearGameListFilterResponse()
                        {
                            MessageID = clearGameListFilterRequest0.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess
                        });

                        break;
                    }


                case MediusCreateGameRequest createGameRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {createGameRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {createGameRequest} without being logged in.");

                        // validate name
                        if (!Program.PassTextFilter(Config.TextFilterContext.GAME_NAME, Convert.ToString(createGameRequest.GameName)))
                        {
                            data.ClientObject.Queue(new MediusCreateGameResponse()
                            {
                                MessageID = createGameRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusFail
                            });
                            return;
                        }

                        // Send to plugins
                        await Program.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_ON_CREATE_GAME, new OnPlayerRequestArgs() { Player = data.ClientObject, Request = createGameRequest });

                        Program.Manager.CreateGame(data.ClientObject, createGameRequest);
                        break;
                    }

                case MediusCreateGameRequest0 createGameRequest0:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {createGameRequest0} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {createGameRequest0} without being logged in.");

                        // validate name
                        if (!Program.PassTextFilter(Config.TextFilterContext.GAME_NAME, Convert.ToString(createGameRequest0.GameName)))
                        {
                            data.ClientObject.Queue(new MediusCreateGameResponse()
                            {
                                MessageID = createGameRequest0.MessageID,
                                StatusCode = MediusCallbackStatus.MediusFail
                            });
                            return;
                        }

                        // Send to plugins
                        await Program.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_ON_CREATE_GAME, new OnPlayerRequestArgs() { Player = data.ClientObject, Request = createGameRequest0 });

                        Program.Manager.CreateGame(data.ClientObject, createGameRequest0);
                        break;
                    }

                case MediusCreateGameRequest1 createGameRequest1:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {createGameRequest1} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {createGameRequest1} without being logged in.");

                        // validate name
                        if (!Program.PassTextFilter(Config.TextFilterContext.GAME_NAME, Convert.ToString(createGameRequest1.GameName)))
                        {
                            data.ClientObject.Queue(new MediusCreateGameResponse()
                            {
                                MessageID = createGameRequest1.MessageID,
                                StatusCode = MediusCallbackStatus.MediusFail
                            });
                            return;
                        }

                        // Send to plugins
                        await Program.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_ON_CREATE_GAME, new OnPlayerRequestArgs() { Player = data.ClientObject, Request = createGameRequest1 });

                        Program.Manager.CreateGame(data.ClientObject, createGameRequest1);
                        break;
                    }


                case MediusJoinGameRequest joinGameRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {joinGameRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {joinGameRequest} without being logged in.");

                        // Send to plugins
                        await Program.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_ON_JOIN_GAME, new OnPlayerRequestArgs() { Player = data.ClientObject, Request = joinGameRequest });

                        Program.Manager.JoinGame(data.ClientObject, joinGameRequest);
                        break;
                    }

                case MediusJoinGameRequest0 joinGameRequest0:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {joinGameRequest0} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {joinGameRequest0} without being logged in.");

                        // Send to plugins
                        await Program.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_ON_JOIN_GAME, new OnPlayerRequestArgs() { Player = data.ClientObject, Request = joinGameRequest0 });

                        Program.Manager.JoinGame0(data.ClientObject, joinGameRequest0);
                        break;
                    }

                case MediusWorldReport0 worldReport0:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {worldReport0} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {worldReport0} without being logged in.");

                        if (data.ClientObject.CurrentGame != null)
                            await data.ClientObject.CurrentGame.OnWorldReport0(worldReport0);

                        break;
                    }

                case MediusWorldReport worldReport:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {worldReport} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {worldReport} without being logged in.");

                        if (data.ClientObject.CurrentGame != null)
                            await data.ClientObject.CurrentGame.OnWorldReport(worldReport);

                        break;
                    }

                case MediusPlayerReport playerReport:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {playerReport} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel},{data.ClientObject} sent {playerReport} without being logged in.");

                        // 
                        if (data.ClientObject.CurrentGame?.Id == playerReport.MediusWorldID &&
                            data.ClientObject.SessionKey == playerReport.SessionKey)
                        {
                            data.ClientObject.CurrentGame?.OnPlayerReport(playerReport);
                        }
                        break;
                    }

                case MediusEndGameReport endGameReport:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {endGameReport} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {endGameReport} without being logged in.");

                        if (data.ClientObject.CurrentGame != null)
                            await data.ClientObject.CurrentGame.OnEndGameReport(endGameReport);
                        break;
                    }

                #endregion

                #region Channel

                case MediusGetLobbyPlayerNames_ExtraInfoRequest getLobbyPlayerNames_ExtraInfoRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getLobbyPlayerNames_ExtraInfoRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getLobbyPlayerNames_ExtraInfoRequest} without being logged in.");

                        var channel = data.ClientObject.CurrentChannel;
                        if (channel == null)
                        {
                            data.ClientObject.Queue(new MediusGetLobbyPlayerNames_ExtraInfoResponse()
                            {
                                MessageID = getLobbyPlayerNames_ExtraInfoRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNoResult,
                                EndOfList = true
                            });
                        }
                        else
                        {
                            var results = channel.Clients.Where(x => x.IsConnected).Select(x => new MediusGetLobbyPlayerNames_ExtraInfoResponse()
                            {
                                MessageID = getLobbyPlayerNames_ExtraInfoRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                AccountID = x.AccountId,
                                AccountName = x.AccountName,
                                OnlineState = new MediusPlayerOnlineState()
                                {
                                    ConnectStatus = x.PlayerStatus,
                                    GameName = x.CurrentGame?.GameName,
                                    LobbyName = x.CurrentChannel?.Name,
                                    MediusGameWorldID = x.CurrentGame?.Id ?? -1,
                                    MediusLobbyWorldID = x.CurrentChannel?.Id ?? -1
                                },
                                EndOfList = false
                            }).ToArray();

                            if (results.Length > 0)
                                results[results.Length - 1].EndOfList = true;

                            data.ClientObject.Queue(results);
                        }

                        break;
                    }

                case MediusGetLobbyPlayerNamesRequest getLobbyPlayerNamesRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getLobbyPlayerNamesRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getLobbyPlayerNamesRequest} without being logged in.");

                        var channel = data.ClientObject.CurrentChannel;
                        if (channel == null)
                        {
                            data.ClientObject.Queue(new MediusGetLobbyPlayerNamesResponse()
                            {
                                MessageID = getLobbyPlayerNamesRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNoResult,
                                EndOfList = true
                            });
                        }
                        else
                        {
                            var results = channel.Clients.Where(x => x.IsConnected).Select(x => new MediusGetLobbyPlayerNamesResponse()
                            {
                                MessageID = getLobbyPlayerNamesRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                AccountID = x.AccountId,
                                AccountName = x.AccountName,
                                EndOfList = false
                            }).ToArray();

                            if (results.Length > 0)
                                results[results.Length - 1].EndOfList = true;

                            data.ClientObject.Queue(results);
                        }
                        
                        break;
                    }

                case MediusGetWorldSecurityLevelRequest getWorldSecurityLevelRequest:
                    { 
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getWorldSecurityLevelRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getWorldSecurityLevelRequest} without being logged in.");
                        
                        //Fetch Channel by MediusID and AppID
                        var channel = Program.Manager.GetChannelByChannelId(getWorldSecurityLevelRequest.MediusWorldID, data.ClientObject.ApplicationId);
                        
                        //Send back Successful SecurityLevel and AppType for the correct Channel
                        data.ClientObject.Queue(new MediusGetWorldSecurityLevelResponse()
                        {
                            MessageID = getWorldSecurityLevelRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                            MediusWorldID = getWorldSecurityLevelRequest.MediusWorldID,
                            AppType = channel.AppType,
                            SecurityLevel = channel.SecurityLevel
                        });
                        break;
                    }

                case MediusSetLobbyWorldFilterRequest setLobbyWorldFilterRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {setLobbyWorldFilterRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {setLobbyWorldFilterRequest} without being logged in.");

                        data.ClientObject.Queue(new MediusSetLobbyWorldFilterResponse()
                        {
                            MessageID = setLobbyWorldFilterRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                            FilterMask1 = setLobbyWorldFilterRequest.FilterMask1,
                            FilterMask2 = setLobbyWorldFilterRequest.FilterMask2,
                            FilterMask3 = setLobbyWorldFilterRequest.FilterMask3,
                            FilterMask4 = setLobbyWorldFilterRequest.FilterMask4,
                            FilterMaskLevel = setLobbyWorldFilterRequest.FilterMaskLevel,
                            LobbyFilterType = setLobbyWorldFilterRequest.LobbyFilterType
                        });


                        break;
                    }

                case MediusSetLobbyWorldFilterRequest1 setLobbyWorldFilterRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {setLobbyWorldFilterRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {setLobbyWorldFilterRequest} without being logged in.");

                        data.ClientObject.Queue(new MediusSetLobbyWorldFilterResponse1()
                        {
                            MessageID = setLobbyWorldFilterRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                            FilterMask1 = setLobbyWorldFilterRequest.FilterMask1,
                            FilterMask2 = setLobbyWorldFilterRequest.FilterMask2,
                            FilterMask3 = setLobbyWorldFilterRequest.FilterMask3,
                            FilterMask4 = setLobbyWorldFilterRequest.FilterMask4,
                            FilterMaskLevel = setLobbyWorldFilterRequest.FilterMaskLevel,
                            LobbyFilterType = setLobbyWorldFilterRequest.LobbyFilterType
                        });


                        break;
                    }

                case MediusCreateChannelRequest createChannelRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {createChannelRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {createChannelRequest} without being logged in.");

                        // Create channel
                        Channel channel = new Channel(createChannelRequest);

                        // Check for channel with same name
                        var existingChannel = Program.Manager.GetChannelByChannelName(channel.Name, channel.ApplicationId);
                        if (existingChannel != null)
                        {
                            // Send to client
                            data.ClientObject.Queue(new MediusCreateChannelResponse()
                            {
                                MessageID = createChannelRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusChannelNameExists,
                                MediusWorldID = existingChannel.Id
                            });
                        }
                        else
                        {
                            // Add
                            Program.Manager.AddChannel(channel);

                            // Send to client
                            data.ClientObject.Queue(new MediusCreateChannelResponse()
                            {
                                MessageID = createChannelRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                MediusWorldID = channel.Id
                            });
                        }
                        break;
                    }

                case MediusCreateChannelRequest0 createChannelRequest0:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {createChannelRequest0} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {createChannelRequest0} without being logged in.");

                        // Create channel
                        Channel channel = new Channel(createChannelRequest0);

                        // Check for channel with same name
                        var existingChannel = Program.Manager.GetChannelByChannelName(channel.Name, channel.ApplicationId);
                        if (existingChannel != null)
                        {
                            // Send to client
                            data.ClientObject.Queue(new MediusCreateChannelResponse()
                            {
                                MessageID = createChannelRequest0.MessageID,
                                StatusCode = MediusCallbackStatus.MediusChannelNameExists,
                                MediusWorldID = existingChannel.Id
                            });
                        }
                        else
                        {
                            // Add
                            Program.Manager.AddChannel(channel);

                            // Send to client
                            data.ClientObject.Queue(new MediusCreateChannelResponse()
                            {
                                MessageID = createChannelRequest0.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                MediusWorldID = channel.Id
                            });
                        }
                        break;
                    }

                case MediusJoinChannelRequest joinChannelRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {joinChannelRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {joinChannelRequest} without being logged in.");

                        var channel = Program.Manager.GetChannelByChannelId(joinChannelRequest.MediusWorldID, data.ClientObject.ApplicationId);
                        if (channel == null)
                        {
                            // Log
                            Logger.Warn($"{data.ClientObject} attemping to join non-existent channel {joinChannelRequest}");

                            data.ClientObject.Queue(new MediusJoinChannelResponse()
                            {
                                MessageID = joinChannelRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusChannelNotFound
                            });
                        }
                        else if (channel.SecurityLevel == MediusWorldSecurityLevelType.WORLD_SECURITY_PLAYER_PASSWORD && joinChannelRequest.LobbyChannelPassword != channel.Password)
                        {
                            data.ClientObject.Queue(new MediusJoinChannelResponse()
                            {
                                MessageID = joinChannelRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusInvalidPassword
                            });
                        }
                        else
                        {
                            // Join new channel
                            data.ClientObject.JoinChannel(channel);

                            // Indicate the client is connecting to a different part of Medius
                            data.ClientObject.KeepAliveUntilNextConnection();

                            //
                            data.ClientObject.Queue(new MediusJoinChannelResponse()
                            {
                                MessageID = joinChannelRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                ConnectInfo = new NetConnectionInfo()
                                {
                                    AccessKey = data.ClientObject.Token,
                                    SessionKey = data.ClientObject.SessionKey,
                                    WorldID = channel.Id,
                                    ServerKey = Program.GlobalAuthPublic,
                                    AddressList = new NetAddressList()
                                    {
                                        AddressList = new NetAddress[Constants.NET_ADDRESS_LIST_COUNT]
                                        {
                                            new NetAddress() { Address = Program.LobbyServer.IPAddress.ToString(), Port = (uint)Program.LobbyServer.Port, AddressType = NetAddressType.NetAddressTypeExternal},
                                            new NetAddress() { AddressType = NetAddressType.NetAddressNone},
                                        }
                                    },
                                    Type = NetConnectionType.NetConnectionTypeClientServerTCP
                                }
                            });
                        }
                        break;
                    }

                case MediusChannelInfoRequest channelInfoRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {channelInfoRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {channelInfoRequest} without being logged in.");

                        // Find channel
                        var channel = Program.Manager.GetChannelByChannelId(channelInfoRequest.MediusWorldID, data.ClientObject.ApplicationId);

                        if (channel == null)
                        {
                            // No channels
                            data.ClientObject.Queue(new MediusChannelInfoResponse()
                            {
                                MessageID = channelInfoRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess
                            });
                        }
                        else
                        {
                            data.ClientObject.Queue(new MediusChannelInfoResponse()
                            {
                                MessageID = channelInfoRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                LobbyName = channel.Name,
                                ActivePlayerCount = channel.PlayerCount,
                                MaxPlayers = channel.MaxPlayers
                            });
                        }
                        break;
                    }

                case MediusChannelListRequest channelListRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {channelListRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {channelListRequest} without being logged in.");

                        List<MediusChannelListResponse> channelResponses = new List<MediusChannelListResponse>();
                        
                        var lobbyChannels = Program.Manager.GetChannelList(
                            data.ClientObject.ApplicationId,
                            channelListRequest.PageID,
                            channelListRequest.PageSize,
                            ChannelType.Lobby);
                        
                        var gameChannels = Program.Manager.GetChannelList(
                            data.ClientObject.ApplicationId,
                            channelListRequest.PageID,
                            channelListRequest.PageSize,
                            ChannelType.Game);

                        
                        foreach (var channel in lobbyChannels)
                        {
                            channelResponses.Add(new MediusChannelListResponse()
                            {
                                MessageID = channelListRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                MediusWorldID = channel.Id,
                                LobbyName = channel.Name,
                                PlayerCount = channel.PlayerCount,
                                EndOfList = false
                            });
                        }
                        
                        foreach (var channel in gameChannels)
                        {
                            channelResponses.Add(new MediusChannelListResponse()
                            {
                                MessageID = channelListRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                MediusWorldID = channel.Id,
                                LobbyName = channel.Name,
                                PlayerCount = channel.PlayerCount,
                                EndOfList = false
                            });
                        }

                        if (channelResponses.Count == 0)
                        {
                            // Return none
                            data.ClientObject.Queue(new MediusChannelListResponse()
                            {
                                MessageID = channelListRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNoResult,
                                EndOfList = true
                            });
                        }
                        else
                        {
                            // Ensure the end of list flag is set
                            channelResponses[channelResponses.Count - 1].EndOfList = true;

                            // Add to responses
                            data.ClientObject.Queue(channelResponses);
                        }


                        break;
                    }

                case MediusGetTotalChannelsRequest getTotalChannelsRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getTotalChannelsRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getTotalChannelsRequest} without being logged in.");

                        data.ClientObject.Queue(new MediusGetTotalChannelsResponse()
                        {
                            MessageID = getTotalChannelsRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                            Total = Program.Manager.GetChannelCount(ChannelType.Lobby, data.ClientObject.ApplicationId)
                        });
                        break;
                    }

                case MediusChannelList_ExtraInfoRequest1 channelList_ExtraInfoRequest1:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {channelList_ExtraInfoRequest1} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {channelList_ExtraInfoRequest1} without a being logged in.");

                        List<MediusChannelList_ExtraInfoResponse> channelResponses = new List<MediusChannelList_ExtraInfoResponse>();

                        // Deadlocked only uses this to connect to a non-game channel (lobby)
                        // So we'll filter by lobby here
                        var channels = Program.Manager.GetChannelList(
                            data.ClientObject.ApplicationId,
                            channelList_ExtraInfoRequest1.PageID,
                            channelList_ExtraInfoRequest1.PageSize,
                            ChannelType.Lobby);

                        foreach (var channel in channels)
                        {
                            channelResponses.Add(new MediusChannelList_ExtraInfoResponse()
                            {
                                MessageID = channelList_ExtraInfoRequest1.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                MediusWorldID = channel.Id,
                                LobbyName = channel.Name,
                                GameWorldCount = (ushort)channel.GameCount,
                                PlayerCount = (ushort)channel.PlayerCount,
                                MaxPlayers = (ushort)channel.MaxPlayers,
                                GenericField1 = channel.GenericField1,
                                GenericField2 = channel.GenericField2,
                                GenericField3 = channel.GenericField3,
                                GenericField4 = channel.GenericField4,
                                GenericFieldLevel = channel.GenericFieldLevel,
                                SecurityLevel = channel.SecurityLevel,
                                EndOfList = false
                            });
                        }

                        if (channelResponses.Count == 0)
                        {
                            // Return none
                            data.ClientObject.Queue(new MediusChannelList_ExtraInfoResponse()
                            {
                                MessageID = channelList_ExtraInfoRequest1.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNoResult,
                                EndOfList = true
                            });
                        }
                        else
                        {
                            // Ensure the end of list flag is set
                            channelResponses[channelResponses.Count - 1].EndOfList = true;

                            // Add to responses
                            data.ClientObject.Queue(channelResponses);
                        }
                        break;
                    }

                case MediusChannelList_ExtraInfoRequest channelList_ExtraInfoRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {channelList_ExtraInfoRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {channelList_ExtraInfoRequest} without being logged in.");

                        List<MediusChannelList_ExtraInfoResponse> channelResponses = new List<MediusChannelList_ExtraInfoResponse>();

                        // Deadlocked only uses this to connect to a non-game channel (lobby)
                        // So we'll filter by lobby here
                        var channels = Program.Manager.GetChannelList(
                            data.ClientObject.ApplicationId,
                            channelList_ExtraInfoRequest.PageID,
                            channelList_ExtraInfoRequest.PageSize,
                            ChannelType.Lobby);
                        
                        foreach (var channel in channels)
                        {
                            channelResponses.Add(new MediusChannelList_ExtraInfoResponse()
                            {
                                MessageID = channelList_ExtraInfoRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                MediusWorldID = channel.Id,
                                LobbyName = channel.Name,
                                GameWorldCount = (ushort)channel.GameCount,
                                PlayerCount = (ushort)channel.PlayerCount,
                                MaxPlayers = (ushort)channel.MaxPlayers,
                                GenericField1 = channel.GenericField1,
                                GenericField2 = channel.GenericField2,
                                GenericField3 = channel.GenericField3,
                                GenericField4 = channel.GenericField4,
                                GenericFieldLevel = channel.GenericFieldLevel,
                                SecurityLevel = channel.SecurityLevel,
                                EndOfList = false
                            });
                        }

                        if (channelResponses.Count == 0)
                        {
                            // Return none
                            data.ClientObject.Queue(new MediusChannelList_ExtraInfoResponse()
                            {
                                MessageID = channelList_ExtraInfoRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNoResult,
                                EndOfList = true
                            });
                        }
                        else
                        {
                            // Ensure the end of list flag is set
                            channelResponses[channelResponses.Count - 1].EndOfList = true;

                            // Add to responses
                            data.ClientObject.Queue(channelResponses);
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

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getLocationsRequest} without a being logged in.");

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

                #endregion

                #region Medius File Services

                case MediusFileListRequest fileListRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileListRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileListRequest} without being logged in.");

                        List<MediusFileListResponse> fileListResponses = new List<MediusFileListResponse>();

                        var rootPath = Path.GetFullPath(Program.Settings.MediusFileServerRootPath);

                        #region NBA 07 PS3
                        //If its NBA 07 PS3
                        if (data.ApplicationId == 20244)
                        {
                            string nba07Path = rootPath + "/NBA07/";
                            if (nba07Path != null)
                            {
                                var filesList = Program.Manager.GetFilesList();

                                foreach (var file in filesList)
                                {
                                    fileListResponses.Add(new MediusFileListResponse()
                                    {
                                        MessageID = fileListRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        MediusFileToList = new MediusFile
                                        {
                                            Filename = file.Filename,
                                            FileID = file.FileID,
                                            FileSize = file.FileSize,
                                            CreationTimeStamp = Utils.GetUnixTime(),
                                        },
                                        EndOfList = false,
                                    });
                                }

                                if (fileListResponses.Count == 0)
                                {
                                    // Return none
                                    data.ClientObject.Queue(new MediusFileListResponse()
                                    {
                                        MessageID = fileListRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        MediusFileToList = new MediusFile
                                        {

                                        },
                                        EndOfList = true,
                                    });
                                }
                                else
                                {
                                    // Ensure the end of list flag is set
                                    fileListResponses[fileListResponses.Count - 1].EndOfList = true;

                                    // Add to responses
                                    data.ClientObject.Queue(fileListResponses);
                                }
                            }
                        }
                        #endregion

                        else

                        #region Default (MediusNoResult)
                        {
                            // Return none
                            data.ClientObject.Queue(new MediusFileListResponse()
                                {
                                    MessageID = fileListRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult,
                                    MediusFileToList = new MediusFile
                                    {

                                    },
                                    EndOfList = true,
                                });
                        }
                        
                        #endregion
                        
                        break; 
                    }

                case MediusFileGetAttributesRequest fileGetAttributesRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileGetAttributesRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileGetAttributesRequest} without being logged in.");


                        data.ClientObject.Queue(new MediusFileGetAttributesResponse()
                        {
                            MediusFileInfo = new MediusFile()
                            {

                            },
                            MediusFileAttributesResponse = new MediusFileAttributes()
                            {
                                
                            },
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                            MessageID = fileGetAttributesRequest.MessageID,
                        });
                        break;
                    }

                case MediusFileCreateRequest fileCreateRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileCreateRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileCreateRequest} without being logged in.");

                        var path = Program.GetFileSystemPath(fileCreateRequest.MediusFileToCreate.Filename);
                        if (path == null)
                        {
                            data.ClientObject.Queue(new MediusFileUploadServerRequest()
                            {
                                MessageID = fileCreateRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusDBError,
                                iXferStatus = MediusFileXferStatus.Error
                            });
                            break;
                        }

                        if (File.Exists(path))
                        {
                            data.ClientObject.Queue(new MediusFileCreateResponse()
                            {
                                MessageID = fileCreateRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusFileAlreadyExists
                            });
                        }
                        else
                        {
                            using (var fs = File.Create(path))
                            {
                                fs.Write(new byte[fileCreateRequest.MediusFileToCreate.FileSize]);
                            }

                            data.ClientObject.Queue(new MediusFileCreateResponse()
                            {
                                MessageID = fileCreateRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                MediusFileInfo = new MediusFile()
                                {
                                    CreationTimeStamp = Utils.GetUnixTime(),
                                    FileID = 1,
                                    Filename = fileCreateRequest.MediusFileToCreate.Filename,
                                    FileSize = fileCreateRequest.MediusFileToCreate.FileSize,
                                    OwnerPermissionRWX = fileCreateRequest.MediusFileToCreate.OwnerPermissionRWX,
                                    GroupPermissionRWX = fileCreateRequest.MediusFileToCreate.GroupPermissionRWX,
                                    GlobalPermissionRWX = fileCreateRequest.MediusFileToCreate.GlobalPermissionRWX,
                                }
                            });
                        }
                        break;
                    }

                case MediusFileUploadRequest fileUploadRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileUploadRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileUploadRequest} without being logged in.");

                        //Task.Run(async () =>
                        //{
                        //    int j = 0;
                        //    var totalSize = fileUploadRequest.MediusFileInfo.FileSize;
                        //    for (int i = 0; i < totalSize; )
                        //    {


                        //        i += Constants.MEDIUS_FILE_MAX_DOWNLOAD_DATA_SIZE;
                        //    }
                        //});

                        try
                        {
                            var path = Program.GetFileSystemPath(fileUploadRequest.MediusFileInfo.Filename);
                            if (path == null)
                            {
                                data.ClientObject.Queue(new MediusFileUploadServerRequest()
                                {
                                    MessageID = fileUploadRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusDBError,
                                    iXferStatus = MediusFileXferStatus.Error
                                });
                                break;
                            }

                            var stream = File.Open(path, FileMode.OpenOrCreate);
                            data.ClientObject.Upload = new UploadState()
                            {
                                FileId = fileUploadRequest.MediusFileInfo.FileID,
                                Stream = stream,
                                TotalSize = fileUploadRequest.UiDataSize
                            };
                            data.ClientObject.Queue(new MediusFileUploadServerRequest()
                            {
                                iPacketNumber = 0,
                                iReqStartByteIndex = 0,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                iXferStatus = MediusFileXferStatus.Initial
                            });
                        }
                        catch
                        {
                            data.ClientObject.Queue(new MediusFileUploadServerRequest()
                            {
                                MessageID = fileUploadRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusDBError,
                                iXferStatus = MediusFileXferStatus.Error
                            });
                        }
                        break;
                    }

                case MediusFileUploadResponse fileUploadResponse:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileUploadResponse} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileUploadResponse} without being logged in.");

                        if (fileUploadResponse.iXferStatus >= MediusFileXferStatus.End)
                            break;

                        try
                        {
                            var uploadState = data.ClientObject.Upload;
                            uploadState.Stream.Seek(fileUploadResponse.iStartByteIndex, SeekOrigin.Begin);
                            uploadState.Stream.Write(fileUploadResponse.Data, 0, fileUploadResponse.iDataSize);
                            uploadState.BytesReceived += fileUploadResponse.iDataSize;
                            uploadState.PacketNumber++;

                            if (uploadState.BytesReceived < uploadState.TotalSize)
                            {
                                data.ClientObject.Queue(new MediusFileUploadServerRequest()
                                {
                                    MessageID = fileUploadResponse.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                    iPacketNumber = uploadState.PacketNumber,
                                    iReqStartByteIndex = uploadState.BytesReceived,
                                    iXferStatus = MediusFileXferStatus.Mid
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusFileUploadServerRequest()
                                {
                                    MessageID = fileUploadResponse.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                    iPacketNumber = 0,
                                    iReqStartByteIndex = 0,
                                    iXferStatus = MediusFileXferStatus.End
                                });
                            }
                        }
                        catch
                        {
                            data.ClientObject.Queue(new MediusFileUploadServerRequest()
                            {
                                MessageID = fileUploadResponse.MessageID,
                                StatusCode = MediusCallbackStatus.MediusDBError,
                                iXferStatus = MediusFileXferStatus.Error
                            });
                        }

                        break;
                    }

                case MediusFileCloseRequest fileCloseRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileCloseRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileCloseRequest} without being logged in.");

                        if (data.ClientObject.Upload?.FileId == fileCloseRequest.MediusFileInfo.FileID)
                        {
                            data.ClientObject.Upload.Stream?.Close();
                            data.ClientObject.Upload = null;

                            data.ClientObject.Queue(new MediusFileCloseResponse()
                            {
                                MessageID = fileCloseRequest.MessageID,
                                MediusFileInfo = fileCloseRequest.MediusFileInfo,
                                StatusCode = MediusCallbackStatus.MediusSuccess
                            });
                        }
                        else
                        {
                            data.ClientObject.Queue(new MediusFileDownloadResponse()
                            {
                                MessageID = fileCloseRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusDBError
                            });
                        }
                        break;
                    }

                case MediusFileDownloadRequest fileDownloadRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileDownloadRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileDownloadRequest} without being logged in.");

                        var path = Program.GetFileSystemPath(fileDownloadRequest.MediusFileInfo.Filename);
                        if (path == null)
                        {
                            data.ClientObject.Queue(new MediusFileUploadServerRequest()
                            {
                                MessageID = fileDownloadRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusDBError,
                                iXferStatus = MediusFileXferStatus.Error
                            });
                            break;
                        }

                        if (File.Exists(path))
                        {
                            var bytes = File.ReadAllBytes(path);
                            int j = 0;
                            for (int i = 0; i < bytes.Length; i += Constants.MEDIUS_FILE_MAX_DOWNLOAD_DATA_SIZE)
                            {
                                var len = bytes.Length - i;
                                if (len > Constants.MEDIUS_FILE_MAX_DOWNLOAD_DATA_SIZE)
                                    len = Constants.MEDIUS_FILE_MAX_DOWNLOAD_DATA_SIZE;

                                var msg = new MediusFileDownloadResponse()
                                {
                                    MessageID = fileDownloadRequest.MessageID,
                                    iDataSize = len,
                                    iPacketNumber = j,
                                    iXferStatus = j == 0 ? MediusFileXferStatus.Initial : ((len + i) >= bytes.Length ? MediusFileXferStatus.End : MediusFileXferStatus.Mid),
                                    iStartByteIndex = i,
                                    StatusCode = MediusCallbackStatus.MediusSuccess
                                };
                                Array.Copy(bytes, i, msg.Data, 0, len);

                                data.ClientObject.Queue(msg);
                                ++j;
                            }
                        }
                        else
                        {
                            data.ClientObject.Queue(new MediusFileDownloadResponse()
                            {
                                MessageID = fileDownloadRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusFileDoesNotExist
                            });
                        }
                        break;
                    }

                #endregion

                #region Chat / Binary Message

                //Deprecated past Medius 2.10
                case MediusChatToggleRequest chatToggleRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {chatToggleRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {chatToggleRequest} without being logged in.");

                        data.ClientObject.Queue(new MediusChatToggleResponse()
                        {
                            MessageID = chatToggleRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess
                        });

                        break;
                    }

                case MediusGenericChatSetFilterRequest genericChatSetFilterRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {genericChatSetFilterRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {genericChatSetFilterRequest} without being logged in.");

                        data.ClientObject.Queue(new MediusGenericChatSetFilterResponse()
                        {
                            MessageID = genericChatSetFilterRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                            ChatFilter = new MediusGenericChatFilter()
                            {
                                GenericChatFilterBitfield = genericChatSetFilterRequest.GenericChatFilter.GenericChatFilterBitfield
                            }
                        });
                        break;
                    }

                case MediusSetAutoChatHistoryRequest setAutoChatHistoryRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {setAutoChatHistoryRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {setAutoChatHistoryRequest} without being logged in.");

                        data.ClientObject.Queue(new MediusSetAutoChatHistoryResponse()
                        {
                            MessageID = setAutoChatHistoryRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess
                        });
                        break;
                    }

                case MediusGenericChatMessage1 genericChatMessage:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {genericChatMessage} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {genericChatMessage} without being logged in.");

                        // validate message
                        if (!Program.PassTextFilter(Config.TextFilterContext.CHAT, genericChatMessage.Message))
                            return;

                        await ProcessGenericChatMessage(clientChannel, data.ClientObject, genericChatMessage);
                        break;
                    }

                case MediusGenericChatMessage genericChatMessage:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {genericChatMessage} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {genericChatMessage} without being logged in.");

                        // validate message
                        if (!Program.PassTextFilter(Config.TextFilterContext.CHAT, genericChatMessage.Message))
                            return;

                        await ProcessGenericChatMessage(clientChannel, data.ClientObject, genericChatMessage);
                        break;
                    }

                case MediusChatMessage chatMessage:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {chatMessage} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {chatMessage} without being logged in.");

                        // validate message
                        if (!Program.PassTextFilter(Config.TextFilterContext.CHAT, chatMessage.Message))
                            return;

                        await ProcessChatMessage(clientChannel, data.ClientObject, chatMessage);
                        break;
                    }

                case MediusBinaryMessage binaryMessage:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {binaryMessage} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {binaryMessage} without being logged in.");

                        switch (binaryMessage.MessageType)
                        {
                            case MediusBinaryMessageType.BroadcastBinaryMsg:
                                {
                                    data.ClientObject.CurrentChannel?.BroadcastBinaryMessage(data.ClientObject, binaryMessage);
                                    break;
                                }
                            case MediusBinaryMessageType.TargetBinaryMsg:
                                {
                                    var target = Program.Manager.GetClientByAccountId(binaryMessage.TargetAccountID);

                                    target?.Queue(new MediusBinaryFwdMessage()
                                    {
                                        MessageType = binaryMessage.MessageType,
                                        OriginatorAccountID = data.ClientObject.AccountId,
                                        Message = binaryMessage.Message
                                    });
                                    break;
                                }
                            default:
                                {
                                    Logger.Warn($"Unhandled binary message type {binaryMessage.MessageType}");
                                    break;
                                }
                        }
                        break;
                    }

                case MediusBinaryMessage1 binaryMessage:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {binaryMessage} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {binaryMessage} without being logged in.");


                        //Binary Msg Handler Error: [%d]: Player not privileged or MUM erro
                        switch (binaryMessage.MessageType)
                        {

                            case MediusBinaryMessageType.BroadcastBinaryMsg:
                                {
                                    data.ClientObject.CurrentChannel?.BroadcastBinaryMessage(data.ClientObject, binaryMessage);
                                    break;
                                }
                            case MediusBinaryMessageType.TargetBinaryMsg:
                                {
                                    var target = Program.Manager.GetClientByAccountId(binaryMessage.TargetAccountID);

                                    if (target != null)
                                    {
                                        target?.Queue(new MediusBinaryFwdMessage1()
                                        {
                                            MessageType = binaryMessage.MessageType,
                                            OriginatorAccountID = data.ClientObject.AccountId,
                                            Message = binaryMessage.Message
                                        });
                                    } else {
                                        Logger.Info("No players found to send binary msg to");
                                    }

                                    break;
                                }

                            case MediusBinaryMessageType.BroadcastBinaryMsgAcrossEntireUniverse:
                                {

                                    //MUMBinaryFwdFromLobby
                                    //MUMBinaryFwdFromLobby() Error %d MID %s, Orig AID %d, Whisper Target AID %d
                                    Logger.Info($"Sending BroadcastBinaryMsgAcrossEntireUniverse(%d) binary message (%d)");
                                    break;
                                }
                            default:
                                {
                                    Logger.Warn($"Unhandled binary message type {binaryMessage.MessageType}");
                                    break;
                                }
                        }
                        break;
                    }

                #endregion

                #region Misc

                case MediusTokenRequest tokenRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {tokenRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {tokenRequest} without being logged in.");

                        data.ClientObject.Queue(new MediusStatusResponse()
                        {
                            Class = tokenRequest.PacketClass,
                            Type = tokenRequest.PacketType,
                            MessageID = tokenRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess
                        });
                        break;
                    }

                case MediusPostDebugInfoRequest postDebugInfoRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {postDebugInfoRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {postDebugInfoRequest} without being logged in.");

                        if(Settings.PostDebugInfoEnable == false)
                        {

                            data.ClientObject.Queue(new MediusPostDebugInfoResponse
                            {
                                MessageID = postDebugInfoRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusFeatureNotEnabled
                            });
                        } else {

                            data.ClientObject.Queue(new MediusPostDebugInfoResponse
                            {
                                MessageID = postDebugInfoRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess
                            });
                        }

                        break;
                    }

                case MediusTextFilterRequest textFilterRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {textFilterRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {textFilterRequest} without being logged in.");

                        if (textFilterRequest.TextFilterType == MediusTextFilterType.MediusTextFilterPassFail)
                        {
                            if (Program.PassTextFilter(Config.TextFilterContext.DEFAULT, textFilterRequest.Text))
                            {
                                data.ClientObject.Queue(new MediusTextFilterResponse()
                                {
                                    MessageID = textFilterRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                    Text = textFilterRequest.Text
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusTextFilterResponse()
                                {
                                    MessageID = textFilterRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusTextStringInvalid
                                });
                            }
                        }
                        else
                        {
                            data.ClientObject.Queue(new MediusTextFilterResponse()
                            {
                                MessageID = textFilterRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                Text = Program.FilterTextFilter(Config.TextFilterContext.DEFAULT, textFilterRequest.Text).Trim()
                            });
                        }

                        break;
                    }

                //CAC
                case MediusTextFilterRequest1 textFilterRequest1:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {textFilterRequest1} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {textFilterRequest1} without being logged in.");
                        
                        
                        if (Program.PassTextFilter(Config.TextFilterContext.GAME_NAME, Convert.ToString(textFilterRequest1.Text)))
                        {
                            data.ClientObject.Queue(new MediusTextFilterResponse1()
                            {
                                MessageID = textFilterRequest1.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                TextSize = textFilterRequest1.TextSize,
                                Text = textFilterRequest1.Text,
                            });
                        } 
                        else if (Program.PassTextFilter(Config.TextFilterContext.DEFAULT, Convert.ToString(textFilterRequest1.Text)))
                        {
                            data.ClientObject.Queue(new MediusTextFilterResponse1()
                            {
                                MessageID = textFilterRequest1.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                TextSize = textFilterRequest1.TextSize,
                                Text = textFilterRequest1.Text,
                            });
                        }
                        else
                        {
                            data.ClientObject.Queue(new MediusTextFilterResponse1()
                            {
                                MessageID = textFilterRequest1.MessageID,
                                StatusCode = MediusCallbackStatus.MediusFail
                            });
                        }
                        break;
                    }
                    
                case MediusGetMyIPRequest getMyIpRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getMyIpRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getMyIpRequest} without being logged in.");

                        // Ban Mac Check if their in-game
                        _ = Program.Database.GetIsMacBanned(data.MachineId).ContinueWith((r) =>
                        {
                            if(r.Result == true)
                            {
                                #region isMacBanned?
                                Logger.Info(msg: $"getMyIp: Connected User MAC Banned: {r.Result}");

                                if (r.Result)
                                {
                                    // Account is banned
                                    // Tell the client you're no longer privileged
                                    data?.ClientObject?.Queue(new MediusGetMyIPResponse()
                                    {
                                        MessageID = getMyIpRequest.MessageID,
                                        IP = null,
                                        StatusCode = MediusCallbackStatus.MediusPlayerNotPrivileged
                                    });

                                    // Send ban message
                                    QueueBanMessage(data);
                                }
                                #endregion
                            } else {
                                #region Send IP & Success 
                                //Send back other Client's Address 
                                data.ClientObject.Queue(new MediusGetMyIPResponse()
                                {
                                    MessageID = getMyIpRequest.MessageID,
                                    IP = (clientChannel.RemoteAddress as IPEndPoint)?.Address,
                                    StatusCode = MediusCallbackStatus.MediusSuccess
                                });
                                #endregion
                            }
                        });

                        break;
                    }

                case MediusGetServerTimeRequest getServerTimeRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getServerTimeRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getServerTimeRequest} without being logged in.");

                        data.ClientObject.Queue(new MediusGetServerTimeResponse()
                        {
                            MessageID = getServerTimeRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                            Local_server_timezone = MediusTimeZone.MediusTimeZone_GMT
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

        private Task ProcessChatMessage(IChannel clientChannel, ClientObject clientObject, MediusChatMessage chatMessage)
        {
            var channel = clientObject.CurrentChannel;
            var game = clientObject.CurrentGame;
            var allPlayers = channel.Clients;
            var allButSender = channel.Clients.Where(x => x != clientObject);
            var targetPlayer = channel.Clients.FirstOrDefault(x => x.AccountId == chatMessage.TargetID);
            List<BaseScertMessage> chatResponses = new List<BaseScertMessage>();

            // Need to be logged in
            if (!clientObject.IsLoggedIn)
                return Task.CompletedTask;

            // Need to be in a channel
            if (channel == null)
                return Task.CompletedTask;

            switch (chatMessage.MessageType)
            {
                case MediusChatMessageType.Broadcast:
                    {
                        // Relay
                        foreach (var target in allButSender)
                        {
                            target.Queue(new MediusChatFwdMessage()
                            {
                                MessageID = chatMessage.MessageID,
                                OriginatorAccountID = clientObject.AccountId,
                                OriginatorAccountName = clientObject.AccountName,
                                MessageType = chatMessage.MessageType,
                                Message = chatMessage.Message
                            });
                        }
                        break;
                    }
                case MediusChatMessageType.Whisper:
                    {
                        // Send to
                        targetPlayer?.Queue(new MediusChatFwdMessage()
                        {
                            MessageID = new MessageId(),
                            OriginatorAccountID = clientObject.AccountId,
                            OriginatorAccountName = clientObject.AccountName,
                            MessageType = chatMessage.MessageType,
                            Message = chatMessage.Message
                        });
                        break;
                    }

                case MediusChatMessageType.MediusClanChatType:
                    {
                        // Relay
                        foreach (var target in allButSender)
                        {
                            target.Queue(new MediusChatFwdMessage()
                            {
                                MessageID = chatMessage.MessageID,
                                OriginatorAccountID = clientObject.AccountId,
                                OriginatorAccountName = clientObject.AccountName,
                                MessageType = chatMessage.MessageType,
                                Message = chatMessage.Message
                            });
                        }
                        break;
                    }
                default:
                    {
                        Logger.Warn($"Unhandled generic chat message type:{chatMessage.MessageType} {chatMessage}");
                        break;
                    }
            }

            return Task.CompletedTask;
        }


        private async Task ProcessGenericChatMessage(IChannel clientChannel, ClientObject clientObject, IMediusChatMessage chatMessage)
        {
            var channel = clientObject.CurrentChannel;
            var game = clientObject.CurrentGame;
            var allPlayers = channel.Clients;
            var allButSender = channel.Clients.Where(x => x != clientObject);
            List<BaseScertMessage> chatResponses = new List<BaseScertMessage>();

            // ERROR -- Need to be logged in
            if (!clientObject.IsLoggedIn)
                return;

            // Need to be in a channel
            if (channel == null)
                return;

            switch (chatMessage.MessageType)
            {
                case MediusChatMessageType.Broadcast:
                    {
                        // Relay
                        channel.BroadcastChatMessage(allButSender, clientObject, chatMessage.Message);
                        break;

                    }
                case MediusChatMessageType.Whisper:
                    {
                        //Whisper
                        channel.WhisperChatMessage(allButSender, clientObject, chatMessage.Message);
                        break;
                    }
                default:
                    {
                        Logger.Warn($"Unhandled generic chat message type:{chatMessage.MessageType} {chatMessage}");
                        break;
                    }
            }


            // Send to plugins
            await Program.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_ON_CHAT_MESSAGE, new OnPlayerChatMessageArgs() { Player = clientObject, Message = chatMessage });
        }

    }
}
