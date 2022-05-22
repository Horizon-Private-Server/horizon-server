﻿using DotNetty.Common.Internal.Logging;
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
using System.Collections.Generic;
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

        public static ServerSettings Settings = new ServerSettings();

        public static IPAddress SERVER_IP;
        public static string IP_TYPE;


        public static Dictionary<int, MediusManager> Managers = new Dictionary<int, MediusManager>();
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

                    //var dt = DateTime.UtcNow - Utils.GetHighPrecisionUtcTime();
                    //if (Math.Abs(dt.TotalMilliseconds) > 50)
                    //    Logger.Error($"System clock and local clock are out of sync! delta ms: {dt.TotalMilliseconds}");

                    _sw.Restart();
                    _ticks = 0;
                }
#endif

                var tasks = new List<Task>()
                {
                    TcpServer.Tick()
                };

                foreach (var manager in Managers)
                {
                    if (manager.Value.IsConnected)
                    {
                        tasks.Add(manager.Value.Tick());
                    }
                    else if ((Utils.GetHighPrecisionUtcTime() - manager.Value.TimeLostConnection)?.TotalSeconds > Settings.MPSReconnectInterval)
                    {
                        tasks.Add(manager.Value.Start());
                    }
                }

                // Tick plugins
                if ((Utils.GetHighPrecisionUtcTime() - _timeLastPluginTick).TotalMilliseconds > Settings.PluginTickIntervalMs)
                {
                    _timeLastPluginTick = Utils.GetHighPrecisionUtcTime();
                    await Plugins.Tick();
                }

                await Task.WhenAll(tasks);

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
                await Task.WhenAll(Managers.Select(x => x.Value.Stop()));
            }
        }

        static async Task StartServerAsync()
        {
            int waitMs = Settings.MainLoopSleepMs;

            Logger.Info("Initializing DME components...");

            Logger.Info("*****************************************************************");
            string DME_SERVER_VERSION = "2.10.0009";
            Logger.Info($"DME Message Router Version {DME_SERVER_VERSION}");

            int KM_GetSoftwareID = 120;
            Logger.Info($"DME Message Router Application ID {KM_GetSoftwareID}");
            
            #region DateTime
            string date = DateTime.Now.ToString("MMMM/dd/yyyy");
            string time = DateTime.Now.ToString("hh:mm:ss tt");
            Logger.Info($"Date: {date}, Time: {time}");
            #endregion

            #region DME 
            Logger.Info($"Server IP = {SERVER_IP} [{IP_TYPE}]  TCP Port = {Settings.TCPPort}  UDP Port = {Settings.UDPPort}");
            TcpServer.Start();
            #endregion

            Logger.Info("*****************************************************************");

            // build and start medius managers per app id
            foreach (var applicationId in Settings.ApplicationIds)
            {
                var manager = new MediusManager(applicationId);
                Logger.Info($"Starting MPS for appid {applicationId}.");
                await manager.Start();
                Logger.Info($"MPS started.");
                Managers.Add(applicationId, manager);
            }

            // 
            Logger.Info("DME Initalized");

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

            // Update default rsa key
            Pipeline.Attribute.ScertClientAttribute.DefaultRsaAuthKey = Settings.DefaultKey;

            // Update file logger min level
            if (_fileLogger != null)
                _fileLogger.MinLevel = Settings.Logging.LogLevel;

            // Determine server ip
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
        }

            public static MediusManager GetManager(int applicationId, bool useDefaultOnMissing)
        {
            if (Managers.TryGetValue(applicationId, out var manager))
                return manager;

            if (useDefaultOnMissing && Managers.TryGetValue(0, out manager))
                return manager;

            return null;
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
