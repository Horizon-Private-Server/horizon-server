// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Deadlocked.Server.SCERT.Models;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using Medius.Crypto;

namespace Deadlocked.Server.SCERT
{
    public class ScertDecoder : MessageToMessageDecoder<IByteBuffer>
    {
        readonly int maxFrameLength;
        readonly bool failFast;
        readonly Func<RT_MSG_TYPE, CipherContext, ICipher> getCipher;
        bool discardingTooLongFrame;
        long tooLongFrameLength;
        long bytesToDiscard;


        public override bool IsSharable => true;

        /// <summary>
        ///     Create a new instance.
        /// </summary>
        /// <param name="maxFrameLength">
        ///     The maximum length of the frame.  If the length of the frame is
        ///     greater than this value then <see cref="TooLongFrameException" /> will be thrown.
        /// </param>
        /// <param name="failFast">
        ///     If <c>true</c>, a <see cref="TooLongFrameException" /> is thrown as soon as the decoder notices the length
        ///     of the frame will exceeed <see cref="maxFrameLength" /> regardless of whether the entire frame has been
        ///     read. If <c>false</c>, a <see cref="TooLongFrameException" /> is thrown after the entire frame that exceeds
        ///     <see cref="maxFrameLength" /> has been read.
        ///     Defaults to <c>true</c> in other overloads.
        /// </param>
        public ScertDecoder(int maxFrameLength, bool failFast, Func<RT_MSG_TYPE, CipherContext, ICipher> getCipher)
        {
            if (maxFrameLength <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxFrameLength), "maxFrameLength must be a positive integer: " + maxFrameLength);
            }

            this.maxFrameLength = maxFrameLength;
            this.failFast = failFast;
            this.getCipher = getCipher;
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
            if (this.discardingTooLongFrame)
            {
                long bytesToDiscard = this.bytesToDiscard;
                int localBytesToDiscard = (int)Math.Min(bytesToDiscard, input.ReadableBytes);
                input.SkipBytes(localBytesToDiscard);
                bytesToDiscard -= localBytesToDiscard;
                this.bytesToDiscard = bytesToDiscard;

                this.FailIfNecessary(false);
            }

            // 
            int readerStartIndex = input.ReaderIndex;

            // 
            byte id = input.ReadByte();
            byte[] hash = null;
            long frameLength = input.ReadShortLE();

            if (id >= 0x80)
            {
                hash = new byte[4];
                input.ReadBytes(hash);
                id &= 0x7F;
            }

            if (frameLength < 0)
            {
                throw new CorruptedFrameException("negative pre-adjustment length field: " + frameLength);
            }

            if (frameLength > this.maxFrameLength)
            {
                long discard = frameLength - input.ReadableBytes;
                this.tooLongFrameLength = frameLength;

                if (discard < 0)
                {
                    // buffer contains more bytes then the frameLength so we can discard all now
                    input.SkipBytes((int)frameLength);
                }
                else
                {
                    // Enter the discard mode and discard everything received so far.
                    this.discardingTooLongFrame = true;
                    this.bytesToDiscard = discard;
                    input.SkipBytes(input.ReadableBytes);
                }
                this.FailIfNecessary(true);
                return null;
            }

            // never overflows because it's less than maxFrameLength
            int frameLengthInt = (int)frameLength;
            if (input.ReadableBytes < frameLengthInt)
            {
                return null;
            }

            // extract frame
            IByteBuffer messageContents = input.ReadBytes(frameLengthInt);

            // 
            return BaseScertMessage.Instantiate((RT_MSG_TYPE)id, hash, messageContents, getCipher);
        }

        void FailIfNecessary(bool firstDetectionOfTooLongFrame)
        {
            if (this.bytesToDiscard == 0)
            {
                // Reset to the initial state and tell the handlers that
                // the frame was too large.
                long tooLongFrameLength = this.tooLongFrameLength;
                this.tooLongFrameLength = 0;
                this.discardingTooLongFrame = false;
                if (!this.failFast ||
                    this.failFast && firstDetectionOfTooLongFrame)
                {
                    this.Fail(tooLongFrameLength);
                }
            }
            else
            {
                // Keep discarding and notify handlers if necessary.
                if (this.failFast && firstDetectionOfTooLongFrame)
                {
                    this.Fail(this.tooLongFrameLength);
                }
            }
        }

        void Fail(long frameLength)
        {
            if (frameLength > 0)
            {
                throw new TooLongFrameException("Adjusted frame length exceeds " + this.maxFrameLength +
                    ": " + frameLength + " - discarded");
            }
            else
            {
                throw new TooLongFrameException(
                    "Adjusted frame length exceeds " + this.maxFrameLength +
                        " - discarding");
            }
        }

    }
}