using RT.Cryptography;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using RT.Models;
using RT.Common;
using Server.Mods;

namespace Server.Medius.Config
{
    public class ServerSettings
    {
        /// <summary>
        /// Beta specific config.
        /// </summary>
        public BetaConfig Beta { get; set; } = new BetaConfig();

        /// <summary>
        /// How many milliseconds before refreshing the config.
        /// </summary>
        public int RefreshConfigInterval = 5000;

        /// <summary>
        /// Compatible application ids. Null means all are accepted.
        /// </summary>
        public int[] ApplicationIds { get; set; } = null;

        /// <summary>
        /// Announcement.
        /// </summary>
        public string Announcement { get; set; } = "";


        /// <summary>
        /// Usage policy.
        /// </summary>
        public string UsagePolicy { get; set; } = "";


        /// <summary>
        /// Privacy policy.
        /// </summary>
        public string PrivacyPolicy { get; set; } = "";

        /// <summary>
        /// By default the server will grab its public ip.
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
        /// Port of the MUIS server.
        /// </summary>
        public int MUISPort { get; set; } = 10071;

        /// <summary>
        /// Port of the MAS server.
        /// </summary>
        public int MASPort { get; set; } = 10075;

        /// <summary>
        /// Port of the MLS server.
        /// </summary>
        public int MLSPort { get; set; } = 10078;

        /// <summary>
        /// Port of the MPS server.
        /// </summary>
        public int MPSPort { get; set; } = 10077;

        /// <summary>
        /// Port of the NAT server.
        /// </summary>
        public int NATPort { get; set; } = 10070;

        /// <summary>
        /// Time, in seconds, before timing out a Dme server.
        /// </summary>
        public int DmeTimeoutSeconds { get; set; } = 60;

        /// <summary>
        /// Key used to authenticate dme servers.
        /// </summary>
        public PS2_RSA MPSKey { get; set; } = new PS2_RSA(
            new BigInteger("10315955513017997681600210131013411322695824559688299373570246338038100843097466504032586443986679280716603540690692615875074465586629501752500179100369237", 10),
            new BigInteger("17", 10),
            new BigInteger("4854567300243763614870687120476899445974505675147434999327174747312047455575182761195687859800492317495944895566174677168271650454805328075020357360662513", 10)
            );

        /// <summary>
        /// Collection of patches to apply to logged in clients.
        /// </summary>
        public List<Patch> Patches { get; set; } = new List<Patch>();

        /// <summary>
        /// Collection of custom game modes.
        /// </summary>
        public List<Gamemode> Gamemodes { get; set; } = new List<Gamemode>();

        /// <summary>
        /// Log level.
        /// </summary>
        public LogLevel LogLevel { get; set; } = LogLevel.Information;

        /// <summary>
        /// Collection of RT messages to print out
        /// </summary>
        public string[] RtLogFilter { get; set; } = Enum.GetNames(typeof(RT_MSG_TYPE));

        /// <summary>
        /// Collection of Medius Lobby messages to print out
        /// </summary>
        public string[] MediusLobbyLogFilter { get; set; } = Enum.GetNames(typeof(MediusLobbyMessageIds));

        /// <summary>
        /// Collection of Medius Lobby Ext messages to print out
        /// </summary>
        public string[] MediusLobbyExtLogFilter { get; set; } = Enum.GetNames(typeof(MediusLobbyExtMessageIds));

        /// <summary>
        /// Collection of Medius Lobby Ext messages to print out
        /// </summary>
        public string[] MediusMGCLLogFilter { get; set; } = Enum.GetNames(typeof(MediusMGCLMessageIds));

        /// <summary>
        /// Collection of Medius Lobby Ext messages to print out
        /// </summary>
        public string[] MediusDMEExtLogFilter { get; set; } = Enum.GetNames(typeof(MediusDmeMessageIds));

