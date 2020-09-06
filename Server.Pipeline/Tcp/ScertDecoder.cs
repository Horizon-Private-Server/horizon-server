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
    public class ScertDecoder : MessageToMessageDecoder<IByteBuffer>
    {
        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<ScertDecoder>();

        readonly ICipher[] _ciphers = null;
        readonly Func<RT_MSG_TYPE, CipherContext, ICipher> _getCipher = null;

        /// <summary>
        ///     Create a new instance.
        /// </summary>
        public ScertDecoder(params ICipher[] ciphers)
        {
            this._ciphers = ciphers;
            this._getCipher = (id, ctx) =>
            {
                return _ciphers?.FirstOrDefault(x => x.Context == ctx);
            };
        }

        protected override void Decode(IChannelHandlerContext context, IByteBuffer input, List<object> output)
        {
            try
            {
                var decoded = this.Decode(context, input);
                if (decoded != null)
                    output.Add(decoded);
            }
            catch (Exception e)
            {
                Logger.Error(e);
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
            //input.MarkReaderIndex();
            byte id = input.GetByte(input.ReaderIndex);
            byte[] hash = null;
            long frameLength = input.GetShortLE(input.ReaderIndex + 1);
            int totalLength = 3;

            if (frameLength <= 0)
                return BaseScertMessage.Instantiate((RT_MSG_TYPE)(id & 0x7F), null, new byte[0], _getCipher);

            if (id >= 0x80)
            {
                hash = new byte[4];
                input.GetBytes(input.ReaderIndex + 3, hash);
                totalLength += 4;
                id &= 0x7F;
            }

            if (frameLength < 0)
            {
                throw new CorruptedFrameException("negative pre-adjustment length field: " + frameLength);
            }

            // never overflows because it's less than maxFrameLength
            int frameLengthInt = (int)frameLength;
            if (input.ReadableBytes < frameLengthInt)
            {
                //input.ResetReaderIndex();
                return null;
            }

            // extract frame
            byte[] messageContents = new byte[frameLengthInt];
            input.GetBytes(input.ReaderIndex + totalLength, messageContents);

            // 
            input.SetReaderIndex(input.ReaderIndex + totalLength + frameLengthInt);
            return BaseScertMessage.Instantiate((RT_MSG_TYPE)id, hash, messageContents, _getCipher);
        }

    }
}