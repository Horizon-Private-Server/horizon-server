using Deadlocked.Server.Accounts;
using Deadlocked.Server.Config;
using Deadlocked.Server.Medius;
using Medius.Crypto;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Math;
using System;
using System.Collections.Generic;
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

            restart:;

            Console.WriteLine("Starting medius components...");

            UniverseInfoServer.Start();
            AuthenticationServer.Start();
            LobbyServer.Start();
            ProxyServer.Start();
            NATServer.Start();

            // 
            Console.WriteLine("Started. Press 1 to exit. Press 2 to restart.");
            while (true)
            {
                // Remove old clients
                for (int i = 0; i < Clients.Count; ++i)
                {
                    if (Clients[i] == null || !Clients[i].IsConnected)
                    {
                        Clients[i]?.OnDestroy();
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

                // Remove old games
                for (int i = 0; i < Games.Count; ++i)
                {
                    if (Games[i].ReadyToDestroy)
                    {
                        Games[i].SendEndGame();
                        Games.RemoveAt(i);
                        --i;
                    }
                }

                // Tick games
                foreach (var game in Games)
                    game.Tick();

                // Check exit
                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.D1)
                        break;
                    if (key.Key == ConsoleKey.D2)
                    {
                        UniverseInfoServer.Stop();
                        AuthenticationServer.Stop();
                        LobbyServer.Stop();
                        NATServer.Stop();
                        goto restart;
                    }
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

        public static Game GetGameByGameId(int id)
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
