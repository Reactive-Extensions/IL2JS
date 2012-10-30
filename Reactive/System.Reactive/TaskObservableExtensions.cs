using System;
#if DESKTOPCLR20 || DESKTOPCLR40
using System.Threading.Tasks;
#endif
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Collections.Generic;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Disposables;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Linq;
using System.Text;

namespace
#if WM7
Microsoft.Windows.Phone
#endif
Reactive.Threading.Tasks
{
    /// <summary>
    /// Provides a set of static methods for converting Tasks to IObservables.
    /// </summary>
    public static class TaskObservableExtensions
    {
#if !SILVERLIGHT && !NETCF37
        /// <summary>
        /// Returns an observable sequence that contains the values of the underlying task.
        /// </summary>
        public static IObservable<Unit> ToObservable(this Task task)
        {
            if (task == null)
                throw new ArgumentNullException("task");

            return new AnonymousObservable<Unit>(observer =>
            {
                var cancelable = new CancellationDisposable();
                task.ContinueWith(t =>
                {
                    switch (t.Status)
                    {
                        case TaskStatus.RanToCompletion:
                            observer.OnNext(new Unit());
                            observer.OnCompleted();
                            break;
                        case TaskStatus.Faulted:
                            observer.OnError(t.Exception);
                            break;
                        case TaskStatus.Canceled:
                            observer.OnCompleted();
                            break;
                    }
                }, cancelable.Token);

                return cancelable;
            });
        }

        /// <summary>
        /// Returns an observable sequence that contains the values of the underlying task.
        /// </summary>
        public static IObservable<TResult> ToObservable<TResult>(this Task<TResult> task)
        {
            if (task == null)
                throw new ArgumentNullException("task");

            return new AnonymousObservable<TResult>(observer =>
            {
                var cancelable = new CancellationDisposable();
                task.ContinueWith(t =>
                {
                    switch (t.Status)
                    {
                        case TaskStatus.RanToCompletion:
                            observer.OnNext(t.Result);
                            observer.OnCompleted();
                            break;
                        case TaskStatus.Faulted:
                            observer.OnError(t.Exception);
                            break;
                        case TaskStatus.Canceled:
                            observer.OnCompleted();
                            break;
                    }
                }, cancelable.Token);

                return cancelable;
            });
        }
#endif
    }
}
