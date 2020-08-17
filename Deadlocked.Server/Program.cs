using Deadlocked.Server.Accounts;
using Deadlocked.Server.Config;
using Deadlocked.Server.Medius;
using Medius.Crypto;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Math;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;

namespace Deadlocked.Server
{
    class Program
    {
        public const string CONFIG_FILE = "config.json";
        public const string DB_FILE = "db.json";
        public const string KEY = "42424242424242424242424242424242424242424242424242424242424242424242424242424242424242424242424242424242424242424242424242424242";

        public static PS2_RSA GlobalAuthKey = new PS2_RSA(
            new BigInteger("10315955513017997681600210131013411322695824559688299373570246338038100843097466504032586443986679280716603540690692615875074465586629501752500179100369237", 10),
            new BigInteger("17", 10),
            new BigInteger("4854567300243763614870687120476899445974505675147434999327174747312047455575182761195687859800492317495944895566174677168271650454805328075020357360662513", 10)
        );

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
        private static readonly object _sessionKeyCounterLock = (object)_sessionKeyCounter;

        static void Main(string[] args)
        {
            Initialize();

            int sleepMS = TickMS;
            DateTime lastDMECheck = DateTime.UtcNow;
            DateTime lastConfigRefresh = DateTime.UtcNow;

            Console.WriteLine("Starting medius components...");

            Console.WriteLine($"Starting MUIS on port {UniverseInfoServer.Port}.");
            UniverseInfoServer.Start();
            Console.WriteLine($"MUIS started.");

            Console.WriteLine($"Starting MAS on port {AuthenticationServer.Port}.");
            AuthenticationServer.Start();
            Console.WriteLine($"MUIS started.");

            Console.WriteLine($"Starting MLS on port {LobbyServer.Port}.");
            LobbyServer.Start();
            Console.WriteLine($"MUIS started.");

            Console.WriteLine($"Starting MPS on port {ProxyServer.Port}.");
            ProxyServer.Start();
            Console.WriteLine($"MUIS started.");

            Console.WriteLine($"Starting NAT on port {NATServer.Port}.");
            NATServer.Start();
            Console.WriteLine($"MUIS started.");

            // 
            Console.WriteLine("Started.");
            while (true)
            {
                // Remove old clients
                for (int i = 0; i < Clients.Count; ++i)
                {
                    if (Clients[i] == null || !Clients[i].IsConnected)
                    {
                        Console.WriteLine($"Destroying Client SK:{Clients[i].SessionKey} Token:{Clients[i].Token} Name:{Clients[i].ClientAccount?.AccountName}");
                        Clients[i]?.Logout();
                        Clients.RemoveAt(i);
                        --i;
                    }
                }

                // Tick
                UniverseInfoServer.Tick();
                AuthenticationServer.Tick();
                LobbyServer.Tick();
                ProxyServer.Tick();
                NATServer.Tick();

                // Tick channels
                for (int i = 0; i < Channels.Count; ++i)
                {
                    if (Channels[i].ReadyToDestroy)
                    {
                        Console.WriteLine($"Destroying Channel Id:{Channels[i].Id} Name:{Channels[i].Name}");
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
                        Console.WriteLine($"Destroying Game Id:{Games[i].Id} Name:{Games[i].GameName}");
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

            // Load account db
            if (File.Exists(DB_FILE))
            {
                // Populate existing object
                try { JsonConvert.PopulateObject(File.ReadAllText(DB_FILE), Database, serializerSettings); }
                catch (Exception e) { Console.WriteLine(e); }
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
                MaxPlayers = 256,
                Name = "Default",
                Type = ChannelType.Lobby
            });
        }

        /// <summary>
        /// 
        /// </summary>
        static void RefreshConfig()
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

            // Determine server ip
            if (!String.IsNullOrEmpty(Settings.ServerIpOverride))
            {
                SERVER_IP = IPAddress.Parse(Settings.ServerIpOverride);
            }
            else
            {
                SERVER_IP = IPAddress.Parse(GetIPAddress());
            }
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
                Console.WriteLine($"Unable to find DmeServer binary at {dmePath}");
                return;
            }

            var process = Process.GetProcesses().FirstOrDefault(x => x.ProcessName.Contains("DmeServer"));

            if (process == null)
            {
                Console.WriteLine("Dme Server not running. Starting...");

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
