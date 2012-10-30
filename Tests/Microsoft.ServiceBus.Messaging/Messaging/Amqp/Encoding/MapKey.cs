//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
    using System;
    using System.Collections.Generic;

    struct MapKey : IEquatable<MapKey>
    {
        object key;

        public MapKey(object key)
        {
            this.key = key;
        }

        public object Key
        {
            get { return this.key; }
        }

        public bool Equals(MapKey other)
        {
            if (this.key == null && other.key == null)
            {
                return true;
            }

            if (this.key == null || other.key == null)
            {
                return false;
            }

            return this.key.Equals(other.key);
        }

        public override int GetHashCode()
        {
            if (this.key == null)
            {
                return 0;
            }

            return this.key.GetHashCode();
        }

        public override string ToString()
        {
            return key == null ? "<null>" : key.ToString();
        }
    }
}
