namespace System.Collections
{
    internal sealed class Comparer : IComparer
    {
        public static readonly Comparer Default = new Comparer();

        private Comparer()
        {
        }

        public int Compare(object a, object b)
        {
            if (a == b)
            {
                return 0;
            }
            if (a == null)
            {
                return -1;
            }
            if (b == null)
            {
                return 1;
            }
            string str = a as string;
            string str2 = b as string;
            if ((str != null) && (str2 != null))
            {
                return string.Compare(str, str2);
            }

            IComparable comparable = a as IComparable;
            if (comparable == null)
                throw new ArgumentException("implement IComparable");
            return comparable.CompareTo(b);
        }
    }
}