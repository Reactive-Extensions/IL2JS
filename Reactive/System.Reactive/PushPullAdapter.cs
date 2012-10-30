using System;
using System.Collections.Generic;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Collections.Generic;
using
#if WM7

Microsoft.Windows.Phone.
#endif
Reactive.Diagnostics;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Linq;
using System.Text;
using System.Diagnostics;

namespace
#if WM7
Microsoft.Windows.Phone
#endif
Reactive.Collections.Generic
{
    sealed class PushPullAdapter<T> : IObserver<T>, IEnumerator<T>
    {
        Action<Notification<T>> yield;
        Action dispose;
        Func<Notification<T>> moveNext;
        Notification<T> current;
        bool done = false;

        public PushPullAdapter(Action<Notification<T>> yield, Func<Notification<T>> moveNext, Action dispose)
        {
            this.yield = yield;
            this.moveNext = moveNext;
            this.dispose = dispose;
        }

        public void OnNext(T value)
        {
            yield(new Notification<T>.OnNext(value));
        }

        public void OnError(Exception exception)
        {
            yield(new Notification<T>.OnError(exception));
        }

        public void OnCompleted()
        {
            yield(new Notification<T>.OnCompleted());
        }

        public T Current
        {
            get { return current.Value; }
        }

        public void Dispose()
        {
            dispose();
        }

        object System.Collections.IEnumerator.Current
        {
            get { return this.Current; }
        }

        public bool MoveNext()
        {
            if (!done)
            {
                current = moveNext();
                done = current.Kind != NotificationKind.OnNext;
            }

            if (current.Exception != null)
                throw current.Exception.PrepareForRethrow();

            return current.HasValue;
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }
    }
}
