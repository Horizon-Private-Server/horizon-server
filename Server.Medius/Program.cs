using DotNetty.Common.Internal.Logging;
using Microsoft.Extensions.Logging.Console;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using RT.Models;
using Server.Medius.Models;
using Server.Database;
using Server.Medius.Config;
using NReco.Logging.File;
using Server.Common.Logging;
using Server.Plugins;
using Server.Common;
using Haukcode.HighResolutionTimer;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Reflection;
using System.Management;

namespace Server.Medius
{
    public class Program
    {
        public const string CONFIG_FILE = "config.json";
        public const string DB_CONFIG_FILE = "db.config.json";
        public const string PLUGINS_PATH = "plugins/"; 

        public static RSA_KEY GlobalAuthPublic = null;

        public static ServerSettings Settings = new ServerSettings();
        public static DbController Database = new DbController(DB_CONFIG_FILE);

        public static IPAddress SERVER_IP;
        public static string IP_TYPE;

        public static MediusManager Manager = new MediusManager();
        public static PluginsManager Plugins = null;

        public static MAPS ProfileServer = new MAPS();
        public static MAS AuthenticationServer = new MAS();
        public static MLS LobbyServer = new MLS();
        public static MPS ProxyServer = new MPS();

        public static int TickMS => 1000 / (Settings?.TickRate ?? 10);

        private static FileLoggerProvider _fileLogger = null;
        private static ulong _sessionKeyCounter = 0;
        private static int sleepMS = 0;
        private static readonly object _sessionKeyCounterLock = (object)_sessionKeyCounter;
        private static DateTime? _lastSuccessfulDbAuth = null;
        private static DateTime _lastConfigRefresh = Utils.GetHighPrecisionUtcTime();
        private static DateTime _lastComponentLog = Utils.GetHighPrecisionUtcTime();

        private static int _ticks = 0;
        private static Stopwatch _sw = new Stopwatch();
        private static HighResolutionTimer _timer;

        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<Program>();

        static async Task TickAsync()
        {
            try
            {
#if DEBUG || RELEASE
                if (!_sw.IsRunning)
                    _sw.Start();
#endif

#if DEBUG || RELEASE
                ++_ticks;
                if (_sw.Elapsed.TotalSeconds > 5f)
                {
                    // 
                    _sw.Stop();
                    float tps = _ticks / (float)_sw.Elapsed.TotalSeconds;
                    float error = MathF.Abs(Settings.TickRate - tps) / Settings.TickRate;

                    if (error > 0.1f)
                        Logger.Error($"Average TPS: {tps} is {error * 100}% off of target {Settings.TickRate}");

                    //var dt = DateTime.UtcNow - Utils.GetHighPrecisionUtcTime();
                    //if (Math.Abs(dt.TotalMilliseconds) > 50)
                    //    Logger.Error($"System clock and local clock are out of sync! delta ms: {dt.TotalMilliseconds}");

                    _sw.Restart();
                    _ticks = 0;
                }
#endif

                // Attempt to authenticate with the db middleware
                // We do this every 24 hours to get a fresh new token
                if ((_lastSuccessfulDbAuth == null || (Utils.GetHighPrecisionUtcTime() - _lastSuccessfulDbAuth.Value).TotalHours > 24))
                {
                    if (!await Database.Authenticate())
                    {
                        // Log and exit when unable to authenticate
                        Logger.Error("Unable to authenticate with the db middleware server");
                        return;
                    }
                    else
                    {
                        _lastSuccessfulDbAuth = Utils.GetHighPrecisionUtcTime();

#if !DEBUG
                        if (!_hasPurgedAccountStatuses)
                        {
                            _hasPurgedAccountStatuses = await Database.ClearAccountStatuses();
                            await Database.ClearActiveGames();
                        }
#endif
                    }
                }

                // Tick Profiling

                // prof:* Total Number of Connect Attempts (%d), Number Disconnects (%d), Total On (%d)
                // 
                //Logger.Info($"prof:* Total Server Uptime = {GetUptime()} Seconds == (%d days, %d hours, %d minutes, %d seconds)");

                //Logger.Info($"prof:* Total Available RAM = {} bytes");

                // Tick
                await Task.WhenAll(ProfileServer.Tick(), AuthenticationServer.Tick(), LobbyServer.Tick(), ProxyServer.Tick());

                // Tick manager
                await Manager.Tick();

                // Tick plugins
                await Plugins.Tick();

                // 
                if ((Utils.GetHighPrecisionUtcTime() - _lastComponentLog).TotalSeconds > 15f)
                {
                    ProfileServer.Log();
                    AuthenticationServer.Log();
                    LobbyServer.Log();
                    ProxyServer.Log();
                    _lastComponentLog = Utils.GetHighPrecisionUtcTime();
                }

                // Reload config
                if ((Utils.GetHighPrecisionUtcTime() - _lastConfigRefresh).TotalMilliseconds > Settings.RefreshConfigInterval)
                {
                    RefreshConfig();
                    _lastConfigRefresh = Utils.GetHighPrecisionUtcTime();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);

                await ProfileServer.Stop();
                await AuthenticationServer.Stop();
                await LobbyServer.Stop();
                await ProxyServer.Stop();
            }
        }

