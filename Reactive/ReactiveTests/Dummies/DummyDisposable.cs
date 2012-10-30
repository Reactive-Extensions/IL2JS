using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReactiveTests.Dummies
{
    class DummyDisposable : IDisposable
    {
        public static readonly DummyDisposable Instance = new DummyDisposable();

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
