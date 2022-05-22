using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetty.Common.Internal.Logging;
using RT.Common;
using RT.Models;
using Server.Common;
using Server.Database.Models;
using Server.Medius.PluginArgs;
using Server.Plugins;

namespace Server.Medius.Models
{
    public class Party
    {
        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<Game>();

        public static int IdCounter = 1;

        public class PartyClient
        {
            public ClientObject Client;

            public int DmeId;
            public bool InGame;
        }

        public int Id = 0;
        public int ApplicationId = 0;
        public List<PartyClient> Clients = new List<PartyClient>();
        public string PartyName;
        public string PartyPassword;
        public MediusGameHostType GameHostType;
        public int MinPlayers;
        public int MaxPlayers;
        public string Metadata;
        public int GenericField1;
        public int GenericField2;
        public int GenericField3;
        public int GenericField4;
        public int GenericField5;
        public int GenericField6;
        public int GenericField7;
        public int GenericField8;
        public MediusWorldAttributesType Attributes;
        public DMEObject DMEServer;
        public Channel ChatChannel;
        public ClientObject Host;

        public string AccountIdsAtStart => accountIdsAtStart;
        public DateTime UtcTimeCreated => utcTimeCreated;
        public DateTime? UtcTimeStarted => utcTimeStarted;
        public DateTime? UtcTimeEnded => utcTimeEnded;

        protected MediusWorldStatus _worldStatus = MediusWorldStatus.WorldPendingCreation;
        protected bool hasHostJoined = false;
        protected string accountIdsAtStart;
        protected DateTime utcTimeCreated;
        protected DateTime? utcTimeStarted;
        protected DateTime? utcTimeEnded;
        protected DateTime? utcTimeEmpty;
        protected bool destroyed = false;

        public uint Time => (uint)(Utils.GetHighPrecisionUtcTime() - utcTimeCreated).TotalMilliseconds;

        public int PlayerCount => Clients.Count(x => x != null && x.Client.IsConnected && x.InGame);

        public Party(ClientObject client, IMediusRequest partyCreate, Channel chatChannel, DMEObject dmeServer)
        {
            if (partyCreate is MediusPartyCreateRequest r)
                FromPartyCreateRequest(r);

            Id = IdCounter++;

            utcTimeCreated = Utils.GetHighPrecisionUtcTime();
            utcTimeEmpty = null;
            DMEServer = dmeServer;
            ChatChannel = chatChannel;
            ChatChannel?.RegisterParty(this);
            Host = client;

            Logger.Info($"Party {Id}: {PartyName}: Created by {client}");
        }

        public PartyDTO ToPartyDTO()
        {
            return new PartyDTO()
            {
                AppId = ApplicationId,
                PartyCreateDt = utcTimeCreated,
                PartyEndDt = utcTimeEnded,
                PartyStartDt = this.utcTimeStarted,
                GameHostType = this.GameHostType.ToString(),
                PartyId = Id,
                PartyName = PartyName,
                PartyPassword = PartyPassword,
                GenericField1 = this.GenericField1,
                GenericField2 = this.GenericField2,
                GenericField3 = this.GenericField3,
                GenericField4 = this.GenericField4,
                GenericField5 = this.GenericField5,
                GenericField6 = this.GenericField6,
                GenericField7 = this.GenericField7,
                GenericField8 = this.GenericField8,
                MaxPlayers = this.MaxPlayers,
                MinPlayers = this.MinPlayers,
                Metadata = this.Metadata,
                Destroyed = this.destroyed
            };
        }

        private void FromPartyCreateRequest(MediusPartyCreateRequest partyCreate)
        {
            ApplicationId = partyCreate.ApplicationID;
            PartyName = partyCreate.PartyName;
            PartyPassword = partyCreate.PartyPassword;
            MinPlayers = partyCreate.MinPlayers;
            MaxPlayers = partyCreate.MaxPlayers;
            GenericField1 = partyCreate.GenericField1;
            GenericField2 = partyCreate.GenericField2;
            GenericField3 = partyCreate.GenericField3;
            GenericField4 = partyCreate.GenericField4;
            GenericField5 = partyCreate.GenericField5;
            GenericField6 = partyCreate.GenericField6;
            GenericField7 = partyCreate.GenericField7;
            GenericField8 = partyCreate.GenericField8;
            GameHostType = partyCreate.GameHostType;
        }

