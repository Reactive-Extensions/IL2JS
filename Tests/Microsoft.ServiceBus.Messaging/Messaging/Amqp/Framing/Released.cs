//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;

    sealed class Released : Outcome
    {
        public static readonly string Name = "amqp:released:list";
        public static readonly ulong Code = 0x0000000000000026;
        const int Fields = 0;

        public Released() : base(Name, Code) { }

        protected override int FieldCount
        {
            get { return Fields; }
        }

        public override string ToString()
        {
            return "released()";
        }

        protected override void OnEncode(ByteBuffer buffer)
        {
        }

        protected override void OnDecode(ByteBuffer buffer, int count)
        {
        }

        protected override int OnValueSize()
        {
            return 0;
        }
    }
}
