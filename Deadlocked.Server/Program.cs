using Deadlocked.Server.Accounts;
using Deadlocked.Server.Config;
using Deadlocked.Server.Medius;
using Deadlocked.Server.Medius.Models;
using Deadlocked.Server.Medius.Models.Packets;
using Deadlocked.Server.Mods;
using DotNetty.Common.Internal.Logging;
using Medius.Crypto;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
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

namespace Deadlocked.Server
{
    class Program
    {
        public const string CONFIG_FILE = "config.json";
        public const string DB_FILE = "db.json";
        public const string KEY = "42424242424242424242424242424242424242424242424242424242424242424242424242424242424242424242424242424242424242424242424242424242";

        public readonly static PS2_RSA GlobalAuthKey = new PS2_RSA(
            new BigInteger("10315955513017997681600210131013411322695824559688299373570246338038100843097466504032586443986679280716603540690692615875074465586629501752500179100369237", 10),
            new BigInteger("17", 10),
            new BigInteger("4854567300243763614870687120476899445974505675147434999327174747312047455575182761195687859800492317495944895566174677168271650454805328075020357360662513", 10)
        );

        /// <summary>
        /// The DME connects to MPS with its own RSA keypair.
        /// I'm not sure what it's using because this key doesn't appear to work.
        /// Refer to BaseScertMessage.Instantiate() for hack solution
        /// </summary>
        public readonly static PS2_RSA DmeAuthKey = new PS2_RSA(
            new BigInteger("9848219843138420844191243034535393511626819869175602765525114154343233366275827782177650840711581912404543790075101312290915342641475187759789398933592597", 10),
            new BigInteger("17", 10),
            new BigInteger("5213763446367399270454187488871678917920081107210613228807413375828770605675333161178894352915566278302088822845345481840726284620095756152508903398440785", 10)
        );


        public readonly static RSA_KEY GlobalAuthPublic = new RSA_KEY(GlobalAuthKey.N.ToByteArrayUnsigned().Reverse().ToArray());
        public readonly static RSA_KEY GlobalAuthPrivate = new RSA_KEY(GlobalAuthKey.D.ToByteArrayUnsigned().Reverse().ToArray());

        public static List<ClientObject> Clients = new List<ClientObject>();
        public static List<Channel> Channels = new List<Channel>();
        public static List<Game> Games = new List<Game>();

        public static ServerSettings Settings = new ServerSettings();
        public static ServerDB Database = new ServerDB();

        public static IPAddress SERVER_IP = IPAddress.Parse("192.168.0.178");

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
            DateTime lastDMECheck = DateTime.UtcNow;
            DateTime lastConfigRefresh = DateTime.UtcNow;

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
                while (true)
                {
                    // Remove old clients
                    for (int i = 0; i < Clients.Count; ++i)
                    {
                        if (Clients[i] == null || !Clients[i].IsConnected)
                        {
                            Logger.Info($"Destroying Client SK:{Clients[i]} Name:{Clients[i].ClientAccount?.AccountName}");
                            Clients[i]?.Logout();
                            Clients.RemoveAt(i);
                            --i;
                        }
                    }

                    // Tick
                    await UniverseInfoServer.Tick();
                    await AuthenticationServer.Tick();
                    await LobbyServer.Tick();
                    await ProxyServer.Tick();
                    NATServer.Tick();

                    // Tick channels
                    for (int i = 0; i < Channels.Count; ++i)
                    {
                        if (Channels[i].ReadyToDestroy)
                        {
                            Logger.Info($"Destroying Channel Id:{Channels[i].Id} Name:{Channels[i].Name}");
                            Channels.RemoveAt(i);
                            --i;
                        }
                        else
                        {
                            Channels[i].Tick();
                        }
                    }

                    // Tick games
                    for (int i = 0; i < Games.Count; ++i)
                    {
                        if (Games[i].ReadyToDestroy)
                        {
                            Logger.Info($"Destroying Game Id:{Games[i].Id} Name:{Games[i].GameName}");
                            Games[i].EndGame();
                            Games.RemoveAt(i);
                            --i;
                        }
                        else
                        {
                            Games[i].Tick();
                        }
                    }

                    // Check DME
                    if (Program.Settings.DmeRestartOnCrash && !string.IsNullOrEmpty(Program.Settings.DmeStartPath) && (DateTime.UtcNow - lastDMECheck).TotalSeconds > 1)
                    {
                        EnsureDMERunning(Program.Settings.DmeStartPath);
                        lastDMECheck = DateTime.UtcNow;
                    }

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
                NATServer.Stop();
            }
        }

        static void Main(string[] args)
        {
            // 
            InternalLoggerFactory.DefaultFactory.AddProvider(new ConsoleLoggerProvider((s, level) => level >= Settings.LogLevel, false));

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
                // Add empty patch to default config output
                // This helps a user understand the format
                if (Settings.Patches.Count == 0)
                    Settings.Patches.Add(new Patch());

                // Add empty game mode to default config output
                if (Settings.Gamemodes.Count == 0)
                    Settings.Gamemodes.Add(new Gamemode());

                // Save defaults
                File.WriteAllText(CONFIG_FILE, JsonConvert.SerializeObject(Settings, Formatting.Indented));
            }

            // Load account db
            if (File.Exists(DB_FILE))
            {
                // Populate existing object
                try { JsonConvert.PopulateObject(File.ReadAllText(DB_FILE), Database, serializerSettings); }
                catch (Exception e) { Logger.Error(e); }
            }

            // Save db
            Database.Save();

            // Determine server ip
            if (!String.IsNullOrEmpty(Settings.ServerIpOverride))
            {
                SERVER_IP = IPAddress.Parse(Settings.ServerIpOverride);
            }
            else
            {
                SERVER_IP = IPAddress.Parse(GetIPAddress());
            }

            // Initialize default channel
            Channels.Add(new Channel()
            {
                Id = Settings.DefaultChannelId,
                ApplicationId = Program.Settings.ApplicationId,
                MaxPlayers = 256,
                Name = "Default",
                Type = ChannelType.Lobby
            });

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
                // Clear collections to prevent additive loading
                Settings.Patches.Clear();
                Settings.Gamemodes.Clear();
                Settings.RtLogFilter = new string[0];

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

        /// <summary>
        /// Ensures that the DME server is running while this is running.
        /// </summary>
        static void EnsureDMERunning(string dmePath)
        {
            if (!File.Exists(dmePath))
            {
                Logger.Error($"Unable to find DmeServer binary at {dmePath}");
                return;
            }

            var process = Process.GetProcesses().FirstOrDefault(x => x.ProcessName.Contains("DmeServer"));
            if (process == null)
            {
                Logger.Error("Dme Server not running. Starting...");

                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    Process.Start(new ProcessStartInfo()
                    {
                        WorkingDirectory = Environment.CurrentDirectory,
                        FileName = dmePath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    Process.Start(new ProcessStartInfo()
                    {
                        WorkingDirectory = Environment.CurrentDirectory,
                        FileName = dmePath
                    });
                }
            }
        }

        public static Channel GetChannelById(int channelId)
        {
            return Channels.FirstOrDefault(x => x.Id == channelId);
        }

        public static Game GetGameById(int id)
        {
            return Games.FirstOrDefault(x => x.Id == id);
        }

        public static ClientObject GetClientByAccountId(int accountId)
        {
            return Clients.FirstOrDefault(x => x.IsConnected && x.ClientAccount?.AccountId == accountId);
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
