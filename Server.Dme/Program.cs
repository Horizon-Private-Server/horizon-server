using DotNetty.Common.Internal.Logging;
using Microsoft.Extensions.Logging.Console;
using Newtonsoft.Json;
using Org.BouncyCastle.Math;
using RT.Cryptography;
using RT.Models;
using Server.Dme.Config;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Dme
{

    class Program
    {
        public const string CONFIG_FILE = "config.json";
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

        public static int TickMS => 1000 / (Settings?.TickRate ?? 10);

        private static ulong _sessionKeyCounter = 0;
        private static int sleepMS = 0;
        private static readonly object _sessionKeyCounterLock = (object)_sessionKeyCounter;

        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<Program>();



        static async Task StartServerAsync()
        {
            DateTime lastDMECheck = DateTime.UtcNow;
            DateTime lastConfigRefresh = DateTime.UtcNow;

#if DEBUG
            Stopwatch sw = new Stopwatch();
            int ticks = 0;
#endif

            Logger.Info("Starting medius components...");

            Logger.Info($"Starting TCP on port {TcpServer.Port}.");
            TcpServer.Start();
            Logger.Info($"TCP started.");

            Logger.Info($"Connecting to MAS...");
            await Manager.Start();
            Logger.Info($"MAS connected.");

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
                    await TcpServer.Tick();

                    // Tick manager
                    await Manager.Tick();

                    // Send
                    await TcpServer.SendQueue();

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
                await TcpServer.Stop();
                await Manager.Stop();
            }
        }

        static void Main(string[] args)
        {
            // 
            InternalLoggerFactory.DefaultFactory.AddProvider(new ConsoleLoggerProvider((s, level) => level >= Settings.LogLevel, true));

            // 
            Initialize();

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

            // Determine server ip
            if (!String.IsNullOrEmpty(Settings.ServerIpOverride))
            {
                SERVER_IP = IPAddress.Parse(Settings.ServerIpOverride);
            }
            else
            {
                SERVER_IP = IPAddress.Parse(GetIPAddress());
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
