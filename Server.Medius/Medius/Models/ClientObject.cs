using DotNetty.Common.Internal.Logging;
using RT.Common;
using RT.Models;
using Server.Database.Models;
using Server.Medius.PluginArgs;
using Server.Plugins;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Server.Common;
using System.Threading.Tasks;
using System.IO;
using static Server.Medius.Models.Game;

namespace Server.Medius.Models
{
    public class ClientObject
    {
        protected static Random RNG = new Random();

        static readonly IInternalLogger _logger = InternalLoggerFactory.GetInstance<ClientObject>();
        protected virtual IInternalLogger Logger => _logger;

        public List<GameClient> Clients = new List<GameClient>();

        /// <summary>
        /// 
        /// </summary>
        public MediusPlayerStatus PlayerStatus => GetStatus();

        /// <summary>
        /// 
        /// </summary>
        public MGCL_GAME_HOST_TYPE ServerType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int AccountId { get; protected set; } = -1;
        
        /// <summary>
        /// 
        /// </summary>
        public string AccountName { get; protected set; } = null;
        
        /// <summary>
        /// 
        /// </summary>
        public string AccountStats { get; protected set; } = null;

        /// <summary>
        /// Current access token required to access the account.
        /// </summary>
        public string Token { get; protected set; } = null;

        /// <summary>
        /// 
        /// </summary>
        public string SessionKey { get; protected set; } = null;

        /// <summary>
        /// Unique MGCL hardcoded game identifer per Medius title
        /// </summary>
        public int ApplicationId { get; set; } = 0;
        
        /// <summary>
        /// 
        /// </summary>
        public int MediusVersion { get; set; } = 0;

        /// <summary>
        /// 
        /// </summary>
        public int? ClanId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int? WorldId { get; set; } = null;

        /// <summary>
        /// 
        /// </summary>
        public List<GameListFilter> GameListFilters = new List<GameListFilter>();

        /// <summary>
        /// 
        /// </summary>
        public Channel CurrentChannel { get; protected set; } = null;

        /// <summary>
        /// 
        /// </summary>
        public Game CurrentGame { get; protected set; } = null;

        /// <summary>
        /// Current Party
        /// </summary>
        public Party CurrentParty { get; protected set; } = null;

        /// <summary>
        /// 
        /// </summary>
        public int? DmeClientId { get; protected set; } = null;

        /// <summary>
        /// 
        /// </summary>
        public ConcurrentQueue<BaseScertMessage> SendMessageQueue { get; } = new ConcurrentQueue<BaseScertMessage>();

        /// <summary>
        /// 
        /// </summary>
        public DateTime UtcLastServerEchoSent { get; protected set; } = Utils.GetHighPrecisionUtcTime();

        /// <summary>
        /// 
        /// </summary>
        public DateTime UtcLastServerEchoReply { get; protected set; } = Utils.GetHighPrecisionUtcTime();

        /// <summary>
        /// 
        /// </summary>
        public string Metadata { get; set; } = null;

