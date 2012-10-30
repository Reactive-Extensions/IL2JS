using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace ReactiveTests
{
    [DebuggerDisplay("{Value}@{Time}")]
    public class Recorded<T> : IEquatable<Recorded<T>>
    {
        public ushort Time { get; private set; }
        public T Value { get; private set; }

        public Recorded(ushort time, T value)
        {
            Time = time;
            Value = value;
        }

        public bool Equals(Recorded<T> other)
        {
            if (this == other)
                return true;
            if (other == null)
                return false;
            return Time == other.Time && EqualityComparer<T>.Default.Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Recorded<T>);
        }

        public override int GetHashCode()
        {
            return Time.GetHashCode() + EqualityComparer<T>.Default.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value.ToString() + "@" + Time.ToString();
        }
    }
}
