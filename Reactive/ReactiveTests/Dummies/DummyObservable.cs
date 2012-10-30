using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReactiveTests.Dummies
{
    class DummyObservable<T> : IObservable<T>
    {
        public static readonly DummyObservable<T> Instance = new DummyObservable<T>();

        DummyObservable()
        {
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            throw new NotImplementedException();
        }
    }
}
