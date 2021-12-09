using System;
using System.Collections.Generic;
using System.Text;

namespace RT.Common
{
    public enum RT_MSG_TYPE : byte
    {
        RT_MSG_CLIENT_CONNECT_TCP,
        RT_MSG_CLIENT_DISCONNECT,
        RT_MSG_CLIENT_APP_BROADCAST,
        RT_MSG_CLIENT_APP_SINGLE,
        RT_MSG_CLIENT_APP_LIST,
        RT_MSG_CLIENT_ECHO,
        RT_MSG_SERVER_CONNECT_REJECT,
        RT_MSG_SERVER_CONNECT_ACCEPT_TCP,
        RT_MSG_SERVER_CONNECT_NOTIFY,
        RT_MSG_SERVER_DISCONNECT_NOTIFY,
        RT_MSG_SERVER_APP,
        RT_MSG_CLIENT_APP_TOSERVER,
        RT_MSG_UDP_APP,
        RT_MSG_CLIENT_SET_RECV_FLAG,
        RT_MSG_CLIENT_SET_AGG_TIME,
        RT_MSG_CLIENT_FLUSH_ALL,
        RT_MSG_CLIENT_FLUSH_SINGLE,
        RT_MSG_SERVER_FORCED_DISCONNECT,
        RT_MSG_CLIENT_CRYPTKEY_PUBLIC,
        RT_MSG_SERVER_CRYPTKEY_PEER,
        RT_MSG_SERVER_CRYPTKEY_GAME,
        RT_MSG_CLIENT_CONNECT_TCP_AUX_UDP,
        RT_MSG_CLIENT_CONNECT_AUX_UDP,
        RT_MSG_CLIENT_CONNECT_READY_AUX_UDP,
        RT_MSG_SERVER_INFO_AUX_UDP,
        RT_MSG_SERVER_CONNECT_ACCEPT_AUX_UDP,
        RT_MSG_SERVER_CONNECT_COMPLETE,
        RT_MSG_CLIENT_CRYPTKEY_PEER,
        RT_MSG_SERVER_SYSTEM_MESSAGE,
        RT_MSG_SERVER_CHEAT_QUERY,
        RT_MSG_SERVER_MEMORY_POKE,
        RT_MSG_SERVER_ECHO,
        RT_MSG_CLIENT_DISCONNECT_WITH_REASON,
        RT_MSG_CLIENT_CONNECT_READY_TCP,
        RT_MSG_SERVER_CONNECT_REQUIRE,
        RT_MSG_CLIENT_CONNECT_READY_REQUIRE,
        RT_MSG_CLIENT_HELLO,
        RT_MSG_SERVER_HELLO,
        RT_MSG_SERVER_STARTUP_INFO_NOTIFY,
        RT_MSG_CLIENT_PEER_QUERY,
        RT_MSG_SERVER_PEER_QUERY_NOTIFY,
        RT_MSG_CLIENT_PEER_QUERY_LIST,
        RT_MSG_SERVER_PEER_QUERY_LIST_NOTIFY,
        RT_MSG_CLIENT_WALLCLOCK_QUERY,
        RT_MSG_SERVER_WALLCLOCK_QUERY_NOTIFY,
        RT_MSG_CLIENT_TIMEBASE_QUERY,
        RT_MSG_SERVER_TIMEBASE_QUERY_NOTIFY,
        RT_MSG_CLIENT_TOKEN_MESSAGE,
        RT_MSG_SERVER_TOKEN_MESSAGE,
        RT_MSG_CLIENT_SYSTEM_MESSAGE,
        RT_MSG_CLIENT_APP_BROADCAST_QOS,
        RT_MSG_CLIENT_APP_SINGLE_QOS,
        RT_MSG_CLIENT_APP_LIST_QOS,
        RT_MSG_CLIENT_MAX_MSGLEN,
        RT_MSG_SERVER_MAX_MSGLEN,
        RT_MSG_CLIENT_MULTI_APP_TOSERVER = 59,
        RT_MSG_SERVER_MULTI_APP_TOCLIENT,
    }

