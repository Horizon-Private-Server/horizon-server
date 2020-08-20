// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Deadlocked.Server.Medius.Models.Packets;
using Deadlocked.Server.SCERT.Models;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using Medius.Crypto;

namespace Deadlocked.Server.Medius
{
    public class MediusDecoder : MessageToMessageDecoder<IByteBuffer>
    {
        public override bool IsSharable => true;

        /// <summary>
        ///     Create a new instance.
        /// </summary>
        public MediusDecoder()
        {
        }

        protected override void Decode(IChannelHandlerContext context, IByteBuffer input, List<object> output)
        {
            object decoded = this.Decode(context, input);
            if (decoded != null)
            {
                output.Add(decoded);
            }
        }

        /// <summary>
        ///     Create a frame out of the <see cref="IByteBuffer" /> and return it.
        /// </summary>
        /// <param name="context">
        ///     The <see cref="IChannelHandlerContext" /> which this <see cref="ByteToMessageDecoder" /> belongs
        ///     to.
        /// </param>
        /// <param name="input">The <see cref="IByteBuffer" /> from which to read data.</param>
        /// <returns>The <see cref="IByteBuffer" /> which represents the frame or <c>null</c> if no frame could be created.</returns>
        protected virtual object Decode(IChannelHandlerContext context, IByteBuffer input)
        {
            // 
            int readerStartIndex = input.ReaderIndex;

            // 
            byte msgClass = input.ReadByte();
            byte msgType = input.ReadByte();

            // extract frame
            IByteBuffer messageContents = input.ReadBytes(input.ReadableBytes);

            // 
            return BaseMediusMessage.Instantiate(, messageContents, getCipher);
        }

    }
}