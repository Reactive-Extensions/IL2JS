//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    sealed class Accepted : Outcome
    {
        public static readonly string Name = "amqp:accepted:list";
        public static readonly ulong Code = 0x0000000000000024;
        const int Fields = 0;

        public Accepted() : base(Name, Code) { }

        protected override int FieldCount
        {
            get { return Fields; }
        }

        public override string ToString()
        {
            return "accepted()";
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
