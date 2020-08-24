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
        public override PS2_RSA AuthKey => Program.GlobalAuthKey;

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
                        data.ClientObject = Program.Manager.GetDmeByAccessToken(clientConnectTcp.AccessToken);
                        if (data.ClientObject == null)
                        {
                            await DisconnectClient(clientChannel);
                        }
                        else
                        {
                            // 
                            data.ClientObject.OnConnected();

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
                        var game = Program.Manager.GetGameByGameId(gameId);
                        var rClient = Program.Manager.GetClientByAccountId(accountId);
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
                        var game = Program.Manager.GetGameByGameId(gameId);
                        var rClient = Program.Manager.GetClientByAccountId(accountId);

                        // Indicate the client is connecting to a different part of Medius
                        rClient.KeepAliveUntilNextConnection();

                        // Join game
                        rClient.JoinGame(game);

                        // 
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
                        Program.Manager.GetGameByGameId((int)connectNotification.MediusWorldUID)?.OnMediusServerConnectNotification(connectNotification);


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

        public DMEObject GetFreeDme()
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
            dme.BeginSession();
            Program.Manager.AddDmeClient(dme);
            return dme;
        }

        
    }
}
