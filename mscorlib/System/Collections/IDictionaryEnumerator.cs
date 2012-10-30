namespace System.Collections
{
    public interface IDictionaryEnumerator : IEnumerator
    {
        object Key { get; }
        object Value { get; }
        DictionaryEntry Entry { get; }
    }
}
