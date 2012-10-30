//
// A re-implementation of Nullable<T> for the JavaScript runtime.
// Underlying representation is that of T, except JavaScript "null" denotes the "no-value" nullable.
//

using Microsoft.LiveLabs.JavaScript.IL2JS;

namespace System
{
    [UsedType(true)]
    public struct Nullable<T> where T : struct
    {
        // SPECIAL CASE: Implemented by IL2JS to be value itself
        public Nullable(T value) { throw new InvalidOperationException(); }

        // SPECIAL CASE: Implemented by IL2JS to check JavaScript value for null
        public bool HasValue { get { throw new InvalidOperationException(); } }

        // SPECIAL CASE: Implemented by IL2JS to be non-null check
        public T Value { get { throw new InvalidOperationException(); } }

        public T GetValueOrDefault()
        {
            if (!HasValue)
                return default(T);
            return Value;
        }

        public T GetValueOrDefault(T defaultValue)
        {
            if (!HasValue)
                return defaultValue;
            return Value;
        }

        public override bool Equals(object other)
        {
            if (!HasValue)
                return other == null;
            if (other == null)
                return false;
            return Value.Equals(other);
        }

        public override int GetHashCode()
        {
            if (!HasValue)
                return 0;
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            if (!HasValue)
                return "";
            return Value.ToString();
        }
    }
}