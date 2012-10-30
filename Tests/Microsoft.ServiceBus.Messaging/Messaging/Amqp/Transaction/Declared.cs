//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Transaction
{
    using System;
    using System.Text;
    using Microsoft.ServiceBus.Messaging.Amqp.Framing;

    sealed class Declared : Outcome
    {
        public static readonly string Name = "amqp:declared:list";
        public static readonly ulong Code = 0x0000000000000033;
        const int Fields = 1;

        public Declared() : base(Name, Code) { }

        public ArraySegment<byte> TxnId { get; set; }

        protected override int FieldCount
        {
            get { return Fields; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("declared(");
            int count = 0;
            this.AddFieldToString(this.TxnId.Array != null, sb, "txn-id", this.TxnId, ref count);
            sb.Append(')');
            return sb.ToString();
        }

        protected override void OnEncode(ByteBuffer buffer)
        {
            AmqpCodec.EncodeBinary(this.TxnId, buffer);
        }

        protected override void OnDecode(ByteBuffer buffer, int count)
        {
            if (count-- > 0)
            {
                this.TxnId = AmqpCodec.DecodeBinary(buffer);
            }
        }

        protected override int OnValueSize()
        {
            int valueSize = 0;

            valueSize += AmqpCodec.GetBinaryEncodeSize(this.TxnId);

            return valueSize;
        }
    }
}
