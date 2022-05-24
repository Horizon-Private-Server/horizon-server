using Org.BouncyCastle.Math;
using RT.Cryptography;
using Server.Common.Logging;

namespace Server.Test.Config
{
    public class ServerSettings
    {
        /// <summary>
        /// MUM connection information
        /// </summary>
        public MediusSettings Medius { get; set; } = new MediusSettings();

        /// <summary>
        /// How many milliseconds before refreshing the config.
        /// </summary>
        public int RefreshConfigInterval = 5000;

        /// <summary>
        /// Application id.
        /// </summary>
        public int ApplicationId { get; set; } = 0;

        /// <summary>
        /// Logging settings.
        /// </summary>
        public LogSettings Logging { get; set; } = new LogSettings();
    }

    public class MediusSettings
    {
        /// <summary>
        /// Ip of the Medius Authentication server.
        /// </summary>
        public string Ip { get; set; } = "127.0.0.1";

        /// <summary>
        /// The port that the Authentication server is bound to.
        /// </summary>
        public short AuthPort { get; set; } = 10075;

        /// <summary>
        /// The port that the Lobby server is bound to.
        /// </summary>
        public short LobbyPort { get; set; } = 10078;

        /// <summary>
        /// The port that the Proxy server is bound to.
        /// </summary>
        public short ProxyPort { get; set; } = 10077;

        /// <summary>
        /// The port that the Dme server is bound to.
        /// </summary>
        public short DmePort { get; set; } = 10073;

        /// <summary>
        /// Key used to establish initial handshake with MPS.
        /// </summary>
        public PS2_RSA Key { get; set; } = new PS2_RSA(
            new BigInteger("10315955513017997681600210131013411322695824559688299373570246338038100843097466504032586443986679280716603540690692615875074465586629501752500179100369237"),
            new BigInteger("17"),
            new BigInteger("4854567300243763614870687120476899445974505675147434999327174747312047455575182761195687859800492317495944895566174677168271650454805328075020357360662513")
            );
    }
}