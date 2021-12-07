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

namespace Server.Pipeline.Tcp
{
    public class ScertMultiAppDecoder : MessageToMessageDecoder<RT_MSG_CLIENT_MULTI_APP_TOSERVER>
    {
        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<ScertMultiAppDecoder>();

        /// <summary>
        ///     Create a new instance.
        /// </summary>
        public ScertMultiAppDecoder()
        {

        }

        protected override void Decode(IChannelHandlerContext context, RT_MSG_CLIENT_MULTI_APP_TOSERVER input, List<object> output)
        {
            try
            {
                foreach (var message in input.Messages)
                    output.Add(message);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }
}
