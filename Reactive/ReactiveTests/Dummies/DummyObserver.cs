using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReactiveTests.Dummies
{
    class DummyObserver<T> : IObserver<T>
    {
        public static readonly DummyObserver<T> Instance = new DummyObserver<T>();

        DummyObserver()
        {
        }

        public void OnNext(T value)
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception exception)
        {
            throw new NotImplementedException();
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }
    }
}
