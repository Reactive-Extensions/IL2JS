//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    using System.Collections;
    using System.Collections.Generic;
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;

    sealed class AmqpSequence : DescribedList
    {
        public static readonly string Name = "amqp:amqp-sequence:list";
        public static readonly ulong Code = 0x0000000000000076;

        IList innerList;

        public AmqpSequence()
            : this(new List<object>()) 
        {
        }

        public AmqpSequence(IList innerList)
            : base(Name, Code)
        {
            this.innerList = innerList;
        }

        protected override int FieldCount
        {
            get { return this.innerList.Count; }
        }

        public IList List
        {
            get { return this.innerList; }
        }

        public override string ToString()
        {
            return "sequence()";
        }

        protected override int OnValueSize()
        {
            return ListEncoding.GetValueSize(this.innerList);
        }

        protected override void OnEncode(ByteBuffer buffer)
        {
            foreach (object item in this.innerList)
            {
                AmqpEncoding.EncodeObject(item, buffer);
            }
        }

        protected override void OnDecode(ByteBuffer buffer, int count)
        {
            for (int i =0; i < count; i++)
            {
                this.innerList.Add(AmqpEncoding.DecodeObject(buffer));
            }
        }
    }
}
