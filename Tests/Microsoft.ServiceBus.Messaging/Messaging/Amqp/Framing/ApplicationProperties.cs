//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;

    sealed class ApplicationProperties : DescribedMap
    {
        public static readonly string Name = "amqp:application-properties:map";
        public static readonly ulong Code = 0x0000000000000074;

        PropertiesMap propMap;

        public ApplicationProperties() 
            : base(Name, Code) 
        {
        }

        public PropertiesMap Map
        {
            get 
            {
                if (this.propMap == null)
                {
                    this.propMap = new PropertiesMap();
                    this.propMap.SetMap(this.InnerMap);
                }

                return this.propMap; 
            }
        }
    }
}
