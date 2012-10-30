//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Serialization
{
    using System;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct,
        AllowMultiple = false, Inherited = true)]
    public sealed class AmqpContractAttribute : Attribute
    {
        const string ListEncoding = "List";
        const string MapEncoding = "Map";

        string encoding;
        long? code;

        public string Name { get; set; }

        public long Code
        {
            get { return this.code.HasValue ? this.code.Value : 0; }

            set { this.code = value; }
        }

        public string Encoding
        {
            get
            {
                return this.encoding ?? ListEncoding;
            }

            set
            {
                this.ValidateEncoding(value);
                this.encoding = value;
            }
        }

        internal ulong? InternalCode
        {
            get { return this.code.HasValue ? (ulong?)this.code.Value : null; }
        }

        void ValidateEncoding(string encoding)
        {
            if (string.CompareOrdinal(encoding, ListEncoding) != 0 &&
                string.CompareOrdinal(encoding, MapEncoding) != 0)
            {
                throw new ArgumentException("Encoding");
            }
        }
    }
}