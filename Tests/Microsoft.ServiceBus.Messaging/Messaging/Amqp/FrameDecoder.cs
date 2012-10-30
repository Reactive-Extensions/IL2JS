//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp
{
    using System;
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
    using Microsoft.ServiceBus.Messaging.Amqp.Framing;

    sealed class FrameDecoder
    {
        int maxFrameSize;
        ByteBuffer currentFrameBuffer;

        public FrameDecoder(int maxFrameSize)
        {
            this.maxFrameSize = maxFrameSize;
        }

        public ProtocolHeader ExtractProtocolHeader(ByteBuffer buffer)
        {
            if (buffer.Length < ProtocolHeader.Size)
            {
                return null;
            }

            return ProtocolHeader.Decode(buffer);
        }

        public void ExtractFrameBuffers(ByteBuffer buffer, SerializedWorker<ByteBuffer> bufferHandler)
        {
            if (this.currentFrameBuffer != null)
            {
                int sizeToWrite = Math.Min(this.currentFrameBuffer.Size, buffer.Length);

                this.currentFrameBuffer.WriteBytes(buffer.Buffer, buffer.Offset, sizeToWrite);
                buffer.Complete(sizeToWrite);

                if (this.currentFrameBuffer.Size == 0)
                {
                    bufferHandler.DoWork(this.currentFrameBuffer);
                    this.currentFrameBuffer = null;
                }
            }

            while (buffer.Length >= AmqpCodec.MinimumFrameDecodeSize)
            {
                int frameSize = AmqpCodec.GetFrameSize(buffer);
                if (frameSize < AmqpCodec.MinimumFrameDecodeSize || frameSize > this.maxFrameSize)
                {
                    throw new AmqpException(AmqpError.FramingError, SRClient.InvalidFrameSize (frameSize, this.maxFrameSize));
                }

                int sizeToWrite = Math.Min(frameSize, buffer.Length);
                this.currentFrameBuffer = ByteBuffer.Wrap(new byte[frameSize], 0, 0, frameSize);
                this.currentFrameBuffer.WriteBytes(buffer.Buffer, buffer.Offset, sizeToWrite);
                buffer.Complete(sizeToWrite);

                if (frameSize == sizeToWrite)
                {
                    bufferHandler.DoWork(this.currentFrameBuffer);
                    this.currentFrameBuffer = null;
                }
                else
                {
                    break;
                }
            }
        }
    }
}