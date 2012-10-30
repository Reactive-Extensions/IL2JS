//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp
{
    using System;
    using System.Threading;

    /// <summary>
    /// Implements a lock-free but best-effort object pool.
    /// </summary>
    sealed class Pool<T> where T : class
    {
        readonly T[] pool;
        readonly Func<T> ctor;
        int returnIndex;
        int takeIndex;

        public Pool(int maxSize, Func<T> ctor)
        {
            this.pool = new T[maxSize];
            this.ctor = ctor;
            this.returnIndex = -1;
            this.takeIndex = -1;
        }

        // Consumer
        public T Take()
        {
            // We could return null even if there are items in the pool.
            // Hopefully we will get them in following Take calls.
            T item = Interlocked.Exchange(ref this.pool[this.Index(ref this.takeIndex)], null);
            if (item == null)
            {
                Interlocked.Decrement(ref this.takeIndex);
            }

            return item ?? this.ctor();
        }

        // Producer
        public void Return(T item)
        {
            Interlocked.CompareExchange(ref this.pool[this.Index(ref this.returnIndex)], item, null);
        }

        int Index(ref int index)
        {
            return (int)((uint)Interlocked.Increment(ref index) % (uint)this.pool.Length);
        }
    }
}
