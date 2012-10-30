using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReactiveTests.Mocks
{
    class SubscribeThrowsObservable<T> : IObservable<T>
    {
        public IDisposable Subscribe(IObserver<T> observer)
        {
            throw new InvalidOperationException();
        }
    }
}
