using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Ocsp;
using RT.Models;
using Server.Pipeline.Udp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server.Dme.Models
{
    public class ClientObject
    {
        protected static Random RNG = new Random();

        static readonly IInternalLogger _logger = InternalLoggerFactory.GetInstance<ClientObject>();
        protected virtual IInternalLogger Logger => _logger;

        /// <summary>
        /// 
        /// </summary>
        public UdpServer Udp { get; protected set; } = null;

        /// <summary>
        /// 
        /// </summary>
        public int UdpPort => Udp?.Port ?? -1;

        /// <summary>
        /// 
        /// </summary>
        public IChannel Tcp { get; protected set; } = null;

        /// <summary>
        /// 
        /// </summary>
        public int DmeId { get; protected set; } = 0;

        /// <summary>
        /// 
        /// </summary>
        public World DmeWorld { get; protected set; } = null;

        /// <summary>
        /// Current access token required to access the account.
        /// </summary>
        public string Token { get; protected set; } = null;

        /// <summary>
        /// 
        /// </summary>
        public string SessionKey { get; protected set; } = null;

        /// <summary>
        /// 
        /// </summary>
        public int ApplicationId { get; set; } = 0;

        /// <summary>
        /// 
        /// </summary>
        public ushort ScertId { get; set; } = 0;

        /// <summary>
        /// 
        /// </summary>
        public ConcurrentQueue<BaseScertMessage> TcpSendMessageQueue { get; } = new ConcurrentQueue<BaseScertMessage>();

        /// <summary>
        /// 
        /// </summary>
        public ConcurrentQueue<ScertDatagramPacket> UdpSendMessageQueue { get; } = new ConcurrentQueue<ScertDatagramPacket>();

        /// <summary>
        /// 
        /// </summary>
        public DateTime LastTcpMessageUtc { get; protected set; } = DateTime.UtcNow;

        /// <summary>
        /// 
        /// </summary>
        public DateTime LastUdpMessageUtc { get; protected set; } = DateTime.UtcNow;

        /// <summary>
        /// 
        /// </summary>
        public DateTime TimeCreated { get; protected set; } = DateTime.UtcNow;

        /// <summary>
        /// 
        /// </summary>
        public DateTime? TimeAuthenticated { get; protected set; } = null;

        /// <summary>
        /// 
        /// </summary>
        public bool Disconnected { get; protected set; } = false;

        /// <summary>
        /// 
        /// </summary>
        public IPEndPoint RemoteUdpEndpoint { get; set; } = null;

        public virtual bool IsConnectingGracePeriod => !TimeAuthenticated.HasValue && (DateTime.UtcNow - TimeCreated).TotalSeconds < Program.Settings.ClientTimeoutSeconds;
        public virtual bool Timedout => !IsConnectingGracePeriod && Math.Min((DateTime.UtcNow - LastTcpMessageUtc).TotalSeconds, (DateTime.UtcNow - LastUdpMessageUtc).TotalSeconds) > Program.Settings.ClientTimeoutSeconds;
        public virtual bool IsConnected => !Disconnected && !Timedout && Tcp != null && Tcp.Active;
        public virtual bool Destroy => Disconnected || (!IsConnected && !IsConnectingGracePeriod);
        public virtual bool IsDestroyed { get; protected set; } = false;

        public ClientObject(string sessionKey, World dmeWorld, int dmeId)
        {
            // 
            SessionKey = sessionKey;

            // 
            this.DmeId = dmeId;
            this.DmeWorld = dmeWorld;

            //
            Udp = new UdpServer(this);
            Udp.Start();

            // Generate new token
            byte[] tokenBuf = new byte[12];
            RNG.NextBytes(tokenBuf);
            Token = Convert.ToBase64String(tokenBuf);
        }

        /// <summary>
        /// Update last echo time.
        /// </summary>
        /// <param name="utcTime"></param>
        public void OnEcho(bool isTcp, DateTime utcTime)
        {
            if (isTcp && utcTime > LastTcpMessageUtc)
                LastTcpMessageUtc = utcTime;
            else if (!isTcp && utcTime > LastUdpMessageUtc)
                LastUdpMessageUtc = utcTime;
        }

        #region Connection / Disconnection

        public async Task Stop()
        {
            if (IsDestroyed)
                return;

            try
            {
                if (Udp != null)
                    await Udp.Stop();

                if (Tcp != null)
                    await Tcp.CloseAsync();
            }
            catch (Exception)
            {
                
            }

            Tcp = null;
            Udp = null;
            IsDestroyed = true;
        }

        public void OnTcpConnected(IChannel channel)
        {
            Tcp = channel;
        }

        public void OnTcpDisconnected()
        {
            Disconnected = true;
        }

        public void OnUdpConnected()
        {
            if (Tcp != null)
                TimeAuthenticated = DateTime.UtcNow;
        }

        #endregion

        #region Send Queue

        public void EnqueueTcp(BaseScertMessage message)
        {
            TcpSendMessageQueue.Enqueue(message);
        }

        public void EnqueueTcp(IEnumerable<BaseScertMessage> messages)
        {
            foreach (var message in messages)
                EnqueueTcp(message);
        }

        public void EnqueueTcp(BaseMediusMessage message)
        {
            EnqueueTcp(new RT_MSG_SERVER_APP() { Message = message });
        }

        public void EnqueueTcp(IEnumerable<BaseMediusMessage> messages)
        {
            EnqueueTcp(messages.Select(x => new RT_MSG_SERVER_APP() { Message = x }));
        }

        public void EnqueueUdp(BaseScertMessage message)
        {
            Udp?.Send(message);
        }

        public void EnqueueUdp(IEnumerable<BaseScertMessage> messages)
        {
            foreach (var message in messages)
                EnqueueUdp(message);
        }

        public void EnqueueUdp(BaseMediusMessage message)
        {
            EnqueueUdp(new RT_MSG_SERVER_APP() { Message = message });
        }

        public void EnqueueUdp(IEnumerable<BaseMediusMessage> messages)
        {
            EnqueueUdp(messages.Select(x => new RT_MSG_SERVER_APP() { Message = x }));
        }

        #endregion

        public override string ToString()
        {
            return $"(worldId:{DmeWorld.WorldId},clientId:{DmeId})";
        }

    }
}
