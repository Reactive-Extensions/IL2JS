using System;
using System.Collections.Generic;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Collections.Generic;

namespace
#if WM7
Microsoft.Windows.Phone
#endif
Reactive.Collections.Generic
{
    class AnonymousEnumerable<T> : IEnumerable<T>
    {
        Func<IEnumerator<T>> getEnumerator;

        public AnonymousEnumerable(Func<IEnumerator<T>> getEnumerator)
        {
            this.getEnumerator = getEnumerator;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return getEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
