//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    using System;
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
    using Microsoft.ServiceBus.Messaging.Amqp.Transport;

    sealed class ProtocolHeader
    {
        public static readonly ProtocolHeader Amqp100 = new ProtocolHeader(ProtocolId.Amqp, new AmqpVersion(1, 0, 0));
        public static readonly ProtocolHeader AmqpTls100 = new ProtocolHeader(ProtocolId.AmqpTls, new AmqpVersion(1, 0, 0));
        public static readonly ProtocolHeader AmqpSasl100 = new ProtocolHeader(ProtocolId.AmqpSasl, new AmqpVersion(1, 0, 0));

        public const int Size = 8;
        const uint AmqpPrefix = 0x414D5150;

        ProtocolId protocolId;
        AmqpVersion version;

        ProtocolHeader()
        {
        }

        public ProtocolHeader(ProtocolId id, AmqpVersion version)
        {
            this.protocolId = id;
            this.version = version;
            this.InitializePacket();
        }

        public ProtocolId ProtocolId
        {
            get { return this.protocolId; }
        }

        public AmqpVersion Version
        {
            get { return this.version; }
        }

        public ArraySegment<byte> Buffer
        {
            get;
            set;
        }

        public static ProtocolHeader Decode(ByteBuffer buffer)
        {
            if (buffer.Length < ProtocolHeader.Size)
            {
                throw AmqpEncoding.GetEncodingException("BufferSize");
            }

            uint prefix = AmqpBitConverter.ReadUInt(buffer);
            if (prefix != ProtocolHeader.AmqpPrefix)
            {
                throw AmqpEncoding.GetEncodingException("ProtocolName");
            }

            ProtocolHeader header = new ProtocolHeader();
            header.protocolId = (ProtocolId)AmqpBitConverter.ReadUByte(buffer);
            header.version = new AmqpVersion(AmqpBitConverter.ReadUByte(buffer), AmqpBitConverter.ReadUByte(buffer), AmqpBitConverter.ReadUByte(buffer));
            header.Buffer = new ArraySegment<byte>(buffer.Buffer, buffer.Offset - ProtocolHeader.Size, ProtocolHeader.Size);
            return header;
        }

        public override string ToString()
        {
            return string.Format("AMQP {0} {1}", (byte)this.protocolId, this.version);
        }

        public override bool Equals(object obj)
        {
            ProtocolHeader otherHeader = obj as ProtocolHeader;
            if (otherHeader == null)
            {
                return false;
            }

            return otherHeader.protocolId == this.protocolId &&
                otherHeader.version.Equals(this.version);
        }

        public override int GetHashCode()
        {
            int result = ((int)this.protocolId << 24) +
                (this.version.Major << 16) +
                (this.version.Minor << 8) +
                this.version.Revision;
            return result.GetHashCode();
        }

        void InitializePacket()
        {
            if (this.Buffer.Array == null)
            {
                byte[] buffer = new byte[ProtocolHeader.Size] { 0x41, 0x4D, 0x51, 0x50, (byte)this.protocolId, 
                    this.version.Major, this.version.Minor, this.version.Revision };
                this.Buffer = new ArraySegment<byte>(buffer);
            }
        }
    }
}