    public enum MediusCallbackStatus : int
    {
        MediusBeginSessionFailed = -1000,
        MediusAccountAlreadyExists = -999,
        MediusAccountNotFound = -998,
        MediusAccountLoggedIn = -997,
        MediusEndSessionFailed = -996,
        MediusLoginFailed = -995,
        MediusRegistrationFailed = -994,
        MediusIncorrectLoginStep = -993,
        MediusAlreadyLeaderOfClan = -992,
        MediusWMError = -991,
        MediusNotClanLeader = -990,
        MediusPlayerNotPrivileged = -989,
        MediusDBError = -988,
        MediusDMEError = -987,
        MediusExceedsMaxWorlds = -986,
        MediusRequestDenied = -985,
        MediusSetGameListFilterFailed = -984,
        MediusClearGameListFilterFailed = -983,
        MediusGetGameListFilterFailed = -982,
        MediusNumFiltersAtMax = -981,
        MediusFilterNotFound = -980,
        MediusInvalidRequestMsg = -979,
        MediusInvalidPassword = -978,
        MediusGameNotFound = -977,
        MediusChannelNotFound = -976,
        MediusGameNameExists = -975,
        MediusChannelNameExists = -974,
        MediusGameNameNotFound = -973,
        MediusPlayerBanned = -972,
        MediusClanNotFound = -971,
        MediusClanNameInUse = -970,
        MediusSessionKeyInvalid = -969,
        MediusTextStringInvalid = -968,
        MediusFilterFailed = -967,
        MediusFail = -966,
        MediusFileInternalAccessError = -965,
        MediusFileNoPermissions = -964,
        MediusFileDoesNotExist = -963,
        MediusFileAlreadyExists = -962,
        MediusFileInvalidFilename = -961,
        MediusFileQuotaExceeded = -960,
        MediusCacheFailure = -959,
        MediusDataAlreadyExists = -958,
        MediusDataDoesNotExist = -957,
        MediusMaxExceeded = -956,
        MediusKeyError = -955,
        MediusIncompatibleAppID = -954,
        MediusAccountBanned = -953,
        MediusMachineBanned = -952,
        MediusLeaderCannotLeaveClan = -951,
        MediusFeatureNotEnabled = -950,
        MediusDNASSignatureLoggedIn = -949,
        MediusWorldIsFull = -948,
        MediusNotClanMember = -947,
        MediusServerBusy = -946,
        MediusNumGameWorldsPerLobbyWorldExceeded = -945,
        MediusAccountNotUCCompliant = -944,
        MediusPasswordNotUCCompliant = -943,
        MediusGatewayError = -942,
        MediusTransactionCanceled = -941,
        MediusSessionFail = -940,
        MediusTokenAlreadyTaken = -939,
        MediusTokenDoesNotExist = -938,
        MediusSubscriptionAborted = -937,
        MediusSubscriptionInvalid = -936,
        MediusNotAMember = -935,
        MediusSuccess = 0,
        MediusNoResult = 1,
        MediusRequestAccepted = 2,
        MediusWorldCreatedSizeReduced = 3,
        MediusPass = 4,
    }

    public enum MediusAccountType : int
    {
        MediusChildAccount,
        MediusMasterAccount,
    }

    public enum MediusLadderType : int
    {
        MediusLadderTypePlayer = 0,
        MediusLadderTypeClan = 1,
    }

    public enum MediusPlayerStatus : int
    {
        MediusPlayerDisconnected = 0,
        MediusPlayerInAuthWorld,
        MediusPlayerInChatWorld,
        MediusPlayerInGameWorld,
        MediusPlayerInOtherUniverse,
        LastMediusPLayerStatus,
    }

    public enum MediusCharacterEncodingType : int
    {
        MediusCharacterEncoding_NoUpdate,
        MediusCharacterEncoding_ISO8859_1,
        MediusCharacterEncoding_UTF8,
    }

    public enum MediusLanguageType : int
    {
        MediusLanguage_NoUpdate,
        MediusLanguage_USEnglish,
        MediusLanguage_UKEnglish,
        MediusLanguage_Japanese,
        MediusLanguage_Korean,
        MediusLanguage_Italian,
        MediusLanguage_Spanish,
        MediusLanguage_German,
        MediusLanguage_French,
        MediusLanguage_Dutch,
        MediusLanguage_Portuguese,
        MediusLanguage_Chinese,
        MediusLanguage_Taiwanese,
        MediusLanguage_Finnish,
        MediusLanguage_Norwegian,
    }

    public enum MediusClanStatus : int
    {
        ClanActive,
        ClanDisbanded = -1,
    }

    public enum MediusConnectionType : int
    {
        Modem = 0,
        Ethernet = 1,
        Wireless = 2,
    }

    public enum MediusDnasCategory : int
    {
        DnasConsoleID,
        DnasTitleID,
        DnasDiskID,
    }

    public enum MediusPlayerSearchType : int
    {
        PlayerAccountID,
        PlayerAccountName,
    }

    public enum MediusBinaryMessageType : int
    {
        BroadcastBinaryMsg,
        TargetBinaryMsg,
        BroadcastBinaryMsgAcrossEntireUniverse,
    }

    [Flags]
    public enum MediusWorldGenericFieldLevelType : int
    {
        MediusWorldGenericFieldLevel0 = 0,
        MediusWorldGenericFieldLevel1 = (1 << 0),
        MediusWorldGenericFieldLevel2 = (1 << 1),
        MediusWorldGenericFieldLevel3 = (1 << 2),
        MediusWorldGenericFieldLevel4 = (1 << 3),
        MediusWorldGenericFieldLevel12 = (1 << 4),
        MediusWorldGenericFieldLevel123 = (1 << 5),
        MediusWorldGenericFieldLevel1234 = (1 << 6),
        MediusWorldGenericFieldLevel23 = (1 << 7),
        MediusWorldGenericFieldLevel234 = (1 << 8),
        MediusWorldGenericFieldLevel34 = (1 << 9),
    }

    public enum MediusApplicationType : int
    {
        MediusAppTypeGame,
        LobbyChatChannel,
    }

    public enum MediusTextFilterType : int
    {
        MediusTextFilterPassFail = 0,
        MediusTextFilterReplace = 1,
    }

    public enum MediusSortOrder : int
    {
        MEDIUS_ASCENDING,
        MEDIUS_DESCENDING,
    }

    public enum MediusClanInvitationsResponseStatus : int
    {
        ClanInvitationUndecided,
        ClanInvitationAccept,
        ClanInvitationDecline,
        ClanInvitationRevoked,
    }

    public enum MediusPolicyType : int
    {
        Usage,
        Privacy,
    }

    public enum MediusUserAction : int
    {
        KeepAlive,
        JoinedChatWorld,
        LeftGameWorld,
    }

    public enum MediusJoinType : int
    {
        MediusJoinAsPlayer = 0,
        MediusJoinAsSpectator = 1,
        MediusJoinAsMassSpectator = 2,
    }

