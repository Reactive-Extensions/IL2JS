using Microsoft.LiveLabs.JavaScript.IL2JS;

namespace System.Collections.Generic
{
    [UsedType(true)]
    public interface IEnumerable<T> : IEnumerable
    {
        new IEnumerator<T> GetEnumerator();
    }
}
