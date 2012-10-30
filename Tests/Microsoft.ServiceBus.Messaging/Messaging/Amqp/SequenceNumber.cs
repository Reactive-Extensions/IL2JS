//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp
{
    using System;
    using System.Threading;

    struct SequenceNumber : IComparable<SequenceNumber>, IEquatable<SequenceNumber>
    {
        public static SequenceNumber MinValue = uint.MinValue;
        public static SequenceNumber MaxValue = uint.MaxValue;
        const uint CompareRange = (uint)1 << (32 - 1);
        const int AdditionRange = 1 << (32 - 1) - 1;

        int sequenceNumber;

        public SequenceNumber(uint value)
        {
            this.sequenceNumber = (int)value;
        }

        public uint Value
        {
            get { return (uint)this.sequenceNumber; }
        }

        public int CompareTo(SequenceNumber value)
        {
            uint thisSn = (uint)this.sequenceNumber;
            uint valueSn = (uint)value.sequenceNumber;

            if (thisSn == valueSn)
            {
                return 0;
            }
            else if (thisSn < valueSn)
            {
                if (valueSn - thisSn < CompareRange)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            }
            else
            {
                if (thisSn - valueSn > CompareRange)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            }
        }

        public bool Equals(SequenceNumber obj)
        {
            return this.sequenceNumber == obj.sequenceNumber;
        }

        public static implicit operator SequenceNumber(uint value)
        {
            return new SequenceNumber(value);
        }

        public static uint operator +(SequenceNumber value1, int delta)
        {
            if (delta < 0 || delta >= AdditionRange)
            {
                throw new ArgumentOutOfRangeException("delta");
            }

            // RFC 1982: s' = (s + n) modulo (2 ^ SERIAL_BITS)
            return unchecked((uint)value1.sequenceNumber + (uint)delta);
        }

        public static bool operator ==(SequenceNumber value1, SequenceNumber value2)
        {
            return value1.sequenceNumber == value2.sequenceNumber;
        }

        public static bool operator !=(SequenceNumber value1, SequenceNumber value2)
        {
            return value1.sequenceNumber != value2.sequenceNumber;
        }

        public static bool operator >(SequenceNumber value1, SequenceNumber value2)
        {
            return value1.CompareTo(value2) > 0;
        }

        public static bool operator >=(SequenceNumber value1, SequenceNumber value2)
        {
            return !(value1.CompareTo(value2) < 0);
        }

        public static bool operator <(SequenceNumber value1, SequenceNumber value2)
        {
            return value1.CompareTo(value2) < 0;
        }

        public static bool operator <=(SequenceNumber value1, SequenceNumber value2)
        {
            return !(value1.CompareTo(value2) > 0);
        }

        public uint Increment()
        {
            return (uint)unchecked(++this.sequenceNumber);
        }

        public uint InterlockedIncrement()
        {
            return (uint)Interlocked.Increment(ref this.sequenceNumber);
        }

        public override int GetHashCode()
        {
            return this.sequenceNumber.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is SequenceNumber && this.Equals((SequenceNumber)obj);
        }

        public override string ToString()
        {
            return this.Value.ToString();
        }
    }
}
