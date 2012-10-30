namespace System.Collections
{
    public interface IList : ICollection
    {
        object this[int index] { get; set; }
        bool IsReadOnly { get; }
        bool IsFixedSize { get; }
        int Add(object value);
        bool Contains(object value);
        void Clear();
        int IndexOf(object value);
        void Insert(int index, object value);
        void Remove(object value);
        void RemoveAt(int index);
    }
}
