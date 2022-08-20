using DotNetty.Common.Internal.Logging;
using RT.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Math;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RT.Models;
using Server.Medius.Models;
using Server.Database;
using Server.Medius.Config;
using NReco.Logging.File;
using Server.Common.Logging;
using Server.Plugins;
using System.Net.NetworkInformation;
using Server.Common;
using Haukcode.HighResolutionTimer;
using System.Text.RegularExpressions;

namespace Server.Medius
{
    public class Program
    {
        private static string CONFIG_DIRECTIORY = "./";
        public static string CONFIG_FILE => Path.Combine(CONFIG_DIRECTIORY, "medius.json");
        public static string DB_CONFIG_FILE => Path.Combine(CONFIG_DIRECTIORY, "db.config.json");

        public static RSA_KEY GlobalAuthPublic = null;

        public static ServerSettings Settings = new ServerSettings();
        public static DbController Database = null;

        public static IPAddress SERVER_IP = IPAddress.Parse("192.168.0.178");

        public static MediusManager Manager = new MediusManager();
        public static PluginsManager Plugins = null;

        public static MAS AuthenticationServer = new MAS();
        public static MLS LobbyServer = new MLS();
        public static MPS ProxyServer = new MPS();

        public static int TickMS => 1000 / (Settings?.TickRate ?? 10);

        private static Dictionary<int, AppSettings> _appSettings = new Dictionary<int, AppSettings>();
        private static AppSettings _defaultAppSettings = new AppSettings(0);
        private static FileLoggerProvider _fileLogger = null;
        private static ulong _sessionKeyCounter = 0;
        private static int sleepMS = 0;
        private static readonly object _sessionKeyCounterLock = (object)_sessionKeyCounter;
        private static DateTime? _lastSuccessfulDbAuth = null;
        private static DateTime _lastConfigRefresh = Utils.GetHighPrecisionUtcTime();
        private static DateTime _lastComponentLog = Utils.GetHighPrecisionUtcTime();
        private static bool _hasPurgedAccountStatuses = false;

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

                        // pass to manager
                        await Manager.OnDatabaseAuthenticated();

                        // refresh app settings
                        await RefreshAppSettings();

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
                await Task.WhenAll(AuthenticationServer.Tick(), LobbyServer.Tick(), ProxyServer.Tick());

                // Tick manager
                await Manager.Tick();

                // Tick plugins
                await Plugins.Tick();

                // 
                if ((Utils.GetHighPrecisionUtcTime() - _lastComponentLog).TotalSeconds > 15f)
                {
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

                await AuthenticationServer.Stop();
                await LobbyServer.Stop();
                await ProxyServer.Stop();
            }
        }

        static async Task StartServerAsync()
        {
            int waitMs = sleepMS;

            Logger.Info("Starting medius components...");

            Logger.Info($"Starting MAS on port {AuthenticationServer.Port}.");
            AuthenticationServer.Start();
            Logger.Info($"MAS started.");

            Logger.Info($"Starting MLS on port {LobbyServer.Port}.");
            LobbyServer.Start();
            Logger.Info($"MLS started.");

            Logger.Info($"Starting MPS on port {ProxyServer.Port}.");
            ProxyServer.Start();
            Logger.Info($"MPS started.");

            // 
            Logger.Info("Started.");

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
        }

        static async Task Main(string[] args)
        {
            // get path to config directory from first argument
            if (args.Length > 0)
                CONFIG_DIRECTIORY = args[0];

            // 
            Database = new DbController(DB_CONFIG_FILE);

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
            Plugins = new PluginsManager(Settings.PluginsPath);

            // 
            await StartServerAsync();
        }

        static void Initialize()
        {
            RefreshServerIp();
            RefreshConfig();
        }


        /// <summary>
        /// 
        /// </summary>
        static void RefreshConfig()
        {
            var usePublicIp = Settings.UsePublicIp;

            // 
            var serializerSettings = new JsonSerializerSettings()
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,
            };

            // Load settings
            if (File.Exists(CONFIG_FILE))
            {
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

            // Determine server ip
            if (usePublicIp != Settings.UsePublicIp)
            {
                if (!Settings.UsePublicIp)
                {
                    SERVER_IP = Utils.GetLocalIPAddress();
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(Settings.PublicIpOverride))
                        SERVER_IP = IPAddress.Parse(Utils.GetPublicIPAddress());
                    else
                        SERVER_IP = IPAddress.Parse(Settings.PublicIpOverride);
                }
            }

            // Determine server ip
            if (usePublicIp != Settings.UsePublicIp)
                RefreshServerIp();

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

            //
            _ = RefreshAppSettings();

