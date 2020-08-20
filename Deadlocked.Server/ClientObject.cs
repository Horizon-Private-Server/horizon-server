using Deadlocked.Server.Accounts;
using Deadlocked.Server.Medius.Models.Packets;
using Deadlocked.Server.Medius.Models.Packets.Lobby;
using Deadlocked.Server.SCERT.Models.Packets;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Ocsp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Deadlocked.Server
{
    public class ClientObject
    {
        public static Random RNG = new Random();

        public string Token { get; protected set; }
        public string SessionKey { get; protected set; }
        public string DotNettyId { get; set; }
        public DateTime UtcLastEcho { get; protected set; } = DateTime.UtcNow;

        public MediusUserAction Action { get; set; } = MediusUserAction.KeepAlive;
        public MediusPlayerStatus Status { get; set; } = MediusPlayerStatus.MediusPlayerDisconnected;

        public int ApplicationId { get; protected set; } = 0;

        private uint gameListFilterIdCounter = 0;
        public List<GameListFilter> GameListFilters = new List<GameListFilter>();

        public Channel CurrentChannel { get; protected set; } = null;

        private int _currentChannelId = 0;
        public int CurrentChannelId
        {
            get => _currentChannelId;
            set
            {
                _currentChannelId = value;
                CurrentChannel = Program.GetChannelById(value);
            }
        }

        public Game CurrentGame { get; protected set; } = null;

        private int _currentGameId = -1;
        public int CurrentGameId
        {
            get => _currentGameId;
            set
            {
                _currentGameId = value;
                CurrentGame = Program.GetGameById(value);
            }
        }

        public Account ClientAccount { get; protected set; } = null;


        public DateTime? LogoutTime { get; protected set; } = null;
        public bool Timedout => (DateTime.UtcNow - UtcLastEcho).TotalSeconds > Program.Settings.ClientTimeoutSeconds;
        public bool IsConnected => !LogoutTime.HasValue && !Timedout;


        private ConcurrentQueue<BaseScertMessage> LobbyServerMessages = new ConcurrentQueue<BaseScertMessage>();
        private ConcurrentQueue<BaseScertMessage> ProxyServerMessages = new ConcurrentQueue<BaseScertMessage>();

        public ClientObject()
        {
            SessionKey = Program.GenerateSessionKey();
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
        public void AddLobbyMessages(IEnumerable<BaseScertMessage> messages)
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
        public void AddLobbyMessage(BaseScertMessage message)
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
        public List<BaseScertMessage> PullLobbyMessages()
        {
            List<BaseScertMessage> messages = new List<BaseScertMessage>();

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
        public void AddProxyMessages(IEnumerable<BaseScertMessage> messages)
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
        public void AddProxyMessage(BaseScertMessage message)
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
        public List<BaseScertMessage> PullProxyMessages()
        {
            List<BaseScertMessage> messages = new List<BaseScertMessage>();

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
        public void Logout()
        {
            // Unset client in account
            //if (ClientAccount != null && ClientAccount.Client == this)
            //    ClientAccount.Client = null;

            // Move to invalid channel
            CurrentChannelId = -1;

            // Remove reference to account
            ClientAccount = null;

            //
            LogoutTime = DateTime.UtcNow;

            // Remove from program list
            Program.Clients.Remove(this);
        }

        public void Login(Account account)
        {
            // Generate new token
            byte[] tokenBuf = new byte[12];
            RNG.NextBytes(tokenBuf);
            Token = Convert.ToBase64String(tokenBuf);

            // Set account
            ClientAccount = account;
            if (ClientAccount != null)
                ClientAccount.Client = this;

            // Add to program clients list
            Program.Clients.Add(this);
        }

        public GameListFilter SetGameListFilter(MediusSetGameListFilterRequest request)
        {
            GameListFilter result = null;

            GameListFilters.Add(result = new GameListFilter()
            {
                FieldID = gameListFilterIdCounter++,
                Mask = request.Mask,
                BaselineValue = request.BaselineValue,
                ComparisonOperator = request.ComparisonOperator,
                FilterField = request.FilterField
            });

            return result;
        }

        public void ClearGameListFilter(uint filterID)
        {
            GameListFilters.RemoveAll(x => x.FieldID == filterID);
        }

        public bool IsGameMatch(Game game)
        {
            return !GameListFilters.Any(x => !x.IsMatch(game));
        }
    }
}
