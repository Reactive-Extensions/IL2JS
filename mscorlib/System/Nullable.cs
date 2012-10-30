using System.Collections.Generic;

namespace System
{
    public static class Nullable
    {
        public static int Compare<T>(T? n1, T? n2) where T : struct
        {
            if (n1.HasValue)
            {
                if (n2.HasValue)
                    return Comparer<T>.Default.Compare(n1.Value, n2.Value);
                return 1;
            }
            if (n2.HasValue)
                return -1;
            return 0;
        }

        public static bool Equals<T>(T? n1, T? n2) where T : struct
        {
            if (n1.HasValue)
                return (n2.HasValue && EqualityComparer<T>.Default.Equals(n1.Value, n2.Value));
            if (n2.HasValue)
                return false;
            return true;
        }
    }
}
