//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Serialization
{
    using System;

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field,
        AllowMultiple = false, Inherited = true)]
    public sealed class AmqpMemberAttribute : Attribute
    {
        int? order;
        bool? mandatory;

        public string Name 
        {
            get; 
            set; 
        }

        public int Order 
        {
            get { return this.order.HasValue ? this.order.Value : 0; }

            set { this.order = value; }
        }

        public bool Mandatory
        {
            get { return this.mandatory.HasValue ? this.mandatory.Value : false; }

            set { this.mandatory = value; }
        }

        internal int? InternalOrder
        {
            get { return this.order; }
        }

        internal bool? InternalMandatory
        {
            get { return this.mandatory; }
        }
    }
}
