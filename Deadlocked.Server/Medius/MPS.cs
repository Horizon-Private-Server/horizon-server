using Deadlocked.Server.Medius.Models;
using Deadlocked.Server.Medius.Models.Packets;
using Deadlocked.Server.Medius.Models.Packets.Lobby;
using Deadlocked.Server.Medius.Models.Packets.MGCL;
using Deadlocked.Server.SCERT.Models;
using Deadlocked.Server.SCERT.Models.Packets;
using Deadlocked.Server.Stream;
using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;
using Medius.Crypto;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Deadlocked.Server.Medius
{
    public class MPS : BaseMediusComponent
    {
        static readonly IInternalLogger _logger = InternalLoggerFactory.GetInstance<MPS>();

        protected override IInternalLogger Logger => _logger;
        public override string Name => "MPS";
        public override int Port => Program.Settings.MPSPort;
        public override PS2_RSA AuthKey => Program.DmeAuthKey;

        DateTime lastSend = DateTime.UtcNow;

        public MPS()
        {
            _sessionCipher = new PS2_RC4(Utils.FromString(Program.KEY), CipherContext.RC_CLIENT_SESSION);
        }

        protected override async Task ProcessMessage(BaseScertMessage message, IChannel clientChannel, ChannelData data)
        {
            // 
            switch (message)
            {
                case RT_MSG_CLIENT_HELLO clientHello:
                    {
                        Queue(new RT_MSG_SERVER_HELLO(), clientChannel);
                        break;
                    }
                case RT_MSG_CLIENT_CRYPTKEY_PUBLIC clientCryptKeyPublic:
                    {
                        Queue(new RT_MSG_SERVER_CRYPTKEY_PEER() { Key = Utils.FromString(Program.KEY) }, clientChannel);
                        break;
                    }
                case RT_MSG_CLIENT_CONNECT_TCP clientConnectTcp:
                    {
                        data.ApplicationId = clientConnectTcp.AppId;

                        // Find reserved dme object by token
                        data.ClientObject = Program.Clients.FirstOrDefault(x => x.Token == clientConnectTcp.AccessToken);
                        if (data.ClientObject == null)
                        {
                            await DisconnectClient(clientChannel);
                        }
                        else
                        {
                            // Update app id
                            data.ClientObject.ApplicationId = clientConnectTcp.AppId;

                            // 
                            Queue(new RT_MSG_SERVER_CONNECT_REQUIRE() { Contents = Utils.FromString("024802") }, clientChannel);
                        }
                        break;
                    }
                case RT_MSG_CLIENT_CONNECT_READY_REQUIRE clientConnectReadyRequire:
                    {
                        Queue(new RT_MSG_SERVER_CRYPTKEY_GAME()
                        {
                            Key = Utils.FromString(Program.KEY)
                        }, clientChannel);

                        Queue(new RT_MSG_SERVER_CONNECT_ACCEPT_TCP()
                        {
                            UNK_00 = 0,
                            UNK_02 = GenerateNewScertClientId(),
                            UNK_04 = 0,
                            UNK_05 = 0,
                            UNK_06 = 0x0001,
                            IP = (clientChannel.RemoteAddress as IPEndPoint)?.Address
                        }, clientChannel);
                        break;
                    }
                case RT_MSG_CLIENT_CONNECT_READY_TCP clientConnectReadyTcp:
                    {
                        Queue(new RT_MSG_SERVER_CONNECT_COMPLETE()
                        {
                            ARG1 = 0x0001
                        }, clientChannel);

                        Queue(new RT_MSG_SERVER_ECHO(), clientChannel);
                        break;
                    }
                case RT_MSG_SERVER_ECHO serverEchoReply:
                    {

                        break;
                    }
                case RT_MSG_CLIENT_ECHO clientEcho:
                    {
                        Queue(new RT_MSG_CLIENT_ECHO()
                        {
                            Value = clientEcho.Value
                        }, clientChannel);
                        break;
                    }
                case RT_MSG_CLIENT_APP_TOSERVER clientAppToServer:
                    {
                        ProcessMediusMessage(clientAppToServer.Message, clientChannel, data);
                        break;
                    }

                case RT_MSG_CLIENT_DISCONNECT_WITH_REASON clientDisconnectWithReason:
                    {
                        await clientChannel.DisconnectAsync();
                        break;
                    }
                default:
                    {
                        Logger.Warn($"{Name} UNHANDLED MESSAGE: {message}");

                        break;
                    }
            }

            return;
        }

        protected virtual void ProcessMediusMessage(BaseMediusMessage message, IChannel clientChannel, ChannelData data)
        {
            if (message == null)
                return;


            switch (message)
            {
                case MediusServerCreateGameWithAttributesResponse createGameWithAttrResponse:
                    {
                        int gameId = int.Parse(createGameWithAttrResponse.MessageID.Split('-')[0]);
                        int accountId = int.Parse(createGameWithAttrResponse.MessageID.Split('-')[1]);
                        string msgId = createGameWithAttrResponse.MessageID.Split('-')[2];
                        var game = Program.GetGameById(gameId);
                        var rClient = Program.GetClientByAccountId(accountId);
                        game.DMEWorldId = createGameWithAttrResponse.WorldID;

                        rClient.Queue(new MediusCreateGameResponse()
                        {
                            MessageID = msgId,
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                            MediusWorldID = game.Id
                        });


                        break;
                    }
                case MediusServerJoinGameResponse joinGameResponse:
                    {
                        int gameId = int.Parse(joinGameResponse.MessageID.Split('-')[0]);
                        int accountId = int.Parse(joinGameResponse.MessageID.Split('-')[1]);
                        string msgId = joinGameResponse.MessageID.Split('-')[2];
                        var game = Program.GetGameById(gameId);
                        var rClient = Program.GetClientByAccountId(accountId);

                        game.OnPlayerJoined(rClient);

                        rClient.Queue(new MediusJoinGameResponse()
                        {
                            MessageID = msgId,
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                            GameHostType = game.GameHostType,
                            ConnectInfo = new NetConnectionInfo()
                            {
                                AccessKey = joinGameResponse.AccessKey,
                                SessionKey = rClient.SessionKey,
                                WorldID = game.DMEWorldId,
                                ServerKey = joinGameResponse.pubKey,
                                AddressList = new NetAddressList()
                                {
                                    AddressList = new NetAddress[MediusConstants.NET_ADDRESS_LIST_COUNT]
                                    {
                                        new NetAddress() { Address = (data.ClientObject as DMEObject).IP.MapToIPv4().ToString(), Port = (uint)(data.ClientObject as DMEObject).Port, AddressType = NetAddressType.NetAddressTypeExternal},
                                        new NetAddress() { AddressType = NetAddressType.NetAddressNone},
                                    }
                                },
                                Type = NetConnectionType.NetConnectionTypeClientServerTCPAuxUDP
                            }
                        });
                        break;
                    }

                case MediusServerReport serverReport:
                    {
                        (data.ClientObject as DMEObject)?.OnWorldReport(serverReport);

                        break;
                    }
                case MediusServerConnectNotification connectNotification:
                    {
                        Program.GetGameById((int)connectNotification.MediusWorldUID)?.OnMediusServerConnectNotification(connectNotification);


                        break;
                    }

                case MediusServerEndGameResponse endGameResponse:
                    {

                        break;
                    }

                default:
                    {
                        Logger.Warn($"{Name} Unhandled Medius Message: {message}");
                        break;
                    }
            }
        }

        protected DMEObject GetFreeDme()
        {
            try
            {
                return _scertHandler.Group
                    .Select(x => _channelDatas[x.Id.AsLongText()]?.ClientObject)
                    .Where(x => x is DMEObject && x != null)
                    .MinBy(x => (x as DMEObject).CurrentWorlds) as DMEObject;
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return null;
        }

        public DMEObject ReserveDMEObject(MediusServerSessionBeginRequest request)
        {
            var dme = new DMEObject(request);
            Program.Clients.Add(dme);
            return dme;
        }

        public void CreateGame(ClientObject client, MediusCreateGameRequest request)
        {
            // Ensure the name is unique
            // If the host leaves then we unreserve the name
            if (Program.Games.Any(x => x.WorldStatus != MediusWorldStatus.WorldClosed && x.WorldStatus != MediusWorldStatus.WorldInactive && x.GameName == request.GameName && x.Host != null && x.Host.IsConnected))
            {
                client.Queue(new RT_MSG_SERVER_APP()
                {
                    Message = new MediusCreateGameResponse()
                    {
                        MessageID = request.MessageID,
                        MediusWorldID = -1,
                        StatusCode = MediusCallbackStatus.MediusGameNameExists
                    }
                });
                return;
            }

            // 
            var dme = GetFreeDme();
            if (dme == null)
            {
                client.Queue(new RT_MSG_SERVER_APP()
                {
                    Message = new MediusCreateGameResponse()
                    {
                        MessageID = request.MessageID,
                        MediusWorldID = -1,
                        StatusCode = MediusCallbackStatus.MediusExceedsMaxWorlds
                    }
                });
                return;
            }

            var game = new Game(client, request, dme);
            Program.Games.Add(game);

            dme.Queue(new RT_MSG_SERVER_APP()
            {
                Message = new MediusServerCreateGameWithAttributesRequest()
                {
                    MessageID = $"{game.Id}-{client.AccountId}-{request.MessageID}",
                    MediusWorldUID = (uint)game.Id,
                    Attributes = game.Attributes,
                    ApplicationID = Program.Settings.ApplicationId,
                    MaxClients = game.MaxPlayers
                }
            });
        }

        public void JoinGame(ClientObject client, MediusJoinGameRequest request)
        {
            var game = Program.GetGameById(request.MediusWorldID);
            if (game == null)
            {
                client.Queue(new RT_MSG_SERVER_APP()
                {
                    Message = new MediusJoinGameResponse()
                    {
                        MessageID = request.MessageID,
                        StatusCode = MediusCallbackStatus.MediusGameNotFound
                    }
                });
            }
            else if (game.GamePassword != null && game.GamePassword != string.Empty && game.GamePassword != request.GamePassword)
            {
                client.Queue(new MediusJoinGameResponse()
                {
                    MessageID = request.MessageID,
                    StatusCode = MediusCallbackStatus.MediusInvalidPassword
                });
            }
            else
            {
                var dme = game.DMEServer;
                dme.Queue(new MediusServerJoinGameRequest()
                {
                    MessageID = $"{game.Id}-{client.AccountId}-{request.MessageID}",
                    ConnectInfo = new NetConnectionInfo()
                    {
                        Type = NetConnectionType.NetConnectionTypeClientServerTCPAuxUDP,
                        WorldID = game.DMEWorldId,
                        SessionKey = client.SessionKey,
                        ServerKey = Program.GlobalAuthPublic
                    }
                });
            }
        }
    }
}
