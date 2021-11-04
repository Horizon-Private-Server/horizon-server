using DotNetty.Common.Internal.Logging;
using Haukcode.HighResolutionTimer;
using Microsoft.Extensions.Logging.Console;
using Newtonsoft.Json;
using NReco.Logging.File;
using Org.BouncyCastle.Math;
using RT.Cryptography;
using RT.Models;
using Server.Common;
using Server.Common.Logging;
using Server.Dme.Config;
using Server.Plugins;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace Server.Dme
{

    class Program
    {
        public const string CONFIG_FILE = "config.json";
        public const string PLUGINS_PATH = "plugins/";
        public const string KEY = "42424242424242424242424242424242424242424242424242424242424242424242424242424242424242424242424242424242424242424242424242424242";

        public readonly static PS2_RSA GlobalAuthKey = new PS2_RSA(
            new BigInteger("10315955513017997681600210131013411322695824559688299373570246338038100843097466504032586443986679280716603540690692615875074465586629501752500179100369237", 10),
            new BigInteger("17", 10),
            new BigInteger("4854567300243763614870687120476899445974505675147434999327174747312047455575182761195687859800492317495944895566174677168271650454805328075020357360662513", 10)
        );

        public readonly static RSA_KEY GlobalAuthPublic = new RSA_KEY(GlobalAuthKey.N.ToByteArrayUnsigned().Reverse().ToArray());
        public readonly static RSA_KEY GlobalAuthPrivate = new RSA_KEY(GlobalAuthKey.D.ToByteArrayUnsigned().Reverse().ToArray());

        public static ServerSettings Settings = new ServerSettings();

        public static IPAddress SERVER_IP = IPAddress.Parse("192.168.0.178");

        public static MediusManager Manager = new MediusManager();
        public static TcpServer TcpServer = new TcpServer();
        public static PluginsManager Plugins = null;

        private static FileLoggerProvider _fileLogger = null;
        private static ulong _sessionKeyCounter = 0;
        private static readonly object _sessionKeyCounterLock = (object)_sessionKeyCounter;
        private static DateTime _timeLastPluginTick = Utils.GetHighPrecisionUtcTime();

        private static int _ticks = 0;
        private static Stopwatch _sw = new Stopwatch();
        private static HighResolutionTimer _timer;
        private static DateTime _lastConfigRefresh = Utils.GetHighPrecisionUtcTime();

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
                    var averageMsPerTick = 1000 * (_sw.Elapsed.TotalSeconds / _ticks);
                    var error = Math.Abs(Settings.MainLoopSleepMs - averageMsPerTick) / Settings.MainLoopSleepMs;

                    if (error > 0.1f)
                        Logger.Error($"Average Ms between ticks is: {averageMsPerTick} is {error * 100}% off of target {Settings.MainLoopSleepMs}");

                    var dt = DateTime.UtcNow - Utils.GetHighPrecisionUtcTime();
                    if (Math.Abs(dt.TotalMilliseconds) > 50)
                        Logger.Error($"System clock and local clock are out of sync! delta ms: {dt.TotalMilliseconds}");

                    _sw.Restart();
                    _ticks = 0;
                }
#endif

                // Check if connected
                if (Manager.IsConnected)
                {
                    // Tick
                    await Task.WhenAll(TcpServer.Tick(), Manager.Tick());

                    // Tick plugins
                    if ((Utils.GetHighPrecisionUtcTime() - _timeLastPluginTick).TotalMilliseconds > Settings.PluginTickIntervalMs)
                    {
                        _timeLastPluginTick = Utils.GetHighPrecisionUtcTime();
                        Plugins.Tick();
                    }
                }
                else if ((Utils.GetHighPrecisionUtcTime() - Manager.TimeLostConnection)?.TotalSeconds > Settings.MPSReconnectInterval)
                {
                    // Try to reconnect to the proxy server
                    await Manager.Start();
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

                await TcpServer.Stop();
                await Manager.Stop();
            }
        }

        static async Task StartServerAsync()
        {
            int waitMs = Settings.MainLoopSleepMs;

            Logger.Info("Starting medius components...");

            Logger.Info($"Starting TCP on port {TcpServer.Port}.");
            TcpServer.Start();
            Logger.Info($"TCP started.");

            await Manager.Start();

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
                if (Settings.MainLoopSleepMs != waitMs)
                {
                    waitMs = Settings.MainLoopSleepMs;
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
            // 
            var serializerSettings = new JsonSerializerSettings()
            {
                MissingMemberHandling = MissingMemberHandling.Ignore
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

            // Update file logger min level
            if (_fileLogger != null)
                _fileLogger.MinLevel = Settings.Logging.LogLevel;
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
