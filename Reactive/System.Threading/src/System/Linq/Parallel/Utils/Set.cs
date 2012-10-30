// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// Util.cs
//
// <OWNER>igoro</OWNER>
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;

namespace System.Linq.Parallel
{
    /// <summary>
    /// A set for various operations. Shamelessly stolen from LINQ's source code.
    /// @TODO: can the Linq one be used directly now that we are in System.Core
    /// </summary>
    /// <typeparam name="TElement">The kind of elements contained within.</typeparam>
    internal class Set<TElement>
    {

        int[] buckets;
        Slot[] slots;
        int count;
        int freeList;
        IEqualityComparer<TElement> comparer;

        internal Set() : this(null)
        {
        }

        internal Set(IEqualityComparer<TElement> comparer)
        {
            if (comparer == null) comparer = EqualityComparer<TElement>.Default;
            this.comparer = comparer;
            buckets = new int[7];
            slots = new Slot[7];
            freeList = -1;
        }

        // If value is not in set, add it and return true; otherwise return false
        internal bool Add(TElement value)
        {
            return !Find(value, true);
        }

        // Check whether value is in set
        internal bool Contains(TElement value)
        {
            return Find(value, false);
        }

        // If value is in set, remove it and return true; otherwise return false
        internal bool Remove(TElement value)
        {
            int hashCode = comparer.GetHashCode(value) & 0x7FFFFFFF;
            int bucket = hashCode % buckets.Length;
            int last = -1;
            for (int i = buckets[bucket] - 1; i >= 0; last = i, i = slots[i].next)
            {
                if (slots[i].hashCode == hashCode && comparer.Equals(slots[i].value, value))
                {
                    if (last < 0)
                    {
                        buckets[bucket] = slots[i].next + 1;
                    }
                    else
                    {
                        slots[last].next = slots[i].next;
                    }
                    slots[i].hashCode = -1;
                    slots[i].value = default(TElement);
                    slots[i].next = freeList;
                    freeList = i;
                    return true;
                }
            }
            return false;
        }

        internal bool Find(TElement value, bool add)
        {
            int hashCode = comparer.GetHashCode(value) & 0x7FFFFFFF;
            for (int i = buckets[hashCode % buckets.Length] - 1; i >= 0; i = slots[i].next)
            {
                if (slots[i].hashCode == hashCode && comparer.Equals(slots[i].value, value)) return true;
            }
            if (add)
            {
                int index;
                if (freeList >= 0)
                {
                    index = freeList;
                    freeList = slots[index].next;
                }
                else
                {
                    if (count == slots.Length) Resize();
                    index = count;
                    count++;
                }
                int bucket = hashCode % buckets.Length;
                slots[index].hashCode = hashCode;
                slots[index].value = value;
                slots[index].next = buckets[bucket] - 1;
                buckets[bucket] = index + 1;
            }
            return false;
        }

        void Resize()
        {
            int newSize = checked(count * 2 + 1);
            int[] newBuckets = new int[newSize];
            Slot[] newSlots = new Slot[newSize];
            Array.Copy(slots, 0, newSlots, 0, count);
            for (int i = 0; i < count; i++)
            {
                int bucket = newSlots[i].hashCode % newSize;
                newSlots[i].next = newBuckets[bucket] - 1;
                newBuckets[bucket] = i + 1;
            }
            buckets = newBuckets;
            slots = newSlots;
        }

        internal struct Slot
        {
            internal int hashCode;
            internal TElement value;
            internal int next;
        }
    }
}