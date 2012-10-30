//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    using System;
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;

    abstract class Performative : DescribedList
    {
        protected Performative(AmqpSymbol name, ulong code)
            : base(name, code)
        {
        }

        // Incoming
        public ByteBuffer ValueBuffer { get; set; }

        public ArraySegment<byte> Payload { get; set; }

        // Outgoing
        public ArraySegment<byte>[] PayloadList { get; set; }

        public int PayloadSize
        {
            get
            {
                int size = 0;
                if (this.PayloadList != null)
                {
                    for (int i = 0; i < this.PayloadList.Length; ++i)
                    {
                        size += this.PayloadList[i].Count;
                    }
                }
                else
                {
                    size = this.Payload.Count;
                }

                return size;
            }
        }
    }
}