        public string GetActivePlayerList()
        {
            return String.Join(",", this.Clients?.Select(x => x.Client.AccountId.ToString()).Where(x => x != null));
        }

        public virtual async Task Tick()
        {
            // Remove timedout clients
            for (int i = 0; i < Clients.Count; ++i)
            {
                var client = Clients[i];

                if (client == null || client.Client == null || !client.Client.IsConnected || client.Client.CurrentGame?.Id != Id)
                {
                    Clients.RemoveAt(i);
                    --i;
                }
            }

            // Auto close when everyone leaves or if host fails to connect after timeout time
            if (!utcTimeEmpty.HasValue && Clients.Count(x => x.InGame) == 0 && (hasHostJoined || (Utils.GetHighPrecisionUtcTime() - utcTimeCreated).TotalSeconds > Program.Settings.GameTimeoutSeconds))
            {
                utcTimeEmpty = Utils.GetHighPrecisionUtcTime();
            }
        }

        public virtual async Task OnMediusServerConnectNotification(MediusServerConnectNotification notification)
        {
            var player = Clients.FirstOrDefault(x => x.Client.SessionKey == notification.PlayerSessionKey);
            if (player == null)
                return;

            switch (notification.ConnectEventType)
            {
                case MGCL_EVENT_TYPE.MGCL_EVENT_CLIENT_CONNECT:
                    {
                        await OnPlayerJoined(player);
                        break;
                    }
                case MGCL_EVENT_TYPE.MGCL_EVENT_CLIENT_DISCONNECT:
                    {
                        await OnPlayerLeft(player);
                        break;
                    }
            }
        }

        protected virtual async Task OnPlayerJoined(PartyClient player)
        {
            player.InGame = true;

            if (player.Client == Host)
                hasHostJoined = true;
        }

        public virtual void AddPlayer(ClientObject client)
        {
            // Don't add again
            if (Clients.Any(x => x.Client == client))
                return;

            // 
            Logger.Info($"Game {Id}: {PartyName}: {client} added.");

            Clients.Add(new PartyClient()
            {
                Client = client,
                DmeId = client.DmeClientId ?? -1
            });

            // Inform the client of any custom game mode
            //client.CurrentChannel?.SendSystemMessage(client, $"Gamemode is {CustomGamemode?.FullName ?? "default"}.");
        }

        protected virtual async Task OnPlayerLeft(PartyClient player)
        {
            // 
            Logger.Info($"Game {Id}: {PartyName}: {player.Client} left.");

            // 
            player.InGame = false;

            // Update player object
            await player.Client.LeaveParty(this);
            // player.Client.LeaveChannel(ChatChannel);

            // Remove from collection
            await RemovePlayer(player.Client);
        }

        public virtual async Task RemovePlayer(ClientObject client)
        {
            // 
            Logger.Info($"Party {Id}: {PartyName}: {client} removed.");

            // Remove host
            if (Host == client)
            {
                Host = null;
            }


            // Remove from clients list
            Clients.RemoveAll(x => x.Client == client);
        }

        public virtual void OnPartyPlayerReport(MediusPartyPlayerReport report)
        {
            // Ensure report is for correct game world
            if (report.MediusWorldID != Id)
                return;
        }

        public virtual Task PartyCreated()
        {
            return Task.CompletedTask;
        }

        public virtual async Task EndParty()
        {
            // destroy flag
            destroyed = true;

            // 
            Logger.Info($"Game {Id}: {PartyName}: EndParty() called.");

            // Send to plugins
            await Program.Plugins.OnEvent(PluginEvent.MEDIUS_GAME_ON_DESTROYED, new OnPartyArgs() { Party = this });

            // Remove players from game world
            while (Clients.Count > 0)
            {
                var client = Clients[0].Client;
                if (client == null)
                {
                    Clients.RemoveAt(0);
                }
                else
                {
                    await client.LeaveParty(this);
                    // client.LeaveChannel(ChatChannel);
                }
            }


            // Unregister from channel
            ChatChannel?.UnregisterParty(this);

            // Delete db entry if game hasn't started
            // Otherwise do a final update
            if (!utcTimeStarted.HasValue)
            {
                _ = Program.Database.DeleteParty(Id);
            }
            else
            {
                _ = Program.Database.UpdateParty(ToPartyDTO());
            }
        }
    }
}
