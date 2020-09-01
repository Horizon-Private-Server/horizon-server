using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Math;
using RT.Common;
using RT.Cryptography;
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
        /// By default the dme server will grab its public ip.
        /// If this is set, it will use the ip provided here instead.
        /// </summary>
        public string ServerIpOverride { get; set; } = null;

        /// <summary>
        /// Time since last echo response before timing the client out.
        /// </summary>
        public int ClientTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Time since game created and host never connected to close the game world.
        /// </summary>
        public int GameTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Number of seconds before the server should send an echo to the client.
        /// </summary>
        public int ServerEchoInterval { get; set; } = 10;

        /// <summary>
        /// Number of ticks per second.
        /// </summary>
        public int TickRate { get; set; } = 10;

        /// <summary>
        /// Port of the TCP server.
        /// </summary>
        public int TCPPort { get; set; } = 10073;

        /// <summary>
        /// Port to bind the udp server to.
        /// </summary>
        public int UDPPort { get; set; } = 50000;

        /// <summary>
        /// Log level.
        /// </summary>
        public LogLevel LogLevel { get; set; } = LogLevel.Information;

        /// <summary>
        /// Collection of RT messages to print out
        /// </summary>
        public string[] RtLogFilter { get; set; } = Enum.GetNames(typeof(RT_MSG_TYPE));


        private Dictionary<RT_MSG_TYPE, bool> _rtLogFilters = new Dictionary<RT_MSG_TYPE, bool>();


        /// <summary>
        /// Whether or not the given rt message id should be logged
        /// </summary>
        public bool IsLog(RT_MSG_TYPE msgId)
        {
            if (_rtLogFilters.TryGetValue(msgId, out var result))
                return result;

            return false;
        }

        /// <summary>
        /// Does some post processing on the deserialized model.
        /// </summary>
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            // Load rt log filters in dictionary
            _rtLogFilters.Clear();
            if (RtLogFilter != null)
            {
                foreach (var filter in RtLogFilter)
                    _rtLogFilters.Add((RT_MSG_TYPE)Enum.Parse(typeof(RT_MSG_TYPE), filter), true);
            }
        }
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
