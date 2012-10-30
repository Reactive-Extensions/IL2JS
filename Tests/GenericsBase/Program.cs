using System.Collections.Generic;

namespace Microsoft.LiveLabs.JavaScript.Tests
{
    public class FirstOrderClassPolymorphicMethod
    {
        public T Test<T>(IEnumerable<T> t)
        {
            foreach (var i in t)
                return i;
            return default(T);
        }
    }

    public class HigherKindedClassMonomorphicMethod<T>
    {
        public T Test(IEnumerable<T> t)
        {
            foreach (var i in t)
                return i;
            return default(T);
        }
    }

    public class HigherKindedClassPolymorphicMethod<T>
    {
        public T Test<V>(IEnumerable<KeyValuePair<T, V>> t)
        {
            foreach (var i in t)
                return i.Key;
            return default(T);
        }
    }
}