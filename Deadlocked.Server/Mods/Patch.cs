using Deadlocked.Server.Messages;
using Deadlocked.Server.SCERT.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace Deadlocked.Server.Mods
{
    public class Patch
    {

        /// <summary>
        /// Whether the patch file should be applied to connecting clients.
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Only apply patch to clients with a given app id.
        /// </summary>
        public int ApplicationId = 0;

        /// <summary>
        /// Address to apply patch file.
        /// </summary>
        public string Address { get; set; } = "0x00000000";

        /// <summary>
        /// Path to the binary.
        /// </summary>
        public string BinPath { get; set; } = null;

        /// <summary>
        /// Whether or not to use a hook.
        /// </summary>
        public bool HookEnabled { get; set; } = false;

        /// <summary>
        /// Address to place hook.
        /// </summary>
        public string HookAddress { get; set; } = "0x00000000";

        /// <summary>
        /// Value to place at hook address.
        /// </summary>
        public string HookValue { get; set; } = "0x00000000";

        // 
        private uint address = 0;
        private uint hookAddress = 0;
        private uint hookValue = 0;


        /// <summary>
        /// Turns patch into a collection of pokes.
        /// </summary>
        public List<BaseScertMessage> Serialize()
        {
            List<BaseScertMessage> results = new List<BaseScertMessage>();

            if (File.Exists(BinPath))
            {
                // Add payload
                results.AddRange(RT_MSG_SERVER_MEMORY_POKE.FromPayload(address, File.ReadAllBytes(BinPath)));

                // Add hook
                if (HookEnabled)
                {
                    results.Add(new RT_MSG_SERVER_MEMORY_POKE()
                    {
                        Address = hookAddress,
                        Payload = BitConverter.GetBytes(hookValue)
                    });
                }
            }

            return results;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (Address.StartsWith("0x"))
                address = Convert.ToUInt32(Address, 16);
            else
                address = Convert.ToUInt32(Address);

            if (HookAddress.StartsWith("0x"))
                hookAddress = Convert.ToUInt32(HookAddress, 16);
            else
                hookAddress = Convert.ToUInt32(HookAddress);

            if (HookValue.StartsWith("0x"))
                hookValue = Convert.ToUInt32(HookValue, 16);
            else
                hookValue = Convert.ToUInt32(HookValue);
        }
    }
}
