using DotNetty.Common.Internal.Logging;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Newtonsoft.Json;
using NReco.Logging.File;
using Org.BouncyCastle.Math;
using RT.Cryptography;
using RT.Models;
using Server.Common;
using Server.Common.Logging;
using Server.Database;
using Server.Dme.Config;
using Server.Dme.Models;
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

    public class Program
    {
        private static string CONFIG_DIRECTIORY = "./";
        public static string CONFIG_FILE => Path.Combine(CONFIG_DIRECTIORY, "dme.json");
        public static string DB_CONFIG_FILE => Path.Combine(CONFIG_DIRECTIORY, "db.config.json");
        public static string DB_SIM_FILE => Path.Combine(CONFIG_DIRECTIORY, "simulated.db");

        public static readonly Stopwatch Stopwatch = Stopwatch.StartNew();
        public static DbController Database = null;

        public static ServerSettings Settings = new ServerSettings();
        private static Dictionary<int, AppSettings> _appSettings = new Dictionary<int, AppSettings>();
        private static AppSettings _defaultAppSettings = new AppSettings(0);

        public static IPAddress SERVER_IP = IPAddress.Parse("192.168.0.178");

        public static Dictionary<int, MediusManager> Managers = new Dictionary<int, MediusManager>();
        public static TcpServer TcpServer = new TcpServer();
        public static PluginsManager Plugins = null;

        private static FileLoggerProvider _fileLogger = null;
        private static ulong _sessionKeyCounter = 0;
        private static readonly object _sessionKeyCounterLock = (object)_sessionKeyCounter;
        private static DateTime _timeLastPluginTick = Utils.GetHighPrecisionUtcTime();

        private static int _ticks = 0;
        private static Stopwatch _sw = new Stopwatch();
        private static Stopwatch _loopSw = new Stopwatch();
        private static DateTime _lastConfigRefresh = Utils.GetHighPrecisionUtcTime();
        private static DateTime? _lastSuccessfulDbAuth = null;

        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<Program>();

        static int metricCooldownTicks = 0;
        static string metricPrintString = null;
        static int metricIndent = 0;

        static async Task TickAsync()
        {
            try
            {
#if DEBUG
                if (!_sw.IsRunning)
                    _sw.Start();
#endif

#if DEBUG
                ++_ticks;
                if (_sw.Elapsed.TotalSeconds > 5f)
                {
                    // 
                    _sw.Stop();
                    var averageMsPerTick = 1000 * (_sw.Elapsed.TotalSeconds / _ticks);
                    var error = Math.Abs(Settings.MainLoopSleepMs - averageMsPerTick) / Settings.MainLoopSleepMs;

                    //if (error > 0.1f)
                    //    Logger.Error($"Average Ms between ticks is: {averageMsPerTick} is {error * 100}% off of target {Settings.MainLoopSleepMs}");

                    //var dt = DateTime.UtcNow - Utils.GetHighPrecisionUtcTime();
                    //if (Math.Abs(dt.TotalMilliseconds) > 50)
                    //    Logger.Error($"System clock and local clock are out of sync! delta ms: {dt.TotalMilliseconds}");

                    _sw.Restart();
                    _ticks = 0;
                }
#endif

                // Attempt to authenticate with the db middleware
                // We do this every 24 hours to get a fresh new token
                // Auth failure no longer blocks the tick loop — server keeps running with cached data
                if (!await Database.AmIAuthenticated() || (_lastSuccessfulDbAuth == null || (Utils.GetHighPrecisionUtcTime() - _lastSuccessfulDbAuth.Value).TotalHours > 24))
                {
                    if (await Database.Authenticate())
                    {
                        _lastSuccessfulDbAuth = Utils.GetHighPrecisionUtcTime();
                        Logger.Info("Successfully authenticated with the db middleware server");

                        // refresh app settings
                        await RefreshAppSettings();

                        // reconnect to MPS
                        foreach (var manager in Managers)
                            if (manager.Value != null && !manager.Value.IsConnected)
                                await manager.Value.Start();
                    }
                    else
                    {
                        Logger.Error("Unable to authenticate with the db middleware server");
                    }
                }

                await TimeAsync("in", async () =>
                {
                    // handle incoming
                    {
                        var tasks = new List<Task>()
                        {
                            TcpServer.HandleIncomingMessages()
                        };

                        foreach (var manager in Managers)
                        {
                            if (manager.Value.IsConnected)
                            {
                                tasks.Add(manager.Value.HandleIncomingMessages());
                            }
                        }

                        await Task.WhenAll(tasks);
                    }
                });

                await TimeAsync("plugins", async () =>
                {
                    // Tick plugins
                    if ((Utils.GetHighPrecisionUtcTime() - _timeLastPluginTick).TotalMilliseconds > Settings.PluginTickIntervalMs)
                    {
                        _timeLastPluginTick = Utils.GetHighPrecisionUtcTime();
                        await Plugins.Tick();
                    }
                });

                await TimeAsync("out", async () =>
                {
                    // handle outgoing
                    {
                        var tasks = new List<Task>()
                    {
                        TcpServer.HandleOutgoingMessages()
                    };

                        foreach (var manager in Managers)
                        {
                            if (manager.Value.IsConnected)
                            {
                                tasks.Add(manager.Value.HandleOutgoingMessages());
                            }
                            else if ((Utils.GetHighPrecisionUtcTime() - manager.Value.TimeLostConnection)?.TotalSeconds > Settings.MPSReconnectInterval)
                            {
                                tasks.Add(manager.Value.Start());
                            }
                        }

                        await Task.WhenAll(tasks);
                    }
                });

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

                await Task.WhenAll(Managers.Select(x => x.Value.Stop()));
                await TcpServer.Stop();
            }
        }

        static async Task StartServerAsync()
        {
            int waitMs = Settings.MainLoopSleepMs;

            Logger.Info("Starting medius components...");

            // wait for db auth before accepting connections
            while (!await Database.AmIAuthenticated())
            {
                Logger.Info("Waiting for DB middleware to be ready...");
                if (await Database.Authenticate())
                    break;
                await Task.Delay(1000);
            }

            Logger.Info($"Starting TCP on port {TcpServer.Port}.");
            TcpServer.Start();
            Logger.Info($"TCP started.");

            // build and start medius managers per app id
            foreach (var applicationId in Settings.ApplicationIds)
            {
                var manager = new MediusManager(applicationId);
                //Logger.Info($"Starting MPS for appid {applicationId}.");
                //await manager.Start();
                //Logger.Info($"MPS started.");
                Managers.Add(applicationId, manager);
            }

            // 
            Logger.Info("Started.");

            // clock-based tick scheduling — accounts for actual work time
            _loopSw.Start();
            var nextTick = _loopSw.Elapsed;
            var lastMetricFlush = _loopSw.Elapsed;

            // iterate
            while (true)
            {
                // handle tick rate change
                if (Settings.MainLoopSleepMs != waitMs)
                {
                    waitMs = Settings.MainLoopSleepMs;
                    nextTick = _loopSw.Elapsed;
                }

                // tick
                await TimeAsync("tick", TickAsync);

                // flush metrics every 5 seconds (wall-clock based, not tick-count based)
                if ((_loopSw.Elapsed - lastMetricFlush).TotalSeconds >= 5)
                {
                    if (Settings.Logging.LogMetrics && !String.IsNullOrEmpty(metricPrintString))
                    {
                        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                            Logger.Info("\n" + metricPrintString);
                        else
                            Logger.Info(metricPrintString);
                        metricPrintString = "";
                    }
                    lastMetricFlush = _loopSw.Elapsed;
                }

                // wait for next tick, accounting for work time
                nextTick += TimeSpan.FromMilliseconds(waitMs);
                var delay = nextTick - _loopSw.Elapsed;
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay);
                else if (delay < TimeSpan.FromMilliseconds(-waitMs * 2))
                    // More than 2 ticks behind — reset to avoid spiral
                    nextTick = _loopSw.Elapsed;
            }
        }

        public static async Task Main(string[] args)
        {
            // get path to config directory from first argument
            if (args.Length > 0)
                CONFIG_DIRECTIORY = args[0];

            // 
            Database ??= new DbController(DB_CONFIG_FILE, DB_SIM_FILE);

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
            InternalLoggerFactory.DefaultFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter(level => level >= LogSettings.Singleton.LogLevel)
                    .AddConsole();
            });
            //InternalLoggerFactory.DefaultFactory.AddProvider(new ConsoleLoggerProvider((s, level) => level >= LogSettings.Singleton.LogLevel, true));
