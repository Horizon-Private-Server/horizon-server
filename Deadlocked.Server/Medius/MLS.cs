using Deadlocked.Server.Accounts;
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
                        await ProcessMediusMessage(clientAppToServer.Message, clientChannel, clientObject);
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

        protected virtual async Task ProcessMediusMessage(BaseMediusMessage message, IChannel clientChannel, ClientObject clientObject)
        {
            if (message == null)
                return;

            switch (message)
            {
                #region Session

                case MediusSessionEndRequest sessionEndRequest:
                    {
                        // 
                        MediusCallbackStatus result = MediusCallbackStatus.MediusEndSessionFailed;


                        // Check token
                        if (sessionEndRequest.SessionKey == clientObject.SessionKey)
                        {
                            // 
                            result = MediusCallbackStatus.MediusSuccess;
                        }

                        Queue(new MediusSessionEndResponse()
                        {
                            MessageID = sessionEndRequest.MessageID,
                            StatusCode = result
                        }, clientObject);
                        break;
                    }

                #endregion

                #region Login / Logout

                case MediusAccountLogoutRequest accountLogoutRequest:
                    {
                        bool success = false;

                        // ERROR -- Need to be logged in
                        if (clientObject.ClientAccount == null)
                        {
                            await DisconnectClient(clientChannel);
                            break;
                        }

                        // Check token
                        if (accountLogoutRequest.SessionKey == clientObject.SessionKey)
                        {
                            success = true;

                            // 
                            Console.WriteLine($"{clientObject.ClientAccount.AccountName} has logged out.");

                            // Logout
                            clientObject.Logout();
                        }

                        Queue(new MediusAccountLogoutResponse()
                        {
                            MessageID = accountLogoutRequest.MessageID,
                            StatusCode = success ? MediusCallbackStatus.MediusSuccess : MediusCallbackStatus.MediusAccountNotFound
                        }, clientObject);
                        break;
                    }

                #endregion

                #region Announcements / Policy

                case MediusGetAllAnnouncementsRequest getAllAnnouncementsRequest:
                    {
                        Queue(new MediusGetAnnouncementsResponse()
                        {
                            MessageID = getAllAnnouncementsRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                            Announcement = Program.Settings.Announcement,
                            AnnouncementID = 0,
                            EndOfList = true
                        }, clientObject);
                        break;
                    }

                case MediusGetPolicyRequest getPolicyRequest:
                    {
                        string policyText = getPolicyRequest.Policy == MediusPolicyType.Privacy ? Program.Settings.PrivacyPolicy : Program.Settings.UsagePolicy;

                        if (!string.IsNullOrEmpty(policyText))
                            Queue(MediusGetPolicyResponse.FromText(getPolicyRequest.MessageID, policyText), clientObject);
                        else
                            Queue(new MediusGetPolicyResponse() { MessageID = getPolicyRequest.MessageID, StatusCode = MediusCallbackStatus.MediusSuccess, Policy = "", EndOfText = true });
                        break;
                    }

                #endregion

                #region Account

                case MediusAccountGetIDRequest accountGetIdRequest:
                    {
                        if (Program.Database.TryGetAccountByName(accountGetIdRequest.AccountName, out var account))
                        {
                            Queue(new MediusAccountGetIDResponse()
                            {
                                MessageID = accountGetIdRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                AccountID = account.AccountId
                            }, clientObject);
                        }
                        else
                        {
                            Queue(new MediusAccountGetIDResponse()
                            {
                                MessageID = accountGetIdRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusAccountNotFound
                            }, clientObject);
                        }

                        break;
                    }

                case MediusAccountUpdateStatsRequest accountUpdateStatsRequest:
                    {
                        // ERROR -- Need to be logged in
                        if (clientObject.ClientAccount == null)
                        {
                            await DisconnectClient(clientChannel);
                            break;
                        }

                        // Update stats
                        if (clientObject.ClientAccount != null)
                        {
                            clientObject.ClientAccount.Stats = accountUpdateStatsRequest.Stats;
                            Program.Database.Save();
                        }

                        Queue(new MediusAccountUpdateStatsResponse()
                        {
                            MessageID = accountUpdateStatsRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess
                        }, clientObject);
                        break;
                    }

                #endregion

                #region Buddy List

                case MediusAddToBuddyListRequest addToBuddyListRequest:
                    {
                        var statusCode = MediusCallbackStatus.MediusFail;

                        // ERROR -- Need to be logged in
                        if (clientObject.ClientAccount == null)
                        {
                            await DisconnectClient(clientChannel);
                            break;
                        }

                        // find target player
                        if (Program.Database.TryGetAccountById(addToBuddyListRequest.AccountID, out var targetAccount))
                        {
                            if (clientObject.ClientAccount.Friends.Contains(addToBuddyListRequest.AccountID))
                            {

                                statusCode = MediusCallbackStatus.MediusAccountAlreadyExists;
                            }
                            else
                            {
                                // targetAccount

                                // Temporarily auto add player as friend
                                // This should be replaced with a confirm/request system later
                                clientObject.ClientAccount.Friends.Add(addToBuddyListRequest.AccountID);
                                Program.Database.Save();

                                statusCode = MediusCallbackStatus.MediusSuccess;
                            }
                        }

                        Queue(new MediusAddToBuddyListResponse()
                        {
                            MessageID = addToBuddyListRequest.MessageID,
                            StatusCode = statusCode
                        }, clientObject);
                        break;
                    }

                case MediusRemoveFromBuddyListRequest removeFromBuddyListRequest:
                    {
                        // 
                        var statusCode = MediusCallbackStatus.MediusFail;

                        // ERROR -- Need to be logged in
                        if (clientObject.ClientAccount == null)
                        {
                            await DisconnectClient(clientChannel);
                            break;
                        }

                        // 
                        if (clientObject != null && clientObject.ClientAccount != null)
                        {
                            // find target friend
                            if (clientObject.ClientAccount.Friends.Contains(removeFromBuddyListRequest.AccountID))
                            {
                                // remove
                                clientObject.ClientAccount.Friends.Remove(removeFromBuddyListRequest.AccountID);
                                Program.Database.Save();

                                statusCode = MediusCallbackStatus.MediusSuccess;
                            }
                        }

                        Queue(new MediusRemoveFromBuddyListResponse()
                        {
                            MessageID = removeFromBuddyListRequest.MessageID,
                            StatusCode = statusCode
                        }, clientObject);
                        break;
                    }

                case MediusGetBuddyList_ExtraInfoRequest getBuddyList_ExtraInfoRequest:
                    {
                        // ERROR -- Need to be logged in
                        if (clientObject.ClientAccount == null)
                        {
                            await DisconnectClient(clientChannel);
                            break;
                        }

                        // Get friends
                        var friends = clientObject.ClientAccount.Friends?.Select(x => Program.Database.GetAccountById(x)).Where(x => x != null);

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
                            Queue(friendListResponses, clientObject);
                        }
                        else
                        {
                            // No friends
                            Queue(new MediusGetBuddyList_ExtraInfoResponse()
                            {
                                MessageID = getBuddyList_ExtraInfoRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNoResult,
                                EndOfList = true
                            }, clientObject);
                        }
                        break;
                    }

                #endregion

                #region Ignore List

                case MediusGetIgnoreListRequest getIgnoreListRequest:
                    {
                        // Get ignored players
                        var ignored = clientObject.ClientAccount.Ignored.Select(x => Program.Database.GetAccountById(x)).Where(x => x != null);

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
                            Queue(ignoredListResponses, clientObject);
                        }
                        else
                        {
                            // No ignored
                            Queue(new MediusGetIgnoreListResponse()
                            {
                                MessageID = getIgnoreListRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNoResult,
                                EndOfList = true
                            }, clientObject);
                        }
                        break;
                    }

                case MediusAddToIgnoreListRequest addToIgnoreList:
                    {
                        var statusCode = MediusCallbackStatus.MediusFail;

                        // ERROR -- Need to be logged in
                        if (clientObject.ClientAccount == null)
                        {
                            await DisconnectClient(clientChannel);
                            break;
                        }

                        // find target player
                        if (Program.Database.TryGetAccountById(addToIgnoreList.IgnoreAccountID, out var targetAccount))
                        {
                            // Ensure they're not already ignored
                            if (clientObject.ClientAccount.Ignored.Contains(addToIgnoreList.IgnoreAccountID))
                            {
                                statusCode = MediusCallbackStatus.MediusAccountAlreadyExists;
                            }
                            else
                            {
                                // Remove from friends if a friend
                                clientObject.ClientAccount.Friends.Remove(addToIgnoreList.IgnoreAccountID);
                                clientObject.ClientAccount.Ignored.Add(addToIgnoreList.IgnoreAccountID);
                                Program.Database.Save();

                                statusCode = MediusCallbackStatus.MediusSuccess;
                            }
                        }

                        Queue(new MediusAddToIgnoreListResponse()
                        {
                            MessageID = addToIgnoreList.MessageID,
                            StatusCode = statusCode
                        }, clientObject);
                        break;
                    }

                case MediusRemoveFromIgnoreListRequest removeFromIgnoreListRequest:
                    {
                        var statusCode = MediusCallbackStatus.MediusFail;

                        // ERROR -- Need to be logged in
                        if (clientObject.ClientAccount == null)
                        {
                            await DisconnectClient(clientChannel);
                            break;
                        }

                        // 
                        if (clientObject != null && clientObject.ClientAccount != null)
                        {
                            // find target ignored
                            if (clientObject.ClientAccount.Ignored.Contains(removeFromIgnoreListRequest.IgnoreAccountID))
                            {
                                // remove
                                clientObject.ClientAccount.Ignored.Remove(removeFromIgnoreListRequest.IgnoreAccountID);
                                Program.Database.Save();

                                statusCode = MediusCallbackStatus.MediusSuccess;
                            }
                        }

                        Queue(new MediusRemoveFromIgnoreListResponse()
                        {
                            MessageID = removeFromIgnoreListRequest.MessageID,
                            StatusCode = statusCode
                        }, clientObject);
                        break;
                    }

                #endregion

                #region Ladder Stats

                case MediusUpdateLadderStatsWideRequest updateLadderStatsWideRequest:
                    {
                        // ERROR -- Need to be logged in
                        if (clientObject.ClientAccount == null)
                        {
                            await DisconnectClient(clientChannel);
                            break;
                        }

                        switch (updateLadderStatsWideRequest.LadderType)
                        {
                            case MediusLadderType.MediusLadderTypePlayer:
                                {
                                    clientObject.ClientAccount.AccountWideStats = updateLadderStatsWideRequest.Stats;
                                    Program.Database.Save();

                                    Queue(new MediusUpdateLadderStatsWideResponse()
                                    {
                                        MessageID = updateLadderStatsWideRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess
                                    }, clientObject);
                                    break;
                                }
                            case MediusLadderType.MediusLadderTypeClan:
                                {
                                    Queue(new MediusUpdateLadderStatsWideResponse()
                                    {
                                        MessageID = updateLadderStatsWideRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess
                                    }, clientObject);
                                    break;
                                }
                        }

                        break;
                    }

                case MediusGetLadderStatsWideRequest getLadderStatsWideRequest:
                    {
                        int[] stats = null;

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

                        Queue(response, clientObject);
                        break;
                    }

                case MediusLadderList_ExtraInfoRequest ladderList_ExtraInfoRequest:
                    {
                        var account = clientObject.ClientAccount;

                        // ERROR -- Need to be logged in
                        if (account == null)
                        {
                            await DisconnectClient(clientChannel);
                            break;
                        }

                        Queue(new MediusLadderList_ExtraInfoResponse()
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
                        }, clientObject);
                        break;
                    }

                case MediusGetTotalRankingsRequest getTotalRankingsRequest:
                    {
                        // ERROR -- Need to be logged in
                        if (clientObject.ClientAccount == null)
                        {
                            await DisconnectClient(clientChannel);
                            break;
                        }


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
                        Queue(new MediusGetTotalRankingsResponse()
                        {
                            MessageID = getTotalRankingsRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                            TotalRankings = 0
                        }, clientObject);
                        break;
                    }

                case MediusLadderPosition_ExtraInfoRequest ladderPosition_ExtraInfoRequest:
                    {
                        Queue(new MediusLadderPosition_ExtraInfoResponse()
                        {
                            MessageID = ladderPosition_ExtraInfoRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess
                        }, clientObject);
                        break;
                    }

                #endregion

                #region Player Info

                case MediusFindPlayerRequest findPlayerRequest:
                    {
                        Account account = null;

                        if (findPlayerRequest.SearchType == MediusPlayerSearchType.PlayerAccountID && !Program.Database.TryGetAccountById(findPlayerRequest.ID, out account))
                            account = null;
                        else if (findPlayerRequest.SearchType == MediusPlayerSearchType.PlayerAccountName && !Program.Database.TryGetAccountByName(findPlayerRequest.Name, out account))
                            account = null;

                        if (account == null)
                        {
                            Queue(new MediusFindPlayerResponse()
                            {
                                MessageID = findPlayerRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusAccountNotFound,
                                AccountID = findPlayerRequest.ID,
                                AccountName = findPlayerRequest.Name,
                                EndOfList = true
                            }, clientObject);
                        }
                        else
                        {
                            if (account.IsLoggedIn)
                            {
                                Queue(new MediusFindPlayerResponse()
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
                                }, clientObject);
                            }
                            else
                            {
                                Queue(new MediusFindPlayerResponse()
                                {
                                    MessageID = findPlayerRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult,
                                    EndOfList = true
                                }, clientObject);
                            }
                        }
                        break;
                    }

                case MediusPlayerInfoRequest playerInfoRequest:
                    {
                        if (Program.Database.TryGetAccountById(playerInfoRequest.AccountID, out var account))
                        {
                            Queue(new MediusPlayerInfoResponse()
                            {
                                MessageID = playerInfoRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                AccountName = account.AccountName,
                                ApplicationID = Program.Settings.ApplicationId,
                                PlayerStatus = account.IsLoggedIn ? account.Client.Status : MediusPlayerStatus.MediusPlayerDisconnected,
                                ConnectionClass = MediusConnectionType.Ethernet,
                                Stats = account.Stats
                            }, clientObject);
                        }
                        else
                        {
                            Queue(new MediusPlayerInfoResponse()
                            {
                                MessageID = playerInfoRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusAccountNotFound
                            }, clientObject);
                        }
                        break;
                    }


                case MediusUpdateUserState updateUserState:
                    {
                        // ERROR -- Need to be logged in
                        if (clientObject.ClientAccount == null)
                        {
                            await DisconnectClient(clientChannel);
                            break;
                        }

                        clientObject.Action = updateUserState.UserAction;

                        switch (updateUserState.UserAction)
                        {
                            case MediusUserAction.JoinedChatWorld:
                            case MediusUserAction.LeftGameWorld:
                                {
                                    clientObject.Status = MediusPlayerStatus.MediusPlayerInChatWorld;
                                    break;
                                }
                        }
                        break;
                    }

                #endregion

                #region Clan

                case MediusCreateClanRequest createClanRequest:
                    {
                        // Disable for now
                        Queue(new MediusCreateClanResponse()
                        {
                            MessageID = createClanRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess
                        }, clientObject);

                        break;
                    }

                case MediusCheckMyClanInvitationsRequest checkMyClanInvitationsRequest:
                    {
                        // Disabled for now
                        Queue(new MediusCheckMyClanInvitationsResponse()
                        {
                            MessageID = checkMyClanInvitationsRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusNoResult
                        }, clientObject);

                        break;
                    }

                case MediusGetMyClansRequest getMyClansRequest:
                    {
                        // ERROR -- Need to be logged in
                        if (clientObject.ClientAccount == null)
                        {
                            await DisconnectClient(clientChannel);
                            break;
                        }

                        Queue(new MediusGetMyClansResponse()
                        {
                            MessageID = getMyClansRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusClanNotFound,
                            ClanID = -1,
                            ApplicationID = Program.Settings.ApplicationId,
                            ClanName = null,
                            LeaderAccountID = clientObject.ClientAccount.AccountId,
                            LeaderAccountName = clientObject.ClientAccount.AccountName,
                            Stats = "000000000000000000000000",
                            Status = MediusClanStatus.ClanDisbanded,
                            EndOfList = true
                        }, clientObject);
                        break;
                    }

                case MediusGetClanMemberList_ExtraInfoRequest getClanMemberList_ExtraInfoRequest:
                    {
                        Queue(new MediusGetClanMemberList_ExtraInfoResponse()
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
                        }, clientObject);

                        /*
                        responses.Add(new RT_MSG_SERVER_APP()
                        {
                            Queue(new MediusGetClanMemberList_ExtraInfoResponse()
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
                        Queue(new MediusGetClanInvitationsSentResponse()
                        {
                            MessageID = getClanInvitiationsSentRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusNoResult,
                            EndOfList = true
                        }, clientObject);
                        break;
                    }

                case MediusGetClanByIDRequest getClanByIdRequest:
                    {
                        Queue(new MediusGetClanByIDResponse()
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
                        }, clientObject);
                        break;
                    }

                #endregion

                #region Game

                case MediusGetGameListFilterRequest getGameListFilterRequest:
                    {
                        var filters = clientObject.GameListFilters;

                        if (filters == null || filters.Count == 0)
                        {
                            Queue(new MediusGetGameListFilterResponse()
                            {
                                MessageID = getGameListFilterRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNoResult,
                                EndOfList = true
                            }, clientObject);
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
                            Queue(filterResponses, clientObject);
                        }
                        break;
                    }

                case MediusGameList_ExtraInfoRequest gameList_ExtraInfoRequest:
                    {
                        var gameList = Program.Games
                            .Where(x => x.ApplicationId == clientObject.ApplicationId)
                            .Where(x => x.WorldStatus == MediusWorldStatus.WorldActive || x.WorldStatus == MediusWorldStatus.WorldStaging)
                            .Where(x => clientObject.IsGameMatch(x))
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
                            Queue(gameList, clientObject);
                        }
                        else
                        {
                            Queue(new MediusGameList_ExtraInfoResponse()
                            {
                                MessageID = gameList_ExtraInfoRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNoResult,
                                EndOfList = true
                            }, clientObject);
                        }


                        break;
                    }

                case MediusGameInfoRequest gameInfoRequest:
                    {
                        var game = Program.GetGameById(gameInfoRequest.MediusWorldID);

                        if (game == null)
                        {
                            Queue(new MediusGameInfoResponse()
                            {
                                MessageID = gameInfoRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusGameNotFound
                            }, clientObject);
                        }
                        else
                        {
                            Queue(new MediusGameInfoResponse()
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
                            }, clientObject);
                        }

                        break;
                    }

                case MediusGameWorldPlayerListRequest gameWorldPlayerListRequest:
                    {
                        // ERROR -- Need to be logged in
                        if (clientObject.ClientAccount == null)
                        {
                            await DisconnectClient(clientChannel);
                            break;
                        }

                        var game = Program.GetGameById(gameWorldPlayerListRequest.MediusWorldID);
                        if (game == null)
                        {
                            Queue(new MediusGameWorldPlayerListResponse()
                            {
                                MessageID = gameWorldPlayerListRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusGameNotFound
                            }, clientObject);
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

                            Queue(playerList, clientObject);
                        }

                        break;
                    }

                case MediusSetGameListFilterRequest setGameListFilterRequest:
                    {
                        // Set filter
                        var filter = clientObject.SetGameListFilter(setGameListFilterRequest);

                        // Give reply
                        Queue(new MediusSetGameListFilterResponse()
                        {
                            MessageID = setGameListFilterRequest.MessageID,
                            StatusCode = filter == null ? MediusCallbackStatus.MediusFail : MediusCallbackStatus.MediusSuccess,
                            FilterID = filter?.FieldID ?? 0
                        }, clientObject);

                        break;
                    }

                case MediusClearGameListFilterRequest clearGameListFilterRequest:
                    {
                        // Remove
                        clientObject.ClearGameListFilter(clearGameListFilterRequest.FilterID);

                        // 
                        Queue(new MediusClearGameListFilterResponse()
                        {
                            MessageID = clearGameListFilterRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess
                        }, clientObject);

                        break;
                    }


                case MediusCreateGameRequest createGameRequest:
                    {
                        // ERROR -- Need to be logged in
                        if (clientObject.ClientAccount == null)
                        {
                            await DisconnectClient(clientChannel);
                            break;
                        }

                        Program.ProxyServer.CreateGame(clientObject, createGameRequest);
                        break;
                    }

                case MediusJoinGameRequest joinGameRequest:
                    {
                        // ERROR -- Need to be logged in
                        if (clientObject.ClientAccount == null)
                        {
                            await DisconnectClient(clientChannel);
                            break;
                        }

                        Program.ProxyServer.JoinGame(clientObject, joinGameRequest);
                        break;
                    }

                case MediusWorldReport worldReport:
                    {
                        // ERROR -- Need to be logged in
                        if (clientObject.ClientAccount == null)
                        {
                            await DisconnectClient(clientChannel);
                            break;
                        }

                        clientObject.CurrentGame?.OnWorldReport(worldReport);

                        break;
                    }

                case MediusPlayerReport playerReport:
                    {
                        // ERROR -- Need to be logged in
                        if (clientObject.ClientAccount == null)
                        {
                            await DisconnectClient(clientChannel);
                            break;
                        }

                        // 
                        if (clientObject.CurrentGameId == playerReport.MediusWorldID &&
                            clientObject.SessionKey == playerReport.SessionKey)
                        {
                            clientObject.CurrentGame?.OnPlayerReport(playerReport);
                        }
                        break;
                    }

                case MediusEndGameReport endGameReport:
                    {
                        // ERROR -- Need to be logged in
                        if (clientObject.ClientAccount == null)
                        {
                            await DisconnectClient(clientChannel);
                            break;
                        }

                        clientObject.CurrentGame?.OnEndGameReport(endGameReport);

                        break;
                    }

                #endregion

                #region Channel

                case MediusSetLobbyWorldFilterRequest setLobbyWorldFilterRequest:
                    {
                        Queue(new MediusSetLobbyWorldFilterResponse()
                        {
                            MessageID = setLobbyWorldFilterRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                            FilterMask1 = setLobbyWorldFilterRequest.FilterMask1,
                            FilterMask2 = setLobbyWorldFilterRequest.FilterMask2,
                            FilterMask3 = setLobbyWorldFilterRequest.FilterMask3,
                            FilterMask4 = setLobbyWorldFilterRequest.FilterMask4,
                            FilterMaskLevel = setLobbyWorldFilterRequest.FilterMaskLevel,
                            LobbyFilterType = setLobbyWorldFilterRequest.LobbyFilterType
                        }, clientObject);


                        break;
                    }

                case MediusCreateChannelRequest createChannelRequest:
                    {
                        // ERROR -- Need to be logged in
                        if (clientObject.ClientAccount == null)
                        {
                            await DisconnectClient(clientChannel);
                            break;
                        }

                        // Create channel
                        Channel channel = new Channel(createChannelRequest);

                        // Add
                        Program.Channels.Add(channel);

                        // Send to client
                        Queue(new MediusCreateChannelResponse()
                        {
                            MessageID = createChannelRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                            MediusWorldID = channel.Id
                        }, clientObject);
                        break;
                    }

                case MediusJoinChannelRequest joinChannelRequest:
                    {
                        // ERROR -- Need to be logged in
                        if (clientObject.ClientAccount == null)
                        {
                            await DisconnectClient(clientChannel);
                            break;
                        }

                        var channel = Program.GetChannelById(joinChannelRequest.MediusWorldID);
                        if (channel == null)
                        {
                            Queue(new MediusJoinChannelResponse()
                            {
                                MessageID = joinChannelRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusChannelNotFound
                            }, clientObject);
                        }
                        else if (channel.SecurityLevel == MediusWorldSecurityLevelType.WORLD_SECURITY_PLAYER_PASSWORD && joinChannelRequest.LobbyChannelPassword != channel.Password)
                        {
                            Queue(new MediusJoinChannelResponse()
                            {
                                MessageID = joinChannelRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusInvalidPassword
                            }, clientObject);
                        }
                        else
                        {
                            // Tell previous channel player left
                            var prevChannel = Program.Channels.FirstOrDefault(x => x.Id == clientObject.CurrentChannelId);
                            prevChannel?.OnPlayerLeft(clientObject);

                            // 
                            clientObject.CurrentChannelId = channel.Id;

                            // Tell channel
                            channel.OnPlayerJoined(clientObject);

                            Queue(new MediusJoinChannelResponse()
                            {
                                MessageID = joinChannelRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                ConnectInfo = new NetConnectionInfo()
                                {
                                    AccessKey = clientObject.Token,
                                    SessionKey = clientObject.SessionKey,
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
                            }, clientObject);
                        }
                        break;
                    }

                case MediusChannelInfoRequest channelInfoRequest:
                    {
                        // Find channel
                        var channel = Program.GetChannelById(channelInfoRequest.MediusWorldID);

                        if (channel == null)
                        {
                            // No channels
                            Queue(new MediusChannelInfoResponse()
                            {
                                MessageID = channelInfoRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess
                            }, clientObject);
                        }
                        else
                        {
                            Queue(new MediusChannelInfoResponse()
                            {
                                MessageID = channelInfoRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                LobbyName = channel.Name,
                                ActivePlayerCount = channel.PlayerCount,
                                MaxPlayers = channel.MaxPlayers
                            }, clientObject);
                        }
                        break;
                    }

                case MediusChannelListRequest channelListRequest:
                    {
                        List<MediusChannelListResponse> channelResponses = new List<MediusChannelListResponse>();


                        var gameChannels = Program.Channels
                            .Where(x => x.ApplicationId == clientObject.ApplicationId)
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
                            Queue(new MediusChannelListResponse()
                            {
                                MessageID = channelListRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNoResult,
                                EndOfList = true
                            }, clientObject);
                        }
                        else
                        {
                            // Ensure the end of list flag is set
                            channelResponses[channelResponses.Count - 1].EndOfList = true;

                            // Add to responses
                            Queue(channelResponses, clientObject);
                        }


                        break;
                    }

                case MediusChannelList_ExtraInfoRequest channelList_ExtraInfoRequest:
                    {
                        List<MediusChannelList_ExtraInfoResponse> channelResponses = new List<MediusChannelList_ExtraInfoResponse>();


                        // Deadlocked only uses this to connect to a non-game channel (lobby)
                        // So we'll filter by lobby here
                        var channels = Program.Channels
                            .Where(x => x.ApplicationId == clientObject.ApplicationId)
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
                            Queue(new MediusChannelList_ExtraInfoResponse()
                            {
                                MessageID = channelList_ExtraInfoRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNoResult,
                                EndOfList = true
                            }, clientObject);
                        }
                        else
                        {
                            // Ensure the end of list flag is set
                            channelResponses[channelResponses.Count - 1].EndOfList = true;

                            // Add to responses
                            Queue(channelResponses, clientObject);
                        }


                        break;
                    }

                #endregion

                #region File

                case MediusFileCreateRequest fileCreateRequest:
                    {
                        Queue(new MediusFileCreateResponse()
                        {
                            MessageID = fileCreateRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusDBError,
                            MediusFileInfo = new MediusFile()
                            {

                            }
                        }, clientObject);

                        break;
                    }

                case MediusFileDownloadRequest fileDownloadRequest:
                    {
                        Queue(new MediusFileDownloadResponse()
                        {
                            MessageID = fileDownloadRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusDBError
                        }, clientObject);
                        break;
                    }

                #endregion

                #region Chat / Binary Message

                case MediusGenericChatSetFilterRequest genericChatSetFilterRequest:
                    {
                        Queue(new MediusGenericChatSetFilterResponse()
                        {
                            MessageID = genericChatSetFilterRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                            ChatFilter = new MediusGenericChatFilter()
                            {
                                GenericChatFilterBitfield = genericChatSetFilterRequest.GenericChatFilter.GenericChatFilterBitfield
                            }
                        }, clientObject);
                        break;
                    }

                case MediusSetAutoChatHistoryRequest setAutoChatHistoryRequest:
                    {
                        Queue(new MediusSetAutoChatHistoryResponse()
                        {
                            MessageID = setAutoChatHistoryRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess
                        }, clientObject);
                        break;
                    }

                case MediusGenericChatMessage genericChatMessage:
                    {
                        await ProcessGenericChatMessage(clientChannel, clientObject, genericChatMessage);
                        break;
                    }

                case MediusBinaryMessage binaryMessage:
                    {
                        // ERROR -- Need to be logged in
                        if (clientObject.ClientAccount == null)
                        {
                            await DisconnectClient(clientChannel);
                            break;
                        }

                        switch (binaryMessage.MessageType)
                        {
                            case MediusBinaryMessageType.TargetBinaryMsg:
                                {
                                    var target = Program.GetClientByAccountId(binaryMessage.TargetAccountID);

                                    Queue(new MediusBinaryFwdMessage()
                                    {
                                        MessageType = binaryMessage.MessageType,
                                        OriginatorAccountID = clientObject.ClientAccount.AccountId,
                                        Message = binaryMessage.Message
                                    }, target);
                                    break;
                                }
                            case MediusBinaryMessageType.BroadcastBinaryMsg:
                                {
                                    clientObject.CurrentChannel?.BroadcastBinaryMessage(clientObject, binaryMessage);
                                    break;
                                }
                            default:
                                {
                                    Console.WriteLine($"Unhandled binary message type {binaryMessage.MessageType}");
                                    break;
                                }
                        }
                        break;
                    }

                #endregion

                #region Misc

                case MediusTextFilterRequest textFilterRequest:
                    {
                        Queue(new MediusTextFilterResponse()
                        {
                            MessageID = textFilterRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                            Text = textFilterRequest.Text
                        }, clientObject);

                        break;
                    }

                case MediusGetMyIPRequest getMyIpRequest:
                    {
                        Queue(new MediusGetMyIPResponse()
                        {
                            MessageID = getMyIpRequest.MessageID,
                            IP = (clientChannel.RemoteAddress as IPEndPoint)?.Address,
                            StatusCode = MediusCallbackStatus.MediusSuccess
                        }, clientObject);
                        break;
                    }

                case MediusGetServerTimeRequest getServerTimeRequest:
                    {
                        Queue(new MediusGetServerTimeResponse()
                        {
                            MessageID = getServerTimeRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                            Local_server_timezone = MediusTimeZone.MediusTimeZone_GMT
                        }, clientObject);
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
                                    if (game != null && game.Host == clientObject)
                                    {
                                        // Get arg1 if it exists
                                        string arg1 = words.Length > 1 ? words[1].ToLower() : null;

                                        // 
                                        var gamemode = Program.Settings.Gamemodes.FirstOrDefault(x => x.IsValid(game.ApplicationId) && x.Keys != null && x.Keys.Contains(arg1));

                                        if (arg1 == null)
                                        {
                                            channel.SendSystemMessage(clientObject, $"Gamemode is {game.CustomGamemode?.FullName ?? "default"}");
                                        }
                                        else if (arg1 == "reset" || arg1 == "r")
                                        {
                                            channel.BroadcastSystemMessage(allPlayers, "Gamemode set to default.");
                                            game.CustomGamemode = null;
                                        }
                                        else if (gamemode != null)
                                        {
                                            channel.BroadcastSystemMessage(allPlayers, $"Gamemode set to {gamemode.FullName}.");
                                            game.CustomGamemode = gamemode;
                                        }
                                    }
                                    break;
                                }
                            default:
                                {
                                    channel.BroadcastChatMessage(allButSender, clientObject.ClientAccount.AccountId, message);
                                    break;
                                }
                        }
                        break;
                    }
                default:
                    {
                        Console.WriteLine($"Unhandled generic chat message type:{chatMessage.MessageType} {chatMessage}");
                        break;
                    }
            }
        }
    }
}
