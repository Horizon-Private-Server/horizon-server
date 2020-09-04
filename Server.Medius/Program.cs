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
using Server.Mods;
using Server.Medius.Models;
using Server.Database;
using Server.Medius.Config;
using NReco.Logging.File;
using Server.Common.Logging;
using Server.Plugins;

namespace Server.Medius
{
    class Program
    {
        public const string CONFIG_FILE = "config.json";
        public const string DB_CONFIG_FILE = "db.config.json";
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
        public static DbController Database = new DbController(DB_CONFIG_FILE);

        public static IPAddress SERVER_IP = IPAddress.Parse("192.168.0.178");

        public static MediusManager Manager = new MediusManager();
        public static PluginsManager Plugins = null;

        public static MUIS UniverseInfoServer = new MUIS();
        public static MAS AuthenticationServer = new MAS();
        public static MLS LobbyServer = new MLS();
        public static MPS ProxyServer = new MPS();
        public static NAT NATServer = new NAT();

        public static int TickMS => 1000 / (Settings?.TickRate ?? 10);

        private static ulong _sessionKeyCounter = 0;
        private static int sleepMS = 0;
        private static readonly object _sessionKeyCounterLock = (object)_sessionKeyCounter;

        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<Program>();

        static async Task StartServerAsync()
        {
            DateTime lastConfigRefresh = DateTime.UtcNow;

#if DEBUG
            Stopwatch sw = new Stopwatch();
            int ticks = 0;
#endif

            Logger.Info("Starting medius components...");

            Logger.Info($"Starting MUIS on port {UniverseInfoServer.Port}.");
            UniverseInfoServer.Start();
            Logger.Info($"MUIS started.");

            Logger.Info($"Starting MAS on port {AuthenticationServer.Port}.");
            AuthenticationServer.Start();
            Logger.Info($"MAS started.");

            Logger.Info($"Starting MLS on port {LobbyServer.Port}.");
            LobbyServer.Start();
            Logger.Info($"MLS started.");

            Logger.Info($"Starting MPS on port {ProxyServer.Port}.");
            ProxyServer.Start();
            Logger.Info($"MPS started.");

            Logger.Info($"Starting NAT on port {NATServer.Port}.");
            NATServer.Start();
            Logger.Info($"NAT started.");

            // 
            Logger.Info("Started.");

            try
            {
#if DEBUG
                sw.Start();
#endif

                while (true)
                {
#if DEBUG
                    ++ticks;
                    if (sw.Elapsed.TotalSeconds > 5f)
                    {
                        // 
                        sw.Stop();
                        float tps = ticks / (float)sw.Elapsed.TotalSeconds;
                        float error = MathF.Abs(Settings.TickRate - tps) / Settings.TickRate;

                        if (error > 0.1f)
                            Logger.Error($"Average TPS: {tps} is {error}% off of target {Settings.TickRate}");

                        sw.Restart();
                        ticks = 0;
                    }
#endif




                    // Tick
                    await UniverseInfoServer.Tick();
                    await AuthenticationServer.Tick();
                    await LobbyServer.Tick();
                    await ProxyServer.Tick();
                    await NATServer.Tick();

                    // Tick manager
                    Manager.Tick();

                    // Reload config
                    if ((DateTime.UtcNow - lastConfigRefresh).TotalMilliseconds > Settings.RefreshConfigInterval)
                    {
                        RefreshConfig();
                        lastConfigRefresh = DateTime.UtcNow;
                    }



                    Thread.Sleep(sleepMS);
                }
            }
            finally
            {
                await UniverseInfoServer.Stop();
                await AuthenticationServer.Stop();
                await LobbyServer.Stop();
                await ProxyServer.Stop();
                await NATServer.Stop();
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
                    MaxRollingFiles = LogSettings.Singleton.RollingFileCount,
                    FormatLogEntry = (msg) =>
                    {
                        if (msg.LogLevel >= LogSettings.Singleton.LogLevel)
                            return msg.Message;

                        return null;
                    }
                };
                InternalLoggerFactory.DefaultFactory.AddProvider(new FileLoggerProvider(LogSettings.Singleton.LogPath, loggingOptions));
            }

            // Optionally add console logger (always enabled when debugging)
#if DEBUG
            InternalLoggerFactory.DefaultFactory.AddProvider(new ConsoleLoggerProvider((s, level) => level >= LogSettings.Singleton.LogLevel, true));
#else
            if (Settings.LogToConsole)
                InternalLoggerFactory.DefaultFactory.AddProvider(new ConsoleLoggerProvider((s, level) => level >= LogSettings.Singleton.LogLevel, true));
#endif

            // Initialize plugins
            Plugins = new PluginsManager(PLUGINS_PATH);

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
                // Save defaults
                File.WriteAllText(CONFIG_FILE, JsonConvert.SerializeObject(Settings, Formatting.Indented));
            }

            // Set LogSettings singleton
            LogSettings.Singleton = Settings.Logging;

            // Determine server ip
            if (!String.IsNullOrEmpty(Settings.ServerIpOverride))
            {
                SERVER_IP = IPAddress.Parse(Settings.ServerIpOverride);
            }
            else
            {
                SERVER_IP = IPAddress.Parse(GetIPAddress());
            }

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

            // Load tick time into sleep ms for main loop
            sleepMS = TickMS;
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

            // Load tick time into sleep ms for main loop
            sleepMS = TickMS;
        }

        /// <summary>
        /// From https://www.c-sharpcorner.com/blogs/how-to-get-public-ip-address-using-c-sharp1
        /// </summary>
        /// <returns></returns>
        static string GetIPAddress()
        {
            String address;
            WebRequest request = WebRequest.Create("http://checkip.dyndns.org/");
            using (WebResponse response = request.GetResponse())
            using (StreamReader stream = new StreamReader(response.GetResponseStream()))
            {
                address = stream.ReadToEnd();
            }

            int first = address.IndexOf("Address: ") + 9;
            int last = address.LastIndexOf("</body>");
            address = address.Substring(first, last - first);

            return address;
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