        /// <summary>
        /// RTT (ms)
        /// </summary>
        public uint LatencyMs { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        public string[] FriendsListPS3 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<int, string> FriendsList { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int[] Stats { get; set; } = new int[15];

        /// <summary>
        /// 
        /// </summary>
        public int[] WideStats { get; set; } = new int[100];

        /// <summary>
        /// 
        /// </summary>
        public int[] CustomWideStats { get; set; } = new int[0];

        /// <summary>
        /// 
        /// </summary>
        public UploadState Upload { get; set; }

        public virtual bool IsLoggedIn => !_logoutTime.HasValue && _loginTime.HasValue && IsConnected;
        public bool IsInGame => CurrentGame != null && CurrentChannel != null && CurrentChannel.Type == ChannelType.Game;
        //public bool 

        public virtual bool Timedout => (Utils.GetHighPrecisionUtcTime() - UtcLastServerEchoReply).TotalSeconds > Program.Settings.ClientTimeoutSeconds;
        public virtual bool IsConnected => KeepAlive || (_hasSocket && _hasActiveSession && !Timedout);  //(KeepAlive || _hasActiveSession) && !Timedout;

        public bool KeepAlive => _keepAliveTime.HasValue && (Utils.GetHighPrecisionUtcTime() - _keepAliveTime).Value.TotalSeconds < Program.Settings.KeepAliveGracePeriod;

        /// <summary>
        /// 
        /// </summary>
        protected DateTime? _loginTime = null;

        /// <summary>
        /// 
        /// </summary>
        protected DateTime? _logoutTime = null;

        /// <summary>
        /// 
        /// </summary>
        protected bool _hasActiveSession = true;

        /// <summary>
        /// 
        /// </summary>
        private uint _gameListFilterIdCounter = 0;

        /// <summary>
        /// 
        /// </summary>
        private bool _hasSocket = false;

        /// <summary>
        /// 
        /// </summary>
        protected DateTime? _keepAliveTime = null;

        /// <summary>
        /// 
        /// </summary>
        private DateTime _lastServerEchoValue = DateTime.UnixEpoch;



        public ClientObject()
        {
            // Generate new session key
            SessionKey = Program.GenerateSessionKey();

            // Generate new token
            byte[] tokenBuf = new byte[12];
            RNG.NextBytes(tokenBuf);
            Token = Convert.ToBase64String(tokenBuf);
        }

        public void QueueServerEcho()
        {
            SendMessageQueue.Enqueue(new RT_MSG_SERVER_ECHO());
            UtcLastServerEchoSent = Utils.GetHighPrecisionUtcTime();
        }

        public void OnRecvServerEcho(RT_MSG_SERVER_ECHO echo)
        {
            var echoTime = echo.UnixTimestamp.ToUtcDateTime();
            if (echoTime > _lastServerEchoValue)
            {
                _lastServerEchoValue = echoTime;
                UtcLastServerEchoReply = Utils.GetHighPrecisionUtcTime();
                LatencyMs = (uint)(UtcLastServerEchoReply - echoTime).TotalMilliseconds;
            }
        }

        #region Connection / Disconnection

        public void KeepAliveUntilNextConnection()
        {
            _keepAliveTime = Utils.GetHighPrecisionUtcTime();
        }

        public void OnConnected()
        {
            _keepAliveTime = null;
            _hasSocket = true;
        }

        public void OnDisconnected()
        {
            _hasSocket = false;
        }

        #endregion

        #region Status

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private MediusPlayerStatus GetStatus()
        {
            if (!IsConnected || !IsLoggedIn)
                return MediusPlayerStatus.MediusPlayerDisconnected;

            if (IsInGame)
                return MediusPlayerStatus.MediusPlayerInGameWorld;


            return MediusPlayerStatus.MediusPlayerInChatWorld;

            /* // Needs proper handling between Universes for MUIS
            if (IsInOtherUniverse)
                return MediusPlayerStatus.MediusPlayerInOtherUniverse;
            */

        }

        /// <summary>
        /// Posts current account status to database.
        /// </summary>
        protected virtual void PostStatus()
        {
            _ = Program.Database.PostAccountStatus(new AccountStatusDTO()
            {
                AccountId = AccountId,
                LoggedIn = IsLoggedIn,
                ChannelId = CurrentChannel?.Id,
                GameId = CurrentGame?.Id,
                GameName = CurrentGame?.GameName,
                PartyId = CurrentParty?.Id,
                PartyName = CurrentParty?.PartyName,
                WorldId = CurrentGame?.Id ?? CurrentChannel?.Id
            });
        }

        #endregion

        #region Login / Logout

        /// <summary>
        /// 
        /// </summary>
        public async Task Logout()
        {
            // Prevent logout twice
            if (_logoutTime.HasValue || !_loginTime.HasValue)
                return;

            // Raise plugin event
            await Program.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_ON_LOGGED_OUT, new OnPlayerArgs() { Player = this });

            // Leave game
            await LeaveCurrentGame();

            // Leave channel
            LeaveCurrentChannel();

            // Logout
            _logoutTime = Common.Utils.GetHighPrecisionUtcTime();

            // Tell database
            PostStatus();
        }

        public async Task Login(AccountDTO account)
        {
            if (IsLoggedIn)
                throw new InvalidOperationException($"{this} attempting to log into {account} but is already logged in!");

            if (account == null)
                throw new InvalidOperationException($"{this} attempting to log into null account.");

            // 
            AccountId = account.AccountId;
            AccountName = account.AccountName;
            Metadata = account.Metadata;
            ClanId = account.ClanId;
            WideStats = account.AccountWideStats;
            CustomWideStats = account.AccountCustomWideStats;

            //
            FriendsList = account.Friends?.ToDictionary(x => x.AccountId, x => x.AccountName) ?? new Dictionary<int, string>();

            // Raise plugin event
            await Program.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_ON_LOGGED_IN, new OnPlayerArgs() { Player = this });

            // Login
            _loginTime = Common.Utils.GetHighPrecisionUtcTime();

            // Update last sign in date
            _ = Program.Database.PostAccountSignInDate(AccountId, Common.Utils.GetHighPrecisionUtcTime());

            // Update database status
            PostStatus();
        }

