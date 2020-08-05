using Deadlocked.Server.Medius;
using Medius.Crypto;
using Org.BouncyCastle.Math;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Deadlocked.Server
{
    class Program
    {
        public const string KEY = "42424242424242424242424242424242424242424242424242424242424242424242424242424242424242424242424242424242424242424242424242424242";

        public static PS2_RSA GlobalAuthKey = new PS2_RSA(
            new BigInteger("10315955513017997681600210131013411322695824559688299373570246338038100843097466504032586443986679280716603540690692615875074465586629501752500179100369237", 10),
            new BigInteger("17", 10),
            new BigInteger("4854567300243763614870687120476899445974505675147434999327174747312047455575182761195687859800492317495944895566174677168271650454805328075020357360662513", 10)
        );

        public static List<ClientObject> Clients = new List<ClientObject>();
        public static List<Lobby> Lobbies = new List<Lobby>();

        public static IPAddress SERVER_IP = IPAddress.Parse("192.168.0.178");

        public static MUIS UniverseInfoServer = new MUIS();
        public static MAS AuthenticationServer = new MAS();
        public static MLS LobbyServer = new MLS();
        public static MPS ProxyServer = new MPS();
        public static int TickRate = 10;

        public static int AppId = 11184;

        public static int TickMS => 1000 / TickRate;

        static void Main(string[] args)
        {
            int sleepMS = TickMS;

            restart:;

            Console.WriteLine("Starting medius components...");

            UniverseInfoServer.Start();
            AuthenticationServer.Start();
            LobbyServer.Start();
            ProxyServer.Start();

            // 
            Console.WriteLine("Started. Press 1 to exit. Press 2 to restart.");
            while (true)
            {
                // Tick
                UniverseInfoServer.Tick();
                AuthenticationServer.Tick();
                LobbyServer.Tick();
                ProxyServer.Tick();

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
                        goto restart;
                    }
                }

                Thread.Sleep(sleepMS);
            }
        }
    }
}
