using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReactiveTests.Dummies
{
    class DummyEnumerable<T> : IEnumerable<T>
    {
        public static readonly DummyEnumerable<T> Instance = new DummyEnumerable<T>();

        private DummyEnumerable()
        {
        }

        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
