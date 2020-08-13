using Deadlocked.Server.Messages.MGCL;
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Ocsp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Deadlocked.Server
{
    public class DMEObject : ClientObject
    {

        public int MaxWorlds { get; protected set; } = 0;
        public int CurrentWorlds { get; protected set; } = 0;
        public int CurrentPlayers { get; protected set; } = 0;

        public int Port { get; protected set; } = 0;
        public IPAddress IP { get; protected set; } = IPAddress.Any;


        public DMEObject(MediusServerSessionBeginRequest request) : base(null, Program.GenerateSessionKey())
        {
            Port = request.Port;
        }

        public void OnWorldReport(MediusServerReport report)
        {
            MaxWorlds = report.MaxWorlds;
            CurrentWorlds = report.ActiveWorldCount;
            CurrentPlayers = report.TotalActivePlayers;
        }

        public void SetIp(string ip)
        {
            IP = IPAddress.Parse(ip);
        }
    }
}
