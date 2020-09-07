using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;
using RT.Common;
using RT.Cryptography;
using RT.Models;
using Server.Common;
using Server.Medius.Models;
using Server.Medius.PluginArgs;
using Server.Plugins;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Server.Medius
{
    public class MPS : BaseMediusComponent
    {
        static readonly IInternalLogger _logger = InternalLoggerFactory.GetInstance<MPS>();

        protected override IInternalLogger Logger => _logger;
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
                        if (data.State > ClientState.HELLO)
                            throw new Exception($"Unexpected RT_MSG_CLIENT_HELLO from {clientChannel.RemoteAddress}: {clientHello}");

                        data.State = ClientState.HELLO;
                        Queue(new RT_MSG_SERVER_HELLO(), clientChannel);
                        break;
                    }
                case RT_MSG_CLIENT_CRYPTKEY_PUBLIC clientCryptKeyPublic:
                    {
                        if (data.State > ClientState.HANDSHAKE)
                            throw new Exception($"Unexpected RT_MSG_CLIENT_CRYPTKEY_PUBLIC from {clientChannel.RemoteAddress}: {clientCryptKeyPublic}");

                        // Ensure key is correct
                        if (!clientCryptKeyPublic.Key.Reverse().SequenceEqual(Program.Settings.MPSKey.N.ToByteArrayUnsigned()))
                        {
                            Logger.Error($"Client {clientChannel.RemoteAddress} attempting to authenticate with invalid key {clientCryptKeyPublic}");
                            data.State = ClientState.DISCONNECTED;
                            await clientChannel.CloseAsync();
                            break;
                        }

                        data.State = ClientState.CONNECT_1;
                        Queue(new RT_MSG_SERVER_CRYPTKEY_PEER() { Key = Utils.FromString(Program.KEY) }, clientChannel);
                        break;
                    }
                case RT_MSG_CLIENT_CONNECT_TCP clientConnectTcp:
                    {
                        if (data.State > ClientState.CONNECT_1)
                            throw new Exception($"Unexpected RT_MSG_CLIENT_CONNECT_TCP from {clientChannel.RemoteAddress}: {clientConnectTcp}");

                        data.ApplicationId = clientConnectTcp.AppId;
                        data.State = ClientState.AUTHENTICATED;
                        Queue(new RT_MSG_SERVER_CONNECT_ACCEPT_TCP()
                        {
                            UNK_00 = 0,
                            UNK_02 = GenerateNewScertClientId(),
                            UNK_04 = 0,
                            UNK_06 = 0x0001,
                            IP = (clientChannel.RemoteAddress as IPEndPoint)?.Address
                        }, clientChannel);

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
                        if (data.State != ClientState.AUTHENTICATED)
                            throw new Exception($"Unexpected RT_MSG_CLIENT_APP_TOSERVER from {clientChannel.RemoteAddress}: {clientAppToServer}");

                        ProcessMediusMessage(clientAppToServer.Message, clientChannel, data);
                        break;
                    }

                case RT_MSG_CLIENT_DISCONNECT_WITH_REASON clientDisconnectWithReason:
                    {
                        data.State = ClientState.DISCONNECTED;
                        await clientChannel.CloseAsync();
                        break;
                    }
                default:
                    {
                        Logger.Warn($"UNHANDLED MESSAGE: {message}");

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
                // This is a bit of a hack to get our custom dme client to authenticate
                // Our client skips MAS and just connects directly to MPS with this message
                case MediusServerSetAttributesRequest dmeSetAttributesRequest:
                    {
                        // Create DME object
                        var dme = new DMEObject(dmeSetAttributesRequest);
                        dme.ApplicationId = data.ApplicationId;
                        dme.BeginSession();
                        Program.Manager.AddDmeClient(dme);
                        
                        // 
                        data.ClientObject = dme;

                        // 
                        data.ClientObject.OnConnected();

                        Queue(new RT_MSG_SERVER_APP()
                        {
                             Message = new MediusServerSetAttributesResponse()
                             {
                                 MessageID = dmeSetAttributesRequest.MessageID,
                                 Confirmation = MGCL_ERROR_CODE.MGCL_SUCCESS
                             }
                        }, clientChannel);

                        break;
                    }

                case MediusServerCreateGameWithAttributesResponse createGameWithAttrResponse:
                    {
                        int gameId = int.Parse(createGameWithAttrResponse.MessageID.Value.Split('-')[0]);
                        int accountId = int.Parse(createGameWithAttrResponse.MessageID.Value.Split('-')[1]);
                        string msgId = createGameWithAttrResponse.MessageID.Value.Split('-')[2];
                        var game = Program.Manager.GetGameByGameId(gameId);
                        var rClient = Program.Manager.GetClientByAccountId(accountId);

                        if (!createGameWithAttrResponse.IsSuccess)
                        {
                            rClient.Queue(new MediusCreateGameResponse()
                            {
                                MessageID = new MessageId(msgId),
                                StatusCode = MediusCallbackStatus.MediusFail
                            });

                            game.EndGame();
                        }
                        else
                        {
                            game.DMEWorldId = createGameWithAttrResponse.WorldID;
                            rClient.Queue(new MediusCreateGameResponse()
                            {
                                MessageID = new MessageId(msgId),
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                MediusWorldID = game.Id
                            });

                            // Send to plugins
                            Program.Plugins.OnEvent(PluginEvent.MEDIUS_GAME_ON_CREATED, new OnPlayerGameArgs() { Player = rClient, Game = game });
                        }

                        break;
                    }
                case MediusServerJoinGameResponse joinGameResponse:
                    {
                        int gameId = int.Parse(joinGameResponse.MessageID.Value.Split('-')[0]);
                        int accountId = int.Parse(joinGameResponse.MessageID.Value.Split('-')[1]);
                        string msgId = joinGameResponse.MessageID.Value.Split('-')[2];
                        var game = Program.Manager.GetGameByGameId(gameId);
                        var rClient = Program.Manager.GetClientByAccountId(accountId);


                        if (!joinGameResponse.IsSuccess)
                        {
                            rClient.Queue(new MediusJoinGameResponse()
                            {
                                MessageID = new MessageId(msgId),
                                StatusCode = MediusCallbackStatus.MediusFail
                            });
                        }
                        else
                        {
                            // Join game
                            rClient.JoinGame(game);

                            // 
                            rClient.Queue(new MediusJoinGameResponse()
                            {
                                MessageID = new MessageId(msgId),
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
                                        AddressList = new NetAddress[Constants.NET_ADDRESS_LIST_COUNT]
                                        {
                                        new NetAddress() { Address = (data.ClientObject as DMEObject).IP.MapToIPv4().ToString(), Port = (uint)(data.ClientObject as DMEObject).Port, AddressType = NetAddressType.NetAddressTypeExternal},
                                        new NetAddress() { AddressType = NetAddressType.NetAddressNone},
                                        }
                                    },
                                    Type = NetConnectionType.NetConnectionTypeClientServerTCPAuxUDP
                                }
                            });
                        }
                        break;
                    }

                case MediusServerReport serverReport:
                    {
                        (data.ClientObject as DMEObject)?.OnWorldReport(serverReport);

                        break;
                    }
                case MediusServerConnectNotification connectNotification:
                    {
                        Program.Manager.GetGameByDmeWorldId((int)connectNotification.MediusWorldUID)?.OnMediusServerConnectNotification(connectNotification);


                        break;
                    }

                case MediusServerEndGameResponse endGameResponse:
                    {

                        break;
                    }

                default:
                    {
                        Logger.Warn($"Unhandled Medius Message: {message}");
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
