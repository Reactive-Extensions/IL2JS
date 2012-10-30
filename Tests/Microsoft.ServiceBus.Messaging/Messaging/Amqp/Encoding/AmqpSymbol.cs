//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
    using System;

    struct AmqpSymbol : IEquatable<AmqpSymbol>
    {
        int valueSize;

        public AmqpSymbol(string value) : this()
        {
            this.Value = value;
        }

        public string Value
        {
            get;
            private set;
        }

        public int ValueSize
        {
            get
            {
                if (this.valueSize == 0)
                {
                    this.valueSize = SymbolEncoding.GetValueSize(this);
                }

                return this.valueSize;
            }
        }

        public static implicit operator AmqpSymbol(string value)
        {
            return new AmqpSymbol() { Value = value };
        }

        public bool Equals(AmqpSymbol other)
        {
            if (this.Value == null && other.Value == null)
            {
                return true;
            }

            if (this.Value == null || other.Value == null)
            {
                return false;
            }

            return string.Compare(this.Value, other.Value, StringComparison.OrdinalIgnoreCase) == 0;
        }

        public override int GetHashCode()
        {
            if (this.Value == null)
            {
                return 0;
            }

            return this.Value.GetHashCode();
        }

        public override string ToString()
        {
            return this.Value;
        }
    }
}
