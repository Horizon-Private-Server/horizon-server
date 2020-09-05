using DotNetty.Common.Internal.Logging;
using Newtonsoft.Json;
using Server.NAT.Config;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Server.NAT
{
    class Program
    {
        public const string CONFIG_FILE = "config.json";

        public static ServerSettings Settings = new ServerSettings();
        public static NAT NATServer = new NAT();

        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<Program>();


        static void Main(string[] args)
        {
            // 
            Initialize();

            Logger.Info($"Starting NAT on port {NATServer.Port}.");
            Task.WaitAll(NATServer.Start());
            Logger.Info($"NAT started.");

            while (NATServer.IsRunning)
                Thread.Sleep(500);
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
        }
    }
}
