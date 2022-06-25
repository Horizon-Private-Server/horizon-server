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
                        Logger.Error($"Unable to authenticate connection to Cache Server.");
                        return;
                    }
                    else
                    {
                        _lastSuccessfulDbAuth = Utils.GetHighPrecisionUtcTime();

                        Logger.Info("Connected to Cache Server");

#if !DEBUG
                        if (!_hasPurgedAccountStatuses)
                        {
                            _hasPurgedAccountStatuses = await Database.ClearAccountStatuses();
                            await Database.ClearActiveGames();
                        }
#endif
                    }
                }

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
                    await RefreshConfig();
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
            } else if (Settings.RemoteLogViewPort > 0)
            {
                Logger.Info($"* Remote log viewing enabled at port {Settings.RemoteLogViewPort}.");
            }
            #endregion

            Logger.Info("**************************************************");


            #region Anti-Cheat Init (WIP)
            if (Settings.AntiCheatOn == true)
            {
                Logger.Info("Initializing anticheat (WIP)\n");
            }
            #endregion

            #region MediusGetVersion
            if (Settings.MediusServerVersionOverride == true)
            {
                #region MAPS - Zipper Interactive MAG/Socom 4
                if (Settings.EnableMAPS == true)
                {
                    Logger.Info($"MAPS Version: {Settings.MAPSVersion}");
                    Logger.Info($"Enabling MAPS on Server IP = {SERVER_IP} TCP Port = {AuthenticationServer.Port} UDP Port = {AuthenticationServer.Port}.");
                    ProfileServer.Start();
                    Logger.Info("Medius Profile Server Intialized and Now Accepting Clients");
                }
                #endregion

                #region MAS Enabled?
                if (Settings.EnableMAS == true)
                {
                    #region MAS 
                    Logger.Info($"MAS Version: {Settings.MASVersion}");
                    Logger.Info($"Enabling MAS on Server IP = {SERVER_IP} TCP Port = {AuthenticationServer.Port} UDP Port = {AuthenticationServer.Port}.");
                    Logger.Info($"Medius Authentication Server running under ApplicationID {AppIdArray}");

                    //Connecting to Medius Universe Manager 127.0.0.1 10076 1
                    //Connected to Universe Manager server

                    AuthenticationServer.Start();
                    Logger.Info("Medius Authentication Server Initialized");
                    #endregion

                }
                #endregion

                #region MLS Enabled?
                if (Settings.EnableMAS == true)
                {
                    Logger.Info($"MLS Version: {Settings.MLSVersion}");
                    Logger.Info($"Enabling MLS on Server IP = {SERVER_IP} TCP Port = {LobbyServer.Port} UDP Port = {LobbyServer.Port}.");
                    Logger.Info($"Medius Lobby Server running under ApplicationID {AppIdArray}");

                    DMEServerResetMetrics();

                    LobbyServer.Start();
                    Logger.Info("Medius Lobby Server Initialized and Now Accepting Clients");
                }
                #endregion

                #region MPS Enabled?
                if (Settings.EnableMPS == true)
                {
                    Logger.Info($"MPS Version: {Settings.MPSVersion}");
                    Logger.Info($"Enabling MPS on Server IP = {SERVER_IP} TCP Port = {ProxyServer.Port}.");
                    Logger.Info($"Medius Proxy Server running under ApplicationID {AppIdArray}");
                    ProxyServer.Start();
                    Logger.Info("Medius Proxy Server Initialized and Now Accepting Clients");

                }
                #endregion
            }
            else
            {
                // Use hardcoded methods in code to handle specific games server versions
                Logger.Info("Using Game Specific Server Versions");

                #region MAS Enabled?
                if (Settings.EnableMAS == true)
                {
                    Logger.Info($"Enabling MAS on Server IP = {SERVER_IP} TCP Port = {AuthenticationServer.Port} UDP Port = {AuthenticationServer.Port}.");
                    Logger.Info($"Medius Authentication Server running under ApplicationID {AppIdArray}");

                    AuthenticationServer.Start();
                    Logger.Info("Medius Authentication Server Initialized");

                }
                #endregion

                #region MLS Enabled?
                if (Settings.EnableMAS == true)
                {
                    Logger.Info($"Enabling MLS on Server IP = {SERVER_IP} TCP Port = {LobbyServer.Port} UDP Port = {LobbyServer.Port}.");
                    Logger.Info($"Medius Lobby Server running under ApplicationID {AppIdArray}");

                    DMEServerResetMetrics();

                    LobbyServer.Start();
                    Logger.Info("Medius Lobby Server Initialized and Now Accepting Clients");
                }
                #endregion

                #region MPS Enabled?
                if (Settings.EnableMPS == true)
                {
                    Logger.Info($"Enabling MPS on Server IP = {SERVER_IP} TCP Port = {ProxyServer.Port}.");
                    Logger.Info($"Medius Proxy Server running under ApplicationID {AppIdArray}");
                    ProxyServer.Start();
                    Logger.Info("Medius Proxy Server Initialized and Now Accepting Clien
                }
                #endregion

            }

            #region NAT
            //Get NATIp
            if (Settings.NATIp != null)
            {
                try
                {
                    IPHostEntry host = Dns.GetHostEntry(Settings.NATIp);

                    if (Settings.NATIp != host.HostName)
                    {
                        IPAddress ip = IPAddress.Parse(host.AddressList.First().ToString());
                        ip.MapToIPv4();
                        try
                        {
                            DoGetHostAddressEntry(ip);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"Unable to resolve NAT service IP: {ip}  Exiting with exception: {ex}");
                            Environment.Exit(1);
                        }
                    }
                    else
                    {
                        DoGetHostNameEntry(Settings.NATIp);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Unable to resolve NAT service IP: {Settings.NATIp}  Exiting with exception: {ex}");
                    Environment.Exit(1);
                }


            }
            #endregion

            #endregion

            Logger.Info("Medius Stacks Initialized");


            #region MFS
            if (Settings.AllowMediusFileServices == true)
            {
                Logger.Info($"Initializing MFS Download Queue of size {Settings.MFSDownloadQSize}");

                Logger.Info($"Initializing MFS Upload Queue of size {Settings.MFSUploadQSize}");
                MFS_transferInit();

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
            await Initialize();

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

        static async Task Initialize()
        {
            await RefreshConfig();

            #region Locations TEMP
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
            #endregion

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
                            Name = "Yewbell",
                            Type = ChannelType.Lobby,
                        });
                        Manager.AddChannel(new Channel()
                        {
                            ApplicationId = appId,
                            MaxPlayers = 256,
                            Name = "Rueloon",
                            Type = ChannelType.Lobby,
                        });
                        Manager.AddChannel(new Channel()
                        {
                            ApplicationId = appId,
                            MaxPlayers = 256,
                            Name = "Dilzweld",
                            Type = ChannelType.Lobby,
                        });
                        Manager.AddChannel(new Channel()
                        {
                            ApplicationId = appId,
                            MaxPlayers = 256,
                            Name = "Milmarna",
                            Type = ChannelType.Lobby,
                        });
                        Manager.AddChannel(new Channel()
                        {
                            ApplicationId = appId,
                            MaxPlayers = 256,
                            Name = "Romastle Plains",
                            Type = ChannelType.Lobby,
                        });
                        Manager.AddChannel(new Channel()
                        {
                            ApplicationId = appId,
                            MaxPlayers = 256,
                            Name = "Halshinne",
                            Type = ChannelType.Lobby,
                        });
                        Manager.AddChannel(new Channel()
                        {
                            ApplicationId = appId,
                            MaxPlayers = 256,
                            Name = "Lamda Temple",
                            Type = ChannelType.Lobby,
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
        static Task RefreshConfig()
        {
            // 
            var serializerSettings = new JsonSerializerSettings()
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,
            };

            #region Dirs
            string root = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string subdirLogs = root + @"\logs";
            string subdirMFSFiles = root + @"\MFSFiles";
            string subdirHorizonPlugins = root + @"\plugins";
            string subdirConfig = root + @"\config";
            string subdirConfigFile = subdirConfig + @"\" + CONFIG_FILE;
            #endregion

            
            #region Check Config.json
            // Create Defaults if File doesn't exist
            if (!File.Exists(CONFIG_FILE))
            {
                File.WriteAllText(CONFIG_FILE, JsonConvert.SerializeObject(Settings, Formatting.Indented));
            } else {

                // Load Settings
                Settings.Locations?.Clear();

                // Populate existing object
                JsonConvert.PopulateObject(File.ReadAllText(CONFIG_FILE), Settings, serializerSettings);
            }
            #endregion

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
            return Task.CompletedTask;
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

        #region System Time
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

        public static TimeSpan GetUptime()
        {
            ManagementObject mo = new ManagementObject(@"\\.\root\cimv2:Win32_OperatingSystem=@");
            DateTime lastBootUp = ManagementDateTimeConverter.ToDateTime(mo["LastBootUpTime"].ToString());
            return DateTime.Now.ToUniversalTime() - lastBootUp.ToUniversalTime();
        }
        #endregion

        #region DoGetHost
        public static void DoGetHostNameEntry(string hostName)
        {
            IPHostEntry host = Dns.GetHostEntry(hostName);
            try
            {
                Logger.Info($"NAT Service HostName: {hostName} \n      NAT Service IP: {host.AddressList.First()}");
                //Logger.Info($"GetHostEntry({address}) returns HostName: {host.HostName}");
            }
            catch (SocketException ex)
            {
                //unknown host or
                //not every IP has a name
                //log exception (manage it)
                Logger.Error($"Unable to resolve NAT service IP: {host.AddressList.First()}  Exiting with exception: {ex}");
            }
        }

        public static void DoGetHostAddressEntry(IPAddress address)
        {
            IPHostEntry host = Dns.GetHostEntry(address);
            try
            {
                Logger.Info($"NAT Service IP: {host.AddressList.First()}");
                //Logger.Info($"GetHostEntry({address}) returns HostName: {host.HostName}");
            }
            catch (SocketException ex)
            {
                //unknown host or
                //not every IP has a name
                //log exception (manage it)
            }
        }
        #endregion

        public static void MFS_transferInit()
        {

            Logger.Info($"Initializing MFS_transfer with url {Settings.MFSTransferURI} "); //numThreads{}"
        }

        public static void DMEServerResetMetrics()
        {
            //rt_msg_server_reset_connect_metrics
            //rt_msg_server_reset_data_metrics
            //rt_msg_server_reset_message_metrics
            //rt_msg_server_reset_frame_time_metrics

            //ERROR: Could not reset DME Svr metrics[%d]
        }
    }
}
