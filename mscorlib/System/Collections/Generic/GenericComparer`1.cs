namespace System.Collections.Generic
{
    internal class GenericComparer<T> : Comparer<T> where T : IComparable<T>
    {
        public override int Compare(T x, T y)
        {
            if (x != null)
            {
                if (y != null)
                {
                    return x.CompareTo(y);
                }
                return 1;
            }
            if (y != null)
            {
                return -1;
            }
            return 0;
        }

        public override bool Equals(object obj)
        {
            GenericComparer<T> comparer = obj as GenericComparer<T>;
            return (comparer != null);
        }

        public override int GetHashCode()
        {
            return base.GetType().Name.GetHashCode();
        }
    }
}
