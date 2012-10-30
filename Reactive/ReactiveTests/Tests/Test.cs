using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Concurrency;

using
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Linq;

using
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Disposables;

using
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Collections.Generic;

using
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive;

namespace ReactiveTests
{
    public class Test
    {
        public const ushort Created = 100;
        public const ushort Subscribed = 200;
        public const ushort Disposed = 1000;

        public Recorded<Notification<T>> OnNext<T>(ushort ticks, T value)
        {
            return new Recorded<Notification<T>>(ticks, new Notification<T>.OnNext(value));
        }

        public Recorded<Notification<T>> OnCompleted<T>(ushort ticks)
        {
            return new Recorded<Notification<T>>(ticks, new Notification<T>.OnCompleted());
        }

        public Recorded<Notification<T>> OnError<T>(ushort ticks, Exception exception)
        {
            return new Recorded<Notification<T>>(ticks, new Notification<T>.OnError(exception));
        }

        public Subscription Subscribe(ushort start, ushort end)
        {
            return new Subscription(start, end);
        }

        public Subscription Subscribe(ushort start)
        {
            return new Subscription(start);
        }

        public void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
                Assert.Fail(string.Format("Expected {0}.", typeof(TException).Name));
            }
            catch (TException)
            {
            }
            catch (Exception ex)
            {
                Assert.Fail(string.Format("Expected {0} threw {1}.", typeof(TException).Name, ex.GetType().Name));
            }
        }
    }
}
