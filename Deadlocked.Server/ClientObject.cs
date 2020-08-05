using Deadlocked.Server.Messages;
using Org.BouncyCastle.Asn1;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Deadlocked.Server
{
    public class ClientObject
    {
        public static Random RNG = new Random();
        public string Token { get; protected set; }
        public string Username { get; protected set; }
        public string SessionKey { get; protected set; }
        public int AccountId { get; protected set; }

        public MediusUserAction Action { get; set; } = MediusUserAction.KeepAlive;
        public int CurrentWorldID { get; set; } = 0;

        public ClientObject(string username, string sessionKey, int accountId)
        {
            // Generate new token
            byte[] tokenBuf = new byte[12];
            RNG.NextBytes(tokenBuf);
            Token = Convert.ToBase64String(tokenBuf);

            // Set username
            Username = username;

            // Set session key
            SessionKey = sessionKey;

            // 
            AccountId = accountId;
        }
    }
}
