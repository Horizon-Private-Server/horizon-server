﻿using RT.Cryptography;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using RT.Models;
using RT.Common;
using Server.Common.Logging;

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
        /// By default the server will grab its local ip.
        /// If this is set, it will use its public ip instead.
        /// </summary>
        public bool UsePublicIp { get; set; } = false;

        /// <summary>
        /// If UsePublicIp is set to true, allow overriding and skipping using dyndns's dynamic
        /// ip address finder, since it goes down often enough to throw exceptions
        /// </summary>
        public string PublicIpOverride { get; set; } = string.Empty;

        /// <summary>
        /// When a client attempts to log into a non-existent account,
        /// instead of returning account not found,
        /// create the account and log them in.
        /// Necessary for Central Station support.
        /// </summary>
        public bool CreateAccountOnNotFound { get; set; } = false;

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
        /// Period of time when a client is moving between medius server components where the client object will be kept alive.
        /// </summary>
        public int KeepAliveGracePeriod { get; set; } = 8;

        /// <summary>
        /// Number of ticks per second.
        /// </summary>
        public int TickRate { get; set; } = 10;

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
        /// Ip address of the NAT server.
        /// </summary>
        public string NATIp { get; set; } = null;

        /// <summary>
        /// Port of the NAT server.
        /// </summary>
        public int NATPort { get; set; } = 10070;

        /// <summary>
        /// Time, in seconds, before timing out a Dme server.
        /// </summary>
        public int DmeTimeoutSeconds { get; set; } = 60;

        /// <summary>
        /// 'Severity' of the system message sent to notify the user has been banned.
        ///  This is game specific.
        /// </summary>
        public byte BanSystemMessageSeverity { get; set; } = 200;

        /// <summary>
        /// When true, will allow messages like MediusCreateFile, MediusUploadFile, MediusDownloadFile
        /// </summary>
        public bool AllowMediusFileServices { get; set; } = false;

        /// <summary>
        /// Root path of the medius file service directory.
        /// </summary>
        public string MediusFileServerRootPath { get; set; } = "files";

        /// <summary>
        /// Key used to authenticate dme servers.
        /// </summary>
        public RsaKeyPair MPSKey { get; set; } = new RsaKeyPair(
            new BigInteger("10315955513017997681600210131013411322695824559688299373570246338038100843097466504032586443986679280716603540690692615875074465586629501752500179100369237", 10),
            new BigInteger("17", 10),
            new BigInteger("4854567300243763614870687120476899445974505675147434999327174747312047455575182761195687859800492317495944895566174677168271650454805328075020357360662513", 10)
            );

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
        /// Collection of locations.
        /// </summary>
        public List<Location> Locations { get; set; }

        /// <summary>
        /// Regex text filters for 
        /// </summary>
        public Dictionary<TextFilterContext, string> TextBlacklistFilters { get; set; } = new Dictionary<TextFilterContext, string>();

        /// <summary>
        /// Logging settings.
        /// </summary>
        public LogSettings Logging { get; set; } = new LogSettings();

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

    public class Location
    {
        /// <summary>
        /// Id of location.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Name of location.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Name of respective channel.
        /// </summary>
        public string ChannelName { get; set; }

        /// <summary>
        /// Collection of all compatible app ids.
        /// </summary>
        public int[] AppIds { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public MediusWorldGenericFieldLevelType GenericFieldLevel { get; set; }
    }

    public enum TextFilterContext
    {
        DEFAULT,
        ACCOUNT_NAME,
        CLAN_NAME,
        CLAN_MESSAGE,
        CHAT,
        GAME_NAME
    }
}
