namespace System.Collections.Generic
{
    internal class ObjectComparer<T> : Comparer<T>
    {
        public override int Compare(T x, T y)
        {
            return Comparer.Default.Compare(x, y);
        }

        public override bool Equals(object obj)
        {
            ObjectComparer<T> comparer = obj as ObjectComparer<T>;
            return (comparer != null);
        }

        public override int GetHashCode()
        {
            return base.GetType().Name.GetHashCode();
        }
    }
}
