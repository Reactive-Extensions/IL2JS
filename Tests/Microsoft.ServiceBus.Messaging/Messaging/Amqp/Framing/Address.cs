//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    using System;
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;

    abstract class Address
    {
        public static implicit operator Address(string value)
        {
            return new AddressString(value);
        }

        public static int GetEncodeSize(Address address)
        {
            return address == null ? FixedWidth.NullEncoded : address.EncodeSize;
        }

        public static void Encode(ByteBuffer buffer, Address address)
        {
            if (address == null)
            {
                AmqpEncoding.EncodeNull(buffer);
            }
            else
            {
                address.OnEncode(buffer);
            }
        }

        public static Address Decode(ByteBuffer buffer)
        {
            object value = AmqpEncoding.DecodeObject(buffer);
            if (value == null)
            {
                return null;
            }

            if (value is string)
            {
                return (string)value;
            }

            throw new NotSupportedException(value.GetType().ToString());
        }

        public abstract int EncodeSize { get; }

        public abstract void OnEncode(ByteBuffer buffer);

        sealed class AddressString : Address
        {
            string address;

            public AddressString(string id)
            {
                this.address = id;
            }

            public override int EncodeSize
            {
                get { return AmqpCodec.GetStringEncodeSize(this.address); }
            }

            public override void OnEncode(ByteBuffer buffer)
            {
                AmqpCodec.EncodeString(this.address, buffer);
            }

            public override string ToString()
            {
                return this.address;
            }
        }
    }
}
