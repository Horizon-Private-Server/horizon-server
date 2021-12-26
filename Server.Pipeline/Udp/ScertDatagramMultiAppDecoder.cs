// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;
using RT.Common;
using RT.Cryptography;
using RT.Models;

namespace Server.Pipeline.Udp
{
    public class ScertDatagramMultiAppDecoder : MessageToMessageDecoder<ScertDatagramPacket>
    {
        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<ScertDatagramMultiAppDecoder>();

        /// <summary>
        ///     Create a new instance.
        /// </summary>
        public ScertDatagramMultiAppDecoder()
        {

        }

        protected override void Decode(IChannelHandlerContext context, ScertDatagramPacket input, List<object> output)
        {
            try
            {
                if (input.Message is RT_MSG_CLIENT_MULTI_APP_TOSERVER multiApp)
                {
                    foreach (var message in multiApp.Messages)
                        output.Add(new ScertDatagramPacket(message, input.Destination, input.Source));
                }
                else
                {
                    output.Add(input);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }
}