    public enum MediusTimeZone : int
    {
        MediusTimeZone_IDLW = -1200,
        MediusTimeZone_HST = -1000,
        MediusTimeZone_AKST = -900,
        MediusTimeZone_AKDT = -800,
        MediusTimeZone_PST = -801,
        MediusTimeZone_PDT = -700,
        MediusTimeZone_MST = -701,
        MediusTimeZone_MDT = -600,
        MediusTimeZone_CST = -601,
        MediusTimeZone_CDT = -500,
        MediusTimeZone_EST = -501,
        MediusTimeZone_EDT = -400,
        MediusTimeZone_AST = -401,
        MediusTimeZone_NST = -350,
        MediusTimeZone_ADT = -300,
        MediusTimeZone_NDT = -250,
        MediusTimeZone_WAT = -100,
        MediusTimeZone_GMT = 0,
        MediusTimeZone_UTC = 1,
        MediusTimeZone_WET = 2,
        MediusTimeZone_BST = 100,
        MediusTimeZone_IRISHST = 101,
        MediusTimeZone_WEST = 102,
        MediusTimeZone_CET = 103,
        MediusTimeZone_CEST = 200,
        MediusTimeZone_SWEDISHST = 201,
        MediusTimeZone_FST = 202,
        MediusTimeZone_CAT = 203,
        MediusTimeZone_SAST = 204,
        MediusTimeZone_EET = 205,
        MediusTimeZone_ISRAELST = 206,
        MediusTimeZone_EEST = 300,
        MediusTimeZone_BT = 301,
        MediusTimeZone_MSK = 302,
        MediusTimeZone_IRANST = 350,
        MediusTimeZone_MSD = 400,
        MediusTimeZone_INDIANST = 550,
        MediusTimeZone_JT = 750,
        MediusTimeZone_HKT = 800,
        MediusTimeZone_CCT = 801,
        MediusTimeZone_AWST = 802,
        MediusTimeZone_MT = 850,
        MediusTimeZone_KST = 900,
        MediusTimeZone_JST = 901,
        MediusTimeZone_ACST = 950,
        MediusTimeZone_AEST = 1000,
        MediusTimeZone_GST = 1001,
        MediusTimeZone_ACDT = 1050,
        MediusTimeZone_AEDT = 1100,
        MediusTimeZone_SST = 1101,
        MediusTimeZone_NZST = 1200,
        MediusTimeZone_IDLE = 1201,
        MediusTimeZone_NZDT = 1300,
    }

    public enum MediusGameHostType : int
    {
        MediusGameHostClientServer = 0,
        MediusGameHostIntegratedServer = 1,
        MediusGameHostPeerToPeer = 2,
        MediusGameHostLANPlay = 3,
        MediusGameHostClientServerAuxUDP = 4,
    }

    public enum MediusWorldAttributesType : int
    {
        GAME_WORLD_NONE = 0,
        GAME_WORLD_ALLOW_REBROADCAST = (1 << 0),
        GAME_WORLD_ALLOW_SPECTATOR = (1 << 1),
        GAME_WORLD_INTERNAL = (1 << 2),
    }

    public enum MediusChatMessageType : int
    {
        Broadcast,
        Whisper,
        BroadcastAcrossEntireUniverse,
        MediusClanChatType,
        MediusBuddyChatType,
    }

    public enum MediusWorldStatus : int
    {
        WorldInactive,
        WorldStaging,
        WorldActive,
        WorldClosed,
        WorldPendingCreation,
        WorldPendingConnectToGame,
    }

    public enum MediusGameListFilterField : int
    {
        MEDIUS_FILTER_PLAYER_COUNT = 1,
        MEDIUS_FILTER_MIN_PLAYERS = 2,
        MEDIUS_FILTER_MAX_PLAYERS = 3,
        MEDIUS_FILTER_GAME_LEVEL = 4,
        MEDIUS_FILTER_PLAYER_SKILL_LEVEL = 5,
        MEDIUS_FILTER_RULES_SET = 6,
        MEDIUS_FILTER_GENERIC_FIELD_1 = 7,
        MEDIUS_FILTER_GENERIC_FIELD_2 = 8,
        MEDIUS_FILTER_GENERIC_FIELD_3 = 9,
        MEDIUS_FILTER_LOBBY_WORLDID = 10,
        MEDIUS_FILTER_GENERIC_FIELD_4 = 11,
        MEDIUS_FILTER_GENERIC_FIELD_5 = 12,
        MEDIUS_FILTER_GENERIC_FIELD_6 = 13,
        MEDIUS_FILTER_GENERIC_FIELD_7 = 14,
        MEDIUS_FILTER_GENERIC_FIELD_8 = 15,
    }

    [Flags]
    public enum MediusWorldSecurityLevelType : int
    {
        WORLD_SECURITY_NONE = 0,
        WORLD_SECURITY_PLAYER_PASSWORD = (1 << 0),
        WORLD_SECURITY_CLOSED = (1 << 1),
        WORLD_SECURITY_SPECTATOR_PASSWORD = (1 << 2),
    }

    public enum MediusLobbyFilterType : int
    {
        MediusLobbyFilterEqualsLobby = 0,
        MediusLobbyFilterEqualsFilter = 1,
    }

    [Flags]
    public enum MediusLobbyFilterMaskLevelType : int
    {
        MediusLobbyFilterMaskLevel0 = 0,
        MediusLobbyFilterMaskLevel1 = (1 << 0),
        MediusLobbyFilterMaskLevel2 = (1 << 1),
        MediusLobbyFilterMaskLevel3 = (1 << 2),
        MediusLobbyFilterMaskLevel4 = (1 << 3),
        MediusLobbyFilterMaskLevel12 = (1 << 4),
        MediusLobbyFilterMaskLevel123 = (1 << 5),
        MediusLobbyFilterMaskLevel1234 = (1 << 6),
        MediusLobbyFilterMaskLevel23 = (1 << 7),
        MediusLobbyFilterMaskLevel234 = (1 << 8),
        MediusLobbyFilterMaskLevel34 = (1 << 9),
    }

