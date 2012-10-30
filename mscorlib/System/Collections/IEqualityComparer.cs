namespace System.Collections
{
    public interface IEqualityComparer
    {
        bool Equals(object x, object y);
        int GetHashCode(object obj);
    }
}
