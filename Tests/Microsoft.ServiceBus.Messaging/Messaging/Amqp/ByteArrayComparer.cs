//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp
{
    using System;
    using System.Collections.Generic;

    sealed class ByteArrayComparer : IEqualityComparer<ArraySegment<byte>>
    {
        static ByteArrayComparer instance = new ByteArrayComparer();

        ByteArrayComparer()
        {
        }

        public static ByteArrayComparer Instance
        {
            get
            {
                return instance;
            }
        }

        public bool Equals(ArraySegment<byte> x, ArraySegment<byte> y)
        {
            return ByteArrayComparer.AreEqual(x, y);
        }

        public int GetHashCode(ArraySegment<byte> obj)
        {
            int num = obj.Count;
            unchecked
            {
                for (int i = 0; i < obj.Count; ++i)
                {
                    num = (num << 4 - num) ^ obj.Array[i + obj.Offset];
                }
            }

            return num;
        }

        static bool AreEqual(ArraySegment<byte> x, ArraySegment<byte> y)
        {
            if ((x.Array == null) || (y.Array == null))
            {
                return x.Array == null && null == y.Array;
            }

            if (x.Count != y.Count)
            {
                return false;
            }

            for (int i = 0; i < x.Count; i++)
            {
                if (x.Array[i + x.Offset] != y.Array[i + y.Offset])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