    public enum MediusComparisonOperator : int
    {
        LESS_THAN,
        LESS_THAN_OR_EQUAL_TO,
        EQUAL_TO,
        GREATER_THAN_OR_EQUAL_TO,
        GREATER_THAN,
        NOT_EQUALS,
    }

    public enum NetConnectionType : int
    {
        NetConnectionNone = 0,
        NetConnectionTypeClientServerTCP = 1,
        NetConnectionTypePeerToPeerUDP = 2,
        NetConnectionTypeClientServerTCPAuxUDP = 3,
        NetConnectionTypeClientListenerTCP = 4
    }

    public enum SERVER_FORCE_DISCONNECT_REASON : byte
    {
        SERVER_FORCED_DISCONNECT_NONE = 0,
        SERVER_FORCED_DISCONNECT_ERROR = 1,
        SERVER_FORCED_DISCONNECT_SHUTDOWN = 2,
        SERVER_FORCED_DISCONNECT_END_SESSION = 3,
        SERVER_FORCED_DISCONNECT_END_GAME = 4,
        SERVER_FORCED_DISCONNECT_TIME0UT = 5,
        SERVER_FORCED_DISCONNECT_BAD_PERF = 6,
        SERVER_FORCED_DISCONNECT_BANNED = 7
    }

    public enum MGCL_EVENT_TYPE : int
    {
        MGCL_EVENT_CLIENT_DISCONNECT = 0,
        MGCL_EVENT_CLIENT_CONNECT = 1,
    }

    public enum MGCL_ALERT_LEVEL : int
    {
        MGCL_SUCCESS = 0,
        MGCL_CONNECTION_ERROR = -1,
        MGCL_CONNECTION_FAILED = -2,
        MGCL_DISCONNECT_FAILED = -3,
        MGCL_NOT_CONNECTED = -4,
        MGCL_SEND_FAILED = -5,
        MGCL_INITIALIZATION_FAILED = -6,
        MGCL_SHUTDOWN_ERROR = -7,
        MGCL_NETWORK_ERROR = -8,
        MGCL_AUTHENTICATION_FAILED = -9,
        MGCL_SESSIONBEGIN_FAILED = -10,
        MGCL_SESSIONEND_FAILED = -11,
        MGCL_UNSUCCESSFUL = -12,
        MGCL_INVALID_ARG = -13,
        MGCL_NATRESOLVE_FAILED = -14,
        MGCL_GAME_NAME_EXISTS = -15,
        MGCL_WORLDID_INUSE = -16,
        MGCL_DME_ERROR = -17,
        MGCL_CALL_MGCL_CLOSE_BEFORE_REINITIALIZING = -18,
        MGCL_NUM_GAME_WORLDS_PER_LOBBY_WORLD_EXCEEDED = -19,
    }

    [Flags]
    public enum RT_RECV_FLAG : byte
    {
        NONE = 0,
        RECV_BROADCAST = 1,
        RECV_LIST = 2,
        RECV_SINGLE = 4,
        RECV_NOTIFICATION = 8
    }

    public enum NetAddressType : int
    {
        NetAddressNone = 0,
        NetAddressTypeExternal = 1,
        NetAddressTypeInternal = 2,
        NetAddressTypeNATService = 3,
        NetAddressTypeBinaryExternal = 4,
        NetAddressTypeBinaryInternal = 5,
        NetAddressTypeBinaryExternalVport = 6,
        NetAddressTypeBinaryInternalVport = 7,
        NetAddressTypeBinaryNATServices = 8
    }

    public enum NetMessageTypes : byte
    {
        MessageClassDME,
        MessageClassLobby,
        MessageClassApplication,
        MessageClassLobbyReport,
        MessageClassLobbyExt,
        MessageClassLobbyAuthentication,
        MaxMessageClasses,
    }

    public enum MediusDmeMessageIds : byte
    {
        ServerVersion = 0x00,
        Ping = 0x01,
        PacketFragment = 0x02,
        ClientConnects = 0x10,
        RequestServers = 0x13,
        ServerResponse = 0x14,
        UpdateClientStatus = 0x16,
        LANFindPacket = 0x19,
        LANFindResultsPacket = 0x1A,
        LANTextMessage = 0x21,
        LANRawMessage = 0x22
    }

    public enum MediusMGCLMessageIds : byte
    {
        ServerReport = 0x00,
        ServerAuthenticationRequest = 0x01,
        ServerAuthenticationResponse = 0x02,
        ServerSessionBeginRequest = 0x03,
        ServerSessionBeginResponse = 0x04,
        ServerSessionEndRequest = 0x05,
        ServerSessionEndResponse = 0x06,
        ServerCreateGameRequest = 0x07,
        ServerCreateGameResponse = 0x08,
        ServerJoinGameRequest = 0x09,
        ServerJoinGameResponse = 0x0A,
        ServerEndGameRequest = 0x0B,
        ServerEndGameResponse = 0x0C,
        ServerWorldStatusRequest = 0x0D,
        ServerWorldStatusResponse = 0x0E,
        ServerCreateGameOnMeRequest = 0x1F,
        ServerCreateGameOnMeResponse = 0x10,
        ServerEndGameOnMeRequest = 0x11,
        ServerEndGameOnMeResponse = 0x12,
        ServerMoveGameWorldOnMeRequest = 0x14,
        ServerMoveGameWorldOnMeResponse = 0x15,
        ServerSetAttributesRequest = 0x16,
        ServerSetAttributesResponse = 0x17,
        ServerCreateGameWithAttributesRequest = 0x18,
        ServerCreateGameWithAttributesResponse = 0x19,
        ServerConnectGamesRequest = 0x1A,
        ServerConnectGamesResponse = 0x1B,
        ServerConnectNotification = 0x1C,
        ServerDisconnectPlayerRequest = 0x1E,
        ServerWorldReportOnMe = 0x20,
    }

