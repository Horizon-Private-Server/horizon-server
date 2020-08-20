using Deadlocked.Server.Stream;
using Org.BouncyCastle.Asn1.X509;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets
{
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
        CreateGameOnSelf = 0x1F01,
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

    public class NetAddress : IStreamSerializer
    {
        public NetAddressType AddressType;

        public string Address; // NET_MAX_NETADDRESS_LENGTH

        public uint Port;

        public void Deserialize(BinaryReader reader)
        {
            AddressType = reader.Read<NetAddressType>();
            Address = reader.ReadString(MediusConstants.NET_MAX_NETADDRESS_LENGTH);
            Port = reader.ReadUInt32();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(AddressType);
            writer.Write(Address, MediusConstants.NET_MAX_NETADDRESS_LENGTH);
            writer.Write(Port);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
             $"AddressType:{AddressType}" + " " +
$"Address:{Address}" + " " +
$"Port:{Port}";
        }
    }

    public class NetAddressList : IStreamSerializer
    {
        public NetAddress[] AddressList = null;

        public NetAddressList()
        {
            AddressList = new NetAddress[MediusConstants.NET_ADDRESS_LIST_COUNT];
            for (int i = 0; i < MediusConstants.NET_ADDRESS_LIST_COUNT; ++i)
                AddressList[i] = new NetAddress();
        }

        public void Deserialize(BinaryReader reader)
        {
            for (int i = 0; i < AddressList.Length; ++i)
            {
                AddressList[i] = reader.Read<NetAddress>();
            }
        }

        public void Serialize(BinaryWriter writer)
        {
            for (int i = 0; i < AddressList.Length; ++i)
            {
                writer.Write(AddressList[i]);
            }
        }

        public override string ToString()
        {
            return "NetAddresses:<" + String.Join(" ", AddressList.Select(x => x.ToString())) + "> ";
        }
    }

    public class NetConnectionInfo : IStreamSerializer
    {
        public NetConnectionType Type;
        public NetAddressList AddressList = new NetAddressList();
        public int WorldID;
        public RSA_KEY ServerKey = new RSA_KEY();
        public string SessionKey; // NET_SESSION_KEY_LEN
        public string AccessKey; // NET_ACCESS_KEY_LEN

        public void Deserialize(BinaryReader reader)
        {
            Type = reader.Read<NetConnectionType>();
            AddressList = reader.Read<NetAddressList>();
            WorldID = reader.ReadInt32();
            ServerKey = reader.Read<RSA_KEY>();
            SessionKey = reader.ReadString(MediusConstants.NET_SESSION_KEY_LEN);
            AccessKey = reader.ReadString(MediusConstants.NET_ACCESS_KEY_LEN);
            reader.ReadBytes(2);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Type);
            writer.Write(AddressList);
            writer.Write(WorldID);
            writer.Write(ServerKey);
            writer.Write(SessionKey, MediusConstants.NET_SESSION_KEY_LEN);
            writer.Write(AccessKey, MediusConstants.NET_ACCESS_KEY_LEN);
            writer.Write(new byte[2]);
        }

        public override string ToString()
        {
            return $"Type:{Type}" + " " +
$"AddressList:{AddressList}" + " " +
$"WorldID:{WorldID}" + " " +
$"ServerKey:{ServerKey}" + " " +
$"SessionKey:{SessionKey}" + " " +
$"AccessKey:{AccessKey}";
        }
    }

    public class RSA_KEY : IStreamSerializer
    {
        // 
        public uint[] key = new uint[MediusConstants.RSA_SIZE_DWORD];

        public RSA_KEY()
        {

        }

        public RSA_KEY(byte[] keyBytes)
        {
            for (int i = 0; i < key.Length; ++i)
            {
                //key[i] = (uint)((keyBytes[i + 0] << 24) | (keyBytes[i + 1] << 16) | (keyBytes[i + 2] << 8) | (keyBytes[i + 3]));
                key[i] = (uint)((keyBytes[i + 3] << 24) | (keyBytes[i + 2] << 16) | (keyBytes[i + 1] << 8) | (keyBytes[i + 0]));
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            for (int i = 0; i < key.Length; ++i)
                key[i] = reader.ReadUInt32();
        }

        public void Serialize(BinaryWriter writer)
        {
            for (int i = 0; i < key.Length; ++i)
                writer.Write(key[i]);
        }
    }

    public class MediusPlayerOnlineState : IStreamSerializer
    {
        public MediusPlayerStatus ConnectStatus;
        public int MediusLobbyWorldID;
        public int MediusGameWorldID;
        public string LobbyName; // WORLDNAME_MAXLEN
        public string GameName; // WORLDNAME_MAXLEN

        public void Deserialize(BinaryReader reader)
        {
            ConnectStatus = reader.Read<MediusPlayerStatus>();
            MediusLobbyWorldID = reader.ReadInt32();
            MediusGameWorldID = reader.ReadInt32();
            LobbyName = reader.ReadString(MediusConstants.WORLDNAME_MAXLEN);
            GameName = reader.ReadString(MediusConstants.WORLDNAME_MAXLEN);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(ConnectStatus);
            writer.Write(MediusLobbyWorldID);
            writer.Write(MediusGameWorldID);
            writer.Write(LobbyName, MediusConstants.WORLDNAME_MAXLEN);
            writer.Write(GameName, MediusConstants.WORLDNAME_MAXLEN);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
             $"ConnectStatus:{ConnectStatus}" + " " +
$"MediusLobbyWorldID:{MediusLobbyWorldID}" + " " +
$"MediusGameWorldID:{MediusGameWorldID}" + " " +
$"LobbyName:{LobbyName}" + " " +
$"GameName:{GameName}";
        }
    }

    public class MediusFile : IStreamSerializer
    {
        public string Filename; // MediusConstants.MEDIUS_FILE_MAX_FILENAME_LENGTH
        public byte[] ServerChecksum = new byte[MediusConstants.MEDIUS_FILE_CHECKSUM_NUMBYTES];
        public uint FileID;
        public uint FileSize;
        public uint CreationTimeStamp;
        public uint OwnerID;
        public uint GroupID;
        public ushort OwnerPermissionRWX;
        public ushort GroupPermissionRWX;
        public ushort GlobalPermissionRWX;
        public ushort ServerOperationID;

        public void Deserialize(BinaryReader reader)
        {
            // 
            Filename = reader.ReadString(MediusConstants.MEDIUS_FILE_MAX_FILENAME_LENGTH);
            ServerChecksum = reader.ReadBytes(MediusConstants.MEDIUS_FILE_CHECKSUM_NUMBYTES);
            FileID = reader.ReadUInt32();
            FileSize = reader.ReadUInt32();
            CreationTimeStamp = reader.ReadUInt32();
            OwnerID = reader.ReadUInt32();
            GroupID = reader.ReadUInt32();
            OwnerPermissionRWX = reader.ReadUInt16();
            GroupPermissionRWX = reader.ReadUInt16();
            GlobalPermissionRWX = reader.ReadUInt16();
            ServerOperationID = reader.ReadUInt16();
        }

        public void Serialize(BinaryWriter writer)
        {
            // 
            writer.Write(Filename, MediusConstants.MEDIUS_FILE_MAX_FILENAME_LENGTH);
            writer.Write(ServerChecksum);
            writer.Write(FileID);
            writer.Write(FileSize);
            writer.Write(CreationTimeStamp);
            writer.Write(OwnerID);
            writer.Write(GroupID);
            writer.Write(OwnerPermissionRWX);
            writer.Write(GroupPermissionRWX);
            writer.Write(GlobalPermissionRWX);
            writer.Write(ServerOperationID);
        }


        public override string ToString()
        {
            return $"Filename:{Filename}" + " " +
$"ServerChecksum:{BitConverter.ToString(ServerChecksum)}" + " " +
$"FileID:{FileID}" + " " +
$"FileSize:{FileSize}" + " " +
$"CreationTimeStamp:{CreationTimeStamp}" + " " +
$"OwnerID:{OwnerID}" + " " +
$"GroupID:{GroupID}" + " " +
$"OwnerPermissionRWX:{OwnerPermissionRWX}" + " " +
$"GroupPermissionRWX:{GroupPermissionRWX}" + " " +
$"GlobalPermissionRWX:{GlobalPermissionRWX}" + " " +
$"ServerOperationID:{ServerOperationID}";
        }
    }

    public class MediusFileAttributes : IStreamSerializer
    {


        public byte[] Description = new byte[MediusConstants.MEDIUS_FILE_MAX_DESCRIPTION_LENGTH];
        public uint LastChangedTimeStamp;
        public uint LastChangedByUserID;
        public uint NumberAccesses;
        public uint StreamableFlag;
        public uint StreamingDataRate;

        public virtual void Deserialize(BinaryReader reader)
        {
            // 
            Description = reader.ReadBytes(MediusConstants.MEDIUS_FILE_MAX_DESCRIPTION_LENGTH);
            LastChangedTimeStamp = reader.ReadUInt32();
            LastChangedByUserID = reader.ReadUInt32();
            NumberAccesses = reader.ReadUInt32();
            StreamableFlag = reader.ReadUInt32();
            StreamingDataRate = reader.ReadUInt32();
        }

        public virtual void Serialize(BinaryWriter writer)
        {
            // 
            writer.Write(Description);
            writer.Write(LastChangedTimeStamp);
            writer.Write(LastChangedByUserID);
            writer.Write(NumberAccesses);
            writer.Write(StreamableFlag);
            writer.Write(StreamingDataRate);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"Description:{Description}" + " " +
$"LastChangedTimeStamp:{LastChangedTimeStamp}" + " " +
$"LastChangedByUserID:{LastChangedByUserID}" + " " +
$"NumberAccesses:{NumberAccesses}" + " " +
$"StreamableFlag:{StreamableFlag}" + " " +
$"StreamingDataRate:{StreamingDataRate}";
        }
    }

    public class MediusGenericChatFilter : IStreamSerializer
    {


        public byte[] GenericChatFilterBitfield = new byte[MediusConstants.MEDIUS_GENERIC_CHAT_FILTER_BYTES_LEN];

        public void Deserialize(BinaryReader reader)
        {
            // 
            GenericChatFilterBitfield = reader.ReadBytes(MediusConstants.MEDIUS_GENERIC_CHAT_FILTER_BYTES_LEN);
        }

        public void Serialize(BinaryWriter writer)
        {
            // 
            writer.Write(GenericChatFilterBitfield);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"GenericChatFilterBitfield:{GenericChatFilterBitfield}";
        }
    }

    public class GameListFilter
    {
        public uint FieldID;
        public MediusGameListFilterField FilterField;
        public int BaselineValue;
        public MediusComparisonOperator ComparisonOperator;
        public uint Mask;

        public bool IsMatch(Game game)
        {
            if (game == null)
                return false;

            switch (FilterField)
            {
                case MediusGameListFilterField.MEDIUS_FILTER_GAME_LEVEL: return ComparisonOperator.Compare(BaselineValue, game.GameLevel & Mask);
                case MediusGameListFilterField.MEDIUS_FILTER_GENERIC_FIELD_1: return ComparisonOperator.Compare(BaselineValue, game.GenericField1 & Mask);
                case MediusGameListFilterField.MEDIUS_FILTER_GENERIC_FIELD_2: return ComparisonOperator.Compare(BaselineValue, game.GenericField2 & Mask);
                case MediusGameListFilterField.MEDIUS_FILTER_GENERIC_FIELD_3: return ComparisonOperator.Compare(BaselineValue, game.GenericField3 & Mask);
                case MediusGameListFilterField.MEDIUS_FILTER_GENERIC_FIELD_4: return ComparisonOperator.Compare(BaselineValue, game.GenericField4 & Mask);
                case MediusGameListFilterField.MEDIUS_FILTER_GENERIC_FIELD_5: return ComparisonOperator.Compare(BaselineValue, game.GenericField5 & Mask);
                case MediusGameListFilterField.MEDIUS_FILTER_GENERIC_FIELD_6: return ComparisonOperator.Compare(BaselineValue, game.GenericField6 & Mask);
                case MediusGameListFilterField.MEDIUS_FILTER_GENERIC_FIELD_7: return ComparisonOperator.Compare(BaselineValue, game.GenericField7 & Mask);
                case MediusGameListFilterField.MEDIUS_FILTER_GENERIC_FIELD_8: return ComparisonOperator.Compare(BaselineValue, game.GenericField8 & Mask);
                case MediusGameListFilterField.MEDIUS_FILTER_LOBBY_WORLDID: return ComparisonOperator.Compare(BaselineValue, game.Id & Mask);
                case MediusGameListFilterField.MEDIUS_FILTER_MAX_PLAYERS: return ComparisonOperator.Compare(BaselineValue, game.MaxPlayers & Mask);
                case MediusGameListFilterField.MEDIUS_FILTER_MIN_PLAYERS: return ComparisonOperator.Compare(BaselineValue, game.MinPlayers & Mask);
                case MediusGameListFilterField.MEDIUS_FILTER_PLAYER_COUNT: return ComparisonOperator.Compare(BaselineValue, game.PlayerCount & Mask);
                case MediusGameListFilterField.MEDIUS_FILTER_PLAYER_SKILL_LEVEL: return ComparisonOperator.Compare(BaselineValue, game.PlayerSkillLevel & Mask);
                case MediusGameListFilterField.MEDIUS_FILTER_RULES_SET: return ComparisonOperator.Compare(BaselineValue, game.RulesSet & Mask);
                default: return false;
            }
        }
    }
}
