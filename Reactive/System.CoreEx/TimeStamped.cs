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
    /// Represents a timestamped value.
    /// </summary>
    public struct Timestamped<T>
    {
        DateTimeOffset timestamp;
        T value;

        /// <summary>
        /// Gets the timestamp.
        /// </summary>
        public DateTimeOffset Timestamp { get { return timestamp; } }

        /// <summary>
        /// Gets the value.
        /// </summary>
        public T Value { get { return value; } }

        /// <summary>
        /// Constructs a timestamped value.
        /// </summary>
        public Timestamped(T value, DateTimeOffset timestamp)
        {
            this.timestamp = timestamp;
            this.value = value;
        }

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (!(obj is Timestamped<T>))
                return false;

            var other = (Timestamped<T>) obj;
            return other.Timestamp.Equals(Timestamp) && EqualityComparer<T>.Default.Equals(Value, other.Value);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode()
        {
            var valueHashCode = Value == null ? 1979 : Value.GetHashCode();

            return timestamp.GetHashCode() ^ valueHashCode;
        }

        /// <summary>
        /// Returns a string representation of this instance.
        /// </summary>
        public override string ToString()
        {
#if IL2JS
            return String.Format("{0}@{1}", Value, Timestamp);
#else
            return String.Format(CultureInfo.CurrentCulture, "{0}@{1}", Value, Timestamp);
#endif
        }

        /// <summary>
        /// Indicates whether first and second are equal.       
        /// </summary>
        public static bool operator ==(Timestamped<T> first, Timestamped<T> second)
        {
            return first.Equals(second);
        }

        /// <summary>
        /// Indicates whether first and second are not equal.       
        /// </summary>
        public static bool operator !=(Timestamped<T> first, Timestamped<T> second)
        {
            return !first.Equals(second);
        }

    }
}
