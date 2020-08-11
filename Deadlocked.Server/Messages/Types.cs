using Deadlocked.Server.Stream;
using Org.BouncyCastle.Asn1.X509;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Deadlocked.Server.Messages
{
    public enum MediusCallbackStatus : int
    {
        MediusBeginSessionFailed = -1000,  ///< Session begin failed   
        MediusAccountAlreadyExists = -999,  ///< Account already exists, can not register with the same account name.
        MediusAccountNotFound = -998,  ///< Account name was not found.
        MediusAccountLoggedIn = -997,  ///< The account is marked as already being logged in to the system. 
        MediusEndSessionFailed = -996,  ///< Unable to properly end the session. 
        MediusLoginFailed = -995,  ///< Login failed. 
        MediusRegistrationFailed = -994,  ///< Registration failed. 
        MediusIncorrectLoginStep = -993,  ///< The login step was incorrect.  For example, login without having a session. 
        MediusAlreadyLeaderOfClan = -992,  ///< The user is already the leader of a clan, and can not be the leader of multiple clans.
        MediusWMError = -991,  ///< World Manager error.
        MediusNotClanLeader = -990,  ///< The player attempted some request that requires being the leader of the clan.
        MediusPlayerNotPrivileged = -989,  ///< The player is not privileged to make the request.  Typically, the user�s session has been destroyed, but is still connected to the server.
        MediusDBError = -988,  ///< An internal database error occurred.
        MediusDMEError = -987,  ///< A DME layer error.
        MediusExceedsMaxWorlds = -986,  ///< The maximum number of worlds has been exceeded.
        MediusRequestDenied = -985,  ///< The request has been denied.
        MediusSetGameListFilterFailed = -984,  ///< Setting the game list filter failed.
        MediusClearGameListFilterFailed = -983,  ///< Clearing the game list filter failed.
        MediusGetGameListFilterFailed = -982,  ///< Getting the game list filter failed.
        MediusNumFiltersAtMax = -981,  ///< The number of filters is at the maximum.
        MediusFilterNotFound = -980,  ///< The filter being referenced does not exist.
        MediusInvalidRequestMsg = -979,  ///< The request message was invalid.
        MediusInvalidPassword = -978,  ///< The specified password was invalid.
        MediusGameNotFound = -977,  ///< The game was not found.
        MediusChannelNotFound = -976,  ///< The channel was not found.
        MediusGameNameExists = -975,  ///< The game name already exists.
        MediusChannelNameExists = -974,  ///< The channel name already exists.
        MediusGameNameNotFound = -973,  ///< The game name was not found.
        MediusPlayerBanned = -972,  ///< The player has been banned.
        MediusClanNotFound = -971,  ///< The clan was not found.
        MediusClanNameInUse = -970,  ///< The clan name already exists.
        MediusSessionKeyInvalid = -969,  ///< Session key is invalid.
        MediusTextStringInvalid = -968,  ///< The text string is invalid.
        MediusFilterFailed = -967,  ///< The filtering failed.
        MediusFail = -966,  ///< General fail message.
        MediusFileInternalAccessError = -965,  ///< Medius File Services (MFS) Internal error.
        MediusFileNoPermissions = -964,  ///< Insufficient permissions for the  MFS request.
        MediusFileDoesNotExist = -963,  ///< The file requested in MFS does not exist.
        MediusFileAlreadyExists = -962,  ///< The file requested in MFS already exists.
        MediusFileInvalidFilename = -961,  ///< The filename is not valid in MFS.
        MediusFileQuotaExceeded = -960,  ///< The user�s quota has been exceeded.
        MediusCacheFailure = -959,  ///< The cache system had an internal failure.
        MediusDataAlreadyExists = -958,  ///< The data already exists.
        MediusDataDoesNotExist = -957,  ///< The data does not exist.
        MediusMaxExceeded = -956,  ///< A maximum count has been exceeded.
        MediusKeyError = -955,  ///< The key used is incorrect.
        MediusIncompatibleAppID = -954,  ///< The application ID is not compatible.
        MediusAccountBanned = -953,  ///< The account has been banned.
        MediusMachineBanned = -952,  ///< The machine has been banned.
        MediusLeaderCannotLeaveClan = -951,  ///< The leader of the clan can not leave.  Must disband instead.
        MediusFeatureNotEnabled = -950,  ///< The feature requested is not enabled.
        MediusDNASSignatureLoggedIn = -949,  ///< The same DNAS signature is already logged in.
        MediusWorldIsFull = -948,  ///< The world is full.  Unable to join.
        MediusNotClanMember = -947,  ///< The user is not a member of the clan.
        MediusServerBusy = -946,  ///< The server is busy.  Try again later.
        MediusNumGameWorldsPerLobbyWorldExceeded = -945,  ///< The maximum number of game worlds per lobby world has been exceeded.
        MediusAccountNotUCCompliant = -944,  ///< The account name is not UC compliant.
        MediusPasswordNotUCCompliant = -943,  ///< The password is not UC compliant.
        MediusGatewayError = -942,  ///< There is an internal gateway error.
        MediusTransactionCanceled = -941,  ///< The transaction has been cancelled.
        MediusSessionFail = -940,  ///< The session has failed.
        MediusTokenAlreadyTaken = -939,  ///< The token is already in use.
        MediusTokenDoesNotExist = -938,  ///< The token being referenced does not exist.
        MediusSubscriptionAborted = -937,  ///< The subscription has been aborted.
        MediusSubscriptionInvalid = -936,  ///< The subscription is invalid.
        MediusNotAMember = -935,  ///< The user is not a member of an list.
        MediusSuccess = 0,  ///< Success
        MediusNoResult = 1,  ///< No results.  This is a valid state.
        MediusRequestAccepted = 2,  ///< The request has been accepted.
        MediusWorldCreatedSizeReduced = 3,  ///< The world has been created with reduced size.
        MediusPass = 4,  ///< The criteria has been met.
    }

    public enum MediusAccountType : int
    {
        MediusChildAccount,  ///< Child account type.
        MediusMasterAccount,  ///< Master account type.
    }

    public enum MediusLadderType : int
    {
        MediusLadderTypePlayer = 0,  ///< Applies request to player ladders.
        MediusLadderTypeClan = 1,  ///< Applies request to clan ladders.
    }

    public enum MediusPlayerStatus : int
    {
        MediusPlayerDisconnected = 0, ///< Player is not connected.
        MediusPlayerInAuthWorld, ///< Player is currently on an authentication world.
        MediusPlayerInChatWorld, ///< Player is currently in a chat channel.
        MediusPlayerInGameWorld, ///< Player is currently in a game world.
        MediusPlayerInOtherUniverse, ///< Player is online in some other universe.
        LastMediusPLayerStatus,     ///< Reserved for internal use.
    }

    public enum MediusCharacterEncodingType : int
    {
        MediusCharacterEncoding_NoUpdate,  ///< No change to the current encoding.
        MediusCharacterEncoding_ISO8859_1,  ///< ISO-8859-1 single byte encoding 0x00 � 0xFF
        MediusCharacterEncoding_UTF8,  ///< UTF-8 multibyte encoding.
    }

    public enum MediusLanguageType : int
    {
        MediusLanguage_NoUpdate,  ///< No update to the language.  
        MediusLanguage_USEnglish,  ///< US English  
        MediusLanguage_UKEnglish,  ///< UK English  
        MediusLanguage_Japanese,  ///< Japanese
        MediusLanguage_Korean,  ///< Korean
        MediusLanguage_Italian,  ///< Italian
        MediusLanguage_Spanish,  ///< Spanish
        MediusLanguage_German,  ///< German
        MediusLanguage_French,  ///< French
        MediusLanguage_Dutch,  ///< Dutch
        MediusLanguage_Portuguese,  ///< Portuguese
        MediusLanguage_Chinese,  ///< Chinese
        MediusLanguage_Taiwanese,  ///< Taiwanese
        MediusLanguage_Finnish,  ///< Finnish
        MediusLanguage_Norwegian,  ///< Norwegian
    }

    public enum MediusClanStatus : int
    {
        ClanActive,  ///< The clan is active.
        ClanDisbanded = -1,  ///< The clan has been disbanded.
    }

    public enum MediusConnectionType : int
    {
        Modem = 0,  ///< The connection is on a modem.
        Ethernet = 1,  ///< The connection is on Ethernet.
        Wireless = 2,  ///< The connection is wireless.
    }

    public enum MediusDnasCategory : int
    {
        /** DNAS console ID. */
        DnasConsoleID,
        /** DNAS title ID. */
        DnasTitleID,
        /** DNAS disk ID. */
        DnasDiskID,
    }

    public enum MediusPlayerSearchType : int
    {
        PlayerAccountID, ///< Apply search using the account ID field.
        PlayerAccountName, ///< Apply search using the player name field.
    }

    public enum MediusBinaryMessageType : int
    {
        BroadcastBinaryMsg,             ///< send to all in given chat channel
        TargetBinaryMsg,       ///< send directly to another player
        BroadcastBinaryMsgAcrossEntireUniverse, ///< send to all in all given chat
                                                ///<  channels
    }

    public enum MediusWorldGenericFieldLevelType : int
    {
        MediusWorldGenericFieldLevel0 = 0, ///< no server-side filtering
        MediusWorldGenericFieldLevel1 = (1 << 0), ///< use only GenericField1
        MediusWorldGenericFieldLevel2 = (1 << 1), ///< use only GenericField2
        MediusWorldGenericFieldLevel3 = (1 << 2), ///< use only GenericField3
        MediusWorldGenericFieldLevel4 = (1 << 3), ///< use only GenericFiled4
        MediusWorldGenericFieldLevel12 = (1 << 4), ///< use 1 and 2
        MediusWorldGenericFieldLevel123 = (1 << 5), ///< use 1, 2, and 3
        MediusWorldGenericFieldLevel1234 = (1 << 6), ///< use 1, 2, 3, and 4
        MediusWorldGenericFieldLevel23 = (1 << 7), ///< use 2 and 3
        MediusWorldGenericFieldLevel234 = (1 << 8), ///< use 2, 3, and 4
        MediusWorldGenericFieldLevel34 = (1 << 9),///< use 3 and 4
    }

    public enum MediusApplicationType : int
    {
        MediusAppTypeGame,  ///< Game type
        LobbyChatChannel,  ///< Lobby chat channel type
    }

    public enum MediusTextFilterType : int
    {
        MediusTextFilterPassFail = 0, ///< Type of filtering: pass or fail
        MediusTextFilterReplace = 1, ///< Type of filtering: replace text with strike-out characters.
    }

    public enum MediusSortOrder : int
    {
        MEDIUS_ASCENDING, ///< Sort the list in a ascending order.
        MEDIUS_DESCENDING, ///< Sort the list in a descending order.
    }

    public enum MediusClanInvitationsResponseStatus : int
    {
        ClanInvitationUndecided,  ///< Status to join a clan is undecided.
        ClanInvitationAccept,  ///< Accept the invitation to the clan.
        ClanInvitationDecline,  ///< Decline the invitation to the clan.
        ClanInvitationRevoked,  ///< Revoke an outstanding invitation to a potential candidate.
    }

    public enum MediusPolicyType : int
    {
        Usage, ///< Usage policy
        Privacy, ///< Privacy policy
    }

    public enum MediusUserAction : int
    {
        KeepAlive, ///< Used to denote that the player is still online.
        JoinedChatWorld, ///< Sent when a player joins a chat world.
        LeftGameWorld, ///< Sent when a player leaves a game world.
    }

    public enum MediusJoinType :int
    {
        MediusJoinAsPlayer = 0,  ///< Join a game as a normal player.
        MediusJoinAsSpectator = 1,  ///< Join a game as a spectator.
        MediusJoinAsMassSpectator = 2,  ///< Join a game as a large scale spectator.
    }

    public enum MediusTimeZone : int
    {
        MediusTimeZone_IDLW = -1200,     ///< [GMT-12]  IDLW  International Date Line - West
        MediusTimeZone_HST = -1000,      ///< [GMT-10]  HST  Hawaiian Standard Time
        MediusTimeZone_AKST = -900,      ///< [GMT-09]  AKST  Alaska Standard Time
        MediusTimeZone_AKDT = -800,      ///< [GMT-08]  AKDT  Alaska Daylight Time
        MediusTimeZone_PST = -801,       ///< [GMT-08]  PST  Pacific Standard Time
        MediusTimeZone_PDT = -700,       ///< [GMT-07]  PDT  Pacific Daylight Time
        MediusTimeZone_MST = -701,       ///< [GMT-07]  MST  Mountain Standard Time
        MediusTimeZone_MDT = -600,       ///< [GMT-06]  MDT  Mountain Daylight Time
        MediusTimeZone_CST = -601,       ///< [GMT-06]  CST  Central Standard Time
        MediusTimeZone_CDT = -500,       ///< [GMT-05]  CDT  Central Daylight Time
        MediusTimeZone_EST = -501,       ///< [GMT-05]  EST  Eastern Standard Time
        MediusTimeZone_EDT = -400,       ///< [GMT-04]  EDT  Eastern Daylight Time
        MediusTimeZone_AST = -401,       ///< [GMT-04]  AST  Atlantic Standard Time
        MediusTimeZone_NST = -350,       ///< [GMT-03.5]  NST  Newfoundland Standard Time
        MediusTimeZone_ADT = -300,       ///< [GMT-03]  ADT  Atlantic Daylight Time
        MediusTimeZone_NDT = -250,       ///< [GMT-02.5]  NDT  Newfoundland Daylight Time
        MediusTimeZone_WAT = -100,       ///< [GMT-01]  WAT  West Africa Time
        MediusTimeZone_GMT = 0,          ///< [GMT+00]  GMT  Greenwich Mean Time
        MediusTimeZone_UTC = 1,          ///< [GMT+00]  UTC  Universal Time Coordinated
        MediusTimeZone_WET = 2,          ///< [GMT+00]  WET  Western Europe Time
        MediusTimeZone_BST = 100,        ///< [GMT+01]  BST  British Summer Time
        MediusTimeZone_IRISHST = 101,    ///< [GMT+01]  IRISHST  Irish Summer Time
        MediusTimeZone_WEST = 102,       ///< [GMT+01]  WEST  Western Europe Summer Time
        MediusTimeZone_CET = 103,        ///< [GMT+01]  CET  Central European Time
        MediusTimeZone_CEST = 200,       ///< [GMT+02]  CEST  Central European Summer Time
        MediusTimeZone_SWEDISHST = 201,  ///< [GMT+02]  SWEDISHST  Swedish Summer Time
        MediusTimeZone_FST = 202,        ///< [GMT+02]  FST  French Summer Time
        MediusTimeZone_CAT = 203,        ///< [GMT+02]  CAT  Central African Time
        MediusTimeZone_SAST = 204,       ///< [GMT+02]  SAST  South African Standard Time
        MediusTimeZone_EET = 205,        ///< [GMT+02]  EET  Eastern European Time 
        MediusTimeZone_ISRAELST = 206,   ///< [GMT+02]  ISRAELST  Israel Standard Time
        MediusTimeZone_EEST = 300,       ///< [GMT+03]  EEST  Eastern European Summer Time
        MediusTimeZone_BT = 301,         ///< [GMT+03]  BT  Baghdad Time
        MediusTimeZone_MSK = 302,        ///< [GMT+03]  MSK  Moscow Time 
        MediusTimeZone_IRANST = 350,     ///< [GMT+03.5]  IRANST  Iran Standard Time
        MediusTimeZone_MSD = 400,        ///< [GMT+04]  MSD  Moscow Summer Time
        MediusTimeZone_INDIANST = 550,   ///< [GMT+05.5]  INDIANST  Indian Standard Time
        MediusTimeZone_JT = 750,         ///< [GMT+07.5]  JT  Java Time
        MediusTimeZone_HKT = 800,        ///< [GMT+08]  HKT  Hong Kong Time
        MediusTimeZone_CCT = 801,        ///< [GMT+08]  CCT  China Coastal Time
        MediusTimeZone_AWST = 802,       ///< [GMT+08]  AWST  Australian Western Standard Time
        MediusTimeZone_MT = 850,         ///< [GMT+08.5]  MT  Moluccas Time
        MediusTimeZone_KST = 900,        ///< [GMT+09]  KST  Korea Standard Time
        MediusTimeZone_JST = 901,        ///< [GMT+09]  JST  Japan Standard Time
        MediusTimeZone_ACST = 950,       ///< [GMT+09.5]  ACST  Australian Central Standard Time
        MediusTimeZone_AEST = 1000,      ///< [GMT+10]  AEST  Australian Eastern Standard Time
        MediusTimeZone_GST = 1001,       ///< [GMT+10]  GST  Guam Standard Time
        MediusTimeZone_ACDT = 1050,      ///< [GMT+10.5]  ACDT  Australian Central Daylight Time
        MediusTimeZone_AEDT = 1100,      ///< [GMT+11]  AEDT  Australian Eastern Daylight Time
        MediusTimeZone_SST = 1101,       ///< [GMT+11]  SST  Solomon Standard Time
        MediusTimeZone_NZST = 1200,      ///< [GMT+12]  NZST  New Zealand Standard Time
        MediusTimeZone_IDLE = 1201,      ///< [GMT+12]  IDLE  International Date Line - East
        MediusTimeZone_NZDT = 1300,      ///< [GMT+13]  NZDT  New Zealand Daylight Time
    }

    public enum MediusGameHostType : int
    {
        MediusGameHostClientServer = 0,  ///< Create a client-server based game.
        MediusGameHostIntegratedServer = 1,  ///< Create a integrated server game where the game server and a client are on the same host.
        MediusGameHostPeerToPeer = 2,  ///< Host a peer-to-peer game.
        MediusGameHostLANPlay = 3,  ///< Host a LAN based game.
        MediusGameHostClientServerAuxUDP = 4,  ///< Host a client-server, auxiliary UDP game.
    }

    public enum MediusWorldAttributesType : int
    {
        GAME_WORLD_NONE = 0, ///< Default game world attributes.  Nothing special.
        GAME_WORLD_ALLOW_REBROADCAST = (1 << 0), ///< Indicates that this game world 
                                                 ///< supports connected spectator worlds
        GAME_WORLD_ALLOW_SPECTATOR = (1 << 1), ///< Indicates that this world is a 
                                               ///< spectator world
        GAME_WORLD_INTERNAL = (1 << 2), ///< Indicates that this world was generated 
                                        ///< internally, not by a client request
    }

    public enum MediusChatMessageType : int
    {
        Broadcast,                     ///< send to all in given chat channel
        Whisper,                       ///< send directly to another player
        BroadcastAcrossEntireUniverse, ///< send to all in all given chat channels
        MediusClanChatType,                ///< send chat to all members in a clan
        MediusBuddyChatType,       ///< send chat to all members in your buddy list
    }

    public enum MediusWorldStatus : int
    {
        WorldInactive, ///< Game world is not active.
        WorldStaging, ///< Players are staging in the game, but not yet playing.
        WorldActive, ///< Players are playing in the game world.
        WorldClosed, ///< Players are not allowed to join this game world.
        WorldPendingCreation,  ///< Set by server while creation is in progress
        WorldPendingConnectToGame,  ///< Set by server for spectator worlds only 
                                    ///< after creation while connection to host
                                    ///< game world is in progress
    }

    public enum MediusGameListFilterField : int
    {
        MEDIUS_FILTER_PLAYER_COUNT = 1,  ///< Filter based on the number of players in the game.
        MEDIUS_FILTER_MIN_PLAYERS = 2,  ///< Filter based on the minimum number of players for the game.
        MEDIUS_FILTER_MAX_PLAYERS = 3,  ///< Filter based on the maximum number of players for the game.
        MEDIUS_FILTER_GAME_LEVEL = 4,  ///< Filter based on the game level.
        MEDIUS_FILTER_PLAYER_SKILL_LEVEL = 5,  ///< Filter based on the advertised skill level for the game.
        MEDIUS_FILTER_RULES_SET = 6,  ///< Filter based on the rule set for the game.
        MEDIUS_FILTER_GENERIC_FIELD_1 = 7,  ///< Filter on generic field 1
        MEDIUS_FILTER_GENERIC_FIELD_2 = 8,  ///< Filter on generic field 2
        MEDIUS_FILTER_GENERIC_FIELD_3 = 9,  ///< Filter on generic field 3
        MEDIUS_FILTER_LOBBY_WORLDID = 10,  ///< Filter based on the lobby world ID that the game was created in.
        MEDIUS_FILTER_GENERIC_FIELD_4 = 11,  ///< Filter on generic field 4
        MEDIUS_FILTER_GENERIC_FIELD_5 = 12,  ///< Filter on generic field 5
        MEDIUS_FILTER_GENERIC_FIELD_6 = 13,  ///< Filter on generic field 6
        MEDIUS_FILTER_GENERIC_FIELD_7 = 14,  ///< Filter on generic field 7
        MEDIUS_FILTER_GENERIC_FIELD_8 = 15,  ///< Filter on generic field 8
    }

    public enum MediusWorldSecurityLevelType : int
    {
        WORLD_SECURITY_NONE = 0,       ///< No security on world
        WORLD_SECURITY_PLAYER_PASSWORD = (1 << 0), ///< Password required to 
                                                   ///< join as a player
        WORLD_SECURITY_CLOSED = (1 << 1), ///< World is closed to new 
                                          ///< players
        WORLD_SECURITY_SPECTATOR_PASSWORD = (1 << 2), ///< Password is required to 
                                                      ///< join as a spectator
    }

    public enum MediusLobbyFilterType : int
    {
        MediusLobbyFilterEqualsLobby = 0, ///< Lobby filtering rules.  Lobby&Filter = Lobby
        MediusLobbyFilterEqualsFilter = 1, ///< Lobby filtering rules.  Lobby&Filter = Filter
    }

    public enum MediusLobbyFilterMaskLevelType : int
    {
        MediusLobbyFilterMaskLevel0 = 0, ///< not using filter mask
        MediusLobbyFilterMaskLevel1 = (1 << 0),///< use only FilterMask1
        MediusLobbyFilterMaskLevel2 = (1 << 1),///< use only FilterMask2
        MediusLobbyFilterMaskLevel3 = (1 << 2),///< use only FilterMask3
        MediusLobbyFilterMaskLevel4 = (1 << 3),///< use only FilterMask4
        MediusLobbyFilterMaskLevel12 = (1 << 4),///< use 1 and 2
        MediusLobbyFilterMaskLevel123 = (1 << 5),///< use 1, 2 and 3
        MediusLobbyFilterMaskLevel1234 = (1 << 6),///< use 1, 2, 3, and 4
        MediusLobbyFilterMaskLevel23 = (1 << 7),///< use 2 and 3
        MediusLobbyFilterMaskLevel234 = (1 << 8),///< use 2, 3, and 4
        MediusLobbyFilterMaskLevel34 = (1 << 9),///< use 3 and 4
    }

    public enum MediusComparisonOperator : int
    {
        LESS_THAN,  ///< Less than comparison operator
        LESS_THAN_OR_EQUAL_TO,  ///< Less than or equal to comparison operator
        EQUAL_TO,  ///< Equal to comparison operator
        GREATER_THAN_OR_EQUAL_TO,  ///< Greater than or equal to comparison operator
        GREATER_THAN,  ///< Great than comparison operator
        NOT_EQUALS,  ///< Not equals comparison operator
    }

    public enum NetConnectionType : int
    {
        /**
 * (0) This value is used to specify that no information is present
 */
        NetConnectionNone = 0,

        /**
         * (1) This specifies a connection to a Server via TCP
         */
        NetConnectionTypeClientServerTCP = 1,

        /**
         * (2) This specifies a connection to another peer via UDP..
         */
        NetConnectionTypePeerToPeerUDP = 2,

        /**
         * (3) This specifies a connection to a Server via TCP and UDP.  The UDP connection is normal UDP: 
         * there is no reliability or in-order guarantee.
         */
        NetConnectionTypeClientServerTCPAuxUDP = 3,

        /**
         * (4) This specifies a connection to a Server via TCP.  This is reserved for SCE-RT "Spectator" functionality.
         */
        NetConnectionTypeClientListenerTCP = 4
    }

    public enum MGCL_EVENT_TYPE : int
    {
        /** A client disconnected from this game server.*/
        MGCL_EVENT_CLIENT_DISCONNECT = 0,

        /** A server connected to this game server.*/
        MGCL_EVENT_CLIENT_CONNECT = 1,
    }

    public enum MGCL_ALERT_LEVEL : int
    {
        /** SUCCESSFUL response. */
        MGCL_SUCCESS = 0,

        /** Connect terminated. */
        MGCL_CONNECTION_ERROR = -1,

        /** Unable to connect to a target host. */
        MGCL_CONNECTION_FAILED = -2,

        /** Unable to disconnect from a target host. */
        MGCL_DISCONNECT_FAILED = -3,

        /** Attempt to use an API call that requires a connection - without a connection. */
        MGCL_NOT_CONNECTED = -4,

        /** Sending of data failed. */
        MGCL_SEND_FAILED = -5,

        /** Initialization of the MGCL library failed. */
        MGCL_INITIALIZATION_FAILED = -6,

        /** Shutdown of the MGCL library failed. */
        MGCL_SHUTDOWN_ERROR = -7,

        /** A lower level network error occurred. */
        MGCL_NETWORK_ERROR = -8,

        /** Authentication of the MGCL host failed. This may be due to application ID or mismatched security keys. */
        MGCL_AUTHENTICATION_FAILED = -9,

        /** Session begin failed. */
        MGCL_SESSIONBEGIN_FAILED = -10,

        /** Session end failed. */
        MGCL_SESSIONEND_FAILED = -11,

        /** General request failed. */
        MGCL_UNSUCCESSFUL = -12,

        /** An invalid argument was used in a function call. */
        MGCL_INVALID_ARG = -13,

        /** Unable to access the NAT service or resolve the internal NAT address. */
        MGCL_NATRESOLVE_FAILED = -14,

        /** A game with the same name already exists. */
        MGCL_GAME_NAME_EXISTS = -15,

        /** The specified world ID is already in use. */
        MGCL_WORLDID_INUSE = -16,

        /** A lower level DME error has occurred. */
        MGCL_DME_ERROR = -17,

        /** An attempt was made to re-initialize MGCL without first closing the subsystem. */
        MGCL_CALL_MGCL_CLOSE_BEFORE_REINITIALIZING = -18,

        /** The maximum number of games within a lobby world was exceeded. */
        MGCL_NUM_GAME_WORLDS_PER_LOBBY_WORLD_EXCEEDED = -19,
    }

    public enum NetAddressType : int
    {
        /**
         * (0) This value is used to specify "Not in use"
         */
        NetAddressNone = 0,
        /**
         * (1) ASCII string representation of a client's public IPv4 address.
         */
        NetAddressTypeExternal = 1,
        /**
         * (2) ASCII string representation of a client's private IPv4 address.
         */
        NetAddressTypeInternal = 2,
        /**
         * (3) ASCII string representiation of a NAT resolution server's IPv4 address.
         */
        NetAddressTypeNATService = 3,
        /**
         * (4) 4-byte binary representation of a client's public IPv4 address.
         */
        NetAddressTypeBinaryExternal = 4,
        /**
         * (5) 4-byte binary representation of a client's private IPv4 address.
         */
        NetAddressTypeBinaryInternal = 5,
        /**
         * (6) 4-byte binary representation of a client's public IPv4 address.
         * The Port parameter contains a 2-byte virtual port in 2 high bytes and
         * the actual network port in the 2 low bytes.
         */
        NetAddressTypeBinaryExternalVport = 6,
        /**
         * (7) 4-byte binary representation of a client's public IPv4 address.
         * The Port parameter contains a 2-byte virtual port in 2 high bytes and
         * the actual network port in the 2 low bytes.
         */
        NetAddressTypeBinaryInternalVport = 7,
        /**
         * (8) Contains two 4-byte binary representations of NAT resolution servers
         * IPv4 addresses stored back to back.
         */
        NetAddressTypeBinaryNATServices = 8
    }

    public enum NetMessageTypes : int
    {
        /**
 * (1) Identifies messages used internally by the DME.
 */
        MessageClassDME,
        /**
         * (2) Identifies messages used by the Medius Lobby SDK.
         */
        MessageClassLobby,
        /**
         * (3) Identifies messages used by your game.
         */
        MessageClassApplication,
        /**
         * (4) Identifies messages used by the Medius Game Communications Library (MGCL).
         */
        MessageClassLobbyReport,
        /**
         * (5) Identifies additional messages used by the Medius Lobby SDK.
         */
        MessageClassLobbyExt,
        /**
         * (6) Identifies messages used during authentication.
         * (Deprecated)
         */
        MessageClassLobbyAuthentication,
        /**
         * (7) Used as an array allocation size. Must always be the <i>last</i> valid
         * value before ExtraNetMessageClass, not after.
         */
        MaxMessageClasses,
    }

    public enum MGCL_TRUST_LEVEL : int
    {
        /** This server is a trusted game server. */
        MGCL_TRUSTED = 0,

        /** This server is NOT a trusted game server. This is used for all peer-to-peer game hosts.*/
        MGCL_NOT_TRUSTED = 1,
    }

    public enum MGCL_GAME_HOST_TYPE : int
    {
        /** The game server is configured for client-server gaming. */
        MGCLGameHostClientServer = 0,

        /** The game server is configured for an integrated server with both game play and serving. */
        MGCLGameHostIntegratedServer = 1,

        /** The game server is configured for the host in a peer-to-peer game. */
        MGCLGameHostPeerToPeer = 2,

        /** This is the host of a LAN game. */
        MGCLGameHostLANPlay = 3,

        /** This game server is configured for a client-server auxilliary UDP gaming. */
        MGCLGameHostClientServerAuxUDP = 4,
    }

    public enum MGCL_ERROR_CODE : sbyte
    {

        /** SUCCESSFUL response. */
        MGCL_SUCCESS = 0,

        /** Connect terminated. */
        MGCL_CONNECTION_ERROR = -1,

        /** Unable to connect to a target host. */
        MGCL_CONNECTION_FAILED = -2,

        /** Unable to disconnect from a target host. */
        MGCL_DISCONNECT_FAILED = -3,

        /** Attempt to use an API call that requires a connection - without a connection. */
        MGCL_NOT_CONNECTED = -4,

        /** Sending of data failed. */
        MGCL_SEND_FAILED = -5,

        /** Initialization of the MGCL library failed. */
        MGCL_INITIALIZATION_FAILED = -6,

        /** Shutdown of the MGCL library failed. */
        MGCL_SHUTDOWN_ERROR = -7,

        /** A lower level network error occurred. */
        MGCL_NETWORK_ERROR = -8,

        /** Authentication of the MGCL host failed. This may be due to application ID or mismatched security keys. */
        MGCL_AUTHENTICATION_FAILED = -9,

        /** Session begin failed. */
        MGCL_SESSIONBEGIN_FAILED = -10,

        /** Session end failed. */
        MGCL_SESSIONEND_FAILED = -11,

        /** General request failed. */
        MGCL_UNSUCCESSFUL = -12,

        /** An invalid argument was used in a function call. */
        MGCL_INVALID_ARG = -13,

        /** Unable to access the NAT service or resolve the internal NAT address. */
        MGCL_NATRESOLVE_FAILED = -14,

        /** A game with the same name already exists. */
        MGCL_GAME_NAME_EXISTS = -15,

        /** The specified world ID is already in use. */
        MGCL_WORLDID_INUSE = -16,

        /** A lower level DME error has occurred. */
        MGCL_DME_ERROR = -17,

        /** An attempt was made to re-initialize MGCL without first closing the subsystem. */
        MGCL_CALL_MGCL_CLOSE_BEFORE_REINITIALIZING = -18,

        /** The maximum number of games within a lobby world was exceeded. */
        MGCL_NUM_GAME_WORLDS_PER_LOBBY_WORLD_EXCEEDED = -19,
    }

    public enum NetClientStatus : byte
    {
        /**
         * (0) No ClientStatus is available.
         */
        ClientStatusNone,
        /**
         * (1) Client is not connected.
         */
        ClientStatusNotConnected,
        /**
         * (2) Client is connected, but has not called NetJoin().
         */
        ClientStatusConnected,
        /**
         * (3) Client is in the process of joining, and is now receiving its first batch
         * of object and field updates.
         */
        ClientStatusJoining,
        /**
         * (4) The client is now fully synchronized with the game, and has received all
         * initial object creation callbacks, etc.
         */
        ClientStatusJoined,
        /**
         * (5) The client is fully joined and is <i>also</i> the Session Master.
         */
        ClientStatusJoinedSessionMaster,
    }

    public class NetAddress : IStreamSerializer
    {
        /**
         * Defines the type of address stored in the address array below.
         */
        public NetAddressType AddressType;
        /**
         * Blob of address information that is formatted according the
         * AddressType defined above.
         */
        public string Address; // NET_MAX_NETADDRESS_LENGTH
        /**
         * Little endian 2-byte port representation associated with the address
         * defined above or a 2-byte virtual port in the 2 high bytes and the 
         * 2-byte network port in the 2 low bytes.
         */
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
        /** Players online state */
        public MediusPlayerStatus ConnectStatus;
        /** Lobby world ID if the state is in a chat channel */
        public int MediusLobbyWorldID;
        /** Game world ID if the player is in a game. */
        public int MediusGameWorldID;
        /** Lobby world name. */
        public string LobbyName; // WORLDNAME_MAXLEN
        /** Game world name. */
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
        public byte[] Filename = new byte[MediusConstants.MEDIUS_FILE_MAX_FILENAME_LENGTH];
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
            Filename = reader.ReadBytes(MediusConstants.MEDIUS_FILE_MAX_FILENAME_LENGTH);
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
            writer.Write(Filename);
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
$"ServerChecksum:{ServerChecksum}" + " " +
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
}
