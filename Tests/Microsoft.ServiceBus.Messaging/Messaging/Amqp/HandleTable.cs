//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Designed for session channel and link handle lookup. 
    /// Not for general purpose uses.
    /// </summary>
    sealed class HandleTable<T> where T : class
    {
        const int fastSegmentCount = 32;

        uint maxHandle;
        T[] fastSegment;
        Dictionary<uint, T> slowSegment;

        public HandleTable(uint maxHandle)
        {
            this.maxHandle = maxHandle;
            this.fastSegment = new T[fastSegmentCount];
        }

        public IEnumerable<T> Values
        {
            get 
            {
                List<T> values = new List<T>();
                foreach (T t in this.fastSegment)
                {
                    if (t != null)
                    {
                        values.Add(t);
                    }
                }

                if (this.slowSegment != null)
                {
                    values.AddRange(this.slowSegment.Values);
                }

                return values; 
            }
        }

        public T this[uint handle]
        {
            get
            {
                T obj = null;
                if (!this.TryGetObject(handle, out obj))
                {
                    throw new AmqpException(AmqpError.UnattachedHandle, handle.ToString());
                }

                return obj;
            }
        }

        public bool TryGetObject(uint handle, out T value)
        {
            value = null;
            if (handle < this.fastSegment.Length)
            {
                value = this.fastSegment[(int)handle];
            }
            else if (this.slowSegment != null)
            {
                this.slowSegment.TryGetValue(handle, out value);
            }

            return value != null;
        }

        public uint Add(T value)
        {
            for (int i = 0; i < this.fastSegment.Length; ++i)
            {
                if (this.fastSegment[i] == null)
                {
                    this.fastSegment[i] = value;
                    return (uint)i;
                }
            }

            if (this.slowSegment == null)
            {
                this.slowSegment = new Dictionary<uint, T>();
            }

            uint handle = (uint)this.fastSegment.Length;
            while (handle < this.maxHandle && this.slowSegment.ContainsKey(handle))
            {
                ++handle;
            }

            if (handle == this.maxHandle)
            {
                throw new AmqpException(AmqpError.ResourceLimitExceeded, SRClient.AmqpHandleExceeded(this.maxHandle));
            }

            this.slowSegment.Add(handle, value);

            return handle;
        }

        public void Add(uint handle, T value)
        {
            if (handle < this.fastSegment.Length)
            {
                if (this.fastSegment[(int)handle] == null)
                {
                    this.fastSegment[(int)handle] = value;
                }
                else
                {
                    throw new AmqpException(AmqpError.HandleInUse, handle.ToString());
                }
            }
            else
            {
                if (this.slowSegment == null)
                {
                    this.slowSegment = new Dictionary<uint, T>();
                }

                try
                {
                    this.slowSegment.Add(handle, value);
                }
                catch (ArgumentException)
                {
                    throw new AmqpException(AmqpError.HandleInUse, handle.ToString());
                }
            }
        }

        public void Remove(uint handle)
        {
            if (handle < this.fastSegment.Length)
            {
                this.fastSegment[(int)handle] = null;
            }
            else if (this.slowSegment != null)
            {
                this.slowSegment.Remove(handle);
            }
        }

        public void Clear()
        {
            for (int i = 0; i < this.fastSegment.Length; ++i)
            {
                this.fastSegment[i] = null;
            }

            this.slowSegment = null;
        }
    }
}