        public async Task RefreshAccount()
        {
            var accountDto = await Program.Database.GetAccountById(this.AccountId);
            if (accountDto != null)
            {
                FriendsList = accountDto.Friends.ToDictionary(x => x.AccountId, x => x.AccountName);
                ClanId = accountDto.ClanId;
            }
        }

        #endregion

        #region Party

        public async Task LeaveParty(Party party)
        {
            if (CurrentParty != null && CurrentParty == party)
            {
                await LeaveCurrentParty();

                // Tell database
                PostStatus();
            }
        }

        private async Task LeaveCurrentParty()
        {
            if (CurrentParty != null)
            {
                await CurrentParty.RemovePlayer(this);
                CurrentParty = null;
            }
            DmeClientId = null;
        }

        #endregion

        #region Game

        public async Task JoinGame(Game game, int dmeClientIndex)
        {
            // Leave current game
            await LeaveCurrentGame();

            CurrentGame = game;
            DmeClientId = dmeClientIndex;
            await CurrentGame.AddPlayer(this);

            // Tell database
            PostStatus();
        }

        public async Task LeaveGame(Game game)
        {
            if (CurrentGame != null && CurrentGame == game)
            {
                await LeaveCurrentGame();

                // Tell database
                PostStatus();
            }
        }

        private async Task LeaveCurrentGame()
        {
            if (CurrentGame != null)
            {
                await CurrentGame.RemovePlayer(this);
                CurrentGame = null;
            }
            DmeClientId = null;
        }

        #endregion

        #region Channel

        public void JoinChannel(Channel channel)
        {
            // Leave current channel
            LeaveCurrentChannel();

            CurrentChannel = channel;
            CurrentChannel.OnPlayerJoined(this);

            // Tell database
            PostStatus();
        }

        public void LeaveChannel(Channel channel)
        {
            if (CurrentChannel != null && CurrentChannel == channel)
            {
                LeaveCurrentChannel();

                // Tell database
                PostStatus();
            }
        }

        private void LeaveCurrentChannel()
        {
            if (CurrentChannel != null)
            {
                CurrentChannel.OnPlayerLeft(this);
                CurrentChannel = null;
            }
        }

        #endregion

        #region Session

        /// <summary>
        /// 
        /// </summary>
        public void BeginSession()
        {
            _hasActiveSession = true;
        }

        /// <summary>
        /// 
        /// </summary>
        public void EndSession()
        {
            _hasActiveSession = false;
        }

        #endregion

        #region Game List Filter

        public GameListFilter SetGameListFilter(MediusSetGameListFilterRequest request)
        {
            GameListFilter result;
            GameListFilters.Add(result = new GameListFilter()
            {
                FieldID = _gameListFilterIdCounter++,
                Mask = request.Mask,
                BaselineValue = request.BaselineValue,
                ComparisonOperator = request.ComparisonOperator,
                FilterField = request.FilterField
            });

            return result;
        }

        public GameListFilter SetGameListFilter(MediusSetGameListFilterRequest0 request)
        {
            GameListFilter result;
            GameListFilters.Add(result = new GameListFilter()
            {
                FieldID = _gameListFilterIdCounter++,
                Mask = 0xFFFFFFFF,
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

        public override string ToString()
        {
            return $"({AccountId}:{AccountName})";
        }
    }

    public class UploadState
    {
        public FileStream Stream { get; set; }
        public uint FileId { get; set; }
        public int PacketNumber { get; set; }
        public uint TotalSize { get; set; }
        public int BytesReceived { get; set; }
        public DateTime TimeBegan { get; set; } = DateTime.UtcNow;
    }
}
