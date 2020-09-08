using DotNetty.Common.Internal.Logging;
using Microsoft.Extensions.Logging.Console;
using Newtonsoft.Json;
using NReco.Logging.File;
using Org.BouncyCastle.Math;
using RT.Cryptography;
using RT.Models;
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

        public static int TickMS => 1000 / (Settings?.TickRate ?? 10);
        public static int UdpTickMS => 1000 / (Settings?.UdpTickRate ?? 30);

        private static FileLoggerProvider _fileLogger = null;
        private static ulong _sessionKeyCounter = 0;
        private static int _sleepMS = 0;
        private static int _udpSleepMs = 0;
        private static readonly object _sessionKeyCounterLock = (object)_sessionKeyCounter;
        private static bool _isRunning = true;

        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<Program>();



        static async Task StartServerAsync()
        {
            DateTime lastDMECheck = DateTime.UtcNow;
            DateTime lastConfigRefresh = DateTime.UtcNow;
            Stopwatch tickSw = new Stopwatch();

#if DEBUG
            Stopwatch sw = new Stopwatch();
            int ticks = 0;
#endif

            Logger.Info("Starting medius components...");

            Logger.Info($"Starting TCP on port {TcpServer.Port}.");
            TcpServer.Start();
            Logger.Info($"TCP started.");

            await Manager.Start();

            // 
            Logger.Info("Started.");

            new Thread(new ParameterizedThreadStart(async (s) =>
            {
                Stopwatch udpTickSw = new Stopwatch();
                while (_isRunning)
                {
                    // Restart stopwatch
                    udpTickSw.Restart();

                    try
                    {
                        Plugins.OnEvent(PluginEvent.DME_UDP_TICK, null);

                        // 
                        await Manager.TickUdp();
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                    }

                    // Sleep
                    await Task.Delay((int)Math.Max(0, _udpSleepMs - udpTickSw.ElapsedMilliseconds));
                }
            })).Start();

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
                            Logger.Error($"Average TPS: {tps} is {error * 100}% off of target {Settings.TickRate}");

                        sw.Restart();
                        ticks = 0;
                    }
#endif


                    //
                    tickSw.Restart();

                    // Check if connected
                    if (Manager.IsConnected)
                    {
                        // Tick
                        await TcpServer.Tick();

                        // Tick manager
                        await Manager.Tick();

                        // Tick plugins
                        Plugins.Tick();

                        // Send
                        await TcpServer.SendQueue();
                    }
                    else if ((DateTime.UtcNow - Manager.TimeLostConnection)?.TotalSeconds > Settings.MPSReconnectInterval)
                    {
                        // Try to reconnect to the proxy server
                        await Manager.Start();
                    }

                    // Reload config
                    if ((DateTime.UtcNow - lastConfigRefresh).TotalMilliseconds > Settings.RefreshConfigInterval)
                    {
                        RefreshConfig();
                        lastConfigRefresh = DateTime.UtcNow;
                    }

                    await Task.Delay((int)Math.Max(0, _sleepMS - tickSw.ElapsedMilliseconds));
                }
            }
            finally
            {
                _isRunning = false;
                await TcpServer.Stop();
                await Manager.Stop();
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
            if (!Settings.UsePublicIp)
            {
                SERVER_IP = GetLocalIPAddress();
            }
            else
            {
                SERVER_IP = IPAddress.Parse(GetPublicIPAddress());
            }

            // Load tick time into sleep ms for main loop
            _sleepMS = TickMS;
            _udpSleepMs = UdpTickMS;
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

            // Load tick time into sleep ms for main loop
            _sleepMS = TickMS;
            _udpSleepMs = UdpTickMS;
        }

        /// <summary>
        /// From https://www.c-sharpcorner.com/blogs/how-to-get-public-ip-address-using-c-sharp1
        /// </summary>
        /// <returns></returns>
        static string GetPublicIPAddress()
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

        static IPAddress GetLocalIPAddress()
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                return null;

            // order interfaces by speed and filter out down and loopback
            // take first of the remaining
            var firstUpInterface = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(c => c.NetworkInterfaceType != NetworkInterfaceType.Loopback && c.OperationalStatus == OperationalStatus.Up);
            if (firstUpInterface != null)
            {
                var props = firstUpInterface.GetIPProperties();
                // get first IPV4 address assigned to this interface
                return props.UnicastAddresses
                    .Where(c => c.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(c => c.Address)
                    .FirstOrDefault();
            }

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
