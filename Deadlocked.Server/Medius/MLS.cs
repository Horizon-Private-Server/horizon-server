using Deadlocked.Server.Accounts;
using Deadlocked.Server.Medius.Models;
using Deadlocked.Server.Medius.Models.Packets;
using Deadlocked.Server.Medius.Models.Packets.Lobby;
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
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Deadlocked.Server.Medius
{
    public class MLS : BaseMediusComponent
    {
        static readonly IInternalLogger _logger = InternalLoggerFactory.GetInstance<MLS>();

        protected override IInternalLogger Logger => _logger;
        public override string Name => "MLS";
        public override int Port => Program.Settings.MLSPort;
        public override PS2_RSA AuthKey => Program.GlobalAuthKey;

        public MLS()
        {
            _sessionCipher = new PS2_RC4(Utils.FromString(Program.KEY), CipherContext.RC_CLIENT_SESSION);
        }


        public ClientObject ReserveClient(MediusSessionBeginRequest request)
        {
            var client = new ClientObject();
            Program.Clients.Add(client);
            return client;
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
                        // 
                        data.ApplicationId = clientConnectTcp.AppId;
                        data.ClientObject = Program.Clients.FirstOrDefault(x => x.Token == clientConnectTcp.AccessToken);
                        if (data.ClientObject == null)
                        {
                            await DisconnectClient(clientChannel);
                        }
                        else
                        {
                            // Update our client object to use existing one
                            data.ClientObject.ApplicationId = clientConnectTcp.AppId;

                            Queue(new RT_MSG_SERVER_CONNECT_REQUIRE() { Contents = Utils.FromString("024802") }, clientChannel);
                        }

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
                        await ProcessMediusMessage(clientAppToServer.Message, clientChannel, data);
                        break;
                    }

                case RT_MSG_CLIENT_DISCONNECT_WITH_REASON clientDisconnectWithReason:
                    {
                        await DisconnectClient(clientChannel);
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

                        // Remove
                        Program.Clients.Remove(data.ClientObject);

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
                        if (data.ClientObject.ClientAccount == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {accountLogoutRequest} without a being logged in.");

                        // Check token
                        if (accountLogoutRequest.SessionKey == data.ClientObject.SessionKey)
                        {
                            // 
                            Logger.Info($"{data.ClientObject.ClientAccount.AccountName} has logged out.");

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

                        data.ClientObject.Queue(new MediusGetAnnouncementsResponse()
                        {
                            MessageID = getAllAnnouncementsRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                            Announcement = Program.Settings.Announcement,
                            AnnouncementID = 0,
                            EndOfList = true
                        });
                        break;
                    }

                case MediusGetPolicyRequest getPolicyRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getPolicyRequest} without a session.");

                        string policyText = getPolicyRequest.Policy == MediusPolicyType.Privacy ? Program.Settings.PrivacyPolicy : Program.Settings.UsagePolicy;

                        if (!string.IsNullOrEmpty(policyText))
                            data.ClientObject.Queue(MediusGetPolicyResponse.FromText(getPolicyRequest.MessageID, policyText));
                        else
                            data.ClientObject.Queue(new MediusGetPolicyResponse() { MessageID = getPolicyRequest.MessageID, StatusCode = MediusCallbackStatus.MediusSuccess, Policy = "", EndOfText = true });
                        break;
                    }

                #endregion

                #region Account

                case MediusAccountGetIDRequest accountGetIdRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {accountGetIdRequest} without a session.");

                        if (Program.Database.TryGetAccountByName(accountGetIdRequest.AccountName, out var account))
                        {
                            data.ClientObject.Queue(new MediusAccountGetIDResponse()
                            {
                                MessageID = accountGetIdRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                AccountID = account.AccountId
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

                        break;
                    }

                case MediusAccountUpdateStatsRequest accountUpdateStatsRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {accountUpdateStatsRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (data.ClientObject.ClientAccount == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {accountUpdateStatsRequest} without a being logged in.");

                        // Update stats
                        if (data.ClientObject.ClientAccount != null)
                        {
                            data.ClientObject.ClientAccount.Stats = accountUpdateStatsRequest.Stats;
                            Program.Database.Save();
                        }

                        data.ClientObject.Queue(new MediusAccountUpdateStatsResponse()
                        {
                            MessageID = accountUpdateStatsRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess
                        });
                        break;
                    }

                #endregion

                #region Buddy List

                case MediusAddToBuddyListRequest addToBuddyListRequest:
                    {
                        var statusCode = MediusCallbackStatus.MediusFail;

                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {addToBuddyListRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (data.ClientObject.ClientAccount == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {addToBuddyListRequest} without a being logged in.");

                        // find target player
                        if (Program.Database.TryGetAccountById(addToBuddyListRequest.AccountID, out var targetAccount))
                        {
                            if (data.ClientObject.ClientAccount.Friends.Contains(addToBuddyListRequest.AccountID))
                            {

                                statusCode = MediusCallbackStatus.MediusAccountAlreadyExists;
                            }
                            else
                            {
                                // targetAccount

                                // Temporarily auto add player as friend
                                // This should be replaced with a confirm/request system later
                                data.ClientObject.ClientAccount.Friends.Add(addToBuddyListRequest.AccountID);
                                Program.Database.Save();

                                statusCode = MediusCallbackStatus.MediusSuccess;
                            }
                        }

                        data.ClientObject.Queue(new MediusAddToBuddyListResponse()
                        {
                            MessageID = addToBuddyListRequest.MessageID,
                            StatusCode = statusCode
                        });
                        break;
                    }

                case MediusRemoveFromBuddyListRequest removeFromBuddyListRequest:
                    {
                        // 
                        var statusCode = MediusCallbackStatus.MediusFail;

                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {removeFromBuddyListRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (data.ClientObject.ClientAccount == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {removeFromBuddyListRequest} without a being logged in.");

                        // 
                        if (data.ClientObject != null && data.ClientObject.ClientAccount != null)
                        {
                            // find target friend
                            if (data.ClientObject.ClientAccount.Friends.Contains(removeFromBuddyListRequest.AccountID))
                            {
                                // remove
                                data.ClientObject.ClientAccount.Friends.Remove(removeFromBuddyListRequest.AccountID);
                                Program.Database.Save();

                                statusCode = MediusCallbackStatus.MediusSuccess;
                            }
                        }

                        data.ClientObject.Queue(new MediusRemoveFromBuddyListResponse()
                        {
                            MessageID = removeFromBuddyListRequest.MessageID,
                            StatusCode = statusCode
                        });
                        break;
                    }

                case MediusGetBuddyList_ExtraInfoRequest getBuddyList_ExtraInfoRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getBuddyList_ExtraInfoRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (data.ClientObject.ClientAccount == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getBuddyList_ExtraInfoRequest} without a being logged in.");

                        // Get friends
                        var friends = data.ClientObject.ClientAccount.Friends?.Select(x => Program.Database.GetAccountById(x)).Where(x => x != null);

                        // Responses
                        List<MediusGetBuddyList_ExtraInfoResponse> friendListResponses = new List<MediusGetBuddyList_ExtraInfoResponse>();

                        // 
                        if (friends != null)
                        {
                            // Iterate and send to client
                            foreach (var friend in friends)
                            {
                                friendListResponses.Add(new MediusGetBuddyList_ExtraInfoResponse()
                                {
                                    MessageID = getBuddyList_ExtraInfoRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                    AccountID = friend.AccountId,
                                    AccountName = friend.AccountName,
                                    OnlineState = new MediusPlayerOnlineState()
                                    {
                                        ConnectStatus = friend.IsLoggedIn ? (friend.Client?.Status ?? MediusPlayerStatus.MediusPlayerDisconnected) : MediusPlayerStatus.MediusPlayerDisconnected,
                                        MediusLobbyWorldID = friend.Client?.CurrentChannel?.Id ?? Program.Settings.DefaultChannelId,
                                        MediusGameWorldID = friend.Client?.CurrentGame?.Id ?? -1,
                                    },
                                    EndOfList = false
                                });
                            }
                        }

                        if (friendListResponses.Count > 0)
                        {
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
                        if (data.ClientObject.ClientAccount == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getIgnoreListRequest} without a being logged in.");

                        // Get ignored players
                        var ignored = data.ClientObject.ClientAccount.Ignored.Select(x => Program.Database.GetAccountById(x)).Where(x => x != null);

                        // Responses
                        List<MediusGetIgnoreListResponse> ignoredListResponses = new List<MediusGetIgnoreListResponse>();

                        // Iterate and send to client
                        foreach (var player in ignored)
                        {
                            ignoredListResponses.Add(new MediusGetIgnoreListResponse()
                            {
                                MessageID = getIgnoreListRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                IgnoreAccountID = player.AccountId,
                                IgnoreAccountName = player.AccountName,
                                PlayerStatus = player.Client?.Status ?? MediusPlayerStatus.MediusPlayerDisconnected,
                                EndOfList = false
                            });
                        }

                        if (ignoredListResponses.Count > 0)
                        {
                            // End list
                            ignoredListResponses[ignoredListResponses.Count - 1].EndOfList = true;

                            // Send ignored list
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
                        break;
                    }

                case MediusAddToIgnoreListRequest addToIgnoreList:
                    {
                        var statusCode = MediusCallbackStatus.MediusFail;

                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {addToIgnoreList} without a session.");

                        // ERROR -- Need to be logged in
                        if (data.ClientObject.ClientAccount == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {addToIgnoreList} without a being logged in.");

                        // find target player
                        if (Program.Database.TryGetAccountById(addToIgnoreList.IgnoreAccountID, out var targetAccount))
                        {
                            // Ensure they're not already ignored
                            if (data.ClientObject.ClientAccount.Ignored.Contains(addToIgnoreList.IgnoreAccountID))
                            {
                                statusCode = MediusCallbackStatus.MediusAccountAlreadyExists;
                            }
                            else
                            {
                                // Remove from friends if a friend
                                data.ClientObject.ClientAccount.Friends.Remove(addToIgnoreList.IgnoreAccountID);
                                data.ClientObject.ClientAccount.Ignored.Add(addToIgnoreList.IgnoreAccountID);
                                Program.Database.Save();

                                statusCode = MediusCallbackStatus.MediusSuccess;
                            }
                        }

                        data.ClientObject.Queue(new MediusAddToIgnoreListResponse()
                        {
                            MessageID = addToIgnoreList.MessageID,
                            StatusCode = statusCode
                        });
                        break;
                    }

                case MediusRemoveFromIgnoreListRequest removeFromIgnoreListRequest:
                    {
                        var statusCode = MediusCallbackStatus.MediusFail;

                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {removeFromIgnoreListRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (data.ClientObject.ClientAccount == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {removeFromIgnoreListRequest} without a being logged in.");

                        // 
                        if (data.ClientObject != null && data.ClientObject.ClientAccount != null)
                        {
                            // find target ignored
                            if (data.ClientObject.ClientAccount.Ignored.Contains(removeFromIgnoreListRequest.IgnoreAccountID))
                            {
                                // remove
                                data.ClientObject.ClientAccount.Ignored.Remove(removeFromIgnoreListRequest.IgnoreAccountID);
                                Program.Database.Save();

                                statusCode = MediusCallbackStatus.MediusSuccess;
                            }
                        }

                        data.ClientObject.Queue(new MediusRemoveFromIgnoreListResponse()
                        {
                            MessageID = removeFromIgnoreListRequest.MessageID,
                            StatusCode = statusCode
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
                        if (data.ClientObject.ClientAccount == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {updateLadderStatsWideRequest} without a being logged in.");

                        switch (updateLadderStatsWideRequest.LadderType)
                        {
                            case MediusLadderType.MediusLadderTypePlayer:
                                {
                                    data.ClientObject.ClientAccount.AccountWideStats = updateLadderStatsWideRequest.Stats;
                                    Program.Database.Save();

                                    data.ClientObject.Queue(new MediusUpdateLadderStatsWideResponse()
                                    {
                                        MessageID = updateLadderStatsWideRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess
                                    });
                                    break;
                                }
                            case MediusLadderType.MediusLadderTypeClan:
                                {
                                    data.ClientObject.Queue(new MediusUpdateLadderStatsWideResponse()
                                    {
                                        MessageID = updateLadderStatsWideRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess
                                    });
                                    break;
                                }
                        }

                        break;
                    }

                case MediusGetLadderStatsWideRequest getLadderStatsWideRequest:
                    {
                        int[] stats = null;

                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getLadderStatsWideRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (data.ClientObject.ClientAccount == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getLadderStatsWideRequest} without a being logged in.");

                        switch (getLadderStatsWideRequest.LadderType)
                        {
                            case MediusLadderType.MediusLadderTypePlayer:
                                {
                                    if (Program.Database.TryGetAccountById(getLadderStatsWideRequest.AccountID_or_ClanID, out var account))
                                        stats = account.AccountWideStats;
                                    break;
                                }
                            case MediusLadderType.MediusLadderTypeClan:
                                {

                                    break;
                                }
                        }

                        var response = new MediusGetLadderStatsWideResponse()
                        {
                            MessageID = getLadderStatsWideRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                            AccountID_or_ClanID = getLadderStatsWideRequest.AccountID_or_ClanID
                        };

                        if (stats != null)
                            response.Stats = stats;

                        data.ClientObject.Queue(response);
                        break;
                    }

                case MediusLadderList_ExtraInfoRequest ladderList_ExtraInfoRequest:
                    {
                        var account = data.ClientObject.ClientAccount;

                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {ladderList_ExtraInfoRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (data.ClientObject.ClientAccount == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {ladderList_ExtraInfoRequest} without a being logged in.");

                        data.ClientObject.Queue(new MediusLadderList_ExtraInfoResponse()
                        {
                            MessageID = ladderList_ExtraInfoRequest.MessageID,
                            AccountID = account.AccountId,
                            AccountName = account.AccountName,
                            AccountStats = account.Stats,
                            LadderPosition = 0,
                            LadderStat = account.AccountWideStats[ladderList_ExtraInfoRequest.LadderStatIndex],
                            OnlineState = new MediusPlayerOnlineState()
                            {

                            },
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                            EndOfList = true
                        });
                        break;
                    }

                case MediusGetTotalRankingsRequest getTotalRankingsRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getTotalRankingsRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (data.ClientObject.ClientAccount == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getTotalRankingsRequest} without a being logged in.");


                        /*
                        switch (msg.LadderType)
                        {
                            case MediusLadderType.MediusLadderTypeClan:
                                {

                                    break;
                                }
                            case MediusLadderType.MediusLadderTypePlayer:
                                {

                                    break;
                                }
                        }
                        */

                        //
                        data.ClientObject.Queue(new MediusGetTotalRankingsResponse()
                        {
                            MessageID = getTotalRankingsRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                            TotalRankings = 0
                        });
                        break;
                    }

                case MediusLadderPosition_ExtraInfoRequest ladderPosition_ExtraInfoRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {ladderPosition_ExtraInfoRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (data.ClientObject.ClientAccount == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {ladderPosition_ExtraInfoRequest} without a being logged in.");

                        data.ClientObject.Queue(new MediusLadderPosition_ExtraInfoResponse()
                        {
                            MessageID = ladderPosition_ExtraInfoRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess
                        });
                        break;
                    }

                #endregion

                #region Player Info

                case MediusFindPlayerRequest findPlayerRequest:
                    {
                        Account account = null;

                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {findPlayerRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (data.ClientObject.ClientAccount == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {findPlayerRequest} without a being logged in.");

                        if (findPlayerRequest.SearchType == MediusPlayerSearchType.PlayerAccountID && !Program.Database.TryGetAccountById(findPlayerRequest.ID, out account))
                            account = null;
                        else if (findPlayerRequest.SearchType == MediusPlayerSearchType.PlayerAccountName && !Program.Database.TryGetAccountByName(findPlayerRequest.Name, out account))
                            account = null;

                        if (account == null)
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
                            if (account.IsLoggedIn)
                            {
                                data.ClientObject.Queue(new MediusFindPlayerResponse()
                                {
                                    MessageID = findPlayerRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                    ApplicationID = Program.Settings.ApplicationId,
                                    AccountID = account.AccountId,
                                    AccountName = account.AccountName,
                                    ApplicationType = (account.Client.Status == MediusPlayerStatus.MediusPlayerInGameWorld) ? MediusApplicationType.MediusAppTypeGame : MediusApplicationType.LobbyChatChannel,
                                    ApplicationName = "?????",
                                    MediusWorldID = (account.Client.Status == MediusPlayerStatus.MediusPlayerInGameWorld) ? account.Client.CurrentGameId : account.Client.CurrentChannelId,
                                    EndOfList = true
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusFindPlayerResponse()
                                {
                                    MessageID = findPlayerRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult,
                                    EndOfList = true
                                });
                            }
                        }
                        break;
                    }

                case MediusPlayerInfoRequest playerInfoRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {playerInfoRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (data.ClientObject.ClientAccount == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {playerInfoRequest} without a being logged in.");

                        if (Program.Database.TryGetAccountById(playerInfoRequest.AccountID, out var account))
                        {
                            data.ClientObject.Queue(new MediusPlayerInfoResponse()
                            {
                                MessageID = playerInfoRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                AccountName = account.AccountName,
                                ApplicationID = Program.Settings.ApplicationId,
                                PlayerStatus = account.IsLoggedIn ? account.Client.Status : MediusPlayerStatus.MediusPlayerDisconnected,
                                ConnectionClass = MediusConnectionType.Ethernet,
                                Stats = account.Stats
                            });
                        }
                        else
                        {
                            data.ClientObject.Queue(new MediusPlayerInfoResponse()
                            {
                                MessageID = playerInfoRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusAccountNotFound
                            });
                        }
                        break;
                    }


                case MediusUpdateUserState updateUserState:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {updateUserState} without a session.");

                        // ERROR -- Need to be logged in
                        if (data.ClientObject.ClientAccount == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {updateUserState} without a being logged in.");

                        data.ClientObject.Action = updateUserState.UserAction;

                        switch (updateUserState.UserAction)
                        {
                            case MediusUserAction.JoinedChatWorld:
                            case MediusUserAction.LeftGameWorld:
                                {
                                    data.ClientObject.Status = MediusPlayerStatus.MediusPlayerInChatWorld;
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
                        if (data.ClientObject.ClientAccount == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {createClanRequest} without a being logged in.");

                        // Disable for now
                        data.ClientObject.Queue(new MediusCreateClanResponse()
                        {
                            MessageID = createClanRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess
                        });

                        break;
                    }

                case MediusCheckMyClanInvitationsRequest checkMyClanInvitationsRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {checkMyClanInvitationsRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (data.ClientObject.ClientAccount == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {checkMyClanInvitationsRequest} without a being logged in.");

                        // Disabled for now
                        data.ClientObject.Queue(new MediusCheckMyClanInvitationsResponse()
                        {
                            MessageID = checkMyClanInvitationsRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusNoResult
                        });

                        break;
                    }

                case MediusGetMyClansRequest getMyClansRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getMyClansRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (data.ClientObject.ClientAccount == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getMyClansRequest} without a being logged in.");

                        data.ClientObject.Queue(new MediusGetMyClansResponse()
                        {
                            MessageID = getMyClansRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusClanNotFound,
                            ClanID = -1,
                            ApplicationID = Program.Settings.ApplicationId,
                            ClanName = null,
                            LeaderAccountID = data.ClientObject.ClientAccount.AccountId,
                            LeaderAccountName = data.ClientObject.ClientAccount.AccountName,
                            Stats = "000000000000000000000000",
                            Status = MediusClanStatus.ClanDisbanded,
                            EndOfList = true
                        });
                        break;
                    }

                case MediusGetClanMemberList_ExtraInfoRequest getClanMemberList_ExtraInfoRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getClanMemberList_ExtraInfoRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (data.ClientObject.ClientAccount == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getClanMemberList_ExtraInfoRequest} without a being logged in.");

                        data.ClientObject.Queue(new MediusGetClanMemberList_ExtraInfoResponse()
                        {
                            MessageID = getClanMemberList_ExtraInfoRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusClanNotFound,
                            /*
                            AccountID = x.AccountId,
                            AccountName = x.Username,
                            OnlineState = new MediusPlayerOnlineState()
                            {
                                ConnectStatus = MediusPlayerStatus.MediusPlayerInChatWorld,
                                GameName = "ABC",
                                LobbyName = "123",
                                MediusGameWorldID = 2,
                                MediusLobbyWorldID = 2
                            }
                            */
                        });

                        /*
                        responses.Add(new RT_MSG_SERVER_APP()
                        {
                            data.ClientObject.Queue(new MediusGetClanMemberList_ExtraInfoResponse()
                            {
                                MessageID = getClanMemberListExtra.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                AccountID = 2,
                                AccountName = "URMOM",
                                OnlineState = new MediusPlayerOnlineState()
                                {
                                    ConnectStatus = MediusPlayerStatus.MediusPlayerInChatWorld,
                                    GameName = "ABC",
                                    LobbyName = "123",
                                    MediusGameWorldID = 2,
                                    MediusLobbyWorldID = 2
                                },
                                EndOfList = true
                            }
                        });
                        */

                        break;
                    }

                case MediusGetClanInvitationsSentRequest getClanInvitiationsSentRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getClanInvitiationsSentRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (data.ClientObject.ClientAccount == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getClanInvitiationsSentRequest} without a being logged in.");

                        data.ClientObject.Queue(new MediusGetClanInvitationsSentResponse()
                        {
                            MessageID = getClanInvitiationsSentRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusNoResult,
                            EndOfList = true
                        });
                        break;
                    }

                case MediusGetClanByIDRequest getClanByIdRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getClanByIdRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (data.ClientObject.ClientAccount == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getClanByIdRequest} without a being logged in.");

                        data.ClientObject.Queue(new MediusGetClanByIDResponse()
                        {
                            MessageID = getClanByIdRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusNoResult,
                            /*
                            ApplicationID = Program.Settings.ApplicationId,
                            ClanName = "DEV",
                            LeaderAccountID = 1,
                            LeaderAccountName = "Badger41",
                            Status = MediusClanStatus.ClanActive
                            */
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
                        if (data.ClientObject.ClientAccount == null)
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
                        if (data.ClientObject.ClientAccount == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {gameList_ExtraInfoRequest} without a being logged in.");

                        var gameList = Program.Games
                            .Where(x => x.ApplicationId == data.ClientObject.ApplicationId)
                            .Where(x => x.WorldStatus == MediusWorldStatus.WorldActive || x.WorldStatus == MediusWorldStatus.WorldStaging)
                            .Where(x => data.ClientObject.IsGameMatch(x))
                            .Skip((gameList_ExtraInfoRequest.PageID - 1) * gameList_ExtraInfoRequest.PageSize)
                            .Take(gameList_ExtraInfoRequest.PageSize)
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

                case MediusGameInfoRequest gameInfoRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {gameInfoRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (data.ClientObject.ClientAccount == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {gameInfoRequest} without a being logged in.");

                        var game = Program.GetGameById(gameInfoRequest.MediusWorldID);
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
                                ApplicationID = Program.Settings.ApplicationId
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
                        if (data.ClientObject.ClientAccount == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {gameWorldPlayerListRequest} without a being logged in.");

                        var game = Program.GetGameById(gameWorldPlayerListRequest.MediusWorldID);
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
                            var playerList = game.Clients.Where(x => x != null && x.Client.IsConnected).Select(x => new MediusGameWorldPlayerListResponse()
                            {
                                MessageID = gameWorldPlayerListRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                AccountID = x.Client.ClientAccount.AccountId,
                                AccountName = x.Client.ClientAccount.AccountName,
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
                        if (data.ClientObject.ClientAccount == null)
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

                case MediusClearGameListFilterRequest clearGameListFilterRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {clearGameListFilterRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (data.ClientObject.ClientAccount == null)
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


                case MediusCreateGameRequest createGameRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {createGameRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (data.ClientObject.ClientAccount == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {createGameRequest} without a being logged in.");

                        Program.ProxyServer.CreateGame(data.ClientObject, createGameRequest);
                        break;
                    }

                case MediusJoinGameRequest joinGameRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {joinGameRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (data.ClientObject.ClientAccount == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {joinGameRequest} without a being logged in.");

                        Program.ProxyServer.JoinGame(data.ClientObject, joinGameRequest);
                        break;
                    }

                case MediusWorldReport worldReport:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {worldReport} without a session.");

                        // ERROR -- Need to be logged in
                        if (data.ClientObject.ClientAccount == null)
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
                        if (data.ClientObject.ClientAccount == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {playerReport} without a being logged in.");

                        // 
                        if (data.ClientObject.CurrentGameId == playerReport.MediusWorldID &&
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
                        if (data.ClientObject.ClientAccount == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {endGameReport} without a being logged in.");

                        data.ClientObject.CurrentGame?.OnEndGameReport(endGameReport);
                        break;
                    }

                #endregion

                #region Channel

                case MediusSetLobbyWorldFilterRequest setLobbyWorldFilterRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {setLobbyWorldFilterRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (data.ClientObject.ClientAccount == null)
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

                case MediusCreateChannelRequest createChannelRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {createChannelRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (data.ClientObject.ClientAccount == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {createChannelRequest} without a being logged in.");

                        // Create channel
                        Channel channel = new Channel(createChannelRequest);

                        // Add
                        Program.Channels.Add(channel);

                        // Send to client
                        data.ClientObject.Queue(new MediusCreateChannelResponse()
                        {
                            MessageID = createChannelRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                            MediusWorldID = channel.Id
                        });
                        break;
                    }

                case MediusJoinChannelRequest joinChannelRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {joinChannelRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (data.ClientObject.ClientAccount == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {joinChannelRequest} without a being logged in.");

                        var channel = Program.GetChannelById(joinChannelRequest.MediusWorldID);
                        if (channel == null)
                        {
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
                            // Tell previous channel player left
                            var prevChannel = Program.Channels.FirstOrDefault(x => x.Id == data.ClientObject.CurrentChannelId);
                            prevChannel?.OnPlayerLeft(data.ClientObject);

                            // 
                            data.ClientObject.CurrentChannelId = channel.Id;

                            // Tell channel
                            channel.OnPlayerJoined(data.ClientObject);

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
                                        AddressList = new NetAddress[MediusConstants.NET_ADDRESS_LIST_COUNT]
                                        {
                                            new NetAddress() { Address = Program.SERVER_IP.ToString(), Port = (uint)Program.LobbyServer.Port, AddressType = NetAddressType.NetAddressTypeExternal},
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
                        if (data.ClientObject.ClientAccount == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {channelInfoRequest} without a being logged in.");

                        // Find channel
                        var channel = Program.GetChannelById(channelInfoRequest.MediusWorldID);

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
                        if (data.ClientObject.ClientAccount == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {channelListRequest} without a being logged in.");

                        List<MediusChannelListResponse> channelResponses = new List<MediusChannelListResponse>();
                        var gameChannels = Program.Channels
                            .Where(x => x.ApplicationId == data.ClientObject.ApplicationId)
                            .Where(x => x.Type == ChannelType.Game)
                            .Skip((channelListRequest.PageID - 1) * channelListRequest.PageSize)
                            .Take(channelListRequest.PageSize);

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

                case MediusChannelList_ExtraInfoRequest channelList_ExtraInfoRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {channelList_ExtraInfoRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (data.ClientObject.ClientAccount == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {channelList_ExtraInfoRequest} without a being logged in.");

                        List<MediusChannelList_ExtraInfoResponse> channelResponses = new List<MediusChannelList_ExtraInfoResponse>();

                        // Deadlocked only uses this to connect to a non-game channel (lobby)
                        // So we'll filter by lobby here
                        var channels = Program.Channels
                            .Where(x => x.ApplicationId == data.ClientObject.ApplicationId)
                            .Where(x => x.Type == ChannelType.Lobby)
                            .Skip((channelList_ExtraInfoRequest.PageID - 1) * channelList_ExtraInfoRequest.PageSize)
                            .Take(channelList_ExtraInfoRequest.PageSize);

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
                        if (data.ClientObject.ClientAccount == null)
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
                        if (data.ClientObject.ClientAccount == null)
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
                        if (data.ClientObject.ClientAccount == null)
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
                        if (data.ClientObject.ClientAccount == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {setAutoChatHistoryRequest} without a being logged in.");

                        data.ClientObject.Queue(new MediusSetAutoChatHistoryResponse()
                        {
                            MessageID = setAutoChatHistoryRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess
                        });
                        break;
                    }

                case MediusGenericChatMessage genericChatMessage:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {genericChatMessage} without a session.");

                        // ERROR -- Need to be logged in
                        if (data.ClientObject.ClientAccount == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {genericChatMessage} without a being logged in.");

                        await ProcessGenericChatMessage(clientChannel, data.ClientObject, genericChatMessage);
                        break;
                    }

                case MediusBinaryMessage binaryMessage:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {binaryMessage} without a session.");

                        // ERROR -- Need to be logged in
                        if (data.ClientObject.ClientAccount == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {binaryMessage} without a being logged in.");

                        switch (binaryMessage.MessageType)
                        {
                            case MediusBinaryMessageType.TargetBinaryMsg:
                                {
                                    var target = Program.GetClientByAccountId(binaryMessage.TargetAccountID);

                                    target?.Queue(new MediusBinaryFwdMessage()
                                    {
                                        MessageType = binaryMessage.MessageType,
                                        OriginatorAccountID = data.ClientObject.ClientAccount.AccountId,
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

                case MediusTextFilterRequest textFilterRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {textFilterRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (data.ClientObject.ClientAccount == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {textFilterRequest} without a being logged in.");

                        data.ClientObject.Queue(new MediusTextFilterResponse()
                        {
                            MessageID = textFilterRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                            Text = textFilterRequest.Text
                        });

                        break;
                    }

                case MediusGetMyIPRequest getMyIpRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getMyIpRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (data.ClientObject.ClientAccount == null)
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
                        if (data.ClientObject.ClientAccount == null)
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
                        Logger.Warn($"{Name} Unhandled Medius Message: {message}");
                        break;
                    }
            }
        }


        private async Task ProcessGenericChatMessage(IChannel clientChannel, ClientObject clientObject, MediusGenericChatMessage chatMessage)
        {
            var channel = clientObject.CurrentChannel;
            var game = clientObject.CurrentGame;
            var allPlayers = channel.Clients.Select(x => x.Client);
            var allButSender = channel.Clients.Where(x => x.Client != clientObject).Select(x => x.Client);
            List<BaseScertMessage> chatResponses = new List<BaseScertMessage>();

            // ERROR -- Need to be logged in
            if (clientObject.ClientAccount == null)
            {
                await DisconnectClient(clientChannel);
                return;
            }

            // Need to be in a channel
            if (channel == null)
                return;

            // 
            string message = chatMessage.Message.Substring(1);
            string[] words = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words == null || words.Length == 0)
                return;

            switch (chatMessage.MessageType)
            {
                case MediusChatMessageType.Broadcast:
                    {
                        // Relay
                        channel.BroadcastChatMessage(allButSender, clientObject.ClientAccount.AccountId, message);

                        // Handle commands
                        switch (words[0].ToLower())
                        {
                            case "!roll":
                                {
                                    channel.BroadcastSystemMessage(
                                        allPlayers,
                                        $"{clientObject?.ClientAccount?.AccountName ?? "ERROR"} rolled {RNG.Next(0, 100)}"
                                    );
                                    break;
                                }
                            case "!gm":
                                {
                                    if (game != null)
                                    {
                                        // Get arg1 if it exists
                                        string arg1 = words.Length > 1 ? words[1].ToLower() : null;

                                        // 
                                        var gamemode = Program.Settings.Gamemodes.FirstOrDefault(x => x.IsValid(game.ApplicationId) && x.Keys != null && x.Keys.Contains(arg1));

                                        if (arg1 == null)
                                        {
                                            channel.SendSystemMessage(clientObject, $"Gamemode is {game.CustomGamemode?.FullName ?? "default"}");
                                        }
                                        else if (game.Host == clientObject && arg1 == "reset" || arg1 == "r")
                                        {
                                            channel.BroadcastSystemMessage(allPlayers, "Gamemode set to default.");
                                            game.CustomGamemode = null;
                                        }
                                        else if (game.Host == clientObject && gamemode != null)
                                        {
                                            channel.BroadcastSystemMessage(allPlayers, $"Gamemode set to {gamemode.FullName}.");
                                            game.CustomGamemode = gamemode;
                                        }
                                    }
                                    break;
                                }
                        }
                        break;
                    }
                default:
                    {
                        Logger.Warn($"Unhandled generic chat message type:{chatMessage.MessageType} {chatMessage}");
                        break;
                    }
            }
        }

    }
}
