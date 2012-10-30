//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;

    abstract class DescribedMap : AmqpDescribed
    {
        AmqpMap innerMap;

        public DescribedMap(AmqpSymbol name, ulong code)
            : base(name, code)
        {
            this.innerMap = new AmqpMap();
        }

        protected AmqpMap InnerMap
        {
            get { return this.innerMap; }
        }

        public override int GetValueEncodeSize()
        {
            return MapEncoding.GetEncodeSize(this.innerMap);
        }

        public override void EncodeValue(ByteBuffer buffer)
        {
            MapEncoding.Encode(this.innerMap, buffer);
        }

        public override void DecodeValue(ByteBuffer buffer)
        {
            this.innerMap = MapEncoding.Decode(buffer, 0);
        }

        public void DecodeValue(ByteBuffer buffer, int size, int count)
        {
            MapEncoding.ReadMapValue(buffer, this.innerMap, size, count);
        }
    }
}
