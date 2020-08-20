using Deadlocked.Server.Medius.Models.Packets.MGCL;
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Ocsp;
using System;
using System.Collections.Generic;
using System.Linq;
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


        public DMEObject(MediusServerSessionBeginRequest request)
        {
            ApplicationId = request.ApplicationID;
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
                        Console.WriteLine($"Unhandled UriHostNameType {Uri.CheckHostName(ip)} from {ip} in DMEObject.SetIp()");
                        break;
                    }
            }
        }
    }
}