    public enum MediusLobbyMessageIds : byte
    {
        WorldReport0 = 0x00,
        PlayerReport = 0x01,
        EndGameReport = 0x02,
        SessionBegin = 0x03,
        SessionBeginResponse = 0x04,
        SessionEnd = 0x05,
        SessionEndResponse = 0x06,
        AccountLogin = 0x07,
        AccountLoginResponse = 0x08,
        AccountRegistration = 0x09,
        AccountRegistrationResponse = 0x0A,
        AccountGetProfile = 0x0B,
        AccountGetProfileResponse = 0x0C,
        AccountUpdateProfile = 0x0D,
        AccountUpdateProfileResponse = 0x0E,
        AccountUpdatePassword = 0x0F,
        AccountUpdateStats = 0x11,
        AccountUpdateStatsResponse = 0x12,
        AccountDelete = 0x13,
        AccountDeleteResponse = 0x14,
        AccountLogout = 0x15,
        AccountLogoutResponse = 0x16,
        AccountGetID = 0x17,
        AccountGetIDResponse = 0x18,
        AnonymousLogin = 0x19,
        AnonymousLoginResponse = 0x1A,
        GetMyIP = 0x1B,
        GetMyIPResponse = 0x1C,
        CreateGameRequest0 = 0x1D,
        CreateGameResponse = 0x1E,
        CreateGameOnSelf = 0x1F,
        CreateGameOnSelfResponse = 0x20,
        CreateChannelRequest0 = 0x21,
        CreateChannelResponse = 0x22,
        JoinGameRequest0 = 0x23,
        JoinGameResponse = 0x24,
        JoinChannel = 0x25,
        JoinChannelResponse = 0x26,
        JoinChannelFwd = 0x27,
        JoinChannelFwdResponse = 0x28,
        GameList = 0x29,
        GameListResponse = 0x2A,
        ChannelList = 0x2B,
        ChannelListResponse = 0x2C,
        LobbyWorldPlayerList = 0x2D,
        LobbyWorldPlayerListResponse = 0x2E,
        GameWorldPlayerList = 0x2F,
        GameWorldPlayerListResponse = 0x30,
        PlayerInfo = 0x31,
        PlayerInfoResponse = 0x32,
        GameInfo0 = 0x33,
        GameInfoResponse0 = 0x34,
        ChannelInfo = 0x35,
        ChannelInfoResponse = 0x36,
        FindWorldByName = 0x37,
        FindWorldByNameResponse = 0x38,
        FindPlayer = 0x39,
        FindPlayerResponse = 0x3A,
        ChatMessage = 0x3B,
        ChatFwdMessage = 0x3C,
        GetBuddyList = 0x3D,
        GetBuddyListResponse = 0x3E,
        AddToBuddyList = 0x3F,
        AddToBuddyListResponse = 0x40,
        RemoveFromBuddyList = 0x41,
        RemoveFromBuddyListResponse = 0x42,
        AddToBuddyListConfirmationRequest0 = 0x43,
        AddToBuddyListConfirmationResponse = 0x44,
        AddToBuddyListFwdConfirmationRequest0 = 0x45,
        AddToBuddyListFwdConfirmationResponse0 = 0x46,
        Policy = 0x47,
        PolicyResponse = 0x48,
        UpdateUserState = 0x49,
        ErrorMessage = 0x4A,
        GetAnnouncements = 0x4B,
        GetAllAnnouncements = 0x4C,
        GetAnnouncementsResponse = 0x4D,
        SetGameListFilter0 = 0x4E,
        SetGameListFilterResponse0 = 0x4F,
        ClearGameListFilter0 = 0x50,
        ClearGameListFilterResponse = 0x51,
        GetGameListFilter = 0x52,
        GetGameListFilterResponse0 = 0x53,
        CreateClan = 0x54,
        CreateClanResponse = 0x55,
        DisbandClan = 0x56,
        DisbandClanResponse = 0x57,
        GetClanByID = 0x58,
        GetClanByIDResponse = 0x59,
        GetClanByName = 0x5A,
        GetClanByNameResponse = 0x5B,
        TransferClanLeadership = 0x5C,
        TransferClanLeadershipResponse = 0x5D,
        AddPlayerToClan = 0x5E,
        AddPlayerToClanResponse = 0x5F,
        RemovePlayerFromClan = 0x60,
        RemovePlayerFromClanResponse = 0x61,
        InvitePlayerToClan = 0x62,
        InvitePlayerToClanResponse = 0x63,
        CheckMyClanInvitations = 0x64,
        CheckMyClanInvitationsResponse = 0x65,
        RespondToClanInvitation = 0x66,
        RespondToClanInvitationResponse = 0x67,
        RevokeClanInvitation = 0x68,
        RevokeClanInvitationResponse = 0x69,
        RequestClanTeamChallenge = 0x6A,
        RequestClanTeamChallengeResponse = 0x6B,
        GetMyClanMessages = 0x6C,
        GetMyClanMessagesResponse = 0x6D,
        SendClanMessage = 0x6E,
        SendClanMessageResponse = 0x6F,
        ModifyClanMessage = 0x70,
        ModifyClanMessageResponse = 0x71,
        DeleteClanMessage = 0x72,
        DeleteClanMessageResponse = 0x73,
        RespondToClanTeamChallenge = 0x74,
        RespondToClanTeamChallengeResponse = 0x75,
        RevokeClanTeamChallenge = 0x76,
        RevokeClanTeamChallengeResponse = 0x77,
        GetClanTeamChallengeHistory = 0x78,
        GetClanTeamChallengeHistoryResponse = 0x79,
        GetClanInvitationsSent = 0x7A,
        GetClanInvitationsSentResponse = 0x7B,
        GetMyClans = 0x7C,
        GetMyClansResponse = 0x7D,
        GetAllClanMessages = 0x7E,
        GetAllClanMessagesResponse = 0x7F,
        ConfirmClanTeamChallenge = 0x80,
        ConfirmClanTeamChallengeResponse = 0x81,
        GetClanTeamChallenges = 0x82,
        GetClanTeamChallengesResponse = 0x83,
        UpdateClanStats = 0x84,
        UpdateClanStatsResponse = 0x85,
        VersionServer = 0x86,
        VersionServerResponse = 0x87,
        GetWorldSecurityLevel = 0x88,
        GetWorldSecurityLevelResponse = 0x89,
        BanPlayer = 0x8A,
        BanPlayerResponse = 0x8B,
        GetLocations = 0x8C,
        GetLocationsResponse = 0x8D,
        PickLocation = 0x8E,
        PickLocationResponse = 0x8F,
        GetClanMemberList = 0x90,
        GetClanMemberListResponse = 0x91,
        LadderPosition = 0x92,
        LadderPositionResponse = 0x93,
        LadderList = 0x94,
        LadderListResponse = 0x95,
        ChatToggle = 0x96,
        ChatToggleResponse = 0x97,
        TextFilter = 0x98,
        TextFilterResponse = 0x99,
        ServerReassignGameMediusWorldID = 0x9A,
        GetTotalGames = 0x9B,
        GetTotalGamesResponse = 0x9C,
        GetTotalChannels = 0x9D,
        GetTotalChannelsResponse = 0x9E,
        GetLobbyPlayerNames = 0x9F,
        GetLobbyPlayerNamesResponse = 0xA0,
        GetTotalUsers = 0xA1,
        GetTotalUsersResponse = 0xA2,
        SetLocalizationParams = 0xA3,
        SetLocalizationParamsResponse = 0xA4,
        FileCreate = 0xA5,
        FileCreateResponse = 0xA6,
        FileUpload = 0xA7,
        FileUploadResponse = 0xA8,
        FileUploadServerReq = 0xA9,
        FileClose = 0xAA,
        FileCloseResponse = 0xAB,
        FileDownload = 0xAC,
        FileDownloadResponse = 0xAD,
        FileDownloadStream = 0xAE,
        FileDownloadStreamResponse = 0xAF,
        FileDelete = 0xB0,
        FileDeleteResponse = 0xB1,
        FileListFiles = 0xB2,
        FileListFilesResponse = 0xB3,
        FileUpdateAttributes = 0xB4,
        FileUpdateAttributesResponse = 0xB5,
        FileGetAttributes = 0xB6,
        FileGetAttributesResponse = 0xB7,
        FileUpdateMetaData = 0xB8,
        FileUpdateMetaDataResponse = 0xB9,
        FileGetMetaData = 0xBA,
        FileGetMetaDataResponse = 0xBB,
        FileSearchByMetaData = 0xBC,
        FileSearchByMetaDataResponse = 0xBD,
        FileCancelOperation = 0xBE,
        FileCancelOperationResponse = 0xBF,
        GetIgnoreList = 0xC0,
        GetIgnoreListResponse = 0xC1,
        AddToIgnoreList = 0xC2,
        AddToIgnoreListResponse = 0xC3,
        RemoveFromIgnoreList = 0xC4,
        RemoveFromIgnoreListResponse = 0xC5,
        SetMessageAsRead = 0xC6,
        SetMessageAsReadResponse = 0xC7,
        GetUniverseInformation = 0xC8,
        UniverseNewsResponse = 0xC9,
        UniverseStatusListResponse = 0xCA,
        MachineSignaturePost = 0xCB,
        LadderPositionFast = 0xCC,
        LadderPositionFastResponse = 0xCD,
        UpdateLadderStats = 0xCE,
        UpdateLadderStatsResponse = 0xCF,
        GetLadderStats = 0xD0,
        GetLadderStatsResponse = 0xD1,
        ClanLadderList = 0xD2,
        ClanLadderListResponse = 0xD3,
        ClanLadderPosition = 0xD4,
        ClanLadderPositionResponse = 0xD5,
        GetBuddyList_ExtraInfo = 0xD6,
        GetBuddyList_ExtraInfoResponse = 0xD7,
        GetTotalRankings = 0xD8,
        GetTotalRankingsResponse = 0xD9,
        GetClanMemberList_ExtraInfo = 0xDA,
        GetClanMemberList_ExtraInfoResponse = 0xDB,
        GetLobbyPlayerNames_ExtraInfo = 0xDC,
        GetLobbyPlayerNames_ExtraInfoResponse = 0xDD,
        BillingLogin = 0xDE,
        BillingLoginResponse = 0xDF,
        BillingListRequest = 0xE0,
        BillingListResponse = 0xE1,
        BillingDetailRequest = 0xE2,
        BillingDetailResponse = 0xE3,
        PurchaseProductRequest = 0xE4,
        PurchaseProductResponse = 0xE5,
        BillingInfo = 0xE6,
        BillingInfoResponse = 0xE7,
        BillingTunnelRequest = 0xE8,
        BillingTunnelResponse = 0xE9,
        GameList_ExtraInfo0 = 0xEA,
        GameList_ExtraInfoResponse0 = 0xEB,
        ChannelList_ExtraInfo0 = 0xEC,
        ChannelList_ExtraInfoResponse = 0xED,
        InvitePlayerToClan_ByName = 0xEE,
        LadderList_ExtraInfo0 = 0xEF,
        LadderList_ExtraInfoResponse = 0xF0,
        LadderPosition_ExtraInfo = 0xF1,
        LadderPosition_ExtraInfoResponse = 0xF2,
        JoinGame = 0xF3,
        CreateGame1 = 0xF4,
        UtilAddLobbyWorld = 0xF5,
        UtilAddLobbyWorldResponse = 0xF6,
        UtilAddGameWorld = 0xF7,
        UtilAddGameWorldResponse = 0xF8,
        UtilUpdateLobbyWorld = 0xF9,
        UtilUpdateLobbyWorldResponse = 0xFA,
        UtilUpdateGameWorld = 0xFB,
        UtilUpdateGameWorldResponse = 0xFC,
    }

