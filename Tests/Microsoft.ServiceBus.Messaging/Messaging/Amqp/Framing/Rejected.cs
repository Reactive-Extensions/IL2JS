//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    using System.Text;

    sealed class Rejected : Outcome
    {
        public static readonly string Name = "amqp:rejected:list";
        public static readonly ulong Code = 0x0000000000000025;
        const int Fields = 1;

        public Rejected() : base(Name, Code) { }

        public Error Error { get; set; }

        protected override int FieldCount
        {
            get { return Fields; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("rejected(");
            int count = 0;
            this.AddFieldToString(this.Error != null, sb, "error", this.Error, ref count);
            sb.Append(')');
            return sb.ToString();
        }

        protected override void OnEncode(ByteBuffer buffer)
        {
            AmqpCodec.EncodeSerializable(this.Error, buffer);
        }

        protected override void OnDecode(ByteBuffer buffer, int count)
        {
            if (count-- > 0)
            {
                this.Error = AmqpCodec.DecodeKnownType<Error>(buffer);
            }
        }

        protected override int OnValueSize()
        {
            int valueSize = 0;

            valueSize += AmqpCodec.GetSerializableEncodeSize(this.Error);

            return valueSize;
        }
    }
}
