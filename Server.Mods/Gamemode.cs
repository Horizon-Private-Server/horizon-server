using RT.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace Server.Mods
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
        /// The address of the game entrypoint.
        /// </summary>
        public string GameEntrypoint = "0x00000000";

        /// <summary>
        /// The address of the lobby entrypoint.
        /// </summary>
        public string LobbyEntrypoint = "0x00000000";

        /// <summary>
        /// Path to the game mode payload.
        /// </summary>
        public string BinPath { get; set; } = null;


        private uint address = 0;
        private uint gameEntrypoint = 0;
        private uint lobbyEntrypoint = 0;

        /// <summary>
        /// Whether or not the given game mode is valid.
        /// </summary>
        public bool IsValid(int appId)
        {
            return Enabled && appId == ApplicationId && address != 0 && (gameEntrypoint != 0 || lobbyEntrypoint != 0) && File.Exists(BinPath);
        }

        /// <summary>
        /// Apply game mode to a given set of clients.
        /// </summary>
        public List<BaseScertMessage> GetPayload()
        {
            List<BaseScertMessage> messages = new List<BaseScertMessage>();

            // Add paylaod
            messages.AddRange(RT_MSG_SERVER_MEMORY_POKE.FromPayload(address, File.ReadAllBytes(BinPath)));

            return messages;
        }
        
        /// <summary>
        /// 
        /// </summary>
        public void SetModuleEntry(byte[] buffer, int index, bool isEnabled)
        {
            // Add module
            Array.Copy(BitConverter.GetBytes((int)(isEnabled ? 1 : 0)), 0, buffer, index + 0, 4);
            Array.Copy(BitConverter.GetBytes(gameEntrypoint), 0, buffer, index + 4, 4);
            Array.Copy(BitConverter.GetBytes(lobbyEntrypoint), 0, buffer, index + 8, 4);
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (Address.StartsWith("0x"))
                address = Convert.ToUInt32(Address, 16);
            else
                address = Convert.ToUInt32(Address);

            if (GameEntrypoint.StartsWith("0x"))
                gameEntrypoint = Convert.ToUInt32(GameEntrypoint, 16);
            else
                gameEntrypoint = Convert.ToUInt32(GameEntrypoint);

            if (LobbyEntrypoint.StartsWith("0x"))
                lobbyEntrypoint = Convert.ToUInt32(LobbyEntrypoint, 16);
            else
                lobbyEntrypoint = Convert.ToUInt32(LobbyEntrypoint);
        }
    }
}
