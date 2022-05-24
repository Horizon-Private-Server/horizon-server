using DotNetty.Common.Internal.Logging;
using Microsoft.Extensions.Logging.Console;
using Newtonsoft.Json;
using NReco.Logging.File;
using Server.Common;
using Server.Common.Logging;
using Server.Test.Config;
using Server.Test.Medius;
using Server.Test.Test;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Test
{
    class Program
    {
        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<Program>();

        public const string CONFIG_FILE = "config.json";

        static FileLoggerProvider _fileLogger = null;
        static List<BaseClient> _clients = new List<BaseClient>();
        static DateTime _lastComponentLog = Utils.GetHighPrecisionUtcTime();
        static DateTime _lastConfigRefresh = Utils.GetHighPrecisionUtcTime();

        public static ServerSettings Settings = new ServerSettings();

        static async Task StartServerAsync()
        {
            Stopwatch sw = new Stopwatch();

            // Initialize the tests
            _clients.Add(new ClientLoginLogout(Settings.Medius.Ip, Settings.Medius.AuthPort));

            try
            {
                while (true)
                {
                    //
                    sw.Restart();

                    // Tick
                    await Task.WhenAll(_clients.Select(x => x.Tick()));

                    // 
                    if ((Utils.GetHighPrecisionUtcTime() - _lastComponentLog).TotalSeconds > 15f)
                    {
                        foreach (var client in _clients)
                            client.Log();

                        _lastComponentLog = Utils.GetHighPrecisionUtcTime();
                    }

                    // Reload config
                    if ((Utils.GetHighPrecisionUtcTime() - _lastConfigRefresh).TotalMilliseconds > Settings.RefreshConfigInterval)
                    {
                        RefreshConfig();
                        _lastConfigRefresh = Utils.GetHighPrecisionUtcTime();
                    }

                    while (sw.ElapsedMilliseconds < 5)
                        await Task.Yield();
                }
            }
            finally
            {

            }
        }

        static async Task Main(string[] args)
        {
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
    }
}
