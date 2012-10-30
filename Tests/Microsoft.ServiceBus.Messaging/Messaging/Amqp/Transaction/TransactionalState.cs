//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Transaction
{
    using System;
    using System.Text;
    using Microsoft.ServiceBus.Messaging.Amqp.Framing;

    sealed class TransactionalState : DeliveryState
    {
        public static readonly string Name = "amqp:transactional-state:list";
        public static readonly ulong Code = 0x0000000000000034;
        const int Fields = 2;

        public TransactionalState() : base(Name, Code) { }

        public ArraySegment<byte> TxnId { get; set; }

        public Outcome Outcome { get; set; }

        protected override int FieldCount
        {
            get { return Fields; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("txn-state(");
            int count = 0;
            this.AddFieldToString(this.TxnId.Array != null, sb, "txn-id", this.TxnId, ref count);
            this.AddFieldToString(this.Outcome != null, sb, "outcome", this.Outcome, ref count);
            sb.Append(')');
            return sb.ToString();
        }

        protected override void OnEncode(ByteBuffer buffer)
        {
            AmqpCodec.EncodeBinary(this.TxnId, buffer);
            AmqpCodec.EncodeSerializable(this.Outcome, buffer);
        }

        protected override void OnDecode(ByteBuffer buffer, int count)
        {
            if (count-- > 0)
            {
                this.TxnId = AmqpCodec.DecodeBinary(buffer);
            }

            if (count-- > 0)
            {
                this.Outcome = (Outcome)AmqpCodec.DecodeAmqpDescribed(buffer);
            }
        }

        protected override int OnValueSize()
        {
            int valueSize = 0;

            valueSize += AmqpCodec.GetBinaryEncodeSize(this.TxnId);
            valueSize += AmqpCodec.GetSerializableEncodeSize(this.Outcome);

            return valueSize;
        }
    }
}
