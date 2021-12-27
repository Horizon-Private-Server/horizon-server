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

        public static IPAddress SERVER_IP = IPAddress.Parse("192.168.0.178");

        public static MediusManager Manager = new MediusManager();
        public static PluginsManager Plugins = null;

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

                        if (!_hasPurgedAccountStatuses)
                        {
                            _hasPurgedAccountStatuses = await Database.ClearAccountStatuses();
                            await Database.ClearActiveGames();
                        }
                    }
                }

                // Tick
                await Task.WhenAll(AuthenticationServer.Tick(), LobbyServer.Tick(), ProxyServer.Tick());

                // Tick manager
                Manager.Tick();

                // Tick plugins
                Plugins.Tick();

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
            if (Settings.ApplicationIds != null)
            {
                foreach (var appId in Settings.ApplicationIds)
                {
                    Manager.AddChannel(new Channel()
                    {
                        ApplicationId = appId,
                        MaxPlayers = 256,
                        Name = "Default",
                        Type = ChannelType.Lobby
                    });
                }
            }
            else
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
            if (!Settings.UsePublicIp)
            {
                SERVER_IP = Utils.GetLocalIPAddress();
            }
            else
            {
                SERVER_IP = IPAddress.Parse(Utils.GetPublicIPAddress());
            }

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
    }
}