    public enum MediusLobbyExtMessageIds : byte
    {
        CreateChannel1 = 0x00,
        UtilGetServerVersion = 0x01,
        UtilGetServerVersionResponse = 0x02,
        GetUniverse_ExtraInfo = 0x03,
        UniverseStatusList_ExtraInfoResponse = 0x04,
        AddToBuddyListConfirmation = 0x05,
        AddToBuddyListFwdConfirmation = 0x06,
        AddToBuddyListFwdConfirmationResponse = 0x07,
        GetBuddyInvitations = 0x08,
        GetBuddyInvitationsResponse = 0x09,
        DnasSignaturePost = 0x0A,
        UpdateLadderStatsWide = 0x0B,
        UpdateLadderStatsWideResponse = 0x0C,
        GetLadderStatsWide = 0x0D,
        GetLadderStatsWideResponse = 0x0E,
        LadderList_ExtraInfo = 0x0F,
        UtilEventMsgHandler = 0x10,
        UniverseVariableInformationResponse = 0x11,
        SetLobbyWorldFilter = 0x12,
        SetLobbyWorldFilterResponse = 0x13,
        CreateChannel = 0x14,
        ChannelList_ExtraInfo1 = 0x15,
        BinaryMessage = 0x16,
        BinaryFwdMessage = 0x17,
        PostDebugInfo = 0x18,
        PostDebugInfoResponse = 0x19,
        UpdateClanLadderStatsWide_Delta = 0x1A,
        UpdateClanLadderStatsWide_DeltaResponse = 0x1B,
        GetLadderStatsWide_wIDArray = 0x1C,
        GetLadderStatsWide_wIDArray_Response = 0x1D,
        UniverseVariableSvoURLResponse = 0x1E,
        ChannelList_ExtraInfo = 0x1F,
        GenericChatMessage = 0x23,
        GenericChatFwdMessage = 0x24,
        GenericChatSetFilterRequest = 0x25,
        GenericChatSetFilterResponse = 0x26,
        ExtendedSessionBeginRequest = 0x27,
        TokenRequest = 0x28,
        VoteToBanPlayerRequest = 0x2C,
        GetServerTimeRequest = 0x2A,
        GetServerTimeResponse = 0x2B,
        SetAutoChatHistoryRequest = 0x2D,
        SetAutoChatHistoryResponse = 0x2E,
        CreateGame = 0x2F,
        GetGameListFilterResponse = 0x32,
        SetGameListFilter = 0x33,
        SetGameListFilterResponse = 0x34,
        ClearGameListFilter = 0x31,
        WorldReport = 0x30,
        GameInfo = 0x35,
        GameInfoResponse = 0x36,
        GameList_ExtraInfo = 0x37,
        GameList_ExtraInfoResponse = 0x38,
        AccountUpdateStats_OpenAccess = 0x39,
        AccountUpdateStats_OpenAccessResponse = 0x3A,
        AddPlayerToClan_ByClanOfficer = 0x3B,
        AddPlayerToClan_ByClanOfficerResponse = 0x3C,
		// PS3
        TicketLogin = 0x58,
        TicketLoginResponse = 0x59,
        SetLocalizationParams1 = 0x7B,
        SessionBegin1 = 0x8B,
        SetLobbyWorldFilter1 = 0x86,
        SetLobbyWorldFilterResponse1 = 0x87,
        CreateClan2 = 0x75,
    }

