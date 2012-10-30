//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;

    abstract class DescribedAnnotations : DescribedMap
    {
        Annotations annotations;

        protected DescribedAnnotations(AmqpSymbol name, ulong code) 
            : base(name, code) 
        {
        }

        public Annotations Map
        {
            get 
            {
                if (this.annotations == null)
                {
                    this.annotations = new Annotations();
                    this.annotations.SetMap(this.InnerMap);
                }

                return this.annotations; 
            }
        }
    }
}
