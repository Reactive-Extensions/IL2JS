//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Transaction
{
    using System;
    using System.Text;
    using Microsoft.ServiceBus.Messaging.Amqp.Framing;

    sealed class Discharge : Performative
    {
        public static readonly string Name = "amqp:discharge:list";
        public static readonly ulong Code = 0x0000000000000032;
        const int Fields = 2;

        public Discharge() : base(Name, Code) { }

        public ArraySegment<byte> TxnId { get; set; }

        public bool? Fail { get; set; }

        protected override int FieldCount
        {
            get { return Fields; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("discharge(");
            int count = 0;
            this.AddFieldToString(this.TxnId.Array != null, sb, "txn-id", this.TxnId, ref count);
            this.AddFieldToString(this.Fail != null, sb, "fail", this.Fail, ref count);
            sb.Append(')');
            return sb.ToString();
        }

        protected override void OnEncode(ByteBuffer buffer)
        {
            AmqpCodec.EncodeBinary(this.TxnId, buffer);
            AmqpCodec.EncodeBoolean(this.Fail, buffer);
        }

        protected override void OnDecode(ByteBuffer buffer, int count)
        {
            if (count-- > 0)
            {
                this.TxnId = AmqpCodec.DecodeBinary(buffer);
            }

            if (count-- > 0)
            {
                this.Fail = AmqpCodec.DecodeBoolean(buffer);
            }
        }

        protected override int OnValueSize()
        {
            int valueSize = 0;

            valueSize = AmqpCodec.GetBinaryEncodeSize(this.TxnId);
            valueSize += AmqpCodec.GetBooleanEncodeSize(this.Fail);

            return valueSize;
        }
    }
}