    public enum MGCL_TRUST_LEVEL : int
    {
        MGCL_TRUSTED = 0,
        MGCL_NOT_TRUSTED = 1,
    }

    public enum MGCL_GAME_HOST_TYPE : int
    {
        MGCLGameHostClientServer = 0,
        MGCLGameHostIntegratedServer = 1,
        MGCLGameHostPeerToPeer = 2,
        MGCLGameHostLANPlay = 3,
        MGCLGameHostClientServerAuxUDP = 4,
    }

    public enum MGCL_ERROR_CODE : sbyte
    {
        MGCL_SUCCESS = 0,
        MGCL_CONNECTION_ERROR = -1,
        MGCL_CONNECTION_FAILED = -2,
        MGCL_DISCONNECT_FAILED = -3,
        MGCL_NOT_CONNECTED = -4,
        MGCL_SEND_FAILED = -5,
        MGCL_INITIALIZATION_FAILED = -6,
        MGCL_SHUTDOWN_ERROR = -7,
        MGCL_NETWORK_ERROR = -8,
        MGCL_AUTHENTICATION_FAILED = -9,
        MGCL_SESSIONBEGIN_FAILED = -10,
        MGCL_SESSIONEND_FAILED = -11,
        MGCL_UNSUCCESSFUL = -12,
        MGCL_INVALID_ARG = -13,
        MGCL_NATRESOLVE_FAILED = -14,
        MGCL_GAME_NAME_EXISTS = -15,
        MGCL_WORLDID_INUSE = -16,
        MGCL_DME_ERROR = -17,
        MGCL_CALL_MGCL_CLOSE_BEFORE_REINITIALIZING = -18,
        MGCL_NUM_GAME_WORLDS_PER_LOBBY_WORLD_EXCEEDED = -19,
    }

