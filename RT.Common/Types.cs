using System;
using System.Collections.Generic;
using System.Text;

namespace RT.Common
{
    #region RT_MSG_TYPE
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
        RT_MSG_CLIENT_APP_TO_PLUGIN,
        RT_MSG_SERVER_PLUGIN_TO_APP,
    }
    #endregion

    #region MediusCallbackStatus
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
        MediusBillingVerificationRequired = -934,
        MediusAccessLevelInsufficient = -933,
        MediusWorldClosed = -932,
        MediusTransactionTimedOut = -931,
     // MediusCASError = -931 //Zipper Interactive
        MediusStepSendFailed = -930,
        MediusMatchTypeNoMatch_DEPRECATED = -929,
        MediusMatchServerNotFound = -928,
        MediusMatchGameCreationFailed = -927,
        MediusGameListSortOperationFailed = -926,
        MediusNumSortCriteriaAtMax = -925,
        MediusSortCriteriaNotFound = -924,
        MediusEntitlementCheckFailed = -923,
        ExtraMediusCallbackStatus = -1,
        MediusSuccess = 0,
        MediusNoResult = 1,
        MediusRequestAccepted = 2,
        MediusWorldCreatedSizeReduced = 3,
        MediusPass = 4,
        MediusInQueue = 5,
        MediusJoinAssignedGame = 6,
        MediusMatchTypeHostGame = 7,
        MediusMatchTyoeReferral = 8,
        MediusAlreadyInLeastPopulatedChannel = 9,
        MediusVulgarityFound = 10,
        MediusMatchingInProgress = 11,
    }
    #endregion

    public enum MediusAccessLevelType : int
    {
        MEDIUS_ACCESSLEVEL_DEFAULT,
        MEDIUS_ACCESSLEVEL_PRIVILEGED,
        MEDIUS_ACCESSLEVEL_BILLING_VERIFIED,
        MEDIUS_ACCESSLEVEL_MODERATOR = 4,
        MEDIUS_ACCESSLEVEL_RESERVED_3 = 8,
        MEDIUS_ACCESSLEVEL_RESERVED_4 = 16,
        MEDIUS_ACCESSLEVEL_RESERVED_5 = 32,
        MEDIUS_ACCESSLEVEL_RESERVED_6 = 64,
        MEDIUS_ACCESSLEVEL_RESERVED_7 = 128,
        MEDIUS_ACCESSLEVEL_RESERVED_8 = 256,
        MEDIUS_ACCESSLEVEL_RESERVED_9 = 512,
        MEDIUS_ACCESSLEVEL_RESERVED_10 = 1024,
        MEDIUS_ACCESSLEVEL_RESERVED_11 = 2048,
        MEDIUS_ACCESSLEVEL_RESERVED_12 = 4096,
        MEDIUS_ACCESSLEVEL_RESERVED_13 = 8192,
        MEDIUS_ACCESSLEVEL_RESERVED_14 = 16384,
        MEDIUS_ACCESSLEVEL_RESERVED_16 = 65536,
        MEDIUS_ACCESSLEVEL_RESERVED_17 = 131072,
        MEDIUS_ACCESSLEVEL_RESERVED_18 = 262144,
        MEDIUS_ACCESSLEVEL_RESERVED_20 = 1048576,
        MEDIUS_ACCESSLEVEL_RESERVED_21 = 2097152,
        MEDIUS_ACCESSLEVEL_RESERVED_24 = 16777216,
        MEDIUS_ACCESSLEVEL_RESERVED_28 = 268435456,
        //MEDIUS_ACCESSLEVEL_ADMIN
    }

    public enum MediusPasswordType : int
    {
        MediusPasswordNotSet,
        MediusPasswordSet,
    }

    public enum MediusAccountType : int
    {
        MediusChildAccount,
        MediusMasterAccount,
    }

    public enum MediusBuddyAddType : int
    {
        /// <summary>
        /// Add User to your Buddy List,
        /// but without the requirement that the buddy see you on their list
        /// </summary>
        AddSingle,
        /// <summary>
        /// Request that each person appears on the other's buddy list.
        /// </summary>
        AddSymmetric,
        /// <summary>
        /// Placeholder to normalize the field size on different compilers.
        /// </summary>
        ExtraMediusAddType = 0xffffff
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
    #region MediusCharacterEncodingType
    public enum MediusCharacterEncodingType : int
    {
        /// <summary>
        /// No change to current encoding.
        /// </summary>
        MediusCharacterEncoding_NoUpdate,
        /// <summary>
        /// ISO-8859-1 single byte encoding 0x00 - 0xFF.
        /// </summary>
        MediusCharacterEncoding_ISO8859_1,
        /// <summary>
        /// UTF-8 Multibyte Encoding.
        /// </summary>
        MediusCharacterEncoding_UTF8,
        /// <summary>
        /// Placeholder to normalize the field size on different compilers
        /// </summary>
        ExtraMediusCharacterEncodingType = 0xffffff
    }
    #endregion

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

    #region MediusClanStatus
    /// <summary>
    /// Whether or not a clan is active
    /// </summary>
    public enum MediusClanStatus : int
    {
        /// <summary>
        /// The clan is active
        /// </summary>
        ClanActive,
        /// <summary>
        /// The chan has been disbanded
        /// </summary>
        ClanDisbanded = -1,
        /// <summary>
        /// Placeholder to normalize the field size on different compilers
        /// </summary>
        ExtraMediusClanStatus = 0xffffff
    }
    #endregion

    /// <summary>
    /// Specify which type of network connection is being used
    /// Note, the connection type is set during the initial session begin request.
    /// </summary>
    public enum MediusConnectionType : int
    {
        /// <summary>
        /// The connection is on a modem.
        /// </summary>
        Modem = 0,
        /// <summary>
        /// The connection is on a Ethernet.
        /// </summary>
        Ethernet = 1,
        /// <summary>
        /// The connection is wireless
        /// </summary>
        Wireless = 2,
    }

    /// <summary>
    /// Post the dnas signature for this application
    /// The DNAS category must correspond with the type of auth.dat file requested from SCEI.
    /// </summary>
    public enum MediusDnasCategory : int
    {
        /// <summary>
        /// DNAS Console ID
        /// </summary>
        DnasConsoleID,
        /// <summary>
        /// DNAS title ID
        /// </summary>
        DnasTitleID,
        /// <summary>
        /// DNAS disk ID
        /// </summary>
        DnasDiskID,
    }

    #region Medius Device Type
    /// <summary>
    /// Specifies a device type for Account I/O operations
    /// </summary>
    public enum MediusDeviceType : int
    {
        /// <summary>
        /// Use a Memory Card as the target
        /// </summary>
        MEDIUS_MEMCARD,
        /// <summary>
        /// Use the HDD as the target
        /// </summary>
        MEDIUS_HDD,
        /// <summary>
        /// Use Host0 as the target
        /// </summary>
        MEDIUS_HOST0,
        /// <summary>
        /// Placeholder to normalize the field size on different compilers
        /// </summary>
        ExtraMediusDeviceType = 0xffffff
    }
    #endregion

    #region MediusSCETerritory
    /// <summary>
    /// Identifies the appropriate TRC territory for this title, for memory card, and HDD-related operations
    /// </summary>
    public enum MediusSCETerritory : int
    {
        /// <summary>
        /// Sony Computer Entertainment, America
        /// </summary>
        SCEA,
        /// <summary>
        /// Sony Computer Entertainment, Europe
        /// </summary>
        SCEE,
        /// <summary>
        /// Sony Computer Entertainment, Japan
        /// </summary>
        SCEI,
        /// <summary>
        /// Third Party SCEA
        /// </summary>
        SCEA_THIRDPARTY,
        /// <summary>
        /// Third Party SCEE
        /// </summary>
        SCEE_THIRDPARTY,
        /// <summary>
        /// Third Party SCEI
        /// </summary>
        SCEI_THIRDPARTY,
        /// <summary>
        /// Placeholder to normalize the field size on different compilers
        /// </summary>
        ExtraSCETerritoryType = 0xffffff
    }
    #endregion

    #region MediusStoredConfirmationType
    /// <summary>
    /// Error Codes related to storage functions
    /// </summary>
    public enum MediusStoredConfirmationType : int
    {
        /// <summary>
        /// Stored Successfully
        /// </summary>
        MediusStoredSuccess,
        /// <summary>
        /// File not found
        /// </summary>
        MediusStoredFileNotFound = -1,
        /// <summary>
        /// Device not found
        /// </summary>
        MediusStoredDeviceNotFound = -2,
        /// <summary>
        /// Directory Not Found
        /// </summary>
        MediusStoredDirectoryNotFound = -3,
        /// <summary>
        /// File already exists
        /// </summary>
        MediusStoredItemAlreadyExists = -4,
        /// <summary>
        /// Placeholder to normalize the field size on different compilers
        /// </summary>
        ExtraMediusStoredConfirmationType = 0xffffff
    }
    #endregion

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
        BroadcastBinaryMsgDeprecated0,
        TargetBinaryMsgDeprecated0,
        BroadcastBinaryMsgAcrossEntireUniverseDeprecated0
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

    /// <summary>
    /// Whether a text string submitted to a MediusTextFilter() call should be pass/fail or search-and-replace
    /// </summary>
    public enum MediusTextFilterType : int
    {   
        /// <summary>
        /// Type of Filtering: Pass or fail.
        /// </summary>
        MediusTextFilterPassFail = 0,
        /// <summary>
        /// Type of filtering: replace text with strike-out characters.
        /// </summary>
        MediusTextFilterReplace = 1,
        /// <summary>
        /// Placeholder to normalize the field size on different compilers
        /// </summary>
        ExtraMediusTextFilter = 0xffffff
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

    /// <summary>
    /// Medius Time Zones
    /// </summary>
    public enum MediusTimeZone : int
    {
        /// <summary>
        /// [GMT-12] IDLW International Date Line - West
        /// </summary>
        MediusTimeZone_IDLW = -1200,
        /// <summary>
        /// [GMT-10] Hawaiian Standard Time
        /// </summary>
        MediusTimeZone_HST = -1000,
        MediusTimeZone_AKST = -900,
        MediusTimeZone_AKDT = -800,
        /// <summary>
        /// [GMT-8] Pacific Standard Time
        /// </summary>
        MediusTimeZone_PST = -801,
        /// <summary>
        /// [GMT-7] Pacific Daylight Time
        /// </summary>
        MediusTimeZone_PDT = -700,
        /// <summary>
        /// [GMT-7] Mountain Standard Time
        /// </summary>
        MediusTimeZone_MST = -701,
        /// <summary>
        /// [GMT-6] Mountain Daylight Time
        /// </summary>
        MediusTimeZone_MDT = -600,
        /// <summary>
        /// [GMT-6] Central Standard Time
        /// </summary>
        MediusTimeZone_CST = -601,

        /// <summary>
        /// [GMT-5] Central Daylight Time
        /// </summary>
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

    #region MediusFindWorldType
    /// <summary>
    /// Search types for finding chat channels and/or game worlds.
    /// </summary>
    public enum MediusFindWorldType : int
    {
        /// <summary>
        /// Search for a game world with these parameters
        /// </summary>
        FindGameWorld,
        /// <summary>
        /// Search for a lobby chat world with these parameters
        /// </summary>
        FindLobbyWorld,
        /// <summary>
        /// Search for either a game or lobby world with these parameters
        /// </summary>
        FIndAllWorlds,
        /// <summary>
        /// Placeholder to normalize the field size on different compilers
        /// </summary>
        ExtraMediusFindWorldType
    }
    #endregion

    #region MediusGameHostType
    /// <summary>
    /// Defines which host type of game is being described
    /// </summary>
    public enum MediusGameHostType : int
    {
        /// <summary>
        /// Create a client-server based game.
        /// </summary>
        MediusGameHostClientServer = 0,

        /// <summary>
        /// Create a integrated server game where the game server and a client are on the same host.
        /// </summary>
        MediusGameHostIntegratedServer = 1,

        /// <summary>
        /// Host a peer-to-peer game.
        /// </summary>
        MediusGameHostPeerToPeer = 2,

        /// <summary>
        /// Host a LAN based game.
        /// </summary>
        MediusGameHostLANPlay = 3,

        /// <summary>
        /// Host a client-server, auxiliary UDP game.
        /// </summary>
        MediusGameHostClientServerAuxUDP = 4,

        /// <summary>
        /// Host a client-server, primary UDP game.
        /// </summary>
        MediusGameHostClientServerUDP = 5,

        /// <summary>
        /// 
        /// </summary>
        MediusGameHostIndependent = 6,

        /// <summary>
        /// Game Hosts are at Max
        /// </summary>
        MediusGameHostMax = 7,
    }
    #endregion

    #region MediusVoteActionType
    /// <summary>
    /// Enumeration used to identify action of MediusVoteToBanPlayer Request (add/remove)
    /// </summary>
    public enum MediusVoteActionType : int
    {
        /// <summary>
        /// Invalid Vote Action
        /// </summary>
        MediusInvalidVoteAction,
        /// <summary>
        /// Add a vote to ban a player
        /// </summary>
        MediusAddVote,
        /// <summary>
        /// Remove a vote to ban a player
        /// </summary>
        MediusRemoveVote,
        /// <summary>
        /// Placeholder to normalize the field size on different compilers
        /// </summary>
        ExtraMediusVoteActionType = 0xffffff
    }
    #endregion

    public enum MediusWorldAttributesType : int
    {
        GAME_WORLD_NONE = 0,
        GAME_WORLD_ALLOW_REBROADCAST = (1 << 0),
        GAME_WORLD_ALLOW_SPECTATOR = (1 << 1),
        GAME_WORLD_INTERNAL = (1 << 2),
    }

    #region MediusChatMessageType
    /// <summary>
    /// As of 2.10, MediusBuddyChatType is not supported yet. BroadcastAcrossEntireUniverse is usable, but highly discouraged.
    /// Special server side flags needed to enable this type of chat message due to the high load it represents
    /// </summary>
    public enum MediusChatMessageType : int
    {
        /// <summary>
        /// Sends to all in given chat channel
        /// </summary>
        Broadcast,
        /// <summary>
        /// Sends directly to another player
        /// </summary>
        Whisper,
        /// <summary>
        /// Sends to all given chat channels
        /// </summary>
        BroadcastAcrossEntireUniverse,
        /// <summary>
        /// Sends to all members of a clan
        /// </summary>
        MediusClanChatType,
        /// <summary>
        /// Sends chat to all members in your buddy list
        /// </summary>
        MediusBuddyChatType,
        /// <summary>
        /// Placeholder to normalize the field size on different compilers
        /// </summary>
        ExtraMediusChatMessageType = 0xffffff
    }
    #endregion
    
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

    #region MediusGenerateRandomSelection
    /// <summary>
    /// Generate / Do not generate flags for random name generation during acccount logins.
    /// NOTES: Deprecated as of 2.10, will be removed in a future release of the API.
    /// </summary>
    public enum MediusGenerateRandomSelection : int
    {
        /// <summary>
        /// Do Not generate a random name on login.
        /// </summary>
        NotGenerate = 0,
        /// <summary>
        /// Generate a random name for login.
        /// </summary>
        GenerateRandom = 100,
        /// <summary>
        /// Placeholder to normalize the field size on different compilers
        /// </summary>
        ExtraMediusGenerateRandomSelection
    }
    #endregion

    #region MediusWorldSecurityLevelType
    /// <summary>
    /// Security level for a world. Determines if passwords are needed.
    /// </summary>
    [Flags]
    public enum MediusWorldSecurityLevelType : int
    {
        /// <summary>
        /// No security on world
        /// </summary>
        WORLD_SECURITY_NONE = 0,
        /// <summary>
        /// Password required to join as a player
        /// </summary>
        WORLD_SECURITY_PLAYER_PASSWORD = (1 << 0),
        /// <summary>
        /// World is closed to new players
        /// </summary>
        WORLD_SECURITY_CLOSED = (1 << 1),
        /// <summary>
        /// Password is required to join as a spectator
        /// </summary>
        WORLD_SECURITY_SPECTATOR_PASSWORD = (1 << 2),
        /// <summary>
        /// Placeholder to normalize the field size on different compilers
        /// </summary>
        WORLD_SECURITY_EXTRA = 0xFFFFFF
    }
    #endregion

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

    #region NetConnectionType
    public enum NetConnectionType : int
    {
        /// <summary>
        /// This value is used to specify that no information is present
        /// </summary>
        NetConnectionNone = 0,

        /// <summary>
        /// This specifies a connection to a Server via TCP
        /// </summary>
        NetConnectionTypeClientServerTCP = 1,

        /// <summary>
        /// This specifies a connection to another peer via UDP
        /// </summary>
        NetConnectionTypePeerToPeerUDP = 2,

        /// <summary>
        /// This specifies a connection to a Server via TCP and UDP.  The UDP connection is normal UDP: 
        /// there is no reliability or in-order guarantee.
        /// </summary>
        NetConnectionTypeClientServerTCPAuxUDP = 3,

        /// <summary>
        /// This specifies a connection to a Server via TCP.  This is reserved for SCE-RT "Spectator" functionality.
        /// </summary>
        NetConnectionTypeClientListenerTCP = 4,

        /// <summary>
        /// This specifies a connection to a Server via UDP
        /// </summary>
        NetConnectionTypeClientServerUDP = 5,

        /// <summary>
        /// This specifies a connection to a Server via TCP and UDP.  This is reserved for SCE-RT "Spectator" functionality.
        /// </summary>
        NetConnectionTypeClientListenerTCPAuxUDP = 6,

        /// <summary>
        ///  This specifies a connection to a Server via UDP.  This is reserved for SCE-RT "Spectator" functionality.
        /// </summary>
        NetConnectionTypeClientListenerUDP = 7,

        /// <summary>
        /// Ensures that all values are stored as 32-bit integers on all compilers.
        /// </summary>
        ExtraNetConnectionType = 0xffffff
    }
    #endregion

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

    #region NetMessageTypes
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
    #endregion

    #region MediusDmeMessageIds
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
        LANRawMessage = 0x22,

        //
        ClientUpdate = 0x15,
    }
    #endregion

    #region MediusMGCLMessageIds
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
        ServerCreateGameOnSelfRequest0 = 0x0F,
        ServerCreateGameOnMeResponse = 0x10,
        ServerEndGameOnMeRequest = 0x11,
        ServerEndGameOnMeResponse = 0x12,
        ServerMoveGameWorldOnMeRequest = 0x14,
        ServerMoveGameWorldOnMeResponse = 0x15,
        ServerSetAttributesRequest = 0x16,
        ServerSetAttributesResponse = 0x17,
        ServerCreateGameWithAttributesRequest = 0x18,
        ServerCreateGameWithAttributesResponse = 0x19,
        ServerSessionBeginRequest1 = 0x21,
        ServerSessionBeginResponse1 = 0x22,
        ServerConnectGamesRequest = 0x1A,
        ServerConnectGamesResponse = 0x1B,
        ServerConnectNotification = 0x1C,
        ServerCreateGameOnSelfRequest = 0x1D,
        ServerDisconnectPlayerRequest = 0x1E,
        ServerCreateGameOnMeRequest = 0x1F,
        ServerWorldReportOnMe = 0x20,
    }
    #endregion

    #region MediusLobbyMessageIds
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
        AccountUpdatePasswordStatusResponse = 0x10,
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
        StatusResponse0 = 0xA4,
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
        UtilAddGameWorldRequest = 0xF7,
        UtilAddGameWorldResponse = 0xF8,
        UtilUpdateLobbyWorldRequest = 0xF9,
        UtilUpdateLobbyWorldResponse = 0xFA,
        UtilUpdateGameWorldRequest = 0xFB,
        UtilUpdateGameWorldStatusResponse = 0xFC,
    }
    #endregion

    #region MediusLobbyExtMessageIds
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
        VoteToBanPlayer = 0x2C,
        SetAutoChatHistoryRequest = 0x2D,
        SetAutoChatHistoryResponse = 0x2E,
        CreateGame = 0x2F,
        WorldReport = 0x30,
        ClearGameListFilter = 0x31,
        GetGameListFilterResponse = 0x32,
        SetGameListFilter = 0x33,
        SetGameListFilterResponse = 0x34,
        GameInfo = 0x35,
        GameInfoResponse = 0x36,
        GameList_ExtraInfo = 0x37,
        GameList_ExtraInfoResponse = 0x38,
        AccountUpdateStats_OpenAccess = 0x39,
        AccountUpdateStats_OpenAccessResponse = 0x3A,
        AddPlayerToClan_ByClanOfficer = 0x3B,
        AddPlayerToClan_ByClanOfficerResponse = 0x3C,

        // PS3
        CrossChatMessage = 0x3D,
        CroxxChatFwdMessage = 0x3E,
        QueueUpdateMessage = 0x3F,
        QueueCompleteMessage = 0x40,
        GetAccessLevelInfoRequest = 0x41,
        GetAccessLevelInfoResponse = 0x42,
        AccessLevelInfoUnsolicitedResponse = 0x43,

        UtilGetTotalGamesFilteredRequest = 0x48,
        UtilGetTotalGamesFilteredResponse = 0x49,
        AccountLoginRequest1 = 0x4A,
        AccountLoginResponse1 = 0x4B,
        AccountLoginRequest2 = 0x4C,
        AccountLoginResponse2 = 0x4D,
        AddAliasRequest = 0x4E,
        AddAliasResponse = 0x4F,
        DeleteAliasRequest = 0x50,
        DeleteAliasResponse = 0x51,
        GetMyAliasesRequest = 0x52,
        GetMyAliasesResponse = 0x53,
        BuddySetListRequest = 0x54,
        BuddySetListResponse = 0x55,
        IgnoreSetListRequest = 0x56,
        IgnoreSetListResponse = 0x57,
        TicketLogin = 0x58,
        TicketLoginResponse = 0x59,
        SetLocalizationParamsRequest2 = 0x5A,
        BinaryMessage1 = 0x5B,
        BinaryFwdMessage1 = 0x5C,
        MatchGetSupersetListRequest = 0x5D,
        MatchGetSupersetListResponse = 0x5E,
        GetBuddyInvitationsSentResponse = 0x61,
        GetBuddyInvitationsSentResponse1 = 0x62,
        AddToBuddyListConfirmationRequest = 0x63,
        AddToBuddyListConfirmationResponse = 0x64,
        PartyCreateRequest = 0x65,
        PartyCreateResponse = 0x66,

        GameListRequest = 0x69,
        PartyListResponse = 0x6A,
        PlayerIgnoresMeRequest = 0x6B,
        PlayerIgnoresMeResponse = 0x6C,
        NpIdPostRequest = 0x6D,
        StatusResponse_0 = 0x6E,
        MediusNpIdsGetByAccountNamesRequest = 0x6F,
        MediusNpIdsGetByAccountNamesResponse = 0x70,
        JoinLeastPopulatedChannelRequest = 0x71,
        JoinLeastPopulatedChannelResponse = 0x72,
        GenericChatMessage1 = 0x73,
        GenericChatFwdMessage1 = 0x74,
        MediusTextFilter1 = 0x75,
        MediusTextFilterResponse1 = 0x76,
        GetMyClanMessagesResponse = 0x77,
        MatchPartyRequest = 0x78,
        MatchSetGameStateRequest = 0x79,
        MatchSetGameStateStatusResponse = 0x7A,
        SetLocalizationParamsRequest1 = 0x7B,
        ClanRenameRequest = 0x7C,
        ClanRenameStatusResponse = 0x7D,
        SetGameListSortRequest = 0x7E,
        SetGameListSortResponse = 0x7F,
        GetGameListSortRequest = 0x80,
        GetGameListSortResponse = 0x81,
        ClearGameListSortRequest = 0x82,
        ClearGameListSortStatusResponse = 0x83,
        SetGameListSortPriorityRequest = 0x84,
        SetGameListSortPriorityStatusResponse = 0x85,
        SetLobbyWorldFilter1 = 0x86,
        SetLobbyWorldFilterResponse1 = 0x87, //PartyJoinByIndexRequest
        MatchFindGameRequest = 0x88,
        MatchFindGameStatusResponse = 0x89,
        AssignedGameToJoinMessage = 0x8A,
        SessionBegin1 = 0x8B,
        UpdateChannelRequest = 0x8C,
        UpdateChannelResponse = 0x8D,
        PartyJoinByIndexResponse = 0x8E,
        PartyPlayerReport = 0x8F,
        GroupJoinChannelRequest = 0x90,
        AssignedLobbyToJoinMessage = 0x91,
        MatchCancelRequest = 0x92,
        MatchCancelStatusResponse = 0x93,
        MatchCreateGameRequest = 0x94,
        MatchCreateGameResponse = 0x95,
        PartyList_ExtraInfoRequest = 0x96,
        PartyList_ExtraInfoResponse = 0x97,
        UnkR2Request = 0xA1,
        UnkR2Response = 0xA2,
        SetLocalizationParamsStatusResponse1 = 0xA4,
    }
    #endregion

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
    #region Medius Unified Community Gateway
    public enum MUCG_MessageTypes : byte
    {
        MUCG_MsgVersion = 0,
        MUCG_MsgError = 1,
        MUCG_MsgLobbyInitialize = 2,
        MUCG_MsgLobbyAcceptance = 3,
        MUCG_MsgReqConnectedAccounts = 4,
        MUCG_MsgAccountAdd = 5,
        MUCG_MsgAccountConnect = 6,
        MUCG_MsgAccountDisconnect = 7,
        MUCG_MsgAccountRemove = 8,
        MUCG_MsgReqBuddyListPresence = 9,
        MUCG_MsgReqAccountPresence = 10,
        MUCG_MsgC, at = 11,
        MUCG_MsgPresence = 12,
        MUCG_MsgBuddyAdd = 13,
        MUCG_MsgBuddyRemove = 14,
        MUCG_MsgSync = 15,
        MUCG_MsgRecoverMode = 16,
        MUCG_MsgRecoverAccount = 17,
        MUCG_MsgProcessSync = 18,
        MUCG_MsgEventOnOffline = 19,
        MUCG_MsgEventBuddyListMod = 20,
        MUCG_MsgClanAdd = 21,
        MUCG_MsgClanRemove = 22,
        MUCG_MsgClanMemberAdd = 23,
        MUCG_MsgClanMemberRemove = 24,
        MUCG_MsgClanInviteAdd = 25,
        MUCG_MsgClanInviteRemove = 26,
        MUCG_MsgClanChat = 27,
        MUCG_MsgReqClanInvites = 28,
        MUCG_MsgClanInvite = 29,
        MUCG_MsgReqClanMembers = 30,
        MUCG_MsgEventClanMemberListMod = 31,
        MUCG_MsgEventClanInviteListMod = 32,
        MUCG_MsgEventClanDisband = 33,
        MUCG_MsgIgnoreAdd = 34,
        MUCG_MsgIgnoreRemove = 35,
        MUCG_MsgReqIgnoreListIDs = 36,
        MUCG_MsgReqClanIDs = 37,
        MUCG_MsgClanID = 38,
        MUCG_Msg_End = 39,
    }

    public enum MUCG_RESULT : byte
    {
        MUCG_RESULT_OK = 0,
        MUCG_RESULT_CONNECT_INIT_FAILED = 1,
        MUCG_RESULT_CONNECT_FAILED = 2,
        MUCG_RESULT_RT_MSG_ERROR = 3,
        MUCG_RESULT_DISCONNECT_FAILURE = 4,
        MUCG_RESULT_UPDATE_FAILURE = 5,
        MUCG_RESULT_INCOMPATIBLE_VERSION = 6,
        MUCG_RESULT_PARSE_ERROR = 7,
        MUCG_RESULT_PACK_ERROR = 8,
        MUCG_RESULT_INVALID_PARAMETER = 9,
        MUCG_RESULT_DATA_PERSIST_FAILED = 10,
        MUCG_RESULT_DATA_NOT_READY = 11,
        MUCG_RESULT_GATEWAY_ERROR = 12,
        MAX_MUCG_RESULT = 13
    }
    #endregion

    #region Anti-Cheat
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
    #endregion

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

    #region MediusTokenCategoryType
    /// <summary>
    /// Enumeration used to identify category of a MediusToken
    /// </summary>
    public enum MediusTokenCategoryType : int
    {
        /// <summary>
        /// Invalid token category
        /// </summary>
        MediusInvalidToken = 0,
        /// <summary>
        /// Generic token category 1
        /// </summary>
        MediusGenericToken1 = 1,
        /// <summary>
        /// Generic token category 2
        /// </summary>
        MediusGenericToken2 = 2,
        /// <summary>
        /// Generic token category 3
        /// </summary>
        MediusGenericToken3 = 3,
        /// <summary>
        /// Token Assosciated with the account
        /// </summary>
        MediusAccountToken = 4,
        /// <summary>
        /// Token associated with a clan
        /// </summary>
        MediusClanToken = 5,
        /// <summary>
        /// Placeholder to normalize the field size on different compilers
        /// </summary>
        ExtraMediusTokenCategoryType = 0xffffff
    }
    #endregion

    public enum MediusFileXferStatus : int
    {
        Error = 0,
        Initial = 1,
        Mid = 2,
        End = 3
    }
}