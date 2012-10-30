using Microsoft.LiveLabs.JavaScript.IL2JS;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace System.Collections.Generic
{
    public class EqualityComparer<T> : IEqualityComparer, IEqualityComparer<T>
    {
        private static EqualityComparer<T> defaultComparer;

        private EqualityComparer()
        {
        }

        // Must suppress export on x and y since type's Equals function works on them directly
        [Import("function(type, x, y) { return type.Equals(x, y); }")]
        extern private static bool Equals(Type type, [NoInterop(true)]T x, [NoInterop(true)]T y);

        // Must suppress export on obj since type's Hash function works on them directly
        [Import("function(type, obj) { return type.Hash(obj); }")]
        extern private static int GetHashCode(Type type, [NoInterop(true)]T obj);

        public bool Equals(T x, T y) { return Equals(typeof(T), x, y); }

        public int GetHashCode(T obj) { return GetHashCode(typeof(T), obj); }

        bool IEqualityComparer.Equals(object x, object y)
        {
            if (x == y)
                return true;
            if ((x != null) && (y != null))
            {
                if (x is T && y is T)
                    return Equals((T)x, (T)y);
                throw new ArgumentException();
            }
            return false;
        }

        int IEqualityComparer.GetHashCode(object obj)
        {
            if (obj != null)
            {
                if (obj is T)
                    return GetHashCode((T)obj);
                throw new ArgumentException();
            }
            return 0;
        }

        public static EqualityComparer<T> Default
        {
            get
            {
                if (defaultComparer == null)
                    defaultComparer = new EqualityComparer<T>();
                return defaultComparer;
            }
        }
    }
}
