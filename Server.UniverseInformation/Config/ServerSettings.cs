using Org.BouncyCastle.Math;
using RT.Cryptography;
using Server.Common.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.UnivereInformation.Config
{
    public class ServerSettings
    {
        /// <summary>
        /// How many milliseconds before refreshing the config.
        /// </summary>
        public int RefreshConfigInterval = 5000;

        /// <summary>
        /// Port of the MUIS server.
        /// </summary>
        public int[] Ports { get; set; } = new int[] { 10071 };

        /// <summary>
        /// Key used to authenticate clients.
        /// </summary>
        public RsaKeyPair DefaultKey { get; set; } = new RsaKeyPair(
            new BigInteger("10315955513017997681600210131013411322695824559688299373570246338038100843097466504032586443986679280716603540690692615875074465586629501752500179100369237", 10),
            new BigInteger("17", 10),
            new BigInteger("4854567300243763614870687120476899445974505675147434999327174747312047455575182761195687859800492317495944895566174677168271650454805328075020357360662513", 10)
            );

        /// <summary>
        /// Whether or not to encrypt messages.
        /// </summary>
        public bool EncryptMessages { get; set; } = true;

        /// <summary>
        /// Universes.
        /// </summary>
        public Dictionary<int, UniverseInfo> Universes { get; set; } = new Dictionary<int, UniverseInfo>();

        /// <summary>
        /// Logging settings.
        /// </summary>
        public LogSettings Logging { get; set; } = new LogSettings();
    }

    public class UniverseInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Endpoint { get; set; }
        public string SvoURL { get; set; }
        public string ExtendedInfo { get; set; }
        public int Port { get; set; }
        public uint UniverseId { get; set; }
    }
}
