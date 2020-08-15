using Deadlocked.Server.Messages;
using Deadlocked.Server.Messages.DME;
using Deadlocked.Server.Messages.Lobby;
using Deadlocked.Server.Messages.MGCL;
using Deadlocked.Server.Messages.RTIME;
using Deadlocked.Server.Stream;
using Medius.Crypto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Deadlocked.Server.Medius
{
    public class MPS : BaseMediusComponent
    {
        public override int Port => 10079;
        DateTime lastSend = DateTime.UtcNow;

        public MPS()
        {
            _sessionCipher = new PS2_RC4(Utils.FromString(Program.KEY), CipherContext.RC_CLIENT_SESSION);
        }

        protected override void Tick(ClientSocket client)
        {
            List<BaseMessage> recv = new List<BaseMessage>();
            List<BaseMessage> responses = new List<BaseMessage>();

            lock (_queue)
            {
                while (_queue.Count > 0)
                    recv.Add(_queue.Dequeue());
            }

            foreach (var msg in recv)
                HandleCommand(msg, client, ref responses);

            // 
            var targetMsgs = client.Client?.PullProxyMessages();
            if (targetMsgs != null && targetMsgs.Count > 0)
                responses.AddRange(targetMsgs);

            // 
            if (shouldEcho)
                Echo(client, ref responses);

            // 
            responses.Send(client);
        }

        protected override int HandleCommand(BaseMessage message, ClientSocket client, ref List<BaseMessage> responses)
        {
            // 
            if (message.Id != RT_MSG_TYPE.RT_MSG_CLIENT_ECHO && message.Id != RT_MSG_TYPE.RT_MSG_SERVER_ECHO && message.Id != RT_MSG_TYPE.RT_MSG_CLIENT_TIMEBASE_QUERY)
                Console.WriteLine($"MPS {client?.Client?.ClientAccount?.AccountName}: " + message.ToString());

            // 
            switch (message.Id)
            {
                case RT_MSG_TYPE.RT_MSG_CLIENT_HELLO:
                    {
                        responses.Add(new RT_MSG_SERVER_HELLO());

                        break;
                    }
                case RT_MSG_TYPE.RT_MSG_CLIENT_CRYPTKEY_PUBLIC:
                    {
                        responses.Add(new RT_MSG_SERVER_CRYPTKEY_PEER() { Key = Utils.FromString(Program.KEY) });
                        break;
                    }
                case RT_MSG_TYPE.RT_MSG_CLIENT_CONNECT_TCP:
                    {
                        var m00 = message as RT_MSG_CLIENT_CONNECT_TCP;
                        client.SetToken(m00.AccessToken);

                        responses.Add(new RT_MSG_SERVER_CONNECT_REQUIRE() { Contents = Utils.FromString("024802") });
                        break;
                    }
                case RT_MSG_TYPE.RT_MSG_CLIENT_CONNECT_READY_REQUIRE:
                    {
                        responses.Add(new RT_MSG_SERVER_CRYPTKEY_GAME() { Key = Utils.FromString(Program.KEY) });
                        responses.Add(new RT_MSG_SERVER_CONNECT_ACCEPT_TCP()
                        {
                            UNK_00 = 0,
                            UNK_01 = 0,
                            UNK_02 = 0x01,
                            UNK_06 = 0x01,
                            IP = (client.RemoteEndPoint as IPEndPoint).Address
                        });
                        break;
                    }
                case RT_MSG_TYPE.RT_MSG_CLIENT_CONNECT_READY_TCP:
                    {
                        responses.Add(new RT_MSG_SERVER_CONNECT_COMPLETE() { ARG1 = 0x0001 });
                        break;
                    }
                case RT_MSG_TYPE.RT_MSG_SERVER_ECHO:
                    {

                        break;
                    }
                case RT_MSG_TYPE.RT_MSG_CLIENT_APP_TOSERVER:
                    {
                        var appMsg = (message as RT_MSG_CLIENT_APP_TOSERVER).AppMessage;

                        switch (appMsg.Id)
                        {
                            case MediusAppPacketIds.MediusServerCreateGameWithAttributesResponse:
                                {
                                    var msg = appMsg as MediusServerCreateGameWithAttributesResponse;

                                    int gameId = int.Parse(msg.MessageID.Split('-')[0]);
                                    int accountId = int.Parse(msg.MessageID.Split('-')[1]);
                                    string msgId = msg.MessageID.Split('-')[2];
                                    var game = Program.GetGameById(gameId);
                                    var rClient = Program.GetClientByAccountId(accountId);
                                    game.DMEWorldId = msg.WorldID;

                                    rClient.AddLobbyMessage(new RT_MSG_SERVER_APP()
                                    {
                                        AppMessage = new MediusCreateGameResponse()
                                        {
                                            MessageID = msgId,
                                            StatusCode = MediusCallbackStatus.MediusSuccess,
                                            MediusWorldID = game.Id
                                        }
                                    });


                                    break;
                                }
                            case MediusAppPacketIds.MediusServerJoinGameResponse:
                                {
                                    var msg = appMsg as MediusServerJoinGameResponse;

                                    int gameId = int.Parse(msg.MessageID.Split('-')[0]);
                                    int accountId = int.Parse(msg.MessageID.Split('-')[1]);
                                    string msgId = msg.MessageID.Split('-')[2];
                                    var game = Program.GetGameById(gameId);
                                    var rClient = Program.GetClientByAccountId(accountId);

                                    game.OnPlayerJoined(rClient);
                                    rClient.AddLobbyMessage(new RT_MSG_SERVER_APP()
                                    {
                                        AppMessage = new MediusJoinGameResponse()
                                        {
                                            MessageID = msgId,
                                            StatusCode = MediusCallbackStatus.MediusSuccess,
                                            GameHostType = game.GameHostType,
                                            ConnectInfo = new NetConnectionInfo()
                                            {
                                                AccessKey = msg.AccessKey,
                                                SessionKey = rClient.SessionKey,
                                                WorldID = game.DMEWorldId,
                                                ServerKey = msg.pubKey,
                                                AddressList = new NetAddressList()
                                                {
                                                    AddressList = new NetAddress[MediusConstants.NET_ADDRESS_LIST_COUNT]
                                                        {
                                                            //new NetAddress() { Address = Program.SERVER_IP.ToString(), Port = (uint)Program.ProxyServer.Port, AddressType = NetAddressType.NetAddressTypeExternal},
#if TRUE || RELEASE
                                                            new NetAddress() { Address = (client.RemoteEndPoint as IPEndPoint).Address.ToString(), Port = (uint)(client.Client as DMEObject).Port, AddressType = NetAddressType.NetAddressTypeExternal},
#else                                                        
                                                            new NetAddress() { Address = (client.Client as DMEObject).IP.ToString(), Port = (uint)(client.Client as DMEObject).Port, AddressType = NetAddressType.NetAddressTypeExternal},
#endif
                                                            new NetAddress() { AddressType = NetAddressType.NetAddressNone},
                                                        }
                                                },
                                                Type = NetConnectionType.NetConnectionTypeClientServerTCPAuxUDP
                                            }
                                        }
                                    });
                                    break;
                                }

                            case MediusAppPacketIds.MediusServerReport:
                                {
                                    (client.Client as DMEObject)?.OnWorldReport(appMsg as MediusServerReport);

                                    break;
                                }
                            case MediusAppPacketIds.MediusServerConnectNotification:
                                {
                                    var msg = appMsg as MediusServerConnectNotification;

                                    Program.GetGameById((int)msg.MediusWorldUID)?.OnMediusServerConnectNotification(msg);
                                    

                                    break;
                                }

                            case MediusAppPacketIds.MediusServerEndGameResponse:
                                {
                                    var msg = appMsg as MediusServerEndGameResponse;

                                    break;
                                }

                            default:
                                {
                                    Console.WriteLine($"MPS Unhandled App Message: {appMsg.Id} {appMsg}");
                                    break;
                                }
                        }
                        break;
                    }

                case RT_MSG_TYPE.RT_MSG_CLIENT_ECHO:
                    {
                        responses.Add(new RT_MSG_CLIENT_ECHO() { Value = (message as RT_MSG_CLIENT_ECHO).Value });
                        break;
                    }
                case RT_MSG_TYPE.RT_MSG_CLIENT_DISCONNECT:
                case RT_MSG_TYPE.RT_MSG_CLIENT_DISCONNECT_WITH_REASON:
                    {
                        client.Disconnect();
                        break;
                    }
                default:
                    {
                        Console.WriteLine($"MPS Unhandled Medius Command: {message.Id} {message}");
                        break;
                    }
            }

            return 0;
        }


        protected ClientSocket GetFreeDme()
        {
            try
            {
                return Clients.Where(x => x.Client is DMEObject).MinBy(x => (x.Client as DMEObject).CurrentWorlds);
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine(e);
            }

            return null;
        }


        public void CreateGame(ClientSocket client, MediusCreateGameRequest request)
        {
            // Ensure the name is unique
            if (Program.Games.Any(x => x.GameName == request.GameName))
            {
                client.Client.AddLobbyMessage(new RT_MSG_SERVER_APP()
                {
                    AppMessage = new MediusCreateGameResponse()
                    {
                        MessageID = request.MessageID,
                        MediusWorldID = -1,
                        StatusCode = MediusCallbackStatus.MediusGameNameExists
                    }
                });
                return;
            }

            // 
            var dme = GetFreeDme()?.Client as DMEObject;
            if (dme == null)
            {
                client.Client.AddLobbyMessage(new RT_MSG_SERVER_APP()
                {
                    AppMessage = new MediusCreateGameResponse()
                    {
                        MessageID = request.MessageID,
                        MediusWorldID = -1,
                        StatusCode = MediusCallbackStatus.MediusExceedsMaxWorlds
                    }
                });
                return;
            }

            var game = new Game(client.Client, request, dme);
            Program.Games.Add(game);

            dme.AddProxyMessage(new RT_MSG_SERVER_APP()
            {
                AppMessage = new MediusServerCreateGameWithAttributesRequest()
                {
                    MessageID = $"{game.Id}-{client.Client.ClientAccount.AccountId}-{request.MessageID}",
                    MediusWorldUID = (uint)game.Id,
                    Attributes = game.Attributes,
                    ApplicationID = Program.Settings.ApplicationId,
                    MaxClients = game.MaxPlayers
                }
            });
        }

        public void JoinGame(ClientSocket client, MediusJoinGameRequest request)
        {
            var game = Program.GetGameById(request.MediusWorldID);
            if (game == null)
            {
                client.Client.AddLobbyMessage(new RT_MSG_SERVER_APP()
                {
                    AppMessage = new MediusJoinGameResponse()
                    {
                        MessageID = request.MessageID,
                        StatusCode = MediusCallbackStatus.MediusGameNotFound
                    }
                });
            }
            else if (game.GamePassword != null && game.GamePassword != string.Empty && game.GamePassword != request.GamePassword)
            {
                client.Client.AddLobbyMessage(new RT_MSG_SERVER_APP()
                {
                    AppMessage = new MediusJoinGameResponse()
                    {
                        MessageID = request.MessageID,
                        StatusCode = MediusCallbackStatus.MediusInvalidPassword
                    }
                });
            }
            else
            {
                var dme = game.DMEServer;
                dme.AddProxyMessage(new RT_MSG_SERVER_APP()
                {
                    AppMessage = new MediusServerJoinGameRequest()
                    {
                        MessageID = $"{game.Id}-{client.Client.ClientAccount.AccountId}-{request.MessageID}",
                        ConnectInfo = new NetConnectionInfo()
                        {
                            Type = NetConnectionType.NetConnectionTypeClientServerTCPAuxUDP,
                            WorldID = game.DMEWorldId,
                            SessionKey = request.SessionKey,
                            ServerKey = new RSA_KEY(Program.GlobalAuthKey.N.ToByteArrayUnsigned().Reverse().ToArray())
                        }
                    }
                });
            }
        }
    }
}
