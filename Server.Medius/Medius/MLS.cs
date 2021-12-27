using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;
using RT.Common;
using RT.Cryptography;
using RT.Models;
using RT.Models.Misc;
using Server.Common;
using Server.Database.Models;
using Server.Medius.Models;
using Server.Medius.PluginArgs;
using Server.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Server.Medius
{
    public class MLS : BaseMediusComponent
    {
        static readonly IInternalLogger _logger = InternalLoggerFactory.GetInstance<MLS>();

        protected override IInternalLogger Logger => _logger;
        public override int Port => Program.Settings.MLSPort;

        public MLS()
        {

        }

        public ClientObject ReserveClient(MediusSessionBeginRequest request)
        {
            var client = new ClientObject();
            client.BeginSession();
            return client;
        }

        public ClientObject ReserveClient1(MediusSessionBegin1Request request)
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
                        // initialize default key
                        scertClient.CipherService.SetCipher(CipherContext.RSA_AUTH, scertClient.GetDefaultRSAKey(Program.Settings.DefaultKey));

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

                            Queue(new RT_MSG_SERVER_CONNECT_REQUIRE() { ReqServerPassword = 0, Contents = Utils.FromString("4802") }, clientChannel);
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

        protected virtual async Task ProcessMediusMessage(BaseMediusMessage message, IChannel clientChannel, ChannelData data)
        {
            if (message == null)
                return;

            switch (message)
            {
                #region Session

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

                #region Login / Logout

                case MediusAccountLogoutRequest accountLogoutRequest:
                    {
                        MediusCallbackStatus status = MediusCallbackStatus.MediusFail;

                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {accountLogoutRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {accountLogoutRequest} without a being logged in.");

                        // Check token
                        if (accountLogoutRequest.SessionKey == data.ClientObject.SessionKey)
                        {
                            // 
                            Logger.Info($"{data.ClientObject.AccountName} has logged out.");

                            // 
                            status = MediusCallbackStatus.MediusSuccess;

                            // Logout
                            data.ClientObject.Logout();
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
                        Program.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_ON_GET_ALL_ANNOUNCEMENTS, new OnPlayerRequestArgs()
                        {
                            Player = data.ClientObject,
                            Request = getAllAnnouncementsRequest
                        });

                        _ = Program.Database.GetLatestAnnouncements().ContinueWith((r) =>
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
                        Program.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_ON_GET_ANNOUNCEMENTS, new OnPlayerRequestArgs()
                        {
                            Player = data.ClientObject,
                            Request = getAnnouncementsRequest
                        });

                        _ = Program.Database.GetLatestAnnouncement().ContinueWith((r) =>
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
                        Program.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_ON_GET_POLICY, new OnPlayerRequestArgs()
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

                #region Account

                case MediusAccountGetIDRequest accountGetIdRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {accountGetIdRequest} without a session.");

                        _ = Program.Database.GetAccountByName(accountGetIdRequest.AccountName).ContinueWith((r) =>
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

                case MediusAccountUpdateStatsRequest accountUpdateStatsRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {accountUpdateStatsRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {accountUpdateStatsRequest} without a being logged in.");

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

                case MediusAddToBuddyListRequest addToBuddyListRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {addToBuddyListRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {addToBuddyListRequest} without a being logged in.");

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

                case MediusRemoveFromBuddyListRequest removeFromBuddyListRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {removeFromBuddyListRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {removeFromBuddyListRequest} without a being logged in.");

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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel},{data.ClientObject} sent {getBuddyList_ExtraInfoRequest} without a being logged in.");

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
                                AccountName = friend.Value,
                                OnlineState = new MediusPlayerOnlineState()
                                {
                                    ConnectStatus = (friendClient != null && friendClient.IsLoggedIn) ? friendClient.Status : MediusPlayerStatus.MediusPlayerDisconnected,
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

                        /*
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
                                            ConnectStatus = (friendClient != null && friendClient.IsLoggedIn) ? friendClient.Status : MediusPlayerStatus.MediusPlayerDisconnected,
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
                        */
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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getIgnoreListRequest} without a being logged in.");

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
                                        PlayerStatus = playerClient?.Status ?? MediusPlayerStatus.MediusPlayerDisconnected,
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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {addToIgnoreList} without a being logged in.");

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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {removeFromIgnoreListRequest} without a being logged in.");

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

                case MediusUpdateLadderStatsWideRequest updateLadderStatsWideRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {updateLadderStatsWideRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {updateLadderStatsWideRequest} without a being logged in.");

                        if (data.ClientObject.CurrentGame != null && !data.ClientObject.CurrentGame.AcceptStats)
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
                                        Stats = updateLadderStatsWideRequest.Stats
                                    }).ContinueWith((r) =>
                                    {
                                        if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                            return;

                                        if (r.IsCompletedSuccessfully && r.Result)
                                        {
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
                                    _ = Program.Database.PostClanLadderStats(new StatPostDTO()
                                    {
                                        AccountId = data.ClientObject.AccountId,
                                        Stats = updateLadderStatsWideRequest.Stats
                                    }).ContinueWith((r) =>
                                    {
                                        if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                            return;

                                        if (r.IsCompletedSuccessfully && r.Result)
                                        {
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

                case MediusGetLadderStatsWideRequest getLadderStatsWideRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getLadderStatsWideRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getLadderStatsWideRequest} without a being logged in.");

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

                case MediusLadderList_ExtraInfoRequest ladderList_ExtraInfoRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {ladderList_ExtraInfoRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {ladderList_ExtraInfoRequest} without a being logged in.");

                        //
                        _ = Program.Database.GetLeaderboard(ladderList_ExtraInfoRequest.LadderStatIndex + 1, (int)ladderList_ExtraInfoRequest.StartPosition - 1, (int)ladderList_ExtraInfoRequest.PageSize).ContinueWith((r) =>
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

                case MediusGetTotalRankingsRequest getTotalRankingsRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getTotalRankingsRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getTotalRankingsRequest} without a being logged in.");

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

                case MediusLadderPosition_ExtraInfoRequest ladderPosition_ExtraInfoRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {ladderPosition_ExtraInfoRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {ladderPosition_ExtraInfoRequest} without a being logged in.");


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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {clanLadderListRequest} without a being logged in.");

                        //
                        _ = Program.Database.GetClanLeaderboard(clanLadderListRequest.ClanLadderStatIndex, (int)clanLadderListRequest.StartPosition - 1, (int)clanLadderListRequest.PageSize).ContinueWith((r) =>
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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {findPlayerRequest} without a being logged in.");

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
                                ApplicationType = (foundPlayer.Status == MediusPlayerStatus.MediusPlayerInGameWorld) ? MediusApplicationType.MediusAppTypeGame : MediusApplicationType.LobbyChatChannel,
                                ApplicationName = "?????",
                                MediusWorldID = (foundPlayer.Status == MediusPlayerStatus.MediusPlayerInGameWorld) ? foundPlayer.CurrentGame?.Id ?? -1 : foundPlayer.CurrentChannel?.Id ?? -1,
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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {playerInfoRequest} without a being logged in.");

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
                                    PlayerStatus = (playerClientObject != null && playerClientObject.IsLoggedIn) ? playerClientObject.Status : MediusPlayerStatus.MediusPlayerDisconnected,
                                    ConnectionClass = MediusConnectionType.Ethernet,
                                    Stats = mediusStats
                                });
                            }
                            else
                            {
                                data?.ClientObject?.Queue(new MediusPlayerInfoResponse()
                                {
                                    MessageID = playerInfoRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusAccountNotFound
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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {updateUserState} without a being logged in.");

                        switch (updateUserState.UserAction)
                        {
                            case MediusUserAction.LeftGameWorld:
                                {
                                    //data.ClientObject.LeaveGame(data.ClientObject.CurrentGame);
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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {createClanRequest} without a being logged in.");


                        _ = Program.Database.CreateClan(new Database.Models.CreateClanDTO()
                        {
                            AccountId = data.ClientObject.AccountId,
                            ClanName = createClanRequest.ClanName,
                            AppId = data.ClientObject.ApplicationId
                        }).ContinueWith((r) =>
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

                                // store
                                _ = data.ClientObject.RefreshAccount();
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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {checkMyClanInvitationsRequest} without a being logged in.");

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
                                    ClanInvitationID = x.Invitation.Id,
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

                case MediusGetMyClansRequest getMyClansRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getMyClansRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getMyClansRequest} without a being logged in.");

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

                        break;
                    }

                case MediusGetClanMemberList_ExtraInfoRequest getClanMemberList_ExtraInfoRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getClanMemberList_ExtraInfoRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getClanMemberList_ExtraInfoRequest} without a being logged in.");

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
                                            ConnectStatus = account?.Status ?? MediusPlayerStatus.MediusPlayerDisconnected,
                                            GameName = account?.CurrentGame?.GameName ?? "",
                                            LobbyName = account?.CurrentChannel?.Name ?? "",
                                            MediusGameWorldID = account?.CurrentGame?.Id ?? -1,
                                            MediusLobbyWorldID = account?.CurrentChannel?.Id ?? -1
                                        },
                                        Stats = Convert.FromBase64String(r.Result.ClanMediusStats),
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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getClanInvitiationsSentRequest} without a being logged in.");

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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getClanByIdRequest} without a being logged in.");

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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getClanByNameRequest} without a being logged in.");

                        _ = Program.Database.GetClanByName(getClanByNameRequest.ClanName).ContinueWith(r =>
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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getClanLadderPositionRequest} without a being logged in.");

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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {updateClanStatsRequest} without a being logged in.");

                        _ = Program.Database.PostClanMediusStatus(updateClanStatsRequest.ClanID, Convert.ToBase64String(updateClanStatsRequest.Stats)).ContinueWith((r) =>
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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {invitePlayerToClanRequest} without a being logged in.");

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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {respondToClanInvitationRequest} without a being logged in.");

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

                                // store
                                _ = data.ClientObject.RefreshAccount();
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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {revokeClanInvitationRequest} without a being logged in.");

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
                            }
                            else
                            {
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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {disbandClanRequest} without a being logged in.");

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
                            }
                            else
                            {
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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getMyClanMessagesRequest} without a being logged in.");

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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getAllClanMessagesRequest} without a being logged in.");

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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {sendClanMessageRequest} without a being logged in.");

                        // ERROR -- Need clan
                        if (!data.ClientObject.ClanId.HasValue)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {sendClanMessageRequest} without having a clan.");

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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {modifyClanMessageRequest} without a being logged in.");

                        // ERROR -- Need clan
                        if (!data.ClientObject.ClanId.HasValue)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {modifyClanMessageRequest} without having a clan.");

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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {transferClanLeadershipRequest} without a being logged in.");

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

                #region Game

                case MediusGetGameListFilterRequest getGameListFilterRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getGameListFilterRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getGameListFilterRequest} without a being logged in.");

                        var filters = data.ClientObject.GameListFilters;
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
                        break;
                    }

                case MediusGameList_ExtraInfoRequest gameList_ExtraInfoRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {gameList_ExtraInfoRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {gameList_ExtraInfoRequest} without a being logged in.");

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

                        break;
                    }

                case MediusGameList_ExtraInfoRequest0 gameList_ExtraInfoRequest0:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {gameList_ExtraInfoRequest0} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {gameList_ExtraInfoRequest0} without a being logged in.");

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

                        break;
                    }

                case MediusGameInfoRequest gameInfoRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {gameInfoRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {gameInfoRequest} without a being logged in.");

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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {gameInfoRequest0} without a being logged in.");

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

                case MediusGameWorldPlayerListRequest gameWorldPlayerListRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {gameWorldPlayerListRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {gameWorldPlayerListRequest} without a being logged in.");

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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {setGameListFilterRequest} without a being logged in.");

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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {setGameListFilterRequest0} without a being logged in.");

                        // Set filter
                        var filter = data.ClientObject.SetGameListFilter(setGameListFilterRequest0);

                        // Give reply
                        data.ClientObject.Queue(new MediusSetGameListFilterResponse0()
                        {
                            MessageID = setGameListFilterRequest0.MessageID,
                            StatusCode = filter == null ? MediusCallbackStatus.MediusFail : MediusCallbackStatus.MediusSuccess,
                            FilterID = filter?.FieldID ?? 0
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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {clearGameListFilterRequest} without a being logged in.");

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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {clearGameListFilterRequest0} without a being logged in.");

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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {createGameRequest} without a being logged in.");

                        // Send to plugins
                        Program.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_ON_CREATE_GAME, new OnPlayerRequestArgs() { Player = data.ClientObject, Request = createGameRequest });

                        Program.Manager.CreateGame(data.ClientObject, createGameRequest);
                        break;
                    }

                case MediusCreateGameRequest1 createGameRequest1:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {createGameRequest1} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {createGameRequest1} without a being logged in.");

                        // Send to plugins
                        Program.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_ON_CREATE_GAME, new OnPlayerRequestArgs() { Player = data.ClientObject, Request = createGameRequest1 });

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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {joinGameRequest} without a being logged in.");

                        // Send to plugins
                        Program.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_ON_JOIN_GAME, new OnPlayerRequestArgs() { Player = data.ClientObject, Request = joinGameRequest });

                        Program.Manager.JoinGame(data.ClientObject, joinGameRequest);
                        break;
                    }

                case MediusWorldReport worldReport:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {worldReport} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {worldReport} without a being logged in.");

                        data.ClientObject.CurrentGame?.OnWorldReport(worldReport);

                        break;
                    }

                case MediusPlayerReport playerReport:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {playerReport} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel},{data.ClientObject} sent {playerReport} without a being logged in.");

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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {endGameReport} without a being logged in.");

                        data.ClientObject.CurrentGame?.OnEndGameReport(endGameReport);
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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getLobbyPlayerNames_ExtraInfoRequest} without a being logged in.");

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
                                    ConnectStatus = x.Status,
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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getLobbyPlayerNamesRequest} without a being logged in.");

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

                case MediusSetLobbyWorldFilterRequest setLobbyWorldFilterRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {setLobbyWorldFilterRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {setLobbyWorldFilterRequest} without a being logged in.");

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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {setLobbyWorldFilterRequest} without a being logged in.");

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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {createChannelRequest} without a being logged in.");

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

                case MediusJoinChannelRequest joinChannelRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {joinChannelRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {joinChannelRequest} without a being logged in.");

                        var channel = Program.Manager.GetChannelByChannelId(joinChannelRequest.MediusWorldID);
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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {channelInfoRequest} without a being logged in.");

                        // Find channel
                        var channel = Program.Manager.GetChannelByChannelId(channelInfoRequest.MediusWorldID);

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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {channelListRequest} without a being logged in.");

                        List<MediusChannelListResponse> channelResponses = new List<MediusChannelListResponse>();
                        var gameChannels = Program.Manager.GetChannelList(
                            data.ClientObject.ApplicationId,
                            channelListRequest.PageID,
                            channelListRequest.PageSize,
                            ChannelType.Game);

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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getTotalChannelsRequest} without a being logged in.");

                        data.ClientObject.Queue(new MediusGetTotalChannelsResponse()
                        {
                            MessageID = getTotalChannelsRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                            Total = Program.Manager.GetChannelCount(ChannelType.Lobby)
                        });

                        break;
                    }

                case MediusChannelList_ExtraInfoRequest channelList_ExtraInfoRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {channelList_ExtraInfoRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {channelList_ExtraInfoRequest} without a being logged in.");

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

                #region File

                case MediusFileCreateRequest fileCreateRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileCreateRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileCreateRequest} without a being logged in.");

                        data.ClientObject.Queue(new MediusFileCreateResponse()
                        {
                            MessageID = fileCreateRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusDBError,
                            MediusFileInfo = new MediusFile()
                            {

                            }
                        });
                        break;
                    }

                case MediusFileDownloadRequest fileDownloadRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileDownloadRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileDownloadRequest} without a being logged in.");

                        data.ClientObject.Queue(new MediusFileDownloadResponse()
                        {
                            MessageID = fileDownloadRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusDBError
                        });
                        break;
                    }

                #endregion

                #region Chat / Binary Message

                case MediusGenericChatSetFilterRequest genericChatSetFilterRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {genericChatSetFilterRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {genericChatSetFilterRequest} without a being logged in.");

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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {setAutoChatHistoryRequest} without a being logged in.");

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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {genericChatMessage} without a being logged in.");

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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {genericChatMessage} without a being logged in.");

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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {chatMessage} without a being logged in.");

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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {binaryMessage} without a being logged in.");

                        switch (binaryMessage.MessageType)
                        {
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
                            case MediusBinaryMessageType.BroadcastBinaryMsg:
                                {
                                    data.ClientObject.CurrentChannel?.BroadcastBinaryMessage(data.ClientObject, binaryMessage);
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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {tokenRequest} without a being logged in.");

                        data.ClientObject.Queue(new MediusStatusResponse()
                        {
                            Class = tokenRequest.PacketClass,
                            Type = tokenRequest.PacketType,
                            MessageID = tokenRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess
                        });
                        break;
                    }

                case MediusTextFilterRequest textFilterRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {textFilterRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {textFilterRequest} without a being logged in.");

                        data.ClientObject.Queue(new MediusTextFilterResponse()
                        {
                            MessageID = textFilterRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                            Text = textFilterRequest.Text
                        });

                        break;
                    }

                case MediusTextFilterRequest1 textFilterRequest1:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {textFilterRequest1} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {textFilterRequest1} without a being logged in.");


                        data.ClientObject.Queue(new MediusTextFilterResponse1()
                        {
                            MessageID = textFilterRequest1.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess
                        });

                        break;
                    }

                case MediusGetMyIPRequest getMyIpRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getMyIpRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getMyIpRequest} without a being logged in.");

                        data.ClientObject.Queue(new MediusGetMyIPResponse()
                        {
                            MessageID = getMyIpRequest.MessageID,
                            IP = (clientChannel.RemoteAddress as IPEndPoint)?.Address,
                            StatusCode = MediusCallbackStatus.MediusSuccess
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
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getServerTimeRequest} without a being logged in.");

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

        private async Task ProcessChatMessage(IChannel clientChannel, ClientObject clientObject, MediusChatMessage chatMessage)
        {
            var channel = clientObject.CurrentChannel;
            var game = clientObject.CurrentGame;
            var allPlayers = channel.Clients;
            var allButSender = channel.Clients.Where(x => x != clientObject);
            var targetPlayer = channel.Clients.FirstOrDefault(x => x.AccountId == chatMessage.TargetID);
            List<BaseScertMessage> chatResponses = new List<BaseScertMessage>();

            // Need to be logged in
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
                        foreach (var target in allPlayers)
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
                default:
                    {
                        Logger.Warn($"Unhandled generic chat message type:{chatMessage.MessageType} {chatMessage}");
                        break;
                    }
            }
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
                default:
                    {
                        Logger.Warn($"Unhandled generic chat message type:{chatMessage.MessageType} {chatMessage}");
                        break;
                    }
            }


            // Send to plugins
            Program.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_ON_CHAT_MESSAGE, new OnPlayerChatMessageArgs() { Player = clientObject, Message = chatMessage });

        }

    }
}
