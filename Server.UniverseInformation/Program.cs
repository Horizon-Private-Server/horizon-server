using DotNetty.Common.Internal.Logging;
using Microsoft.Extensions.Logging.Console;
using Newtonsoft.Json;
using NReco.Logging.File;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC.Rfc7748;
using RT.Cryptography;
using Server.Common.Logging;
using Server.UnivereInformation.Config;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Server.UnivereInformation
{
    class Program
    {
        public const string CONFIG_FILE = "config.json";
        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<Program>();

        public static ServerSettings Settings = new ServerSettings();

        public readonly static PS2_RSA GlobalAuthKey = new PS2_RSA(
            new BigInteger("10315955513017997681600210131013411322695824559688299373570246338038100843097466504032586443986679280716603540690692615875074465586629501752500179100369237", 10),
            new BigInteger("17", 10),
            new BigInteger("4854567300243763614870687120476899445974505675147434999327174747312047455575182761195687859800492317495944895566174677168271650454805328075020357360662513", 10)
        );

        public static MUIS[] UniverseInfoServers = null;

        private static FileLoggerProvider _fileLogger = null;


        static async Task StartServerAsync()
        {
            DateTime lastConfigRefresh = DateTime.UtcNow;

            UniverseInfoServers = new MUIS[Settings.Ports.Length];
            for (int i = 0; i < UniverseInfoServers.Length; ++i)
            {
                Logger.Info($"Starting MUIS on port {Settings.Ports[i]}.");
                UniverseInfoServers[i] = new MUIS(Settings.Ports[i]);
                UniverseInfoServers[i].Start();
                Logger.Info($"MUIS started.");
            }

            try
            {
                while (true)
                {
                    // Tick
                    await Task.WhenAll(UniverseInfoServers.Select(x => x.Tick()));

                    // Reload config
                    if ((DateTime.UtcNow - lastConfigRefresh).TotalMilliseconds > Settings.RefreshConfigInterval)
                    {
                        RefreshConfig();
                        lastConfigRefresh = DateTime.UtcNow;
                    }

                    await Task.Delay(100);
                }
            }
            finally
            {
                await Task.WhenAll(UniverseInfoServers.Select(x => x.Stop()));
            }
        }

        static void Main(string[] args)
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

            // 
            StartServerAsync().Wait();
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
                // Add default localhost entry
                Settings.Universes.Add(0, new UniverseInfo()
                {
                    Name = "sample universe",
                    Endpoint = "url",
                    Port = 10075,
                    UniverseId = 1
                });

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