        static async Task StartServerAsync()
        {
            int waitMs = sleepMS;
            string AppIdArray = string.Join(", ", Settings.ApplicationIds);

            Logger.Info("Initializing medius components...");
            Logger.Info("**************************************************");
            #region MediusGetBuildTimeStamp
            var MediusBuildTimeStamp = GetLinkerTime(Assembly.GetEntryAssembly());
            Logger.Info($"* MediusBuildTimeStamp at {MediusBuildTimeStamp}");
            #endregion

            string datetime = DateTime.Now.ToString("MMMM/dd/yyyy hh:mm:ss tt");
            Logger.Info($"* Launched on {datetime}");

            //ProcesId and Parent ProcessId
            //Logger.Info($":");

            if(Database._settings.SimulatedMode == true)
            {
                Logger.Info("* Database Disabled Medius Stack");
            } else
            {
                Logger.Info("* Database Enabled Medius Stack");
            }

            Logger.Info($"* Server Key Type: {Settings.EncryptMessages}");

            #region Remote Log Viewing
            if (Settings.RemoteLogViewPort == 0)
            {
                //* Remote log viewing setup failure with port %d.
                Logger.Info("* Remote log viewing disabled.");
            } else if (Settings.RemoteLogViewPort != 0)
            {
                Logger.Info($"* Remote log viewing enabled at port {Settings.RemoteLogViewPort}.");
            }
            #endregion

            Logger.Info("**************************************************");


            #region MediusGetVersion
            if (Settings.MediusServerVersionOverride == true)
            {
                #region MAS Enabled?
                if (Settings.EnableMAS == true)
                {
                    Logger.Info($"MAS Version: {Settings.MASVersion}");
                }
                #endregion
                #region MLS Enabled?
                if (Settings.EnableMAS == true)
                {
                    Logger.Info($"MLS Version: {Settings.MLSVersion}");
                }
                #endregion
                #region MPS Enabled?
                if (Settings.EnableMPS == true)
                {
                    Logger.Info($"MPS Version: {Settings.MPSVersion}");
                }
                #endregion
            }
            else
            {
                // Use hardcoded methods in code to handle specific games server versions
                Logger.Info("Using Game Specific Server Versions");
            }

            //* Diagnostic Profiling Enabled: %d Counts

            //Test:NGS Environment flag: %d

            //Billing Service Provider

            //Server-Side Vulgarity Filter Switch 
            //Valid Characters= %s
            //Dictionary Hard[%s] SoftNo[%s] SoftYes[%s] Substring[%s] Substring[%s]

            //ERROR: Could not reset DME Svr metrics[%d]?
            //TOMUM -  SEND PERCENTAGE[%d] RECV PERCENTAGE [%d]
            //DME SVR -  SEND BYTES[%ld] RECV BYTES[%ld]
            //SYS -  MAX SYS[%f]
            //Error initializing MediusTimer.  Continuing...

            //MediusParseLadderList0AppIDs
            //BinaryParseInitialize

            if (Settings.NATIp != null)
            {
                IPAddress ip = IPAddress.Parse(Settings.NATIp);
                DoGetHostEntry(ip);
            }
            string rt_msg_client_get_version_string = "rt_msg_client version: 1.08.0206";
            Logger.Info($"Initialized Message Client Library {rt_msg_client_get_version_string}");

            //DMEServer Enabling Dynamic Client Memory
            //rt_msg_server_enable_dynamic_client_memory
            //DmeServerEnableDynamicClientMemory failed. Continuing

            //Server IP = %s  TCP Port = %d  UDP Port = %d
            //Server version = 1.3.0
            //Messaging Version = %d.%02d.%04d
            //%s library version = %d.%02d.%04d
            //Medius Lobby Server Intialized and Now Accepting Clients

            //Unable to connect to Cache Server. Error %d
            //Connected to Cache Server

            //MediusConnectLobbyToMUCG
            //Connecting to MUCG %s %d %d
            //MUCGIP
            //MUCGPort
            //WorldID
            //MUMIP
            //MUMPort
            //ForwardChatMsgFromMUCG
            //ForwardClanChatMsgFromMUCG
            //CurrentOnlineCount return value
            // Recovery Callback
            //EventsRequested
            //MUCGProcessSyncCB
            //MediusMUCGEventCB

            //Error connecting to MUCG

            //MUCGSendSync returned error %d

            //MFS_ProcessDownloadRequests
            //Error processing download queue. Error %d
            //MFS_ProcessUploadRequests
            //Error processing upload queue. Error %d

            //socket:Lost connection to Cache Server. Reconnecting[%d]
            //Unable to connect to Cache Server. Error %d
            //"Connected to Cache Server

            //ERR: LOST CONNECTION WITH MUM -- Cleaning up DME Worlds.  ATTEMPTING TO RE-ESTABLISH!!

            //MediusConnectLobbyToMUM

            //MUCGGetConnectState
            //Lost Connection to MUCG. Will attempt reconnect in %d seconds

            //ForceConfigReload
            //
            //ConfigManager Cannot Reload Configuration File %d

            //Reloading Dictionary Files
            //clSoftNo
            //clSoftYes
            //clHard
            //FPATExists
            //load_fpat
            //read_file_type
            //read_fpat
            //Incorrect file type in %s.\n
            //Unable to open %s.\n
            //fpat
            //loadclassifier

            //DmeServerUpdateAllWorlds
            //update:Error Code %d Updating All Worlds
            //update:DME Server Network Error = %d

            //AOSCacheFlushCheck
            //Error during AOS cache flush

            //BillingProviderProcessResultQueue
            //BillingProviderProcessResultQueue error.

            //RunningStatus
            //Shutting down NotificationScheduler.
            //Error shutting down MFS download queue
            //Error shutting down MFS upload queue

            //DMEServerCleanup or DMEServerCleanupWorld
            //update:DmeServerCleanupWorld error - world %d error =%d
            //update:DmeServerCleanup error =%d

            //Destroy Billing
            //BillingProviderDestroy
            //update:Ending Medius Lobby Server Operations

            //CacheDestroy
            //clSoftNo
            //clSoftYes
            //clHard
            //fpat

            //pendingTransClose
            //destroyMediusHttpd
            //MFS_transferDestroy
            //ClanCache_Destroy
            //Deleting ClanCache Error: %d


            #endregion

            #region Anti-Cheat Init (WIP)
            if (Settings.AntiCheatOn == true) {
                Logger.Info("Initializing Anti-Cheat (WIP)");


            }
            #endregion

            #region Zipper Interactive MAG/Socom 4 - MAPS
            if (Settings.EnableMAPS == true)
            {
                Logger.Info($"Starting MAPS on port {ProfileServer.Port}.");
                ProfileServer.Start();
                Logger.Info("Medius Profile Server Intialized and Now Accepting Clients");
            }
            #endregion

            #region MAS 
            if (Settings.EnableMAS == true)
            {
                Logger.Info($"Enabling MAS on Server IP = {SERVER_IP} TCP Port = {AuthenticationServer.Port} UDP Port = {AuthenticationServer.Port}.");
                Logger.Info($"Medius Authentication Server running under ApplicationID {AppIdArray}");
                AuthenticationServer.Start();
                Logger.Info("Medius Authentication Server Initialized and Now Accepting Clients");
            }
            #endregion

            #region MLS
            if (Settings.EnableMLS == true)
            {
                Logger.Info($"Enabling MLS on Server IP = {SERVER_IP} TCP Port = {LobbyServer.Port} UDP Port = {LobbyServer.Port}.");
                Logger.Info($"Medius Lobby Server running under ApplicationID {AppIdArray}");
                LobbyServer.Start();
                Logger.Info("Medius Lobby Server Initialized and Now Accepting Clients");
            }
            #endregion

            #region MPS
            if ( Settings.EnableMPS == true)
            {
                Logger.Info($"Enabling MPS on Server IP = {SERVER_IP} TCP Port = {ProxyServer.Port}.");
                Logger.Info($"Medius Proxy Server running under ApplicationID {AppIdArray}");
                ProxyServer.Start();
                Logger.Info("Medius Proxy Server Initialized and Now Accepting Clients");
            }
            #endregion

            #region Server IP
            Logger.Info($"Server IP = {SERVER_IP} [{IP_TYPE}]");

            #endregion

            Logger.Info("Medius Stacks Initialized");


            #region MFS
            if (Settings.AllowMediusFileServices == true)
            {
                Logger.Info($"Initializing MFS Download Queue of size {Settings.MFSDownloadQSize}");
                Logger.Info($"Initializing MFS Upload Queue of size {Settings.MFSUploadQSize}");
                Logger.Info($"MFS Queue Timeout Interval {Settings.MFSQueueTimeoutInterval}");
            }

            #endregion

            #region Timer
            // start timer
            _timer = new HighResolutionTimer();
            _timer.SetPeriod(waitMs);
            _timer.Start();

            // iterate
            while (true)
            {
                // handle tick rate change
                if (sleepMS != waitMs)
                {
                    waitMs = sleepMS;
                    _timer.Stop();
                    _timer.SetPeriod(waitMs);
                    _timer.Start();
                }

                // tick
                await TickAsync();

                // wait for next tick
                _timer.WaitForTrigger();
            }

            #endregion
        }

