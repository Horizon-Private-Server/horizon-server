﻿using DotNetty.Codecs;
using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;
using RT.Cryptography;
using RT.Models;
using RT.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Pipeline.Tcp
{
    public class ScertEncoder : MessageToMessageEncoder<BaseScertMessage>
    {
        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<ScertEncoder>();
        
        readonly ICipher[] _ciphers = null;
        readonly Func<RT_MSG_TYPE, CipherContext, ICipher> _getCipher = null;

        public ScertEncoder(params ICipher[] ciphers)
        {
            this._ciphers = ciphers;
            this._getCipher = (id, ctx) =>
            {
                return _ciphers?.FirstOrDefault(x => x.Context == ctx);
            };
        }

        protected override void Encode(IChannelHandlerContext ctx, BaseScertMessage message, List<object> output)
        {
            if (message is null)
                return;

            // Log
            if (message.CanLog())
                Logger.Info($"SEND {ctx.Channel}: {message}");

            //
            if (!ctx.HasAttribute(Constants.SCERT_CLIENT))
                ctx.GetAttribute(Constants.SCERT_CLIENT).Set(new Attribute.ScertClientAttribute());
            var scertClient = ctx.GetAttribute(Constants.SCERT_CLIENT).Get();

            // 
            scertClient.OnMessage(message);

            // Serialize
            var msgs = message.Serialize(scertClient.MediusVersion, scertClient.CipherService);

            // 
            foreach (var msg in msgs)
            {
                var byteBuffer = ctx.Allocator.Buffer(msg.Length);
                byteBuffer.WriteBytes(msg);
                output.Add(byteBuffer);
            }
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            Logger.Error(exception);
            context.CloseAsync();
        }
    }
}