using System;
using System.Collections.Generic;

namespace
#if WM7
Microsoft.Windows.Phone.
#endif
 Interactive.Linq
{
    /// <summary>
    /// Provides a set of static methods for creating enumerators.
    /// </summary>
    public static class Enumerator
    {
        /// <summary>
        /// Hides the identity of an enumerator.
        /// </summary>
        public static IEnumerator<TSource> AsEnumerator<TSource>(this IEnumerator<TSource> enumerator)
        {
            if (enumerator == null)
                throw new ArgumentNullException("enumerator");

            return new HiddenEnumerator<TSource>(enumerator);
        }

        sealed class HiddenEnumerator<T> : IEnumerator<T>
        {
            IEnumerator<T> enumerator;

            public HiddenEnumerator(IEnumerator<T> enumerator)
            {
                this.enumerator = enumerator;
            }

            public T Current
            {
                get { return enumerator.Current; }
            }

            public void Dispose()
            {
                enumerator.Dispose();
            }

            object System.Collections.IEnumerator.Current
            {
                get { return enumerator.Current; }
            }

            public bool MoveNext()
            {
                return enumerator.MoveNext();
            }

            public void Reset()
            {
                enumerator.Reset();
            }
        }
    }
}
