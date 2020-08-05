using Deadlocked.Server.Accounts;
using Deadlocked.Server.Messages;
using Org.BouncyCastle.Asn1;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Deadlocked.Server
{
    public class ClientObject
    {
        public static Random RNG = new Random();
        public string Token { get; protected set; }
        public string SessionKey { get; protected set; }
        public DateTime UtcLastEcho { get; protected set; } = DateTime.UtcNow;

        public MediusUserAction Action { get; set; } = MediusUserAction.KeepAlive;
        public MediusPlayerStatus Status { get; set; } = MediusPlayerStatus.MediusPlayerDisconnected;
        public int CurrentChannelId { get; set; } = -1;

        public Account ClientAccount { get; protected set; } = null;


        public bool IsConnected => CurrentChannelId >= 0 && (DateTime.UtcNow - UtcLastEcho).TotalSeconds < Program.Settings.ClientTimeoutSeconds;


        private ConcurrentQueue<BaseMessage> LobbyServerMessages = new ConcurrentQueue<BaseMessage>();
        private ConcurrentQueue<BaseMessage> ProxyServerMessages = new ConcurrentQueue<BaseMessage>();

        public ClientObject(Account clientAccount, string sessionKey)
        {
            // Generate new token
            byte[] tokenBuf = new byte[12];
            RNG.NextBytes(tokenBuf);
            Token = Convert.ToBase64String(tokenBuf);

            // Set account
            ClientAccount = clientAccount;

            // Set session key
            SessionKey = sessionKey;
        }

        /// <summary>
        /// Update last echo time.
        /// </summary>
        /// <param name="utcTime"></param>
        public void OnEcho(DateTime utcTime)
        {
            if (utcTime > UtcLastEcho)
                UtcLastEcho = utcTime;
        }

        #region Target Messages

        /// <summary>
        /// 
        /// </summary>
        /// <param name="messages"></param>
        public void AddLobbyMessages(IEnumerable<BaseMessage> messages)
        {
            lock (LobbyServerMessages)
            {
                foreach (var message in messages)
                    LobbyServerMessages.Enqueue(message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public void AddLobbyMessage(BaseMessage message)
        {
            lock (LobbyServerMessages)
            {
                LobbyServerMessages.Enqueue(message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<BaseMessage> PullLobbyMessages()
        {
            List<BaseMessage> messages = new List<BaseMessage>();

            lock (LobbyServerMessages)
            {
                while (LobbyServerMessages.TryDequeue(out var result))
                    messages.Add(result);
            }

            return messages;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="messages"></param>
        public void AddProxyMessages(IEnumerable<BaseMessage> messages)
        {
            lock (ProxyServerMessages)
            {
                foreach (var message in messages)
                    ProxyServerMessages.Enqueue(message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public void AddProxyMessage(BaseMessage message)
        {
            lock (ProxyServerMessages)
            {
                ProxyServerMessages.Enqueue(message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<BaseMessage> PullProxyMessages()
        {
            List<BaseMessage> messages = new List<BaseMessage>();

            lock (ProxyServerMessages)
            {
                while (ProxyServerMessages.TryDequeue(out var result))
                    messages.Add(result);
            }

            return messages;
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        public void OnDestroy()
        {
            // Unset client in account
            if (ClientAccount != null && ClientAccount.Client == this)
                ClientAccount.Client = null;

            // Move to invalid channel
            CurrentChannelId = -1;

            // Remove reference to account
            ClientAccount = null;
        }
    }
}
