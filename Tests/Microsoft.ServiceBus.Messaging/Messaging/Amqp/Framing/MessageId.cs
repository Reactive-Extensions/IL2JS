//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    using System;
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;

    abstract class MessageId
    {
        public static implicit operator MessageId(ulong value)
        {
            return new MessageIdUlong(value);
        }

        public static implicit operator MessageId(Guid value)
        {
            return new MessageIdUuid(value);
        }

        public static implicit operator MessageId(ArraySegment<byte> value)
        {
            return new MessageIdBinary(value);
        }

        public static implicit operator MessageId(string value)
        {
            return new MessageIdString(value);
        }

        public static int GetEncodeSize(MessageId messageId)
        {
            return messageId == null ? FixedWidth.NullEncoded : messageId.EncodeSize;
        }

        public static void Encode(ByteBuffer buffer, MessageId messageId)
        {
            if (messageId == null)
            {
                AmqpEncoding.EncodeNull(buffer);
            }
            else
            {
                messageId.OnEncode(buffer);
            }
        }

        public static MessageId Decode(ByteBuffer buffer)
        {
            object value = AmqpEncoding.DecodeObject(buffer);
            if (value == null)
            {
                return null;
            }

            if (value is ulong)
            {
                return (ulong)value;
            }

            if (value is Guid)
            {
                return (Guid)value;
            }

            if (value is ArraySegment<byte>)
            {
                return (ArraySegment<byte>)value;
            }

            if (value is string)
            {
                return (string)value;
            }

            throw new NotSupportedException(value.GetType().ToString());
        }

        public abstract int EncodeSize { get; }

        public abstract void OnEncode(ByteBuffer buffer);

        sealed class MessageIdUlong : MessageId
        {
            ulong id;

            public MessageIdUlong(ulong id)
            {
                this.id = id;
            }

            public override int EncodeSize
            {
                get { return AmqpCodec.GetULongEncodeSize(this.id); }
            }

            public override void OnEncode(ByteBuffer buffer)
            {
                AmqpCodec.EncodeULong(this.id, buffer);
            }

            public override bool Equals(object obj)
            {
                MessageIdUlong other = obj as MessageIdUlong;
                if (other == null)
                {
                    return false;
                }

                return this.id == other.id;
            }

            public override string ToString()
            {
                return this.id.ToString();
            }

            public override int GetHashCode()
            {
                return this.id.GetHashCode();
            }
        }

        sealed class MessageIdUuid : MessageId
        {
            Guid id;

            public MessageIdUuid(Guid id)
            {
                this.id = id;
            }

            public override int EncodeSize
            {
                get { return AmqpCodec.GetUuidEncodeSize(this.id); }
            }

            public override void OnEncode(ByteBuffer buffer)
            {
                AmqpCodec.EncodeUuid(this.id, buffer);
            }

            public override bool Equals(object obj)
            {
                MessageIdUuid other = obj as MessageIdUuid;
                if (other == null)
                {
                    return false;
                }

                return this.id == other.id;
            }

            public override string ToString()
            {
                return this.id.ToString();
            }

            public override int GetHashCode()
            {
                return this.id.GetHashCode();
            }
        }

        sealed class MessageIdBinary : MessageId
        {
            ArraySegment<byte> id;

            public MessageIdBinary(ArraySegment<byte> id)
            {
                this.id = id;
            }

            public override int EncodeSize
            {
                get { return AmqpCodec.GetBinaryEncodeSize(this.id); }
            }

            public override void OnEncode(ByteBuffer buffer)
            {
                AmqpCodec.EncodeBinary(this.id, buffer);
            }

            public override bool Equals(object obj)
            {
                MessageIdBinary other = obj as MessageIdBinary;
                if (other == null)
                {
                    return false;
                }

                return ByteArrayComparer.Instance.Equals(this.id, other.id);
            }

            public override int GetHashCode()
            {
                return ByteArrayComparer.Instance.GetHashCode(this.id);
            }

            public override string ToString()
            {
                return Utils.GetString(this.id);
            }
        }

        sealed class MessageIdString : MessageId
        {
            string id;

            public MessageIdString(string id)
            {
                this.id = id;
            }

            public override int EncodeSize
            {
                get { return AmqpCodec.GetStringEncodeSize(this.id); }
            }

            public override void OnEncode(ByteBuffer buffer)
            {
                AmqpCodec.EncodeString(this.id, buffer);
            }

            public override bool Equals(object obj)
            {
                MessageIdString other = obj as MessageIdString;
                if (other == null)
                {
                    return false;
                }

                return this.id.Equals(other.id);
            }

            public override string ToString()
            {
                return this.id;
            }

            public override int GetHashCode()
            {
                return this.id.GetHashCode();
            }
        }
    }
}