            // Load tick time into sleep ms for main loop
            sleepMS = TickMS;
        }

        static async Task RefreshAppSettings()
        {
            try
            {
                if (!await Database.AmIAuthenticated())
                    return;

                // get supported app ids
                var appIdGroups = await Database.GetAppIds();

                // get settings
                foreach (var appIdGroup in appIdGroups)
                {
                    foreach (var appId in appIdGroup.AppIds)
                    {
                        var settings = await Database.GetServerSettings(appId);
                        if (settings != null)
                        {
                            if (_appSettings.TryGetValue(appId, out var appSettings))
                            {
                                appSettings.SetSettings(settings);
                            }
                            else
                            {
                                appSettings = new AppSettings(appId);
                                appSettings.SetSettings(settings);
                                _appSettings.Add(appId, appSettings);

                                // we also want to send this back to the server since this is new locally
                                // and there might be new setting fields that aren't yet on the db
                                await Database.SetServerSettings(appId, appSettings.GetSettings());
                            }
                        }
                    }
                }

                // get locations
                var locations = await Database.GetLocations();
                var channels = await Database.GetChannels();

                // 

                // add new channels
                foreach (var channel in channels)
                {
                    if (Manager.GetChannelByChannelId(channel.Id, channel.AppId) == null)
                    {
                        Manager.AddChannel(new Channel()
                        {
                            Id = channel.Id,
                            Name = channel.Name,
                            ApplicationId = channel.AppId,
                            MaxPlayers = channel.MaxPlayers,
                            GenericField1 = (uint)channel.GenericField1,
                            GenericField2 = (uint)channel.GenericField2,
                            GenericField3 = (uint)channel.GenericField3,
                            GenericField4 = (uint)channel.GenericField4,
                            GenericFieldLevel = (RT.Common.MediusWorldGenericFieldLevelType)channel.GenericFieldFilter,
                            Type = ChannelType.Lobby
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        static void RefreshServerIp()
        {
            if (!Settings.UsePublicIp)
            {
                SERVER_IP = Utils.GetLocalIPAddress();
            }
            else
            {
                if (string.IsNullOrWhiteSpace(Settings.PublicIpOverride))
                    SERVER_IP = IPAddress.Parse(Utils.GetPublicIPAddress());
                else
                    SERVER_IP = IPAddress.Parse(Settings.PublicIpOverride);
            }
        }

        public static string GenerateSessionKey()
        {
            lock (_sessionKeyCounterLock)
            {
                return (++_sessionKeyCounter).ToString();
            }
        }

        private static string GetTextFilterRegexExpression(int appId, TextFilterContext context)
        {
            var appSettings = GetAppSettingsOrDefault(appId);
            string regex = null;

            switch (context)
            {
                case TextFilterContext.ACCOUNT_NAME: regex = appSettings.TextFilterAccountName; break;
                case TextFilterContext.CHAT: regex = appSettings.TextFilterChat; break;
                case TextFilterContext.CLAN_MESSAGE: regex = appSettings.TextFilterClanMessage; break;
                case TextFilterContext.CLAN_NAME: regex = appSettings.TextFilterClanName; break;
                case TextFilterContext.DEFAULT: regex = appSettings.TextFilterDefault; break;
                case TextFilterContext.GAME_NAME: regex = appSettings.TextFilterGameName; break;
            }

            if (String.IsNullOrEmpty(regex))
                return appSettings.TextFilterDefault;

            return regex;
        }

        public static bool PassTextFilter(int appId, TextFilterContext context, string text)
        {
            var rExp = GetTextFilterRegexExpression(appId, context);
            if (String.IsNullOrEmpty(rExp))
                return true;

            Regex r = new Regex(rExp, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            return !r.IsMatch(text);
        }

        public static string FilterTextFilter(int appId, TextFilterContext context, string text)
        {
            var rExp = GetTextFilterRegexExpression(appId, context);
            if (String.IsNullOrEmpty(rExp))
                return text;

            Regex r = new Regex(rExp, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            return r.Replace(text, "");
        }

        public static string? GetFileSystemPath(int appId, string filename)
        {
            if (!GetAppSettingsOrDefault(appId).EnableMediusFileServices)
                return null;
            if (String.IsNullOrEmpty(Settings.MediusFileServerRootPath))
                return null;
            if (String.IsNullOrEmpty(filename))
                return null;

            var rootPath = Path.GetFullPath(Settings.MediusFileServerRootPath);
            var path = Path.GetFullPath(Path.Combine(Settings.MediusFileServerRootPath, filename, appId.ToString()));

            // prevent filename from moving up directories
            if (!path.StartsWith(rootPath))
                return null;

            return path;
        }

        public static AppSettings GetAppSettingsOrDefault(int appId)
        {
            if (_appSettings.TryGetValue(appId, out var appSettings))
                return appSettings;

            return _defaultAppSettings;
        }
    }
}
