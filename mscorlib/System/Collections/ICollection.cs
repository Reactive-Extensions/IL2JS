namespace System.Collections
{
    public interface ICollection : IEnumerable
    {
        int Count { get; }
        object SyncRoot { get; }
        bool IsSynchronized { get; }
        void CopyTo(Array array, int index);
    }
}
