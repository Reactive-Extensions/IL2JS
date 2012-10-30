using System;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Collections.Generic;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Linq;
using System.Text;

namespace
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive
{
    class SynchronizedObserver<T> : IObserver<T>
    {
        object gate;
        IObserver<T> underlyingObserver;

        public SynchronizedObserver(IObserver<T> underlyingObserver, object gate)
        {
            this.gate = gate;
            this.underlyingObserver = underlyingObserver;
        }

        public void OnNext(T value)
        {
            lock (gate)
            {
                underlyingObserver.OnNext(value);
            }
        }

        public void OnError(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException("exception");

            lock (gate)
            {
                underlyingObserver.OnError(exception);
            }
        }

        public void OnCompleted()
        {
            lock (gate)
            {
                underlyingObserver.OnCompleted();
            }
        }
    }
}
