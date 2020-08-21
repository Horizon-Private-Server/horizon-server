using Deadlocked.Server.Accounts;
using Deadlocked.Server.Medius;
using Deadlocked.Server.Medius.Models.Packets;
using Deadlocked.Server.Medius.Models.Packets.Lobby;
using Deadlocked.Server.SCERT.Models.Packets;
using DotNetty.Common.Internal.Logging;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Ocsp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Deadlocked.Server.Medius.Models
{
    public class ClientObject
    {
        public static Random RNG = new Random();

        static readonly IInternalLogger _logger = InternalLoggerFactory.GetInstance<ClientObject>();
        protected virtual IInternalLogger Logger => _logger;


        public DateTime UtcLastEcho { get; protected set; } = DateTime.UtcNow;

        public MediusUserAction Action { get; set; } = MediusUserAction.KeepAlive;
        public MediusPlayerStatus Status { get; set; } = MediusPlayerStatus.MediusPlayerDisconnected;

        /// <summary>
        /// Current access token required to access the account.
        /// </summary>
        public string Token { get; set; } = null;

        /// <summary>
        /// 
        /// </summary>
        public string SessionKey { get; set; } = null;

        public int ApplicationId { get; set; } = 0;

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
        public virtual bool Timedout => (DateTime.UtcNow - UtcLastEcho).TotalSeconds > Program.Settings.ClientTimeoutSeconds;
        public virtual bool IsConnected => !LogoutTime.HasValue && !Timedout;


        public ConcurrentQueue<BaseScertMessage> SendMessageQueue { get; } = new ConcurrentQueue<BaseScertMessage>();

        public ClientObject()
        {
            // Generate new session key
            SessionKey = Program.GenerateSessionKey();

            // Generate new token
            byte[] tokenBuf = new byte[12];
            RNG.NextBytes(tokenBuf);
            Token = Convert.ToBase64String(tokenBuf);
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

        #region Login / Logout

        /// <summary>
        /// 
        /// </summary>
        public void Logout()
        {
            // Unset client in account
            if (ClientAccount != null && ClientAccount.Client == this)
                ClientAccount.Client = null;

            // Move to invalid channel
            CurrentChannelId = -1;

            // Remove reference to account
            ClientAccount = null;

            //
            LogoutTime = DateTime.UtcNow;
        }

        public void Login(Account account)
        {
            // Set account
            if (account != null)
            {
                account.Client = this;
            }

            // Unset old account
            if (ClientAccount != null)
            {
                ClientAccount.Client = null;
            }

            // 
            ClientAccount = account;
        }

        #endregion

        #region Game List Filter

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

        #endregion

        #region Send Queue

        public void Queue(BaseScertMessage message)
        {
            SendMessageQueue.Enqueue(message);
        }

        public void Queue(IEnumerable<BaseScertMessage> messages)
        {
            foreach (var message in messages)
                Queue(message);
        }

        public void Queue(BaseMediusMessage message)
        {
            Queue(new RT_MSG_SERVER_APP() { Message = message });
        }

        public void Queue(IEnumerable<BaseMediusMessage> messages)
        {
            Queue(messages.Select(x => new RT_MSG_SERVER_APP() { Message = x }));
        }

        #endregion

    }
}
