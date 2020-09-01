using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;
using RT.Common;
using RT.Cryptography;
using RT.Models;
using Server.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Medius
{
    public class MUIS : BaseMediusComponent
    {
        static readonly IInternalLogger _logger = InternalLoggerFactory.GetInstance<MUIS>();

        protected override IInternalLogger Logger => _logger;
        public override int Port => Program.Settings.MUISPort;
        public override PS2_RSA AuthKey => Program.GlobalAuthKey;

        public MUIS()
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
                        if (!Program.Settings.IsCompatAppId(clientConnectTcp.AppId))
                        {
                            Logger.Error($"Client {clientChannel.RemoteAddress} attempting to authenticate with incompatible app id {clientConnectTcp.AppId}");
                            await clientChannel.CloseAsync();
                            return;
                        }

                        data.ApplicationId = clientConnectTcp.AppId;
                        Queue(new RT_MSG_SERVER_CONNECT_REQUIRE() { Contents = Utils.FromString("024802") }, clientChannel);
                        break;
                    }
                case RT_MSG_CLIENT_CONNECT_READY_REQUIRE clientConnectReadyRequire:
                    {
                        Queue(new RT_MSG_SERVER_CRYPTKEY_GAME() { Key = Utils.FromString(Program.KEY) }, clientChannel);
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
                case RT_MSG_CLIENT_CONNECT_READY_TCP clientConnectReadyTcp:
                    {
                        Queue(new RT_MSG_SERVER_CONNECT_COMPLETE() { ARG1 = 0x0001 }, clientChannel);
                        break;
                    }
                case RT_MSG_SERVER_ECHO serverEchoReply:
                    {

                        break;
                    }
                case RT_MSG_CLIENT_ECHO clientEcho:
                    {
                        Queue(new RT_MSG_CLIENT_ECHO() { Value = clientEcho.Value }, clientChannel);
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
                case MediusGetUniverseInformationRequest getUniverseInfo:
                    {
                        // 
                        Queue(new RT_MSG_SERVER_APP()
                        {
                            Message = new MediusUniverseVariableSvoURLResponse()
                            {
                                MessageID = new MessageId(),
                                Result = 1
                            }
                        }, clientChannel);

                        // 
                        Queue(new RT_MSG_SERVER_APP()
                        {
                            Message = new MediusUniverseVariableInformationResponse()
                            {
                                MessageID = getUniverseInfo.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                InfoFilter = getUniverseInfo.InfoType,
                                UniverseID = 1,
                                ExtendedInfo = "",
                                UniverseName = "Ratchet: Deadlocked Production",
                                UniverseDescription = "Ratchet: Deadlocked Production",
                                DNS = "ratchetdl-prod.pdonline.scea.com",
                                Port = Program.AuthenticationServer.Port,
                                EndOfList = true
                            }
                        }, clientChannel);

                        break;
                    }
                default:
                    {
                        Logger.Warn($"UNHANDLED MEDIUS MESSAGE: {message}");
                        break;
                    }
            }
        }
    }
}
