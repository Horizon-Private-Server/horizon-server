using DotNetty.Common.Internal.Logging;
using RT.Models;
using System;
using System.Linq;
using System.Net;

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

        public override bool Timedout => false; // (Utils.GetHighPrecisionUtcTime() - UtcLastEcho).TotalSeconds > Program.Settings.DmeTimeoutSeconds;
        public override bool IsConnected => _hasActiveSession && !Timedout;
        public override bool IsLoggedIn => _hasActiveSession;

        #region DMEObjects
        public DMEObject(MediusServerCreateGameOnSelfRequest request)
        {
            ApplicationId = request.ApplicationID;


            // Generate new session key
            SessionKey = Program.GenerateSessionKey();

            // Generate new token
            byte[] tokenBuf = new byte[12];
            RNG.NextBytes(tokenBuf);
            Token = Convert.ToBase64String(tokenBuf);
        }

        public DMEObject(MediusServerCreateGameOnSelfRequest0 request)
        {
            ApplicationId = request.ApplicationID;

            // Generate new session key
            SessionKey = Program.GenerateSessionKey();

            // Generate new token
            byte[] tokenBuf = new byte[12];
            RNG.NextBytes(tokenBuf);
            Token = Convert.ToBase64String(tokenBuf);
        }

        public DMEObject(MediusServerCreateGameOnMeRequest request)
        {
            ApplicationId = request.ApplicationID;
            WorldId = request.WorldID;

            // Generate new session key
            SessionKey = Program.GenerateSessionKey();

            // Generate new token
            byte[] tokenBuf = new byte[12];
            RNG.NextBytes(tokenBuf);
            Token = Convert.ToBase64String(tokenBuf);
        }

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

        public DMEObject(MediusServerSessionBeginRequest1 request)
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
        #endregion

        public void OnWorldReport(MediusServerReport report)
        {
            MaxWorlds = report.MaxWorlds;
            CurrentWorlds = report.ActiveWorldCount;
            CurrentPlayers = report.TotalActivePlayers;

        }

        #region SetIP
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
        #endregion

        protected override void PostStatus()
        {
            // Don't post stats of DME servers to db
        }
    }
}