    public enum CheatQueryType : byte
    {
        DME_SERVER_CHEAT_QUERY_RAW_MEMORY = 0,
        DME_SERVER_CHEAT_QUERY_SHA1_HASH = 1,
        DME_SERVER_CHEAT_QUERY_MD5_HASH = 2,
        DME_SERVER_CHEAT_QUERY_ACTIVE_THREAD_CNT = 3,
        DME_SERVER_CHEAT_QUERY_REMENANT_THREAD_CNT = 4,
        DME_SERVER_CHEAT_QUERY_SALTY_SHA1_HASH = 5,
        DME_SERVER_CHEAT_QUERY_SALTY_MD5_HASH = 6,
        DME_SERVER_CHEAT_QUERY_4BYTE_POKE_ADDRESS = 8,
        DME_SERVER_CHEAT_QUERY_NO_OP_FUNC_ADDRESS = 9,
        DME_SERVER_CHEAT_QUERY_INTERRUPT_CNT = 10,
        DME_SERVER_CHEAT_QUERY_THREAD_INFO = 11,
        DME_SERVER_CHEAT_QUERY_STRING_EXISTS = 12,
        DME_SERVER_CHEAT_QUERY_IOP_RAW_MEMORY = 13,
        DME_SERVER_CHEAT_QUERY_IOP_SHA1_HASH = 14,
        DME_SERVER_CHEAT_QUERY_IOP_MD5_HASH = 15,
        DME_SERVER_CHEAT_QUERY_IOP_SALTY_SHA1_HASH = 16,
        DME_SERVER_CHEAT_QUERY_IOP_SALTY_MD5_HASH = 17,
        DME_SERVER_CHEAT_QUERY_IOP_MOD_RAW = 18,
        DME_SERVER_CHEAT_QUERY_IOP_MOD_SHA1 = 19,
        DME_SERVER_CHEAT_QUERY_IOP_MOD_MD5 = 20,
        DME_SERVER_CHEAT_QUERY_IOP_MOD_SALTY_SHA1 = 21,
        DME_SERVER_CHEAT_QUERY_IOP_MOD_SALTY_MD5 = 22,
        DME_SERVER_CHEAT_QUERY_IOP_MOD_CNT = 23,
        DME_SERVER_CHEAT_QUERY_IOP_MOD_TEXT_ADDR = 24,
        DME_SERVER_CHEAT_QUERY_IOP_MOD_TEXT_SIZE = 25,
        DME_SERVER_CHEAT_QUERY_IOP_THREAD_CNT = 26,
        DME_SERVER_CHEAT_QUERY_IOP_MEM_FREE = 27,
        DME_SERVER_CHEAT_QUERY_IOP_MEM_USED = 28
    }

    public enum NetClientStatus : byte
    {
        ClientStatusNone,
        ClientStatusNotConnected,
        ClientStatusConnected,
        ClientStatusJoining,
        ClientStatusJoined,
        ClientStatusJoinedSessionMaster,
    }

    [Flags]
    public enum MediusUniverseVariableInformationInfoFilter : uint
    {
        INFO_UNIVERSES = (1 << 0),
        INFO_NEWS = (1 << 1),
        INFO_ID = (1 << 2),
        INFO_NAME = (1 << 3),
        INFO_DNS = (1 << 4),
        INFO_DESCRIPTION = (1 << 5),
        INFO_STATUS = (1 << 6),
        INFO_BILLING = (1 << 7),
        INFO_EXTRAINFO = (1 << 8),
        INFO_SVO_URL = (1 << 9),

    }

    public enum MediusClanChallengeStatus : int
    {
        ClanChallengeRequest,
        ClanChallengeAccepted,
        ClanChallengeRevoked,
        ClanChallengeRefused,
        ClanChallengeConfirmed,
    }

    public enum MediusClanMessageStatus : int
    {
        ClanMessageUnread,
        ClanMessageModified,
        ClanMessageDeleted,
        ClanMessageRead,
    }

    public enum MediusTokenActionType : int
    {
        MediusInvalidTokenAction = 0,
        MediusAddToken = 1,
        MediusUpdateToken = 2,
        MediusRemoveToken = 3,
    }

    public enum MediusTokenCategoryType : int
    {
        MediusInvalidToken = 0,
        MediusGenericToken1 = 1,
        MediusGenericToken2 = 2,
        MediusGenericToken3 = 3,
        MediusAccountToken = 4,
        MediusClanToken = 5,
    }
}