#else
            if (Settings.Logging.LogToConsole)
            {
                InternalLoggerFactory.DefaultFactory = LoggerFactory.Create(builder =>
                {
                    builder
                        .AddFilter(level => level >= LogSettings.Singleton.LogLevel)
                        .AddConsole();
                });
                //InternalLoggerFactory.DefaultFactory.AddProvider(new ConsoleLoggerProvider((s, level) => level >= LogSettings.Singleton.LogLevel, true));
            }
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
                if (Settings.ApplicationIds.Count == 0)
                    Settings.ApplicationIds.Add(0); // default app id

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
            if (usePublicIp != Settings.UsePublicIp)
                RefreshServerIp();

            // refresh app settings
            _ = RefreshAppSettings();
        }

        static async Task RefreshAppSettings()
        {
            try
            {
                if (!await Database.AmIAuthenticated())
                    return;

                // get supported app ids
                var appIdGroups = await Database.GetAppIds();
                if (appIdGroups == null)
                    return;

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

        public static MediusManager GetManager(int applicationId, bool useDefaultOnMissing)
        {
            if (Managers.TryGetValue(applicationId, out var manager))
                return manager;

            if (useDefaultOnMissing && Managers.TryGetValue(0, out manager))
                return manager;

            return null;
        }

        public static ClientObject GetClientByAccessToken(string accessToken)
        {
            return Managers.Select(x => x.Value.GetClientByAccessToken(accessToken)).FirstOrDefault(x => x != null);
        }

        public static string GenerateSessionKey()
        {
            lock (_sessionKeyCounterLock)
            {
                return (++_sessionKeyCounter).ToString();
            }
        }

        public static AppSettings GetAppSettingsOrDefault(int appId)
        {
            if (_appSettings.TryGetValue(appId, out var appSettings))
                return appSettings;

            return _defaultAppSettings;
        }

        #region Metrics

        public static void Time(string name, Action action)
        {
            if (!Settings.Logging.LogMetrics || metricCooldownTicks > 0)
            {
                action();
                return;
            }

            // 
            long ticksAtStart = Stopwatch.ElapsedTicks;

            // insert row before action
            metricPrintString += $"({"".PadRight(metricIndent * 2, ' ') + name,-32}:    {100:#.000} ms)\n";
            int stringIndex = metricPrintString.Length - 5 - 7;

            // run
            ++metricIndent;
            try
            {
                action();
            }
            finally
            {
                --metricIndent;
            }

            //
            long ticksAfterAction = Stopwatch.ElapsedTicks;
            var actionDurationMs = (1000f * (ticksAfterAction - ticksAtStart)) / (float)System.Diagnostics.Stopwatch.Frequency;

            //
            var replacementString = actionDurationMs.ToString("#.000").PadLeft(7, ' ').Substring(0, 7);
            char[] charArr = metricPrintString.ToCharArray();
            replacementString.CopyTo(0, charArr, stringIndex, replacementString.Length);
            metricPrintString = new string(charArr);
        }

        public static async Task TimeAsync(string name, Func<Task> action)
        {
            if (!Settings.Logging.LogMetrics || metricCooldownTicks > 0)
            {
                await action();
                return;
            }

            // 
            long ticksAtStart = Stopwatch.ElapsedTicks;

            // insert row before action
            metricPrintString += $"({"".PadRight(metricIndent * 2, ' ') + name,-32}:    {100:#.000} ms)\n";
            int stringIndex = metricPrintString.Length - 5 - 7;

            // run
            ++metricIndent;
            try
            {
                await action();
            }
            finally
            {
                --metricIndent;
            }

            //
            long ticksAfterAction = Stopwatch.ElapsedTicks;
            var actionDurationMs = (1000f * (ticksAfterAction - ticksAtStart)) / (float)System.Diagnostics.Stopwatch.Frequency;

            //
            var replacementString = actionDurationMs.ToString("#.000").PadLeft(7, ' ').Substring(0, 7);
            char[] charArr = metricPrintString.ToCharArray();
            replacementString.CopyTo(0, charArr, stringIndex, replacementString.Length);
            metricPrintString = new string(charArr);
        }

        public static async Task<T> TimeAsync<T>(string name, Func<Task<T>> action)
        {
            T result;
            if (!Settings.Logging.LogMetrics || metricCooldownTicks > 0)
            {
                return await action();
            }

            // 
            long ticksAtStart = Stopwatch.ElapsedTicks;

            // insert row before action
            metricPrintString += $"({"".PadRight(metricIndent * 2, ' ') + name,-32}:    {100:#.000} ms)\n";
            int stringIndex = metricPrintString.Length - 5 - 7;

            // run
            ++metricIndent;
            try
            {
                result = await action();
            }
            finally
            {
                --metricIndent;
            }

            //
            long ticksAfterAction = Stopwatch.ElapsedTicks;
            var actionDurationMs = (1000f * (ticksAfterAction - ticksAtStart)) / (float)System.Diagnostics.Stopwatch.Frequency;

            //
            var replacementString = actionDurationMs.ToString("#.000").PadLeft(7, ' ').Substring(0, 7);
            char[] charArr = metricPrintString.ToCharArray();
            replacementString.CopyTo(0, charArr, stringIndex, replacementString.Length);
            metricPrintString = new string(charArr);

            return result;
        }

        #endregion

    }
}
