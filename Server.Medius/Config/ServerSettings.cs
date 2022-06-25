using RT.Cryptography;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using RT.Models;
using RT.Common;
using Server.Common.Logging;
using System.Net;

namespace Server.Medius.Config
{
    public class ServerSettings
    {
        /// <summary>
        /// How many milliseconds before refreshing the config.
        /// </summary>
        public int RefreshConfigInterval = 5000;

        /// <summary>
        /// Beta specific config.
        /// </summary>
        public BetaConfig Beta { get; set; } = new BetaConfig();

        /// <summary>
        /// Compatible application ids. Null means all are accepted.
        /// </summary>
        public int[] ApplicationIds { get; set; } = { 0 };

        #region PublicIp
        /// <summary>
        /// By default the server will grab its local ip.
        /// If this is set, it will use its public ip instead.
        /// </summary>
        public bool UsePublicIp { get; set; } = false;

        /// <summary>
        /// If UsePublicIp is set to true, allow overriding and skipping using dyndns's dynamic
        /// ip address finder, since it goes down often enough to throw exceptions
        /// </summary>
        public string PublicIpOverride { get; set; } = string.Empty;
        #endregion

        /// <summary>
        /// When a client attempts to log into a non-existent account,
        /// instead of returning account not found,
        /// create the account and log them in.
        /// Necessary for Central Station support.
        /// </summary>
        public bool CreateAccountOnNotFound { get; set; } = false;

        /// <summary>
        /// Time since last echo response before timing the client out.
        /// </summary>
        public int ClientTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Time since game created and host never connected to close the game world.
        /// </summary>
        public int GameTimeoutSeconds { get; set; } = 30;

        #region Server Echo
        /// <summary>
        /// Enable/Disable sending of Server Echo for games that don't implement this.
        /// </summary>
        public bool ServerEchoUnsupported { get; set; } = false;

        /// <summary>
        /// Number of seconds before the server should send an echo to the client.
        /// </summary>
        public int ServerEchoInterval { get; set; } = 10;
        #endregion

        /// <summary>
        /// Period of time when a client is moving between medius server components where the client object will be kept alive.
        /// </summary>
        public int KeepAliveGracePeriod { get; set; } = 8;

        /// <summary>
        /// Number of ticks per second.
        /// </summary>
        public int TickRate { get; set; } = 10;

        /// <summary>
        /// LocationID of this Medius Stack, applies to MAS, and MLS
        /// </summary>
        public int LocationID = 0;

        #region Enable Select Servers
        /// <summary>
        /// Enable MAPS Zipper Interactive Only
        /// </summary>
        public bool EnableMAPS { get; set; } = false;

        /// <summary>
        /// Enable MAS
        /// </summary>
        public bool EnableMAS { get; set; } = true;

        /// <summary>
        /// Enable MLS
        /// </summary>
        public bool EnableMLS { get; set; } = true;

        /// <summary>
        /// Enable MPS
        /// </summary>
        public bool EnableMPS { get; set; } = true;
        #endregion

        #region Ports
        /// <summary>
        /// Port of the MAPS server.
        /// </summary>
        public int MAPSPort { get; set; } = 10073;

        /// <summary>
        /// Port of the MAS server.
        /// </summary>
        public int MASPort { get; set; } = 10075;

        /// <summary>
        /// Port of the MLS server.
        /// </summary>
        public int MLSPort { get; set; } = 10078;

        /// <summary>
        /// Port of the MPS server.
        /// </summary>
        public int MPSPort { get; set; } = 10077;
        #endregion

        #region Medius Versions
        public bool MediusServerVersionOverride { get; set; } = false;

        public string MASVersion { get; set; } = "Medius Authentication Server Version 3.03.0000";

        public string MLSVersion { get; set; } = "Medius Lobby Server Version 3.03.0000";

        public string MPSVersion { get; set; } = "Medius Proxy Server Version 3.03.0000";

        public string MAPSVersion { get; set; } = "Medius Authorative Profile Server Version 3.03.0000";
        #endregion

        #region Medius Universe Manager Location
        /// <summary>
        /// Provide the IP, Port and WorldID of the MUM that will control this MLS
        /// (no valid defaults)
        /// </summary>
        public string MUMIp { get; set; } = null;
        public int MUMPort { get; set; } = 10076;
        public int MUMWorldID { get; set; } = 1;
        #endregion

        #region NAT SCE-RT Service Location
        /// <summary>
        /// Ip address of the NAT server.
        /// Provide the IP of the SCE-RT NAT Service
        /// Default is: natservice.pdonline.scea.com:10070
        /// </summary>
        public string NATIp { get; set; } = null;
        /// <summary>
        /// Port of the NAT server.
        /// Provide the Port of the SCE-RT NAT Service
        /// </summary>
        public int NATPort { get; set; } = 10070;
        #endregion

        #region Remote Log Viewer Port To Listen
        /// <summary>
        /// Any value greater than 0 will enable remote logging with the SCE-RT logviewer
        /// on that port, which must not be in use by other applications (default 0)
        /// </summary>
        public int RemoteLogViewPort = 0;
        #endregion

