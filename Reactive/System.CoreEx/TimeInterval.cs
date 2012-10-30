using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Linq
{
    /// <summary>
    /// Represents a time interval value.
    /// </summary>
    public struct TimeInterval<T>
    {
        TimeSpan interval;
        T value;

        /// <summary>
        /// Gets the interval.
        /// </summary>
        public TimeSpan Interval { get { return interval; } }

        /// <summary>
        /// Gets the value.
        /// </summary>
        public T Value { get { return value; } }

        /// <summary>
        /// Constructs a timestamped value.
        /// </summary>
        public TimeInterval(T value, TimeSpan interval)
        {
            this.interval = interval;
            this.value = value;
        }


        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (!(obj is TimeInterval<T>))
                return false;

            var other = (TimeInterval<T>)obj;
            return other.Interval.Equals(Interval) && Equals(Value, other.Value);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode()
        {
            var valueHashCode = Value == null ? 1963 : Value.GetHashCode();

            return Interval.GetHashCode() ^ valueHashCode;
        }

        /// <summary>
        /// Returns a string representation of this instance.
        /// </summary>
        public override string ToString()
        {
#if IL2JS
            return String.Format("{0}@{1}", Value, Interval);
#else
            return String.Format(CultureInfo.CurrentCulture, "{0}@{1}", Value, Interval);
#endif
        }
        
        /// <summary>
        /// Indicates whether first and second are equal.       
        /// </summary>
        public static bool operator ==(TimeInterval<T> first, TimeInterval<T> second)
        {
            return first.Equals(second);
        }

        /// <summary>
        /// Indicates whether first and second are not equal.       
        /// </summary>
        public static bool operator !=(TimeInterval<T> first, TimeInterval<T> second)
        {
            return !first.Equals(second);
        }

        

    }
}