        private Dictionary<RT_MSG_TYPE, bool> _rtLogFilters = new Dictionary<RT_MSG_TYPE, bool>();
        private Dictionary<MediusDmeMessageIds, bool> _dmeLogFilters = new Dictionary<MediusDmeMessageIds, bool>();
        private Dictionary<MediusLobbyMessageIds, bool> _lobbyLogFilters = new Dictionary<MediusLobbyMessageIds, bool>();
        private Dictionary<MediusMGCLMessageIds, bool> _mgclLogFilters = new Dictionary<MediusMGCLMessageIds, bool>();
        private Dictionary<MediusLobbyExtMessageIds, bool> _lobbyExtLogFilters = new Dictionary<MediusLobbyExtMessageIds, bool>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="appId"></param>
        /// <returns></returns>
        public bool IsCompatAppId(int appId)
        {
            if (ApplicationIds == null)
                return true;

            return ApplicationIds.Contains(appId);
        }

        /// <summary>
        /// Whether or not the given rt message id should be logged
        /// </summary>
        public bool IsLog(BaseScertMessage message)
        {
            if (message == null)
                return false;

            if (!_rtLogFilters.TryGetValue(message.Id, out var result) || !result)
                return false;

            switch (message)
            {
                case RT_MSG_SERVER_APP serverApp:
                    {
                        switch (serverApp.Message.PacketClass)
                        {
                            case NetMessageTypes.MessageClassDME: { return _dmeLogFilters.TryGetValue((MediusDmeMessageIds)serverApp.Message.PacketType, out var r) && r; }
                            case NetMessageTypes.MessageClassLobby: { return _lobbyLogFilters.TryGetValue((MediusLobbyMessageIds)serverApp.Message.PacketType, out var r) && r; }
                            case NetMessageTypes.MessageClassLobbyReport: { return _mgclLogFilters.TryGetValue((MediusMGCLMessageIds)serverApp.Message.PacketType, out var r) && r; }
                            case NetMessageTypes.MessageClassLobbyExt: { return _lobbyExtLogFilters.TryGetValue((MediusLobbyExtMessageIds)serverApp.Message.PacketType, out var r) && r; }
                        }
                        break;
                    }
            }
            

            return true;
        }

        /// <summary>
        /// Does some post processing on the deserialized model.
        /// </summary>
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            // Load log filters in dictionary
            _rtLogFilters.Clear();
            if (RtLogFilter != null)
            {
                foreach (var filter in RtLogFilter)
                    _rtLogFilters.Add((RT_MSG_TYPE)Enum.Parse(typeof(RT_MSG_TYPE), filter), true);
            }

            _dmeLogFilters.Clear();
            if (MediusDMEExtLogFilter != null)
            {
                foreach (var filter in MediusDMEExtLogFilter)
                    _dmeLogFilters.Add((MediusDmeMessageIds)Enum.Parse(typeof(MediusDmeMessageIds), filter), true);
            }

            _lobbyLogFilters.Clear();
            if (MediusLobbyLogFilter != null)
            {
                foreach (var filter in MediusLobbyLogFilter)
                    _lobbyLogFilters.Add((MediusLobbyMessageIds)Enum.Parse(typeof(MediusLobbyMessageIds), filter), true);
            }

            _mgclLogFilters.Clear();
            if (MediusMGCLLogFilter != null)
            {
                foreach (var filter in MediusMGCLLogFilter)
                    _mgclLogFilters.Add((MediusMGCLMessageIds)Enum.Parse(typeof(MediusMGCLMessageIds), filter), true);
            }

            _lobbyExtLogFilters.Clear();
            if (MediusLobbyExtLogFilter != null)
            {
                foreach (var filter in MediusLobbyExtLogFilter)
                    _lobbyExtLogFilters.Add((MediusLobbyExtMessageIds)Enum.Parse(typeof(MediusLobbyExtMessageIds), filter), true);
            }
        }
    }

    public class BetaConfig
    {
        /// <summary>
        /// Whether the beta settings are enabled.
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Allow the creation of new accounts.
        /// </summary>
        public bool AllowAccountCreation { get; set; } = false;

        /// <summary>
        /// When true, only accounts in the whitelist will be allowed to login.
        /// </summary>
        public bool RestrictSignin { get; set; } = false;

        /// <summary>
        /// Accounts that can be logged into with RestrictSignIn set.
        /// </summary>
        public string[] PermittedAccounts { get; set; } = null;
    }

    public class MPSConfig
    {
        
    }
}
