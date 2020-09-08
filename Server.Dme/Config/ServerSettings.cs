using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Math;
using RT.Common;
using RT.Cryptography;
using Server.Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Server.Dme.Config
{
    public class ServerSettings
    {
        /// <summary>
        /// MUM connection information
        /// </summary>
        public MPSSettings MPS { get; set; } = new MPSSettings();

        /// <summary>
        /// How many milliseconds before refreshing the config.
        /// </summary>
        public int RefreshConfigInterval = 5000;

        /// <summary>
        /// Application id.
        /// </summary>
        public int ApplicationId { get; set; } = 0;


        /// <summary>
        /// By default the server will grab its local ip.
        /// If this is set, it will use its public ip instead.
        /// </summary>
        public bool UsePublicIp { get; set; } = false;

        /// <summary>
        /// Time since last echo response before timing the client out.
        /// </summary>
        public int ClientTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Time since game created and host never connected to close the game world.
        /// </summary>
        public int GameTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Seconds between disconnects before the client attempts to reconnect to the proxy server.
        /// </summary>
        public int MPSReconnectInterval { get; set; } = 15;

        /// <summary>
        /// Number of seconds before the server should send an echo to the client.
        /// </summary>
        public int ServerEchoInterval { get; set; } = 10;

        /// <summary>
        /// Number of ticks per second.
        /// </summary>
        public int TickRate { get; set; } = 10;

        /// <summary>
        /// Number of ticks per second for the udp server.
        /// </summary>
        public int UdpTickRate { get; set; } = 30;

        /// <summary>
        /// Port of the TCP server.
        /// </summary>
        public int TCPPort { get; set; } = 10073;

        /// <summary>
        /// Port to bind the udp server to.
        /// </summary>
        public int UDPPort { get; set; } = 50000;

        /// <summary>
        /// Logging settings.
        /// </summary>
        public LogSettings Logging { get; set; } = new LogSettings();
    }

    public class MPSSettings
    {
        /// <summary>
        /// Ip of the Medius Authentication server.
        /// </summary>
        public string Ip { get; set; } = "127.0.0.1";

        /// <summary>
        /// The port that the Authentication server is bound to.
        /// </summary>
        public int Port { get; set; } = 10077;

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