        static async Task Main(string[] args)
        {
            // 
            Initialize();

            // Add file logger if path is valid
            if (new FileInfo(LogSettings.Singleton.LogPath)?.Directory?.Exists ?? false)
            {
                var loggingOptions = new FileLoggerOptions()
                {
                    Append = false,
                    FileSizeLimitBytes = LogSettings.Singleton.RollingFileSize,
                    MaxRollingFiles = LogSettings.Singleton.RollingFileCount
                };
                InternalLoggerFactory.DefaultFactory.AddProvider(_fileLogger = new FileLoggerProvider(LogSettings.Singleton.LogPath, loggingOptions));
                _fileLogger.MinLevel = Settings.Logging.LogLevel;
            }

            // Optionally add console logger (always enabled when debugging)
#if DEBUG
            InternalLoggerFactory.DefaultFactory.AddProvider(new ConsoleLoggerProvider((s, level) => level >= LogSettings.Singleton.LogLevel, true));
#else
            if (Settings.Logging.LogToConsole)
                InternalLoggerFactory.DefaultFactory.AddProvider(new ConsoleLoggerProvider((s, level) => level >= LogSettings.Singleton.LogLevel, true));
#endif

            // Initialize plugins
            Plugins = new PluginsManager(PLUGINS_PATH);

            // 
            await StartServerAsync();
        }

