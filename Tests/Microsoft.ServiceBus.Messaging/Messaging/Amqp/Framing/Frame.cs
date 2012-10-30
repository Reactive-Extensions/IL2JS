//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    using System;
    using System.Text;
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;

    sealed class Frame
    {
        public const int HeaderSize = 8;
        public static Frame Empty = new Frame(FrameType.Amqp, 0, null);

        const byte DefaultDataOffset = 2;
        int size;
        byte dataOffset;
        ArraySegment<byte> frameBuffer;

        Frame()
        {
        }

        // Creates an outgoing frame (packet)
        public Frame(FrameType type, ushort channel, Performative command)
        {
            this.Type = type;
            this.Channel = channel;
            this.Command = command;
            this.dataOffset = Frame.DefaultDataOffset;
            this.size = HeaderSize;
            if (this.Command != null)
            {
                this.size += AmqpCodec.GetSerializableEncodeSize(this.Command) + this.Command.PayloadSize;
            }
        }

        public FrameType Type
        {
            get;
            private set;
        }

        public ushort Channel
        {
            get;
            set;
        }

        public Performative Command
        {
            get;
            set;
        }

        // Serialized frame (header + command)
        public ArraySegment<byte> Buffer
        {
            get
            {
                if (this.frameBuffer.Count == 0)
                {
                    ByteBuffer buffer = ByteBuffer.Wrap(this.size);
                    this.Encode(buffer);
                }

                return this.frameBuffer;
            }
        }

        // Buffer for the entire frame, valid for received frame only
        public ArraySegment<byte> RawBuffer
        {
            get;
            private set;
        }

        public static Frame Decode(ByteBuffer buffer, bool fullBody = true)
        {
            Frame frame = new Frame();
            frame.RawBuffer = buffer.Array;

            // Header
            frame.size = (int)AmqpBitConverter.ReadUInt(buffer);
            frame.dataOffset = AmqpBitConverter.ReadUByte(buffer);
            frame.Type = (FrameType)AmqpBitConverter.ReadUByte(buffer);
            frame.Channel = AmqpBitConverter.ReadUShort(buffer);
            // skip extended header
            buffer.Complete(frame.dataOffset * 4 - Frame.HeaderSize);

            // Command
            if (buffer.Length > 0)
            {
                frame.Command = (Performative)AmqpCodec.CreateAmqpDescribed(buffer);
                if (fullBody)
                {
                    frame.Command.DecodeValue(buffer);
                }
            }

            return frame;
        }

        public bool IsValid(int maxFrameSize)
        {
            return this.size >= Frame.HeaderSize &&
                this.size <= maxFrameSize &&
                this.dataOffset >= Frame.DefaultDataOffset &&
                (this.dataOffset * 2) <= this.size;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("FRM({0:X4}|{1}|{2}|{3:X2}", this.size, this.dataOffset, (byte)this.Type, this.Channel);
            if (this.Command != null)
            {
                sb.AppendFormat("  {0}", this.Command);

                int payloadSize = this.Command.PayloadSize;
                if (payloadSize > 0)
                {
                    sb.AppendFormat(",{0}", payloadSize);
                }
            }

            sb.Append(')');
            return sb.ToString();
        }

        public void Encode(ByteBuffer buffer)
        {
            AmqpBitConverter.WriteUInt(buffer, (uint)this.size);
            AmqpBitConverter.WriteUByte(buffer, this.dataOffset);
            AmqpBitConverter.WriteUByte(buffer, (byte)this.Type);
            AmqpBitConverter.WriteUShort(buffer, this.Channel);

            if (this.Command != null)
            {
                AmqpCodec.EncodeSerializable(this.Command, buffer);
            }

            this.frameBuffer = buffer.Array;
        }
    }
}