        /// <summary>
        /// Time, in seconds, before timing out a Dme server.
        /// </summary>
        public int DmeTimeoutSeconds { get; set; } = 60;

        #region System Message Test
        /// <summary>
        /// System Message Test
        /// This setting controls whether a single system message is sent
        /// to a user who starts a session. This tests handling of "You have
        /// been banned from the system!" type messages pushed from the server.
        /// 1 = Turned on
        /// 0 = Turned off
        /// </summary>
        public bool SystemMessageSingleTest { get; set; } = false;
        #endregion

        #region DNAS
        /// <summary>
        /// Enable posting of machine signature to database (1 = enable, 0 = disable)
        /// </summary>
        public bool DnasEnablePost { get; set; } = false;
        #endregion

        /// <summary>
        /// 'Severity' of the system message sent to notify the user has been banned.
        ///  This is game specific.
        /// </summary>
        public byte BanSystemMessageSeverity { get; set; } = 200;

        #region Medius File Services - File Server Configuration
        /// <summary>
        /// When true, will allow messages like MediusCreateFile, MediusUploadFile, MediusDownloadFile, MediusFileListFiles
        /// </summary>
        public bool AllowMediusFileServices { get; set; } = false;

        /// <summary>
        /// Set the hostname to the ApacheWebServerHostname
        /// </summary>
        public string MFSTransferURI { get; set; } = "http://192.168.1.86/";

        /// <summary>
        /// Root path of the medius file service directory.
        /// </summary>
        public string MediusFileServerRootPath { get; set; } = "MFSFiles";

        /// <summary>
        /// Max number of download requests in the download queue
        /// </summary>
        public int MFSDownloadQSize = 8192;

        /// <summary>
        /// Max number of upload requests in the download queue
        /// </summary>
        public int MFSUploadQSize = 8192;

        /// <summary>
        /// Time out interval for activity on upload/download
        /// requests, in seconds. Once timeout interval is
        /// exceeded, the request will be removed from queue.
        /// The reference time stamp gets updated when
        /// activity occurs on the request in queue
        /// </summary>
        public int MFSQueueTimeoutInterval = 360;
        #endregion

        #region Chat Channel Audio Headset Support
        /// <summary>
        /// Enable/Disable peer-to-peer Chat Channel Audio Headset Support (0 default)
        /// </summary>
        public bool EnableBroadcastChannel = false;
        #endregion

        #region PostDebugInfo
        /// <summary>
        /// Enable posting of debug information from the client 
        /// Set to false to disable, set to true to enable.            
        /// </summary>
        public bool PostDebugInfoEnable = true;
        #endregion

        #region Anti-Cheat (and related info)

        /// <summary>
        /// Set this to 1 to activate AntiCheat.  Or to 0 (the default) to
        /// deactivate it.
        /// </summary>
        public bool AntiCheatOn = false;

        #endregion

        #region MUCG Connection Attributes
        // (no valid defaults)
        // Uncomment MUCG params to enable connectivity to MUCG

        public string MUCGServerIP = "127.0.0.1";
        public int MUCGServerPort = 10072;
        public int MUCGWorldID = 1;
        #endregion

        #region Billing configuration
        /// <summary>        /// Billing functionality.
        /// Biling enabled settings: 0 = disabled, 1=SCEK, 2=SCEA, 3=SCEJ, 4=SCEE
        /// 
        ///  The following settings enable and configure billing. Setting BillingEnabled
        ///  to non-zero will enable billing support within the MAS service. 
        ///  # to non-zero will enable billing support within the MAS service. 
        /// If the Billing enabled is 1, then the supported module is for SCEK
        /// If the Billing enabled is 2, then the supported module is for SCEA
        /// If the Billing enabled is 3, then the supported module is for SCEJ
        /// If the Billing enabled is 4, then the supported module is for SCEE
        /// Note, the remaining fields have no meaning if billing is disabled. 
        /// The BillingServiceProvider identifies the billing hosting service and
        /// determines which billing plugin will be loaded. BillingProviderIpAddr and 
        /// BillingProviderPort configures access to the billing provider services.
        /// BillingSecurityLevel (0 | 1) determines whether or not encryption is used.
        /// BillingEntryPointReference should be set to the DNS value of the MAS service.
        /// </summary>
        public int BillingEnabled = 0;

        // Provider designation and plugin
        // Biling service provider settings: SCEK, SCERT
        public string BillingServiceProvider = "SCERT";

        public string BillingPluginPath = "./libSCERTBilling_unix.so";

        // IP Address and port of the billing service provider.
        // This is the SCE-RT Product Service if the billing provider is SCE-RT
        // Or the SCEK connection if the provider is SCEK
        public string BillingProviderIpAddr = "127.0.0.1";
        public int BillingServiceProviderPort = 2222;

        // Billing security settings
        public int BillingSecurityLevel = 0;
        public string BillingKeyPath = "./SCERTKey.txt";

