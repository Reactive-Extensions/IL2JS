namespace System.Collections
{
    public interface IDictionary : ICollection
    {
        object this[object key] { get; set; }
        ICollection Keys { get; }
        ICollection Values { get; }
        bool IsReadOnly { get; }
        bool IsFixedSize { get; }
        bool Contains(object key);
        void Add(object key, object value);
        void Clear();
        new IDictionaryEnumerator GetEnumerator();
        void Remove(object key);
    }
}