        static void Initialize()
        {
            RefreshConfig();

            //
            if (Settings.Locations != null)
            {
                foreach (var location in Settings.Locations)
                {
                    if (location.AppIds == null || location.AppIds.Length == 0)
                    {
                        Manager.AddChannel(new Channel()
                        {
                            ApplicationId = 0,
                            MaxPlayers = 256,
                            Name = location.ChannelName ?? location.Name,
                            GenericFieldLevel = location.GenericFieldLevel,
                            Id = location.Id,
                            Type = ChannelType.Lobby
                        });
                    }
                    else
                    {
                        foreach (var appId in location.AppIds)
                        {
                            Manager.AddChannel(new Channel()
                            {
                                ApplicationId = appId,
                                MaxPlayers = 256,
                                Name = location.ChannelName ?? location.Name,
                                GenericFieldLevel = location.GenericFieldLevel,
                                Id = location.Id,
                                Type = ChannelType.Lobby
                            });
                        }
                    }
                }
            }

            if (Settings.ApplicationIds != null)
            {
                foreach (var appId in Settings.ApplicationIds)
                {
                    

                    #region Calling All Cars PS3
                    if (appId == 20623 || appId == 20624) 
                    {
                        Manager.AddChannel(new Channel()
                        {
                            ApplicationId = appId,
                            MaxPlayers = 256,
                            Name = "US",
                            Type = ChannelType.Lobby,
                            GenericField1 = 1,
                            GenericField2 = 1,
                        });
                        Manager.AddChannel(new Channel()
                        {
                            ApplicationId = appId,
                            MaxPlayers = 256,
                            Name = "EU",
                            Type = ChannelType.Lobby,
                            GenericField1 = 1,
                            GenericField2 = 1,
                        });
                    }
                    #endregion

                    #region Motorstorm 1 / Monument Valley
                    //SCEA SCEE SCEI SCEK
                    else if (appId == 20764 || appId == 20364 || appId == 21000 || appId == 21044)
                    {
                        Manager.AddChannel(new Channel()
                        {
                            ApplicationId = appId,
                            MaxPlayers = 256,
                            Name = "Motorstorm US West",
                            Type = ChannelType.Lobby
                        });
                        Manager.AddChannel(new Channel()
                        {
                            ApplicationId = appId,
                            MaxPlayers = 256,
                            Name = "Motorstorm US Central",
                            Type = ChannelType.Lobby
                        });
                        Manager.AddChannel(new Channel()
                        {
                            ApplicationId = appId,
                            MaxPlayers = 256,
                            Name = "Motorstorm US East",
                            Type = ChannelType.Lobby
                        });
                        Manager.AddChannel(new Channel()
                        {
                            ApplicationId = appId,
                            MaxPlayers = 256,
                            Name = "Motorstorm EU",
                            Type = ChannelType.Lobby
                        });
                        /*
                        Manager.AddChannel(new Channel()
                        {
                            ApplicationId = appId,
                            MaxPlayers = 256,
                            Name = "モーターストームJP",
                            Type = ChannelType.Lobby
                        });
                        */
                    }
                    #endregion

                    #region NBA 07
                    else if (appId == 20244)
                    {
                        Manager.AddChannel(new Channel()
                        {
                            ApplicationId = appId,
                            MaxPlayers = 256,
                            Name = "SportsConnect US",
                            Type = ChannelType.Lobby,
                            GenericField1 = 1,
                            GenericField2 = 1,
                        });
                        Manager.AddChannel(new Channel()
                        {
                            ApplicationId = appId,
                            MaxPlayers = 256,
                            Name = "SportsConnect EU",
                            Type = ChannelType.Lobby,
                            GenericField1 = 1,
                            GenericField2 = 1,
                        });
                    }
                    #endregion

                    #region WRC 4
                    else if (appId == 10394)
                    {
                        Manager.AddChannel(new Channel()
                        {
                            ApplicationId = appId,
                            MaxPlayers = 256,
                            Name = "Region US",
                            Type = ChannelType.Lobby,
                        });
                        Manager.AddChannel(new Channel()
                        {
                            ApplicationId = appId,
                            MaxPlayers = 256,
                            Name = "Region EU",
                            Type = ChannelType.Lobby,
                        });
                    }
                    #endregion

                    #region Socom 1
                    else if (appId == 10274)
                    {
                        Manager.AddChannel(new Channel()
                        {
                            ApplicationId = appId,
                            MaxPlayers = 256,
                            Name = "Region US",
                            Type = ChannelType.Lobby,
                        });
                    }
                    #endregion

                    #region Arc the Lad: End of Darkness
                    else if (appId == 10984)
                    {
                        Manager.AddChannel(new Channel()
                        {
                            ApplicationId = appId,
                            MaxPlayers = 256,
                            Name = "Arc",
                            Type = ChannelType.Lobby,
                            GenericField1 = 250,
                        });
                    }
                    #endregion

                    #region Default 
                    if (Manager.GetDefaultLobbyChannel(appId) == null)
                    {
                        Manager.AddChannel(new Channel()
                        {
                            ApplicationId = appId,
                            MaxPlayers = 256,
                            Name = "Default",
                            Type = ChannelType.Lobby
                        });
                    }
                    #endregion
                }
            }
            else
            {
                if (Manager.GetDefaultLobbyChannel(0) == null)
                {
                    Manager.AddChannel(new Channel()
                    {
                        ApplicationId = 0,
                        MaxPlayers = 256,
                        Name = "Default",
                        Type = ChannelType.Lobby
                    });
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        static void RefreshConfig()
        {
            // 
            var serializerSettings = new JsonSerializerSettings()
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,
            };

            // Load settings
            if (File.Exists(CONFIG_FILE))
            {
                Settings.Locations?.Clear();

                // Populate existing object
                JsonConvert.PopulateObject(File.ReadAllText(CONFIG_FILE), Settings, serializerSettings);
            }
            else
            {
                // Save defaults
                File.WriteAllText(CONFIG_FILE, JsonConvert.SerializeObject(Settings, Formatting.Indented));
            }

            // Set LogSettings singleton
            LogSettings.Singleton = Settings.Logging;

            #region Determine Server IP
            if (!Settings.UsePublicIp)
            {
                SERVER_IP = Utils.GetLocalIPAddress();
                IP_TYPE = "Local";
            }
            else
            {
                if (string.IsNullOrWhiteSpace(Settings.PublicIpOverride))
                {
                    SERVER_IP = IPAddress.Parse(Utils.GetPublicIPAddress());
                    IP_TYPE = "Public";

                }
                else
                {
                    SERVER_IP = IPAddress.Parse(Settings.PublicIpOverride);
                    IP_TYPE = "Public (Override)";
                }
            }
            #endregion

            // Update NAT Ip with server ip if null
            if (string.IsNullOrEmpty(Settings.NATIp))
                Settings.NATIp = SERVER_IP.ToString();

            // Update file logger min level
            if (_fileLogger != null)
                _fileLogger.MinLevel = Settings.Logging.LogLevel;

            // Update default rsa key
            Pipeline.Attribute.ScertClientAttribute.DefaultRsaAuthKey = Settings.DefaultKey;

            if (Settings.DefaultKey != null)
                GlobalAuthPublic = new RSA_KEY(Settings.DefaultKey.N.ToByteArrayUnsigned().Reverse().ToArray());

            // Load tick time into sleep ms for main loop
            sleepMS = TickMS;
        }

        public static string GenerateSessionKey()
        {
            lock (_sessionKeyCounterLock)
            {
                return (++_sessionKeyCounter).ToString();
            }
        }
        #region Text Filter
        private static string GetTextFilterRegexExpression(TextFilterContext context)
        {
            if (Settings.TextBlacklistFilters.TryGetValue(context, out var rExp) && !string.IsNullOrEmpty(rExp))
                return rExp;

            if (Settings.TextBlacklistFilters.TryGetValue(TextFilterContext.DEFAULT, out rExp) && !string.IsNullOrEmpty(rExp))
                return rExp;

            return null;
        }

        public static bool PassTextFilter(TextFilterContext context, string text)
        {
            var rExp = GetTextFilterRegexExpression(context);
            if (string.IsNullOrEmpty(rExp))
                return true;

            Regex r = new Regex(rExp, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            return !r.IsMatch(text);
        }

        public static string FilterTextFilter(TextFilterContext context, string text)
        {
            var rExp = GetTextFilterRegexExpression(context);
            if (string.IsNullOrEmpty(rExp))
                return text;

            Regex r = new Regex(rExp, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            return r.Replace(text, "");
        }
        #endregion

        public static string GetFileSystemPath(string filename)
        {
            if (!Settings.AllowMediusFileServices)
                return null;
            if (string.IsNullOrEmpty(Settings.MediusFileServerRootPath))
                return null;
            if (string.IsNullOrEmpty(filename))
                return null;

            var rootPath = Path.GetFullPath(Settings.MediusFileServerRootPath);
            var path = Path.GetFullPath(Path.Combine(Settings.MediusFileServerRootPath, filename));

            // prevent filename from moving up directories
            if (!path.StartsWith(rootPath))
                return null;

            return path;
        }
        
        public static DateTime GetLinkerTime(Assembly assembly)
        {
            const string BuildVersionMetadataPrefix = "+build";

            var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (attribute?.InformationalVersion != null)
            {
                var value = attribute.InformationalVersion;
                var index = value.IndexOf(BuildVersionMetadataPrefix);
                if (index > 0)
                {
                    value = value[(index + BuildVersionMetadataPrefix.Length)..];
                    return DateTime.ParseExact(value, "yyyy-MM-ddTHH:mm:ss:fffZ", CultureInfo.InvariantCulture);
                }
            }

            return default;
        }

        public static void DoGetHostEntry(IPAddress address)
        {
            try
            {
                IPHostEntry host = Dns.GetHostEntry(address);

                Logger.Info($"NAT Service IP: {address}");
                //Logger.Info($"GetHostEntry({address}) returns HostName: {host.HostName}");
            }
            catch (SocketException ex)
            {
                //unknown host or
                //not every IP has a name
                //log exception (manage it)
                Logger.Error($"NAT not resolved {ex}");
            }
        }

        public static TimeSpan GetUptime()
        {
            ManagementObject mo = new ManagementObject(@"\\.\root\cimv2:Win32_OperatingSystem=@");
            DateTime lastBootUp = ManagementDateTimeConverter.ToDateTime(mo["LastBootUpTime"].ToString());
            return DateTime.Now.ToUniversalTime() - lastBootUp.ToUniversalTime();
        }
    }
}
