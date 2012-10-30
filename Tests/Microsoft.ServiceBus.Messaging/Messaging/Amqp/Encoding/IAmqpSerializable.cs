//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
    interface IAmqpSerializable
    {
        int EncodeSize
        {
            get;
        }

        void Encode(ByteBuffer buffer);

        void Decode(ByteBuffer buffer);
    }
}
