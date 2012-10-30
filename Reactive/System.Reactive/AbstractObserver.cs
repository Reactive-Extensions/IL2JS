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
using System.Threading;

namespace
#if WM7
Microsoft.Windows.Phone
#endif
Reactive.Collections.Generic
{
    abstract class AbstractObserver<T> : IObserver<T>
    {
        public AbstractObserver()
        {
            isStopped = false;
        }

        private bool isStopped;

        public void Stop()
        {
            isStopped = true;
        }

        public void OnNext(T value)
        {
            if (!isStopped)
                Next(value);
        }

        protected abstract void Next(T value);

        public void OnError(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException("exception");

            if (!isStopped)
            {
                isStopped = true;
                Error(exception);
            }
        }

        protected abstract void Error(Exception exception);

        public void OnCompleted()
        {
            if (!isStopped)
            {
                isStopped = true;
                Completed();
            }
        }

        protected abstract void Completed();
    }
}