        public string BillingEntryPointRef = "title.pdonline.scea.com";

        // Billing memory allocation scheme.
        public int BillingBucketCount = 5;
        public int BillingPreallocCount = 40;
        #endregion

        #region WMErrorHandling
        // Change API callback return status from WMError to something more useful in certain cases.
        // MediusJoinGame will return MediusGameNotFound if the game no longer exists
        // MediusJoinGame will return MediusWorldIsFull if the game has no open slots as defined by Medius.
        // MediusJoinChannel will return MediusChannelNotFound if the channel no longer exists.
        // MediusJoinChannel will return MediusWorldIsFull if the channel is full.
        // MediusGetGameInfo will return MediusGameNotFound if you pass in a game that either just got destroyed or doesn't exit.
        //MediusGetGamePlayers will return MediusGameNotFound if you pass in a game that doesn't exist.
        public bool WMErrorHandling = true;
        #endregion

        #region Clan 
        // Special configuration to allow for a non-clan leader to retrieve clan team challenges.
        // Set this to 1 to enable this override.
        // Or to 0 (the default) to maintain strict clan leader control.
        public bool EnableNonClanLeaderToGetTeamChallenges = false;

        // If enabled, allows for any member of a clan to post clan ladder scores via the API
        // MediusUpdateClanLadderStatsWide_Delta()
        public bool EnableClanLaddersDeltaOpenAccess = false;
        #endregion

        #region Keys
        /// <summary>
        /// Key used to authenticate dme servers.
        /// </summary>
        public RsaKeyPair MPSKey { get; set; } = new RsaKeyPair(
            new BigInteger("10315955513017997681600210131013411322695824559688299373570246338038100843097466504032586443986679280716603540690692615875074465586629501752500179100369237", 10),
            new BigInteger("17", 10),
            new BigInteger("4854567300243763614870687120476899445974505675147434999327174747312047455575182761195687859800492317495944895566174677168271650454805328075020357360662513", 10)
            );

        /// <summary>
        /// Key used to authenticate clients.
        /// </summary>
        public RsaKeyPair DefaultKey { get; set; } = new RsaKeyPair(
            new BigInteger("10315955513017997681600210131013411322695824559688299373570246338038100843097466504032586443986679280716603540690692615875074465586629501752500179100369237", 10),
            new BigInteger("17", 10),
            new BigInteger("4854567300243763614870687120476899445974505675147434999327174747312047455575182761195687859800492317495944895566174677168271650454805328075020357360662513", 10)
            );
        #endregion

        #region Server Key Type
        /// <summary>
        /// Whether or not to encrypt messages.
        /// 1 to enable encryption/security, 0 to disable (default 0)
        /// </summary>
        public bool EncryptMessages { get; set; } = true;
        #endregion

        #region Locations TEMP
        /// <summary>
        /// Collection of locations.
        /// </summary>
        public List<Location> Locations { get; set; }
        #endregion

        #region VULARITY FILTER
        /// <summary>
        /// Regex text filters for 
        /// </summary>
        public Dictionary<TextFilterContext, string> TextBlacklistFilters { get; set; } = new Dictionary<TextFilterContext, string>();

        #endregion

        /// <summary>
        /// Logging settings.
        /// </summary>
        public LogSettings Logging { get; set; } = new LogSettings();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="appId"></param>
        /// <returns></returns>
        public bool IsCompatAppId(int appId)
        {
            if (ApplicationIds == null)
                return true;

            return ApplicationIds.Contains(appId);
        }
    }

    #region BetaConfig
    public class BetaConfig
    {
        /// <summary>
        /// Whether the beta settings are enabled.
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Allow the creation of new accounts.
        /// </summary>
        public bool AllowAccountCreation { get; set; } = false;

        /// <summary>
        /// When true, only accounts in the whitelist will be allowed to login.
        /// </summary>
        public bool RestrictSignin { get; set; } = false;

        /// <summary>
        /// Accounts that can be logged into with RestrictSignIn set.
        /// </summary>
        public string[] PermittedAccounts { get; set; } = null;
    }
    #endregion

    #region Location
    public class Location
    {
        /// <summary>
        /// Id of location.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Name of location.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Name of respective channel.
        /// </summary>
        public string ChannelName { get; set; }

        /// <summary>
        /// Collection of all compatible app ids.
        /// </summary>
        public int[] AppIds { get; set; }

        /// <summary>
        /// Allows the creator of a lobby world to set the number of GenericFields to use as a generic lobby attribute (1, 2, 3, or 4).
        /// Relevant for server-side filtering.
        /// Notes: A lobby World must be created with the same filter level as the client that will be filtering on.
        /// </summary>
        public MediusWorldGenericFieldLevelType GenericFieldLevel { get; set; }
    }
    #endregion

    #region TextFilterContext
    public enum TextFilterContext
    {
        DEFAULT,
        ACCOUNT_NAME,
        CLAN_NAME,
        CLAN_MESSAGE,
        CHAT,
        GAME_NAME
    }
    #endregion
}
