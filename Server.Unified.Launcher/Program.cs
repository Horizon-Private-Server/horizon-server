using System;
using System.IO;
using System.Threading.Tasks;

namespace Server.Unified.Launcher
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await Start();
        }

        static async Task Start()
        {
            var configDirectory = Path.Combine(Environment.CurrentDirectory, "config");
            if (!Directory.Exists(configDirectory))
                Directory.CreateDirectory(configDirectory);

            if (string.IsNullOrEmpty(configDirectory))
                throw new ArgumentException("Config directory cannot be null or empty.");
            else if (!Directory.Exists(configDirectory))
                throw new DirectoryNotFoundException($"Config directory '{configDirectory}' does not exist.");

            // init database
            var dbConfigPath = Path.Combine(configDirectory, Server.Medius.Program.DB_CONFIG_FILE);
            if (!File.Exists(dbConfigPath))
            {
                var config = new Database.Config.DbSettings();
                config.SimulatedMode = true;
                config.SimulatedEncryptionKey = Guid.NewGuid().ToString(); // generate random key
                File.WriteAllText(dbConfigPath, Newtonsoft.Json.JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented));
            }

            // init db controller
            var dbController = new Database.DbController(dbConfigPath, Path.Combine(configDirectory, "simulated.db"));
            Medius.Program.Database = dbController;
            Dme.Program.Database = dbController;

            // init medius config
            var mediusConfigPath = Path.Combine(configDirectory, Path.GetFileName(Server.Medius.Program.CONFIG_FILE));
            if (!File.Exists(mediusConfigPath))
            {
                var config = new Server.Medius.Config.ServerSettings();
                config.PluginsPath = "medius-plugins/";
                File.WriteAllText(mediusConfigPath, Newtonsoft.Json.JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented));

                // force create plugins directory
                var mediusPluginDirectory = Path.Combine(Environment.CurrentDirectory, config.PluginsPath);
                if (!Directory.Exists(mediusPluginDirectory))
                    Directory.CreateDirectory(mediusPluginDirectory);
            }

            // init dme config
            var dmeConfigPath = Path.Combine(configDirectory, Path.GetFileName(Server.Dme.Program.CONFIG_FILE));
            if (!File.Exists(dmeConfigPath))
            {
                var config = new Server.Dme.Config.ServerSettings();
                config.PluginsPath = "dme-plugins/";
                config.ApplicationIds.Clear();
                config.ApplicationIds.Add(0);
                File.WriteAllText(dmeConfigPath, Newtonsoft.Json.JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented));

                // force create plugins directory
                var dmePluginDirectory = Path.Combine(Environment.CurrentDirectory, config.PluginsPath);
                if (!Directory.Exists(dmePluginDirectory))
                    Directory.CreateDirectory(dmePluginDirectory);
            }

            // start all servers
            var natTask = Task.Run(async () => await Server.NAT.Program.Main(new string[] { configDirectory }));
            var muisTask = Task.Run(async () => await Server.UnivereInformation.Program.Main(new string[] { configDirectory }));
            var mediusTask = Task.Run(async () => await Server.Medius.Program.Main(new string[] { configDirectory }));
            await Task.Delay(5000);
            var dmeTask = Task.Run(async () => await Server.Dme.Program.Main(new string[] { configDirectory }));

            // wait for all servers to complete (they won't, unless there's an error)
            await Task.WhenAll(natTask, mediusTask, muisTask, dmeTask);
        }
    }
}
