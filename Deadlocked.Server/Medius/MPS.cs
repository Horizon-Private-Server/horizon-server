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
            if ((DateTime.UtcNow - lastSend).TotalSeconds > 2)
            {
                try
                {
                    if (File.Exists("mps.txt"))
                    {
                        string str = File.ReadAllText("mps.txt");
                        Regex r = new Regex(@"/\*(.*?)\*/", RegexOptions.Singleline);
                        str = r.Replace(str, "");

                        r = new Regex(@"//(.*?)\r?\n", RegexOptions.Multiline);
                        str = r.Replace(str, "\r\n");

                        responses.Add(new RawMessage(RT_MSG_TYPE.RT_MSG_CLIENT_APP_SINGLE)
                        {
                            Contents = Utils.FromString(str.Replace(" ", "").Replace("\r", "").Replace("\n", "").Replace("-", ""))
                        });
                    }
                }
                catch (Exception e)
                {

                }

                lastSend = DateTime.UtcNow;
            }

            // 
            var targetMsgs = client.Client?.PullProxyMessages();
            if (targetMsgs != null && targetMsgs.Count > 0)
                responses.AddRange(targetMsgs);

            // 
            if (shouldEcho)
                Echo(client, ref responses);

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
                case RT_MSG_TYPE.RT_MSG_CLIENT_CONNECT_TCP_AUX_UDP:
                    {
                        break;
                        var m00 = message as RT_MSG_CLIENT_CONNECT_TCP_AUX_UDP;
                        client.SetToken(m00.AccessToken);

                        Console.WriteLine($"CLIENT CONNECTED TO AUX UDP MPS WITH SESSION KEY {m00.SessionKey} and ACCESS TOKEN {m00.AccessToken}");

                        if (client.Client == null)
                        {
                            responses.Add(new RawMessage(RT_MSG_TYPE.RT_MSG_CLIENT_DISCONNECT_WITH_REASON) { Contents = new byte[1] });
                        }
                        else
                        {
                            client.Client.Status = MediusPlayerStatus.MediusPlayerInGameWorld;
                            responses.Add(new RT_MSG_SERVER_CONNECT_REQUIRE() { Contents = Utils.FromString("0648024802") });
                        }
                        break;
                    }
                case RT_MSG_TYPE.RT_MSG_CLIENT_CONNECT_READY_REQUIRE:
                    {
                        responses.Add(new RT_MSG_SERVER_CRYPTKEY_GAME() { Key = Utils.FromString(Program.KEY) });
                        //responses.Add(new RT_MSG_SERVER_CRYPTKEY_PEER() { Key = Utils.FromString(Program.KEY) });

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
                        //var game = Program.Games.FirstOrDefault(x => x.Clients.Any(x => x.Client == client.Client));

                        //responses.Add(new RT_MSG_SERVER_STARTUP_INFO_NOTIFY() { Contents = Utils.FromString("0400000000") });
                        //responses.Add(new RT_MSG_SERVER_INFO_AUX_UDP() { Ip = Program.SERVER_IP, Port = (ushort)(game?.DME.Port ?? 0) });

                        //responses.Add(new RT_MSG_SERVER_INFO_AUX_UDP() { Ip = Program.SERVER_IP, Port = 10070 });
                        responses.Add(new RT_MSG_SERVER_CONNECT_COMPLETE() { ARG1 = 0x0001 });
                        break;
                    }
                case RT_MSG_TYPE.RT_MSG_CLIENT_CONNECT_READY_AUX_UDP:
                    {
                        break;
                        responses.Add(new RT_MSG_SERVER_CONNECT_COMPLETE()
                        {
                            ARG1 = (byte)(_clients.Count > 1 ? 1 : 1)
                        });
                        responses.Add(new RT_MSG_SERVER_APP() { AppMessage = new DMEServerVersion() { } });

                        for (byte i = 0; i < _clients.Count; ++i)
                        {
                            var iClient = _clients[i];

                            var remoteEP = (iClient.RemoteEndPoint as IPEndPoint);
                            var localEP = (iClient.LocalEndPoint as IPEndPoint);

                            var rawMsg = new RawAppMessage(0x1800)
                            {
                                Contents = Utils.FromString($"02 00 00 00 {i:X2} 00 00 00 C0 A8 00 B2 9A 18 00 00 4B 14 BC 13 9A 18".Replace(" ", ""))
                            };
                            Array.Copy(remoteEP.Address.GetAddressBytes(), 0, rawMsg.Contents, 16, 4);
                            Array.Copy(BitConverter.GetBytes((short)remoteEP.Port), 0, rawMsg.Contents, 20, 2);
                            Array.Copy(localEP.Address.GetAddressBytes(), 0, rawMsg.Contents, 8, 4);
                            Array.Copy(BitConverter.GetBytes((short)localEP.Port), 0, rawMsg.Contents, 12, 2);

                            Array.Copy(IPAddress.Parse("55.55.55.55").GetAddressBytes(), 0, rawMsg.Contents, 16, 4);
                            Array.Copy(BitConverter.GetBytes((short)12345), 0, rawMsg.Contents, 20, 2);

                            var rawMsg2 = new RawAppMessage(0x0100)
                            {
                                Contents = Utils.FromString("00 00 00".Replace(" ", ""))
                            };


                            //var connectsMsg = new DMEClientConnects()
                            //{
                            //    PlayerIndex = i,
                            //    Key = new RSA_KEY(Program.GlobalAuthKey.N.ToByteArrayUnsigned().Reverse().ToArray()),
                            //    PlayerIp = (iClient.RemoteEndPoint as IPEndPoint).Address,
                            //};

                            //var clientStatusMsg = new DMEUpdateClientStatus()
                            //{
                            //    PlayerIndex = i,
                            //    Status = i == 0 ? NetClientStatus.ClientStatusJoinedSessionMaster : NetClientStatus.ClientStatusJoining,
                            //    UNK_06 = (ushort)(i == 0 ? 1 : 0),
                            //    UNK_0A = (ushort)(i == 0 ? 1 : 2)
                            //};

                            var buffer = new byte[1024 * 10];
                            int length = 0;

                            // Serialize message
                            using (MemoryStream stream = new MemoryStream(buffer, true))
                            {
                                using (BinaryWriter writer = new BinaryWriter(stream))
                                {
                                    writer.Write(rawMsg.Id);
                                    rawMsg.Serialize(writer);
                                    writer.Write(rawMsg2.Id);
                                    rawMsg2.Serialize(writer);
                                    //writer.Write(connectsMsg.Id);
                                    //connectsMsg.Serialize(writer);
                                    //writer.Write(clientStatusMsg.Id);
                                    //clientStatusMsg.Serialize(writer);
                                    length = (int)writer.BaseStream.Position;
                                }
                            }

                            var result = new byte[length + 0];
                            Array.Copy(buffer, 0, result, 0, length);

                            foreach (var c in _clients)
                            {
                                c.Client.AddProxyMessage(new RawMessage(RT_MSG_TYPE.RT_MSG_SERVER_APP)
                                {
                                    Contents = result
                                });
                            }
                        }

                        //responses.Add(new RawMessage(RT_MSG_TYPE.RT_MSG_CLIENT_APP_SINGLE)
                        //{
                        //    Contents = Utils.FromString("00 18 02 00 00 00 00 00 00 00 C0 A8 01 42 9A 18 00 00 4B 14 BC 13 9A 18 00 00 01 00 00 00 00 10 02 00 4B 14 BC 13 00 00 6B 8F 99 EC 1B AF 06 D2 67 42 84 B5 30 5E E6 E3 8B 1D E7 33 1F 2F BF 31 DE 49 72 28 B7 C5 21 62 F1 8D AE 89 13 C4 0C 43 C0 E8 90 D1 4E EE 16 AD 07 C6 4F D9 28 1D 8B 97 2D 78 BE 78 D1 B2 90 CE 00 16 05 00 03 00 01 00 00 00 01 00 00 00 00 00 00 00".Replace(" ", ""))
                        //});
                        //responses.Add(new RawMessage(RT_MSG_TYPE.RT_MSG_CLIENT_APP_SINGLE)
                        //{
                        //    Contents = Utils.FromString("00 18 02 00 00 00 02 00 00 00 C0 A8 00 04 F5 18 00 00 45 95 4F BC F5 18 00 00 01 00 00 00 00 10 02 02 45 95 4F BC 00 00 6B 8F 99 EC 1B AF 06 D2 67 42 84 B5 30 5E E6 E3 8B 1D E7 33 1F 2F BF 31 DE 49 72 28 B7 C5 21 62 F1 8D AE 89 13 C4 0C 43 C0 E8 90 D1 4E EE 16 AD 07 C6 4F D9 28 1D 8B 97 2D 78 BE 78 D1 B2 90 CE 00 16 04 02 03 00 00 00 00 00 02 00 00 00 00 00 00 00".Replace(" ", ""))
                        //});
                        //responses.Add(new RawMessage(RT_MSG_TYPE.RT_MSG_CLIENT_APP_SINGLE)
                        //{
                        //    Contents = Utils.FromString("00 16 05 00 03 00 01 00 6C D5 01 00 00 00 00 00 00 00 00 09 D7 71 09 00 06 00 00 00 00 17 03 00 00 00 00 04 00 01 00 00 04 6E 00 00 00 00 00 00 74 0D 02 00 00 00 00 00 74 4E 57 5F 47 61 6D 65 53 65 74 74 69 6E 67 00 50 6A 00 00 00 00 00 00 00 00 00 00 00 00 00 00 44 33 24 4B 20 7B 52 57 24 7D 00 00 00 00 00 00 00 65 76 69 6E 00 5F 53 6D 61 73 68 65 72 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 80 46 00 00 00 00 00 00 0E 4D 6E 4C 00 00 00 00 00 3C 33 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 03 00 13 00 FF FF FF FF FF FF FF FF FF FF FF FF 00 00 00 00 FF FF FF FF FF FF FF FF FF FF FF FF 00 00 02 00 FF FF FF FF FF FF FF FF FF FF FF FF 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 02 00 00 00 FF FF FF FF FF FF FF FF 28 00 00 00 00 00 00 00 00 00 00 00 01 01 01 01 01 01 00 01 01 00 00 01 01 01 00 14 03 FF 00 00 00 01 05 01 09 7D 0F 00 9C 7C 10 00 FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF 58 81 DA 44 AE F8 D7 44 00 00 80 BF 00 00 80 BF 00 00 80 BF 00 00 80 BF 00 00 80 BF 00 00 80 BF 00 00 C8 42 00 00 DC 42 00 00 80 BF 00 00 80 BF 00 00 80 BF 00 00 80 BF 00 00 80 BF 00 00 80 BF".Replace(" ", ""))
                        //});
                        Console.WriteLine("SENT CONNECT");
                        break;
                    }
                case RT_MSG_TYPE.RT_MSG_SERVER_ECHO:
                    {

                        break;
                    }
                case RT_MSG_TYPE.RT_MSG_CLIENT_SET_RECV_FLAG:
                    {

                        break;
                    }
                case RT_MSG_TYPE.RT_MSG_CLIENT_APP_BROADCAST:
                    {
                        var msg = message as RT_MSG_CLIENT_APP_BROADCAST;

                        var game = Program.Games.FirstOrDefault(x => x.Clients.Any(x=> x.Client == client.Client));
                        if (game != null)
                        {
                            byte clientIndex = (byte)game.Clients.Select(x => x.Client).ToList().IndexOf(client.Client);
                            foreach (var peer in game.Clients)
                            {
                                if (peer.Client != client.Client)
                                {
                                    byte[] buffer = new byte[msg.Contents.Length + 2];
                                    buffer[0] = clientIndex;
                                    Array.Copy(msg.Contents, 0, buffer, 2, msg.Contents.Length);
                                    peer.Client.AddProxyMessage(new RawMessage(RT_MSG_TYPE.RT_MSG_CLIENT_APP_SINGLE)
                                    {
                                        Contents = buffer
                                    });
                                }
                            }
                        }
                        break;
                    }
                case RT_MSG_TYPE.RT_MSG_CLIENT_APP_LIST:
                    {
                        var msg = message as RT_MSG_CLIENT_APP_LIST;

                        if (msg.Contents != null && msg.Contents.Length > 0)
                        {
                            var game = Program.Games.FirstOrDefault(x => x.Clients.Any(x => x.Client == client.Client));
                            if (game != null)
                            {
                                byte clientIndex = (byte)game.Clients.Select(x => x.Client).ToList().IndexOf(client.Client);
                                foreach (var peer in game.Clients)
                                {
                                    if (peer.Client != client.Client)
                                    {
                                        byte[] buffer = new byte[msg.Contents.Length + 2 - 3];
                                        buffer[0] = clientIndex;
                                        Array.Copy(msg.Contents, 3, buffer, 2, msg.Contents.Length - 3);
                                        peer.Client.AddProxyMessage(new RawMessage(RT_MSG_TYPE.RT_MSG_CLIENT_APP_SINGLE)
                                        {
                                            Contents = buffer
                                        });
                                    }
                                }
                            }
                        }
                        break;
                    }
                case RT_MSG_TYPE.RT_MSG_CLIENT_TIMEBASE_QUERY:
                    {
                        var msg = message as RT_MSG_CLIENT_TIMEBASE_QUERY;
                        var game = Program.Games.FirstOrDefault(x => x.Clients.Any(x => x.Client == client.Client));

                        responses.Add(new RT_MSG_SERVER_TIMEBASE_QUERY_NOTIFY()
                        {
                            ClientTime = msg.Timestamp,
                            ServerTime = game?.Time ?? 0
                        });

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
                                    var game = Program.Games.FirstOrDefault(x => x.Id == gameId);
                                    var rClient = Program.Clients.FirstOrDefault(x => x.ClientAccount.AccountId == accountId);
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
                                    var game = Program.Games.FirstOrDefault(x => x.Id == gameId);
                                    var rClient = Program.Clients.FirstOrDefault(x => x.ClientAccount.AccountId == accountId);

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
                                                            new NetAddress() { Address = Program.SERVER_IP.ToString(), Port = (uint)10072, AddressType = NetAddressType.NetAddressTypeExternal},
                                                                new NetAddress() { AddressType = NetAddressType.NetAddressNone},
                                                        }
                                                },
                                                Type = NetConnectionType.NetConnectionTypeClientServerTCPAuxUDP
                                            }
                                        }
                                    });
                                    break;
                                }

                            case MediusAppPacketIds.ExtendedSessionBeginRequest:
                                {
                                    break;
                                    var sessionBeginMsg = appMsg as MediusExtendedSessionBeginRequest;

                                    responses.Add(new RT_MSG_SERVER_APP()
                                    {
                                        AppMessage = new MediusSessionBeginResponse()
                                        {
                                            MessageID = sessionBeginMsg.MessageID,
                                            SessionKey = "13088",
                                            StatusCode = MediusCallbackStatus.MediusSuccess
                                        }
                                    });
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
    }
}
