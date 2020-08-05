using Deadlocked.Server.Accounts;
using Deadlocked.Server.Messages;
using Deadlocked.Server.Messages.Lobby;
using Deadlocked.Server.Messages.RTIME;
using Medius.Crypto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Deadlocked.Server.Medius
{
    public class MLS : BaseMediusComponent
    {
        public override int Port => 10078;

        static string[] INIT_1D_MESSAGES =
        {
            "05CDFA00000000080000010000BC9E77B0DC8DDCB08E41C4F637BDDEC9",
            "05CEFA000078100F00000100001FB479EDE006BE04AA78E639C51FD6F0",
            "05CFFA000000000C0000010000B5CEA072FCA3BF08C0A05EFA2106A1BE",
            "05D0FA0000000008000001000093E29AF5051FE14BA91ED2FF17E3C4C4",
            "05D1FA00000000080000010000BBCBDF273A184FE4BA735F867C25B7B2",
            "05D2FA000000000800000100000DA0152F16F5184C215A9B5C40299B5F",
            "05D3FA000000000C0000010000B17C90678DD09703C4AF77CDD5456424",
            "05D4FA000000000C00000100000DFD1196CE9E1219BFEAB4ED042CCEEE",
            "05D5FA000000000C0000010000898375D0A30EB86BC4A3A26E80CF675A",
            "05D6FA000000000C0000010000BFAC60651325AEC1A077262A6AB8312B",
            "05D7FA000000000C0000010000388DED87E97AF868F663C2B68F7028A5",
            "05D8FA000000000C0000010000EB12D96C966BB1C50C3D3881138B1306",
            "05D9FA000000000C000001000077183CF0128767D38DF9E6DE76B424CE",
            "05DAFA000000000C0000010000636220336C2301E4992F8D98C36A2A06",
            "05DBFA00000000080000010000A83C2C4AAFC257FF70C03C61F3C71C0F",
            "05DCFA00000001080000010000170FB1AF29A2A732AFEEA35A5E187E59",
            "05DDFA00008000080000010000D8360E93C93E708830F92BDF46D3AFC2"
        };

        public MLS()
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
            var targetMsgs = client.Client?.PullLobbyMessages();
            if (targetMsgs != null && targetMsgs.Count > 0)
                responses.AddRange(targetMsgs);

            // 
            if (shouldEcho)
                Echo(client, ref responses);

            responses.Send(client);
        }

        protected override int HandleCommand(BaseMessage message, ClientSocket client, ref List<BaseMessage> responses)
        {
            // 
            if (message.Id != RT_MSG_TYPE.RT_MSG_CLIENT_ECHO)
                Console.WriteLine(message.ToString());

            // 
            switch (message.Id)
            {
                case RT_MSG_TYPE.RT_MSG_CLIENT_HELLO:

                    new RT_MSG_SERVER_HELLO().Send(client);

                    break;
                case RT_MSG_TYPE.RT_MSG_CLIENT_CRYPTKEY_PUBLIC:
                    {
                        responses.Add(new RT_MSG_SERVER_CRYPTKEY_PEER() { Key = Utils.FromString(Program.KEY) });
                        break;
                    }
                case RT_MSG_TYPE.RT_MSG_CLIENT_CONNECT_TCP:
                    {
                        var m00 = message as RT_MSG_CLIENT_CONNECT_TCP;
                        client.SetToken(m00.AccessToken);

                        Console.WriteLine($"CLIENT CONNECTED TO MLS WITH SESSION KEY {m00.SessionKey} and ACCESS TOKEN {m00.AccessToken}");

                        var clientObject = Program.Clients.FirstOrDefault(x => x.Token == m00.AccessToken && x.SessionKey == m00.SessionKey);
                        if (clientObject == null)
                            responses.Add(new RawMessage(RT_MSG_TYPE.RT_MSG_CLIENT_DISCONNECT_WITH_REASON) { Contents = new byte[1] });
                        else
                            responses.Add(new RT_MSG_SERVER_CONNECT_REQUIRE() { ARG1 = 0x02, ARG2 = 0x48, ARG3 = 0x02 });

                        break;
                    }
                case RT_MSG_TYPE.RT_MSG_CLIENT_CONNECT_READY_REQUIRE:
                    {
                        responses.Add(new RT_MSG_SERVER_CRYPTKEY_GAME() { Key = Utils.FromString(Program.KEY) });
                        responses.Add(new RT_MSG_SERVER_CONNECT_ACCEPT_TCP() { UNK_00 = 0x0019, UNK_02 = 0xACAF, IP = (client.RemoteEndPoint as IPEndPoint).Address });
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
                case RT_MSG_TYPE.RT_MSG_SERVER_CHEAT_QUERY:
                    {
                        int id1D = (message as RT_MSG_SERVER_CHEAT_QUERY).UNK_01;
                        int off1D = id1D - 0xCC;
                        switch (id1D)
                        {
                            case 0xCD:
                                {
                                    //responses.Add(new RawMessage(RT_MSG_TYPE.RT_MSG_SERVER_APP)
                                    //{
                                    //    Contents = Utils.FromString("0132303030303030303000000000000000000000000000000000000000004261646765723431000000000000000000000000000000000000000000000000B02B0000020000000100000000000000A3010000390300004E070000AF050000E3020000610400000000000000010000B454040008D800005AF4000008000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")
                                    //});
                                    //responses.Add(new RawMessage(RT_MSG_TYPE.RT_MSG_SERVER_APP)
                                    //{
                                    //    Contents = Utils.FromString("01D73100000000000000000000000000000000000000000000000000000063B2090053686F756C642049205472793F00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")
                                    //});

                                    if (off1D > client.ComponentState)
                                        client.ComponentState = off1D;

                                    // 05CEFA000078100F00000100001FB479EDE006BE04AA78E639C51FD6F0
                                    responses.Add(new RT_MSG_SERVER_CHEAT_QUERY()
                                    {
                                        UNK_00 = 0x05,
                                        UNK_01 = 0xCE,
                                        UNK_02 = 0xFA,
                                        UNK_05 = 0x1078,
                                        UNK_07 = 0x000F,
                                        UNK_09 = 0x0100,
                                        UNK_0D = Utils.FromString("1FB479EDE006BE04AA78E639C51FD6F0")
                                    });
                                    break;
                                }
                            case 0xCE:
                                {
                                    if (off1D > client.ComponentState)
                                        client.ComponentState = off1D;

                                    responses.Add(new RawMessage(RT_MSG_TYPE.RT_MSG_SERVER_CHEAT_QUERY)
                                    {
                                        Contents = Utils.FromString(INIT_1D_MESSAGES[client.ComponentState])
                                    });
                                    break;
                                }
                            case 0xCF:
                                {
                                    if (off1D > client.ComponentState)
                                        client.ComponentState = off1D;

                                    responses.Add(new RawMessage(RT_MSG_TYPE.RT_MSG_SERVER_CHEAT_QUERY)
                                    {
                                        Contents = Utils.FromString(INIT_1D_MESSAGES[client.ComponentState])
                                    });
                                    break;
                                }
                        }
                        break;
                    }
                case RT_MSG_TYPE.RT_MSG_CLIENT_APP_TOSERVER:
                    {
                        var appMsg = (message as RT_MSG_CLIENT_APP_TOSERVER).AppMessage;

                        switch (appMsg.Id)
                        {
                            case MediusAppPacketIds.GetLadderStatsWide:
                                {
                                    var getLadderStatsWide = appMsg as MediusGetLadderStatsWideRequest;
                                    responses.Add(new RT_MSG_SERVER_APP()
                                    {
                                        AppMessage = new MediusGetLadderStatsWideResponse()
                                        {
                                            StatusCode = MediusCallbackStatus.MediusSuccess,
                                            AccountID_or_ClanID = getLadderStatsWide.AccountID_or_ClanID
                                        }
                                    });
                                    break;
                                }
                            case MediusAppPacketIds.AccountUpdateStats:
                                {
                                    responses.Add(new RT_MSG_SERVER_APP()
                                    {
                                        AppMessage = new MediusAccountUpdateStatsResponse()
                                        {
                                            Response = 0
                                        }
                                    });
                                    break;
                                }
                            case MediusAppPacketIds.GenericChatSetFilterRequest:
                                {
                                    responses.Add(new RT_MSG_SERVER_APP()
                                    {
                                        AppMessage = new MediusGenericChatSetFilterResponse()
                                        {
                                            StatusCode = MediusCallbackStatus.MediusSuccess,
                                            ChatFilter = new MediusGenericChatFilter()
                                            {
                                                GenericChatFilterBitfield = Utils.FromString("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF")
                                            }
                                        }
                                    });
                                    break;
                                }
                            case MediusAppPacketIds.SetAutoChatHistoryRequest:
                                {
                                    responses.Add(new RT_MSG_SERVER_APP()
                                    {
                                        AppMessage = new MediusSetAutoChatHistoryResponse()
                                        {
                                            StatusCode = MediusCallbackStatus.MediusSuccess
                                        }
                                    });
                                    break;
                                }
                            case MediusAppPacketIds.GetAllAnnouncements:
                                {
                                    responses.Add(new RT_MSG_SERVER_APP()
                                    {
                                        AppMessage = new MediusGetAnnouncementsResponse()
                                        {
                                            StatusCode = MediusCallbackStatus.MediusSuccess,
                                            Announcement = Program.Settings.Announcement,
                                            AnnouncementID = 0,
                                            EndOfList = true
                                        }
                                    });
                                    break;
                                }
                            case MediusAppPacketIds.GetMyClans:
                                {
                                    responses.Add(new RT_MSG_SERVER_APP()
                                    {
                                        AppMessage = new MediusGetMyClansResponse()
                                        {
                                            StatusCode = MediusCallbackStatus.MediusNoResult,
                                            /*
                                            ClanID = 1,
                                            ApplicationID = Program.Settings.ApplicationId,
                                            ClanName = "YEET",
                                            LeaderAccountID = client.Client.ClientAccount.AccountId,
                                            LeaderAccountName = client.Client.ClientAccount.AccountName,
                                            Stats = "0000000000000000000A800C",
                                            Status = MediusClanStatus.ClanActive,
                                            */
                                            EndOfList = true
                                        }
                                    });
                                    break;
                                }
                            case MediusAppPacketIds.FileDownload:
                                {
                                    var fileDownloadReq = appMsg as MediusFileDownloadRequest;

                                    // stats maybe?
                                    responses.Add(new RT_MSG_SERVER_APP()
                                    {
                                        AppMessage = new MediusFileDownloadResponse()
                                        {
                                            MessageID = fileDownloadReq.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusDBError
                                        }
                                    });
                                    break;
                                }
                            case MediusAppPacketIds.GetBuddyList_ExtraInfo:
                                {
                                    var getBuddyListReq = appMsg as MediusGetBuddyList_ExtraInfoRequest;

                                    if (true)
                                    {
                                        // Not implemented
                                        // Just send none
                                        responses.Add(new RT_MSG_SERVER_APP()
                                        {
                                            AppMessage = new MediusGetBuddyList_ExtraInfoResponse()
                                            {
                                                MessageID = getBuddyListReq.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusNoResult,
                                                EndOfList = true
                                            }
                                        });
                                    }
                                    break;
                                }
                            case MediusAppPacketIds.GetClanMemberList_ExtraInfo:
                                {
                                    var getClanMemberListExtra = appMsg as MediusGetClanMemberList_ExtraInfoRequest;

                                    

                                    responses.AddRange(Program.Clients.Select(x => new RT_MSG_SERVER_APP()
                                    {
                                        AppMessage = new MediusGetClanMemberList_ExtraInfoResponse()
                                        {
                                            MessageID = getClanMemberListExtra.MessageID,
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
                                        }
                                    }));

                                    /*
                                    responses.Add(new RT_MSG_SERVER_APP()
                                    {
                                        AppMessage = new MediusGetClanMemberList_ExtraInfoResponse()
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
                            case MediusAppPacketIds.GetClanInvitationsSent:
                                {
                                    var getClanInvSentReq = appMsg as MediusGetClanInvitationsSentRequest;

                                    responses.Add(new RT_MSG_SERVER_APP()
                                    {
                                        AppMessage = new MediusGetClanInvitationsSentResponse()
                                        {
                                            MessageID = getClanInvSentReq.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusNoResult,
                                            EndOfList = true
                                        }
                                    });
                                    break;
                                }
                            case MediusAppPacketIds.GetClanByID:
                                {
                                    var getClanByIdReq = appMsg as MediusGetClanByIDRequest;

                                    responses.Add(new RT_MSG_SERVER_APP()
                                    {
                                        AppMessage = new MediusGetClanByIDResponse()
                                        {
                                            MessageID = getClanByIdReq.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusNoResult,
                                            /*
                                            ApplicationID = Program.Settings.ApplicationId,
                                            ClanName = "DEV",
                                            LeaderAccountID = 1,
                                            LeaderAccountName = "Badger41",
                                            Status = MediusClanStatus.ClanActive
                                            */
                                        }
                                    });
                                    break;
                                }
                            case MediusAppPacketIds.FindPlayer:
                                {
                                    var findPlayerReq = appMsg as MediusFindPlayerRequest;
                                    Account account = null;

                                    if (findPlayerReq.SearchType == MediusPlayerSearchType.PlayerAccountID && !Program.Database.GetAccountById(findPlayerReq.ID, out account))
                                        account = null;
                                    else if (findPlayerReq.SearchType == MediusPlayerSearchType.PlayerAccountName && !Program.Database.GetAccountByName(findPlayerReq.Name, out account))
                                        account = null;

                                    if (account == null)
                                    {
                                        responses.Add(new RT_MSG_SERVER_APP()
                                        {
                                            AppMessage = new MediusFindPlayerResponse()
                                            {
                                                MessageID = findPlayerReq.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusAccountNotFound,
                                                AccountID = findPlayerReq.ID,
                                                AccountName = findPlayerReq.Name,
                                                EndOfList = true
                                            }
                                        });
                                    }
                                    else
                                    {
                                        responses.Add(new RT_MSG_SERVER_APP()
                                        {
                                            AppMessage = new MediusFindPlayerResponse()
                                            {
                                                MessageID = findPlayerReq.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                                ApplicationID = Program.Settings.ApplicationId,
                                                AccountID = account.AccountId,
                                                AccountName = account.AccountName,
                                                ApplicationType = MediusApplicationType.LobbyChatChannel,
                                                ApplicationName = "?????",
                                                MediusWorldID = account.IsLoggedIn ? account.Client.CurrentChannelId : -1,
                                                EndOfList = true
                                            }
                                        });
                                    }
                                    break;
                                }
                            case MediusAppPacketIds.TextFilter:
                                {
                                    var textFilterReq = appMsg as MediusTextFilterRequest;

                                    responses.Add(new RT_MSG_SERVER_APP()
                                    {
                                        AppMessage = new MediusTextFilterResponse()
                                        {
                                            MessageID = textFilterReq.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusSuccess,
                                            Text = textFilterReq.Text
                                        }
                                    });

                                    break;
                                }
                            case MediusAppPacketIds.BinaryMessage:
                                {
                                    var binaryMessage = appMsg as MediusBinaryMessage;

                                    responses.Add(new RT_MSG_SERVER_APP()
                                    {
                                        AppMessage = new MediusBinaryFwdMessage()
                                        {
                                            MessageType = binaryMessage.MessageType,
                                            OriginatorAccountID = client.Client.ClientAccount.AccountId,
                                            Message = binaryMessage.Message
                                        }
                                    });
                                    break;
                                }
                            case MediusAppPacketIds.LadderPosition_ExtraInfo:
                                {
                                    var ladderPosExtraReq = appMsg as MediusLadderPosition_ExtraInfoRequest;

                                    responses.Add(new RT_MSG_SERVER_APP()
                                    {
                                        AppMessage = new MediusLadderPosition_ExtraInfoResponse()
                                        {
                                            MessageID = ladderPosExtraReq.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusSuccess
                                        }
                                    });
                                    break;
                                }
                            case MediusAppPacketIds.PlayerInfo:
                                {
                                    var playerInfoReq = appMsg as MediusPlayerInfoRequest;

                                    if (Program.Database.GetAccountById(playerInfoReq.AccountID, out var account))
                                    {
                                        responses.Add(new RT_MSG_SERVER_APP()
                                        {
                                            AppMessage = new MediusPlayerInfoResponse()
                                            {
                                                MessageID = playerInfoReq.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                                AccountName = account.AccountName,
                                                ApplicationID = Program.Settings.ApplicationId,
                                                PlayerStatus = account.IsLoggedIn ?  account.Client.Status : MediusPlayerStatus.MediusPlayerDisconnected,
                                                ConnectionClass = MediusConnectionType.Ethernet
                                            }
                                        });
                                    }
                                    else
                                    {
                                        responses.Add(new RT_MSG_SERVER_APP()
                                        {
                                            AppMessage = new MediusPlayerInfoResponse()
                                            {
                                                MessageID = playerInfoReq.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusAccountNotFound
                                            }
                                        });
                                    }
                                    break;
                                }
                            case MediusAppPacketIds.Policy:
                                {
                                    var policyReq = appMsg as MediusGetPolicyRequest;
                                    string policyText = policyReq.Policy == MediusPolicyType.Privacy ? Program.Settings.PrivacyPolicy : Program.Settings.UsagePolicy;

                                    responses.AddRange(MediusGetPolicyResponse.FromText(policyText).Select(x => new RT_MSG_SERVER_APP() { AppMessage = x }));
                                    break;
                                }
                            case MediusAppPacketIds.CreateChannel:
                                {
                                    var createChannelReq = appMsg as MediusCreateChannelRequest;

                                    // Create channel
                                    Channel channel = new Channel();

                                    // Add
                                    Program.Channels.Add(channel);

                                    // Send to client
                                    responses.Add(new RT_MSG_SERVER_APP()
                                    {
                                        AppMessage = new MediusCreateChannelResponse()
                                        {
                                            MessageID = createChannelReq.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusSuccess,
                                            MediusWorldID = channel.Id
                                        }
                                    });
                                    break;
                                }
                            case MediusAppPacketIds.JoinChannel:
                                {
                                    var joinChannelReq = appMsg as MediusJoinChannelRequest;

                                    var channel = Program.Channels.FirstOrDefault(x => x.Id == joinChannelReq.MediusWorldID);
                                    if (channel == null)
                                    {
                                        responses.Add(new RT_MSG_SERVER_APP()
                                        {
                                            AppMessage = new MediusJoinChannelResponse()
                                            {
                                                StatusCode = MediusCallbackStatus.MediusChannelNotFound
                                            }
                                        });
                                    }
                                    else
                                    {
                                        client.Client.CurrentChannelId = channel.Id;

                                        responses.Add(new RT_MSG_SERVER_APP()
                                        {
                                            AppMessage = new MediusJoinChannelResponse()
                                            {
                                                MessageID = joinChannelReq.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                                ConnectInfo = new NetConnectionInfo()
                                                {
                                                    AccessKey = client.Client.Token,
                                                    SessionKey = client.Client.SessionKey,
                                                    WorldID = channel.Id,
                                                    ServerKey = new RSA_KEY(Program.GlobalAuthKey.N.ToByteArrayUnsigned().Reverse().ToArray()),
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
                                            }
                                        });
                                    }
                                    break;
                                }
                            case MediusAppPacketIds.PlayerReport:
                                {
                                    var msg = appMsg as MediusPlayerReport;

                                    // 
                                    if (client.Client != null && client.Client.CurrentChannelId == msg.MediusWorldID && client.Client.SessionKey == msg.SessionKey)
                                    {
                                        var channel = Program.Channels.FirstOrDefault(x => x.Id == msg.MediusWorldID);
                                        if (channel != null)
                                            channel.OnPlayerReport(client.Client, msg);
                                    }
                                    break;
                                }
                            case MediusAppPacketIds.WorldReport:
                                {
                                    var msg = appMsg as MediusWorldReport;

                                    var game = Program.Games.FirstOrDefault(x => x.Id == msg.MediusWorldID);
                                    if (game != null)
                                    {
                                        game.OnWorldReport(msg);
                                    }

                                    break;
                                }
                            case MediusAppPacketIds.GetIgnoreList:
                                {
                                    var ignoreListReq = appMsg as MediusGetIgnoreListRequest;

                                    // No ignore list
                                    responses.Add(new RT_MSG_SERVER_APP()
                                    {
                                        AppMessage = new  MediusGetIgnoreListResponse()
                                        {
                                            MessageID = ignoreListReq.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusNoResult,
                                            EndOfList = true
                                        } 
                                    });

                                    break;
                                }
                            case MediusAppPacketIds.AccountLogout:
                                {
                                    var accountLogoutReq = appMsg as MediusAccountLogoutRequest;
                                    bool success = false;

                                    // Check token
                                    if (accountLogoutReq.SessionKey == client.Client.SessionKey)
                                    {
                                        success = true;
                                        
                                        // Logout
                                        Program.Clients.Remove(client.Client);
                                    }

                                    responses.Add(new RT_MSG_SERVER_APP()
                                    {
                                        AppMessage = new MediusAccountLogoutResponse()
                                        {
                                            StatusCode = success ? MediusCallbackStatus.MediusSuccess : MediusCallbackStatus.MediusAccountNotFound
                                        }
                                    });
                                    break;
                                }
                            case MediusAppPacketIds.UpdateUserState:
                                {
                                    var updateUserReq = appMsg as MediusUpdateUserState;

                                    client.Client.Action = updateUserReq.UserAction;

                                    break;
                                }
                            case MediusAppPacketIds.GetServerTimeRequest:
                                {
                                    var getServerTimeReq = appMsg as MediusGetServerTimeRequest;

                                    responses.Add(new RT_MSG_SERVER_APP()
                                    {
                                        AppMessage = new MediusGetServerTimeResponse()
                                        {
                                            MessageID = getServerTimeReq.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusSuccess,
                                            Local_server_timezone = MediusTimeZone.MediusTimeZone_GMT 
                                        }
                                    });
                                    break;
                                }
                            case MediusAppPacketIds.CreateGame:
                                {
                                    var createGameReq = appMsg as MediusCreateGameRequest;

                                    var lobby = Program.Channels.FirstOrDefault(x => x.Id == client.Client.CurrentChannelId);
                                    if (lobby == null)
                                    {
                                        responses.Add(new RT_MSG_SERVER_APP()
                                        {
                                            AppMessage = new MediusCreateGameResponse()
                                            {
                                                MessageID = createGameReq.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusChannelNotFound
                                            }
                                        });
                                    }
                                    else
                                    {
                                        responses.Add(new RT_MSG_SERVER_APP()
                                        {
                                            AppMessage = new MediusCreateGameResponse()
                                            {
                                                MessageID = createGameReq.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                                MediusWorldID = client.Client.CurrentChannelId
                                            }
                                        });
                                    }

                                    break;
                                }
                            case MediusAppPacketIds.JoinGame:
                                {
                                    var joinGameReq = appMsg as MediusJoinGameRequest;

                                    var lobby = Program.Channels.FirstOrDefault(x => x.Id == joinGameReq.MediusWorldID);
                                    if (lobby == null)
                                    {
                                        responses.Add(new RT_MSG_SERVER_APP()
                                        {
                                            AppMessage = new MediusJoinGameResponse()
                                            {
                                                MessageID = joinGameReq.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusGameNotFound
                                            }
                                        });
                                    }
                                    else
                                    {
                                        responses.Add(new RT_MSG_SERVER_APP()
                                        {
                                            AppMessage = new MediusJoinGameResponse()
                                            {
                                                MessageID = joinGameReq.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                                GameHostType = MediusGameHostType.MediusGameHostClientServer,
                                                ConnectInfo = new NetConnectionInfo()
                                                {
                                                    AccessKey = client.Client.Token,
                                                    SessionKey = client.Client.SessionKey,
                                                    WorldID = lobby.Id,
                                                    ServerKey = new RSA_KEY(Program.GlobalAuthKey.N.ToByteArrayUnsigned().Reverse().ToArray()),
                                                    AddressList = new NetAddressList()
                                                    {
                                                        AddressList = new NetAddress[MediusConstants.NET_ADDRESS_LIST_COUNT]
                                                            {
                                                            new NetAddress() { Address = Program.SERVER_IP.ToString(), Port = (uint)Program.ProxyServer.Port, AddressType = NetAddressType.NetAddressTypeExternal},
                                                            new NetAddress() { AddressType = NetAddressType.NetAddressNone},
                                                            }
                                                    },
                                                    Type = NetConnectionType.NetConnectionTypeClientServerTCP
                                                }
                                            }
                                        });
                                    }
                                    break;
                                }
                            case MediusAppPacketIds.GenericChatMessage:
                                {
                                    var genericChatMessage = appMsg as MediusGenericChatMessage;

                                    // Send it back for now
                                    responses.Add(new RT_MSG_SERVER_APP()
                                    {
                                        AppMessage = new MediusGenericChatFwdMessage()
                                        {
                                            OriginatorAccountID = client.Client.ClientAccount.AccountId,
                                            OriginatorAccountName = client.Client.ClientAccount.AccountName,
                                            Message = genericChatMessage.Message,
                                            MessageType = genericChatMessage.MessageType,
                                            TimeStamp = Utils.GetUnixTime()
                                        }
                                    });
                                    break;
                                }
                            case MediusAppPacketIds.SessionEnd:
                                {
                                    var sessionEndReq = appMsg as MediusSessionEndRequest;
                                    bool success = false;


                                    // Check token
                                    if (sessionEndReq.SessionKey == client.Client.SessionKey)
                                    {
                                        // 
                                        success = true;
                                    }

                                    responses.Add(new RT_MSG_SERVER_APP()
                                    {
                                        AppMessage = new MediusSessionEndResponse()
                                        {
                                            MessageID = sessionEndReq.MessageID,
                                            StatusCode = success ? MediusCallbackStatus.MediusSuccess : MediusCallbackStatus.MediusEndSessionFailed
                                        }
                                    });
                                    break;
                                }
                            default:
                                {
                                    Console.WriteLine($"MLS Unhandled App Message: {appMsg.Id} {appMsg}");
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
                default:
                    {
                        Console.WriteLine($"MLS Unhandled Medius Command: {message.Id} {message}");
                        break;
                    }
            }

            return 0;
        }
    }
}
