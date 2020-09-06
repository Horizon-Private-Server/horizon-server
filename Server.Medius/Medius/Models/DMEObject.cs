using DotNetty.Common.Internal.Logging;
using RT.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Server.Medius.Models
{
    public class DMEObject : ClientObject
    {
        static readonly IInternalLogger _logger = InternalLoggerFactory.GetInstance<DMEObject>();
        protected override IInternalLogger Logger => _logger;

        public int MaxWorlds { get; protected set; } = 0;
        public int CurrentWorlds { get; protected set; } = 0;
        public int CurrentPlayers { get; protected set; } = 0;

        public int Port { get; protected set; } = 0;
        public IPAddress IP { get; protected set; } = IPAddress.Any;

        public override bool Timedout => false; // (DateTime.UtcNow - UtcLastEcho).TotalSeconds > Program.Settings.DmeTimeoutSeconds;
        public override bool IsConnected => _hasActiveSession && !Timedout;

        public DMEObject(MediusServerSetAttributesRequest request)
        {
            Port = (int)request.ListenServerAddress.Port;
            SetIp(request.ListenServerAddress.Address);

            // Generate new session key
            SessionKey = Program.GenerateSessionKey();

            // Generate new token
            byte[] tokenBuf = new byte[12];
            RNG.NextBytes(tokenBuf);
            Token = Convert.ToBase64String(tokenBuf);
        }

        public DMEObject(MediusServerSessionBeginRequest request)
        {
            ApplicationId = request.ApplicationID;
            Port = request.Port;

            // Generate new session key
            SessionKey = Program.GenerateSessionKey();

            // Generate new token
            byte[] tokenBuf = new byte[12];
            RNG.NextBytes(tokenBuf);
            Token = Convert.ToBase64String(tokenBuf);
        }

        public void OnWorldReport(MediusServerReport report)
        {
            MaxWorlds = report.MaxWorlds;
            CurrentWorlds = report.ActiveWorldCount;
            CurrentPlayers = report.TotalActivePlayers;
        }

        public void SetIp(string ip)
        {
            switch (Uri.CheckHostName(ip))
            {
                case UriHostNameType.IPv4:
                    {
                        IP = IPAddress.Parse(ip);
                        break;
                    }
                case UriHostNameType.Dns:
                    {
                        IP = Dns.GetHostAddresses(ip).FirstOrDefault()?.MapToIPv4() ?? IPAddress.Any;
                        break;
                    }
                default:
                    {
                        Logger.Error($"Unhandled UriHostNameType {Uri.CheckHostName(ip)} from {ip} in DMEObject.SetIp()");
                        break;
                    }
            }
        }

        protected override void PostStatus()
        {
            // Don't post stats of DME servers to db
        }
    }
}
