//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
    class DescribedType
    {
        public DescribedType(object descriptor, object value)
        {
            this.Descriptor = descriptor;
            this.Value = value;
        }

        public object Descriptor
        {
            get;
            set;
        }

        public object Value
        {
            get;
            set;
        }

        public override string ToString()
        {
            return this.Descriptor.ToString();
        }
    }
}
