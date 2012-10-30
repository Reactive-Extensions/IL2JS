using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReactiveTests.Mocks
{
    class NullEnumeratorEnumerable<T> : IEnumerable<T>
    {
        public static readonly NullEnumeratorEnumerable<T> Instance = new NullEnumeratorEnumerable<T>();

        private NullEnumeratorEnumerable()
        {
        }

        public IEnumerator<T> GetEnumerator()
        {
            return null;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
