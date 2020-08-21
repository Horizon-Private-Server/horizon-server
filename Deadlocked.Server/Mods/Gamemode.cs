using Deadlocked.Server.Medius.Models;
using Deadlocked.Server.SCERT.Models;
using Deadlocked.Server.SCERT.Models.Packets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace Deadlocked.Server.Mods
{
    public class Gamemode
    {
        /// <summary>
        /// Whether or not the game mode is enabled.
        /// </summary>
        public bool Enabled = false;

        /// <summary>
        /// Application id the game mode is for.
        /// </summary>
        public int ApplicationId = 0;

        /// <summary>
        /// The fullname of the game mode.
        /// </summary>
        public string FullName = null;

        /// <summary>
        /// Strings that satisfy !gm <key> for this game mode.
        /// </summary>
        public string[] Keys = null;

        /// <summary>
        /// The address to inject the game mode to.
        /// </summary>
        public string Address = "0x00000000";

        /// <summary>
        /// Path to the game mode payload.
        /// </summary>
        public string BinPath { get; set; } = null;


        private uint address = 0;

        /// <summary>
        /// Whether or not the given game mode is valid.
        /// </summary>
        public bool IsValid(int appId)
        {
            return Enabled && appId == ApplicationId && address != 0 && File.Exists(BinPath);
        }

        /// <summary>
        /// Apply game mode to a given set of clients.
        /// </summary>
        public void Apply(IEnumerable<ClientObject> clients)
        {
            List<BaseScertMessage> messages = new List<BaseScertMessage>();

            // Add paylaod
            messages.AddRange(RT_MSG_SERVER_MEMORY_POKE.FromPayload(address, File.ReadAllBytes(BinPath)));

            // Add module
            byte[] moduleEntry = new byte[16];
            Array.Copy(BitConverter.GetBytes((int)1), 0, moduleEntry, 0, 4);
            Array.Copy(BitConverter.GetBytes(address), 0, moduleEntry, 4, 4);

            // 
            messages.Add(new RT_MSG_SERVER_MEMORY_POKE()
            {
                Address = 0x000CF000 + (0 * 16),
                Payload = moduleEntry
            });

            // Send each
            foreach (var client in clients)
                client?.Queue(messages);
        }

        /// <summary>
        /// Disables the gamemode module.
        /// </summary>
        public static void Disable(IEnumerable<ClientObject> clients)
        {
            // reset
            var modulePokes = RT_MSG_SERVER_MEMORY_POKE.FromPayload(0x000CF000 + (0 * 16), new byte[16]);

            // Send each
            foreach (var client in clients)
                client?.Queue(modulePokes);
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (Address.StartsWith("0x"))
                address = Convert.ToUInt32(Address, 16);
            else
                address = Convert.ToUInt32(Address);
        }
    }
}
