using Microsoft.LiveLabs.JavaScript.IL2JS;

namespace System
{
    [UsedType(true)]
    public interface IEquatable<T>
    {
        [Used(true)]
        bool Equals(T other);
    }
}
