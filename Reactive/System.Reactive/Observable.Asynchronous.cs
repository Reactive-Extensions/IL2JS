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
 Reactive;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Concurrency;

namespace
#if WM7
Microsoft.Windows.Phone
#endif
Reactive.Linq
{
	public static partial class Observable
	{
        /// <summary>
        /// Converts a Begin/End invoke function pair into a asynchronous function.
        /// </summary>
        public static Func<IObservable<TResult>> FromAsyncPattern<TResult>(Func<AsyncCallback, object, IAsyncResult> begin, Func<IAsyncResult, TResult> end)
        {
            if (begin == null)
                throw new ArgumentNullException("begin");
            if (end == null)
                throw new ArgumentNullException("end");

            return () =>
            {
                var subject = new AsyncSubject<TResult>(Scheduler.ThreadPool);
                begin(iar =>
                {
                    TResult result;
                    try
                    {
                        result = end(iar);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(result);
                    subject.OnCompleted();
                }, null);
                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts a Begin/End invoke function pair into a asynchronous function.
        /// </summary>
        public static Func<T1, IObservable<TResult>> FromAsyncPattern<T1, TResult>(Func<T1, AsyncCallback, object, IAsyncResult> begin, Func<IAsyncResult, TResult> end)
        {
            if (begin == null)
                throw new ArgumentNullException("begin");
            if (end == null)
                throw new ArgumentNullException("end");

            return x =>
            {
                var subject = new AsyncSubject<TResult>(Scheduler.ThreadPool);
                begin(x, iar =>
                {
                    TResult result;
                    try
                    {
                        result = end(iar);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(result);
                    subject.OnCompleted();
                }, null);
                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts a Begin/End invoke function pair into a asynchronous function.
        /// </summary>
        public static Func<T1, T2, IObservable<TResult>> FromAsyncPattern<T1, T2, TResult>(Func<T1, T2, AsyncCallback, object, IAsyncResult> begin, Func<IAsyncResult, TResult> end)
        {
            if (begin == null)
                throw new ArgumentNullException("begin");
            if (end == null)
                throw new ArgumentNullException("end");

            return (x, y) =>
            {
                var subject = new AsyncSubject<TResult>(Scheduler.ThreadPool);
                begin(x, y, iar =>
                {
                    TResult result;
                    try
                    {
                        result = end(iar);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(result);
                    subject.OnCompleted();
                }, null);
                return subject.AsObservable();
            };
        }

#if DESKTOPCLR20 || DESKTOPCLR40

        /// <summary>
        /// Converts a Begin/End invoke function pair into a asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, IObservable<TResult>> FromAsyncPattern<T1, T2, T3, TResult>(Func<T1, T2, T3, AsyncCallback, object, IAsyncResult> begin, Func<IAsyncResult, TResult> end)
        {
            if (begin == null)
                throw new ArgumentNullException("begin");
            if (end == null)
                throw new ArgumentNullException("end");

            return (x, y, z) =>
            {
                var subject = new AsyncSubject<TResult>(Scheduler.ThreadPool);
                begin(x, y, z, iar =>
                {
                    TResult result;
                    try
                    {
                        result = end(iar);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(result);
                    subject.OnCompleted();
                }, null);
                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts a Begin/End invoke function pair into a asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, IObservable<TResult>> FromAsyncPattern<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, AsyncCallback, object, IAsyncResult> begin, Func<IAsyncResult, TResult> end)
        {
            if (begin == null)
                throw new ArgumentNullException("begin");
            if (end == null)
                throw new ArgumentNullException("end");

            return (x, y, z, a) =>
            {
                var subject = new AsyncSubject<TResult>(Scheduler.ThreadPool);
                begin(x, y, z, a, iar =>
                {
                    TResult result;
                    try
                    {
                        result = end(iar);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(result);
                    subject.OnCompleted();
                }, null);
                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts a Begin/End invoke function pair into a asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, IObservable<TResult>> FromAsyncPattern<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, AsyncCallback, object, IAsyncResult> begin, Func<IAsyncResult, TResult> end)
        {
            if (begin == null)
                throw new ArgumentNullException("begin");
            if (end == null)
                throw new ArgumentNullException("end");

            return (x, y, z, a, b) =>
            {
                var subject = new AsyncSubject<TResult>(Scheduler.ThreadPool);
                begin(x, y, z, a, b, iar =>
                {
                    TResult result;
                    try
                    {
                        result = end(iar);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(result);
                    subject.OnCompleted();
                }, null);
                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts a Begin/End invoke function pair into a asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, IObservable<TResult>> FromAsyncPattern<T1, T2, T3, T4, T5, T6, TResult>(Func<T1, T2, T3, T4, T5, T6, AsyncCallback, object, IAsyncResult> begin, Func<IAsyncResult, TResult> end)
        {
            if (begin == null)
                throw new ArgumentNullException("begin");
            if (end == null)
                throw new ArgumentNullException("end");

            return (x, y, z, a, b, c) =>
            {
                var subject = new AsyncSubject<TResult>(Scheduler.ThreadPool);
                begin(x, y, z, a, b, c, iar =>
                {
                    TResult result;
                    try
                    {
                        result = end(iar);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(result);
                    subject.OnCompleted();
                }, null);
                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts a Begin/End invoke function pair into a asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, IObservable<TResult>> FromAsyncPattern<T1, T2, T3, T4, T5, T6, T7, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, AsyncCallback, object, IAsyncResult> begin, Func<IAsyncResult, TResult> end)
        {
            if (begin == null)
                throw new ArgumentNullException("begin");
            if (end == null)
                throw new ArgumentNullException("end");

            return (x, y, z, a, b, c, d) =>
            {
                var subject = new AsyncSubject<TResult>(Scheduler.ThreadPool);
                begin(x, y, z, a, b, c, d, iar =>
                {
                    TResult result;
                    try
                    {
                        result = end(iar);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(result);
                    subject.OnCompleted();
                }, null);
                return subject.AsObservable();
            };
        }


        /// <summary>
        /// Converts a Begin/End invoke function pair into a asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, IObservable<TResult>> FromAsyncPattern<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, AsyncCallback, object, IAsyncResult> begin, Func<IAsyncResult, TResult> end)
        {
            if (begin == null)
                throw new ArgumentNullException("begin");
            if (end == null)
                throw new ArgumentNullException("end");

            return (x, y, z, a, b, c, d, e) =>
            {
                var subject = new AsyncSubject<TResult>(Scheduler.ThreadPool);
                begin(x, y, z, a, b, c, d, e, iar =>
                {
                    TResult result;
                    try
                    {
                        result = end(iar);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(result);
                    subject.OnCompleted();
                }, null);
                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts a Begin/End invoke function pair into a asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, IObservable<TResult>> FromAsyncPattern<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, AsyncCallback, object, IAsyncResult> begin, Func<IAsyncResult, TResult> end)
        {
            if (begin == null)
                throw new ArgumentNullException("begin");
            if (end == null)
                throw new ArgumentNullException("end");

            return (x, y, z, a, b, c, d, e, f) =>
            {
                var subject = new AsyncSubject<TResult>(Scheduler.ThreadPool);
                begin(x, y, z, a, b, c, d, e, f, iar =>
                {
                    TResult result;
                    try
                    {
                        result = end(iar);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(result);
                    subject.OnCompleted();
                }, null);
                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts a Begin/End invoke function pair into a asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, IObservable<TResult>> FromAsyncPattern<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, AsyncCallback, object, IAsyncResult> begin, Func<IAsyncResult, TResult> end)
        {
            if (begin == null)
                throw new ArgumentNullException("begin");
            if (end == null)
                throw new ArgumentNullException("end");

            return (x, y, z, a, b, c, d, e, f, g) =>
            {
                var subject = new AsyncSubject<TResult>(Scheduler.ThreadPool);
                begin(x, y, z, a, b, c, d, e, f, g, iar =>
                {
                    TResult result;
                    try
                    {
                        result = end(iar);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(result);
                    subject.OnCompleted();
                }, null);
                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts a Begin/End invoke function pair into a asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, IObservable<TResult>> FromAsyncPattern<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, AsyncCallback, object, IAsyncResult> begin, Func<IAsyncResult, TResult> end)
        {
            if (begin == null)
                throw new ArgumentNullException("begin");
            if (end == null)
                throw new ArgumentNullException("end");

            return (x, y, z, a, b, c, d, e, f, g, h) =>
            {
                var subject = new AsyncSubject<TResult>(Scheduler.ThreadPool);
                begin(x, y, z, a, b, c, d, e, f, g, h, iar =>
                {
                    TResult result;
                    try
                    {
                        result = end(iar);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(result);
                    subject.OnCompleted();
                }, null);
                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts a Begin/End invoke function pair into a asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, IObservable<TResult>> FromAsyncPattern<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, AsyncCallback, object, IAsyncResult> begin, Func<IAsyncResult, TResult> end)
        {
            if (begin == null)
                throw new ArgumentNullException("begin");
            if (end == null)
                throw new ArgumentNullException("end");

            return (x, y, z, a, b, c, d, e, f, g, h, i) =>
            {
                var subject = new AsyncSubject<TResult>(Scheduler.ThreadPool);
                begin(x, y, z, a, b, c, d, e, f, g, h, i, iar =>
                {
                    TResult result;
                    try
                    {
                        result = end(iar);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(result);
                    subject.OnCompleted();
                }, null);
                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts a Begin/End invoke function pair into a asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, IObservable<TResult>> FromAsyncPattern<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, AsyncCallback, object, IAsyncResult> begin, Func<IAsyncResult, TResult> end)
        {
            if (begin == null)
                throw new ArgumentNullException("begin");
            if (end == null)
                throw new ArgumentNullException("end");

            return (x, y, z, a, b, c, d, e, f, g, h, i, j) =>
            {
                var subject = new AsyncSubject<TResult>(Scheduler.ThreadPool);
                begin(x, y, z, a, b, c, d, e, f, g, h, i, j, iar =>
                {
                    TResult result;
                    try
                    {
                        result = end(iar);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(result);
                    subject.OnCompleted();
                }, null);
                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts a Begin/End invoke function pair into a asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, IObservable<TResult>> FromAsyncPattern<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, AsyncCallback, object, IAsyncResult> begin, Func<IAsyncResult, TResult> end)
        {
            if (begin == null)
                throw new ArgumentNullException("begin");
            if (end == null)
                throw new ArgumentNullException("end");

            return (x, y, z, a, b, c, d, e, f, g, h, i, j, k) =>
            {
                var subject = new AsyncSubject<TResult>(Scheduler.ThreadPool);
                begin(x, y, z, a, b, c, d, e, f, g, h, i, j, k, iar =>
                {
                    TResult result;
                    try
                    {
                        result = end(iar);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(result);
                    subject.OnCompleted();
                }, null);
                return subject.AsObservable();
            };
        }

#endif
        /// <summary>
        /// Converts the function into an asynchronous function.
        /// </summary>
        public static Func<IObservable<TResult>> ToAsync<TResult>(this Func<TResult> function)
        {
            if (function == null)
                throw new ArgumentNullException("function");

            return ToAsync(function, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Converts the function into an asynchronous function.
        /// </summary>
        public static Func<IObservable<TResult>> ToAsync<TResult>(this Func<TResult> function, IScheduler scheduler)
        {
            if (function == null)
                throw new ArgumentNullException("function");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return () =>
            {
                var subject = new AsyncSubject<TResult>(scheduler);
                scheduler.Schedule(() =>
                {
                    var result = default(TResult);
                    try
                    {
                        result = function();
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(result);
                    subject.OnCompleted();
                });
                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts the function into an asynchronous function.
        /// </summary>
        public static Func<T, IObservable<TResult>> ToAsync<T, TResult>(this Func<T, TResult> function)
        {
            if (function == null)
                throw new ArgumentNullException("function");

            return ToAsync(function, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Converts the function into an asynchronous function.
        /// </summary>
        public static Func<T, IObservable<TResult>> ToAsync<T, TResult>(this Func<T, TResult> function, IScheduler scheduler)
        {
            if (function == null)
                throw new ArgumentNullException("function");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return (first) =>
            {
                var subject = new AsyncSubject<TResult>(scheduler);
                scheduler.Schedule(() =>
                {
                    var result = default(TResult);
                    try
                    {
                        result = function(first);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(result);
                    subject.OnCompleted();
                });
                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts the function into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, IObservable<TResult>> ToAsync<T1, T2, TResult>(this Func<T1, T2, TResult> function)
        {
            if (function == null)
                throw new ArgumentNullException("function");

            return ToAsync(function, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Converts the function into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, IObservable<TResult>> ToAsync<T1, T2, TResult>(this Func<T1, T2, TResult> function, IScheduler scheduler)
        {
            if (function == null)
                throw new ArgumentNullException("function");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return (first, second) =>
            {
                var subject = new AsyncSubject<TResult>(scheduler);
                scheduler.Schedule(() =>
                {
                    var result = default(TResult);
                    try
                    {
                        result = function(first, second);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(result);
                    subject.OnCompleted();
                });
                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts the function into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, IObservable<TResult>> ToAsync<T1, T2, T3, TResult>(this Func<T1, T2, T3, TResult> function)
        {
            if (function == null)
                throw new ArgumentNullException("function");

            return ToAsync(function, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Converts the function into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, IObservable<TResult>> ToAsync<T1, T2, T3, TResult>(this Func<T1, T2, T3, TResult> function, IScheduler scheduler)
        {
            if (function == null)
                throw new ArgumentNullException("function");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return (first, second, third) =>
            {
                var subject = new AsyncSubject<TResult>(scheduler);
                scheduler.Schedule(() =>
                {
                    var result = default(TResult);
                    try
                    {
                        result = function(first, second, third);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(result);
                    subject.OnCompleted();
                });
                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts the function into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, IObservable<TResult>> ToAsync<T1, T2, T3, T4, TResult>(this Func<T1, T2, T3, T4, TResult> function)
        {
            if (function == null)
                throw new ArgumentNullException("function");

            return ToAsync(function, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Converts the function into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, IObservable<TResult>> ToAsync<T1, T2, T3, T4, TResult>(this Func<T1, T2, T3, T4, TResult> function, IScheduler scheduler)
        {
            if (function == null)
                throw new ArgumentNullException("function");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return (first, second, third, fourth) =>
            {
                var subject = new AsyncSubject<TResult>(scheduler);
                scheduler.Schedule(() =>
                {
                    var result = default(TResult);
                    try
                    {
                        result = function(first, second, third, fourth);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(result);
                    subject.OnCompleted();
                });
                return subject.AsObservable();
            };
        }
#if DESKTOPCLR20 || DESKTOPCLR40
        /// <summary>
        /// Converts the function into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, IObservable<TResult>> ToAsync<T1, T2, T3, T4, T5, TResult>(this Func<T1, T2, T3, T4, T5, TResult> function)
        {
            if (function == null)
                throw new ArgumentNullException("function");

            return ToAsync(function, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Converts the function into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, IObservable<TResult>> ToAsync<T1, T2, T3, T4, T5, TResult>(this Func<T1, T2, T3, T4, T5, TResult> function, IScheduler scheduler)
        {
            if (function == null)
                throw new ArgumentNullException("function");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return (first, second, third, fourth, fifth) =>
            {
                var subject = new AsyncSubject<TResult>(scheduler);
                scheduler.Schedule(() =>
                {
                    var result = default(TResult);
                    try
                    {
                        result = function(first, second, third, fourth, fifth);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(result);
                    subject.OnCompleted();
                });
                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts the function into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, IObservable<TResult>> ToAsync<T1, T2, T3, T4, T5, T6, TResult>(this Func<T1, T2, T3, T4, T5, T6, TResult> function)
        {
            if (function == null)
                throw new ArgumentNullException("function");

            return ToAsync(function, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Converts the function into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, IObservable<TResult>> ToAsync<T1, T2, T3, T4, T5, T6, TResult>(this Func<T1, T2, T3, T4, T5, T6, TResult> function, IScheduler scheduler)
        {
            if (function == null)
                throw new ArgumentNullException("function");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return (first, second, third, fourth, fifth, sixth) =>
            {
                var subject = new AsyncSubject<TResult>(scheduler);
                scheduler.Schedule(() =>
                {
                    var result = default(TResult);
                    try
                    {
                        result = function(first, second, third, fourth, fifth, sixth);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(result);
                    subject.OnCompleted();
                });
                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts the function into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, IObservable<TResult>> ToAsync<T1, T2, T3, T4, T5, T6, T7, TResult>(this Func<T1, T2, T3, T4, T5, T6, T7, TResult> function)
        {
            if (function == null)
                throw new ArgumentNullException("function");

            return ToAsync(function, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Converts the function into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, IObservable<TResult>> ToAsync<T1, T2, T3, T4, T5, T6, T7, TResult>(this Func<T1, T2, T3, T4, T5, T6, T7, TResult> function, IScheduler scheduler)
        {
            if (function == null)
                throw new ArgumentNullException("function");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return (first, second, third, fourth, fifth, sixth, seventh) =>
            {
                var subject = new AsyncSubject<TResult>(scheduler);
                scheduler.Schedule(() =>
                {
                    var result = default(TResult);
                    try
                    {
                        result = function(first, second, third, fourth, fifth, sixth, seventh);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(result);
                    subject.OnCompleted();
                });
                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts the function into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, IObservable<TResult>> ToAsync<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> function)
        {
            if (function == null)
                throw new ArgumentNullException("function");

            return ToAsync(function, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Converts the function into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, IObservable<TResult>> ToAsync<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> function, IScheduler scheduler)
        {
            if (function == null)
                throw new ArgumentNullException("function");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return (first, second, third, fourth, fifth, sixth, seventh, eight) =>
            {
                var subject = new AsyncSubject<TResult>(scheduler);
                scheduler.Schedule(() =>
                {
                    var result = default(TResult);
                    try
                    {
                        result = function(first, second, third, fourth, fifth, sixth, seventh, eight);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(result);
                    subject.OnCompleted();
                });
                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts the function into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, IObservable<TResult>> ToAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> function)
        {
            if (function == null)
                throw new ArgumentNullException("function");

            return ToAsync(function, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Converts the function into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, IObservable<TResult>> ToAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> function, IScheduler scheduler)
        {
            if (function == null)
                throw new ArgumentNullException("function");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return (first, second, third, fourth, fifth, sixth, seventh, eight, ninth) =>
            {
                var subject = new AsyncSubject<TResult>(scheduler);
                scheduler.Schedule(() =>
                {
                    var result = default(TResult);
                    try
                    {
                        result = function(first, second, third, fourth, fifth, sixth, seventh, eight, ninth);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(result);
                    subject.OnCompleted();
                });
                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts the function into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, IObservable<TResult>> ToAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> function)
        {
            if (function == null)
                throw new ArgumentNullException("function");

            return ToAsync(function, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Converts the function into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, IObservable<TResult>> ToAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> function, IScheduler scheduler)
        {
            if (function == null)
                throw new ArgumentNullException("function");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return (first, second, third, fourth, fifth, sixth, seventh, eight, ninth, tenth) =>
            {
                var subject = new AsyncSubject<TResult>(scheduler);
                scheduler.Schedule(() =>
                {
                    var result = default(TResult);
                    try
                    {
                        result = function(first, second, third, fourth, fifth, sixth, seventh, eight, ninth, tenth);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(result);
                    subject.OnCompleted();
                });
                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts the function into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, IObservable<TResult>> ToAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult> function)
        {
            if (function == null)
                throw new ArgumentNullException("function");

            return ToAsync(function, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Converts the function into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, IObservable<TResult>> ToAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult> function, IScheduler scheduler)
        {
            if (function == null)
                throw new ArgumentNullException("function");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return (first, second, third, fourth, fifth, sixth, seventh, eight, ninth, tenth, eleventh) =>
            {
                var subject = new AsyncSubject<TResult>(scheduler);
                scheduler.Schedule(() =>
                {
                    var result = default(TResult);
                    try
                    {
                        result = function(first, second, third, fourth, fifth, sixth, seventh, eight, ninth, tenth, eleventh);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(result);
                    subject.OnCompleted();
                });
                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts the function into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, IObservable<TResult>> ToAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult> function)
        {
            if (function == null)
                throw new ArgumentNullException("function");

            return ToAsync(function, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Converts the function into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, IObservable<TResult>> ToAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult> function, IScheduler scheduler)
        {
            if (function == null)
                throw new ArgumentNullException("function");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return (first, second, third, fourth, fifth, sixth, seventh, eight, ninth, tenth, eleventh, twelfth) =>
            {
                var subject = new AsyncSubject<TResult>(scheduler);
                scheduler.Schedule(() =>
                {
                    var result = default(TResult);
                    try
                    {
                        result = function(first, second, third, fourth, fifth, sixth, seventh, eight, ninth, tenth, eleventh, twelfth);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(result);
                    subject.OnCompleted();
                });
                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts the function into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, IObservable<TResult>> ToAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult> function)
        {
            if (function == null)
                throw new ArgumentNullException("function");

            return ToAsync(function, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Converts the function into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, IObservable<TResult>> ToAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult> function, IScheduler scheduler)
        {
            if (function == null)
                throw new ArgumentNullException("function");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return (first, second, third, fourth, fifth, sixth, seventh, eight, ninth, tenth, eleventh, twelfth, thirteenth) =>
            {
                var subject = new AsyncSubject<TResult>(scheduler);
                scheduler.Schedule(() =>
                {
                    var result = default(TResult);
                    try
                    {
                        result = function(first, second, third, fourth, fifth, sixth, seventh, eight, ninth, tenth, eleventh, twelfth, thirteenth);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(result);
                    subject.OnCompleted();
                });
                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts the function into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, IObservable<TResult>> ToAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult> function)
        {
            if (function == null)
                throw new ArgumentNullException("function");

            return ToAsync(function, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Converts the function into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, IObservable<TResult>> ToAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult> function, IScheduler scheduler)
        {
            if (function == null)
                throw new ArgumentNullException("function");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return (first, second, third, fourth, fifth, sixth, seventh, eight, ninth, tenth, eleventh, twelfth, thirteenth, fourteenth) =>
            {
                var subject = new AsyncSubject<TResult>(scheduler);
                scheduler.Schedule(() =>
                {
                    var result = default(TResult);
                    try
                    {
                        result = function(first, second, third, fourth, fifth, sixth, seventh, eight, ninth, tenth, eleventh, twelfth, thirteenth, fourteenth);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(result);
                    subject.OnCompleted();
                });
                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts the function into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, IObservable<TResult>> ToAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult> function)
        {
            if (function == null)
                throw new ArgumentNullException("function");

            return ToAsync(function, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Converts the function into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, IObservable<TResult>> ToAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult> function, IScheduler scheduler)
        {
            if (function == null)
                throw new ArgumentNullException("function");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return (first, second, third, fourth, fifth, sixth, seventh, eight, ninth, tenth, eleventh, twelfth, thirteenth, fourteenth, fifteenth) =>
            {
                var subject = new AsyncSubject<TResult>(scheduler);
                scheduler.Schedule(() =>
                {
                    var result = default(TResult);
                    try
                    {
                        result = function(first, second, third, fourth, fifth, sixth, seventh, eight, ninth, tenth, eleventh, twelfth, thirteenth, fourteenth, fifteenth);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(result);
                    subject.OnCompleted();
                });
                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts the function into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, IObservable<TResult>> ToAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult> function)
        {
            if (function == null)
                throw new ArgumentNullException("function");

            return ToAsync(function, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Converts the function into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, IObservable<TResult>> ToAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult> function, IScheduler scheduler)
        {
            if (function == null)
                throw new ArgumentNullException("function");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return (first, second, third, fourth, fifth, sixth, seventh, eight, ninth, tenth, eleventh, twelfth, thirteenth, fourteenth, fifteenth, sixteenth) =>
            {
                var subject = new AsyncSubject<TResult>(scheduler);
                scheduler.Schedule(() =>
                {
                    var result = default(TResult);
                    try
                    {
                        result = function(first, second, third, fourth, fifth, sixth, seventh, eight, ninth, tenth, eleventh, twelfth, thirteenth, fourteenth, fifteenth, sixteenth);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(result);
                    subject.OnCompleted();
                });
                return subject.AsObservable();
            };
        }
#endif
        /// <summary>
        /// Converts the action into an asynchronous function.
        /// </summary>
        public static Func<IObservable<Unit>> ToAsync(this Action action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            return ToAsync(action, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Converts the action into an asynchronous function.
        /// </summary>
        public static Func<IObservable<Unit>> ToAsync(this Action action, IScheduler scheduler)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return () =>
            {
                var subject = new AsyncSubject<Unit>(scheduler);
                scheduler.Schedule(() =>
                {
                    try
                    {
                        action();
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(new Unit());
                    subject.OnCompleted();
                });

                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts the action into an asynchronous function.
        /// </summary>
        public static Func<TSource, IObservable<Unit>> ToAsync<TSource>(this Action<TSource> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            return ToAsync(action, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Converts the action into an asynchronous function.
        /// </summary>
        public static Func<TSource, IObservable<Unit>> ToAsync<TSource>(this Action<TSource> action, IScheduler scheduler)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return (first) =>
            {
                var subject = new AsyncSubject<Unit>(scheduler);
                scheduler.Schedule(() =>
                {
                    try
                    {
                        action(first);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(new Unit());
                    subject.OnCompleted();
                });
                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts the action into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, IObservable<Unit>> ToAsync<T1, T2>(this Action<T1, T2> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            return ToAsync(action, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Converts the action into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, IObservable<Unit>> ToAsync<T1, T2>(this Action<T1, T2> action, IScheduler scheduler)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return (first, second) =>
            {
                var subject = new AsyncSubject<Unit>(scheduler);
                scheduler.Schedule(() =>
                {
                    try
                    {
                        action(first, second);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(new Unit());
                    subject.OnCompleted();
                });
                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts the action into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, IObservable<Unit>> ToAsync<T1, T2, T3>(this Action<T1, T2, T3> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            return ToAsync(action, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Converts the action into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, IObservable<Unit>> ToAsync<T1, T2, T3>(this Action<T1, T2, T3> action, IScheduler scheduler)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return (first, second, third) =>
            {
                var subject = new AsyncSubject<Unit>(scheduler);
                scheduler.Schedule(() =>
                {
                    try
                    {
                        action(first, second, third);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(new Unit());
                    subject.OnCompleted();
                });
                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts the action into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, IObservable<Unit>> ToAsync<T1, T2, T3, T4>(this Action<T1, T2, T3, T4> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            return ToAsync(action, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Converts the action into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, IObservable<Unit>> ToAsync<T1, T2, T3, T4>(this Action<T1, T2, T3, T4> action, IScheduler scheduler)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return (first, second, third, fourth) =>
            {
                var subject = new AsyncSubject<Unit>(scheduler);
                scheduler.Schedule(() =>
                {
                    try
                    {
                        action(first, second, third, fourth);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(new Unit());
                    subject.OnCompleted();
                });
                return subject.AsObservable();
            };
        }

#if DESKTOPCLR20 || DESKTOPCLR40

        /// <summary>
        /// Converts the action into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, IObservable<Unit>> ToAsync<T1, T2, T3, T4, T5>(this Action<T1, T2, T3, T4, T5> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            return ToAsync(action, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Converts the action into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, IObservable<Unit>> ToAsync<T1, T2, T3, T4, T5>(this Action<T1, T2, T3, T4, T5> action, IScheduler scheduler)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return (first, second, third, fourth, fifth) =>
            {
                var subject = new AsyncSubject<Unit>(scheduler);
                scheduler.Schedule(() =>
                {
                    try
                    {
                        action(first, second, third, fourth, fifth);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(new Unit());
                    subject.OnCompleted();
                });
                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts the action into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, IObservable<Unit>> ToAsync<T1, T2, T3, T4, T5, T6>(this Action<T1, T2, T3, T4, T5, T6> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            return ToAsync(action, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Converts the action into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, IObservable<Unit>> ToAsync<T1, T2, T3, T4, T5, T6>(this Action<T1, T2, T3, T4, T5, T6> action, IScheduler scheduler)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return (first, second, third, fourth, fifth, sixth) =>
            {
                var subject = new AsyncSubject<Unit>(scheduler);
                scheduler.Schedule(() =>
                {
                    try
                    {
                        action(first, second, third, fourth, fifth, sixth);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(new Unit());
                    subject.OnCompleted();
                });
                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts the action into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, IObservable<Unit>> ToAsync<T1, T2, T3, T4, T5, T6, T7>(this Action<T1, T2, T3, T4, T5, T6, T7> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            return ToAsync(action, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Converts the action into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, IObservable<Unit>> ToAsync<T1, T2, T3, T4, T5, T6, T7>(this Action<T1, T2, T3, T4, T5, T6, T7> action, IScheduler scheduler)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return (first, second, third, fourth, fifth, sixth, seventh) =>
            {
                var subject = new AsyncSubject<Unit>(scheduler);
                scheduler.Schedule(() =>
                {
                    try
                    {
                        action(first, second, third, fourth, fifth, sixth, seventh);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(new Unit());
                    subject.OnCompleted();
                });
                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts the action into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, IObservable<Unit>> ToAsync<T1, T2, T3, T4, T5, T6, T7, T8>(this Action<T1, T2, T3, T4, T5, T6, T7, T8> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            return ToAsync(action, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Converts the action into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, IObservable<Unit>> ToAsync<T1, T2, T3, T4, T5, T6, T7, T8>(this Action<T1, T2, T3, T4, T5, T6, T7, T8> action, IScheduler scheduler)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return (first, second, third, fourth, fifth, sixth, seventh, eight) =>
            {
                var subject = new AsyncSubject<Unit>(scheduler);
                scheduler.Schedule(() =>
                {
                    try
                    {
                        action(first, second, third, fourth, fifth, sixth, seventh, eight);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(new Unit());
                    subject.OnCompleted();
                });
                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts the action into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, IObservable<Unit>> ToAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            return ToAsync(action, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Converts the action into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, IObservable<Unit>> ToAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> action, IScheduler scheduler)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return (first, second, third, fourth, fifth, sixth, seventh, eighth, ninth) =>
            {
                var subject = new AsyncSubject<Unit>(scheduler);
                scheduler.Schedule(() =>
                {
                    try
                    {
                        action(first, second, third, fourth, fifth, sixth, seventh, eighth, ninth);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(new Unit());
                    subject.OnCompleted();
                });
                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts the action into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, IObservable<Unit>> ToAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            return ToAsync(action, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Converts the action into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, IObservable<Unit>> ToAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> action, IScheduler scheduler)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return (first, second, third, fourth, fifth, sixth, seventh, eighth, ninth, tenth) =>
            {
                var subject = new AsyncSubject<Unit>(scheduler);
                scheduler.Schedule(() =>
                {
                    try
                    {
                        action(first, second, third, fourth, fifth, sixth, seventh, eighth, ninth, tenth);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(new Unit());
                    subject.OnCompleted();
                });
                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts the action into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, IObservable<Unit>> ToAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            return ToAsync(action, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Converts the action into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, IObservable<Unit>> ToAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> action, IScheduler scheduler)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return (first, second, third, fourth, fifth, sixth, seventh, eighth, ninth, tenth, eleventh) =>
            {
                var subject = new AsyncSubject<Unit>(scheduler);
                scheduler.Schedule(() =>
                {
                    try
                    {
                        action(first, second, third, fourth, fifth, sixth, seventh, eighth, ninth, tenth, eleventh);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(new Unit());
                    subject.OnCompleted();
                });
                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts the action into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, IObservable<Unit>> ToAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            return ToAsync(action, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Converts the action into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, IObservable<Unit>> ToAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> action, IScheduler scheduler)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return (first, second, third, fourth, fifth, sixth, seventh, eighth, ninth, tenth, eleventh, twelfth) =>
            {
                var subject = new AsyncSubject<Unit>(scheduler);
                scheduler.Schedule(() =>
                {
                    try
                    {
                        action(first, second, third, fourth, fifth, sixth, seventh, eighth, ninth, tenth, eleventh, twelfth);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(new Unit());
                    subject.OnCompleted();
                });
                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts the action into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, IObservable<Unit>> ToAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            return ToAsync(action, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Converts the action into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, IObservable<Unit>> ToAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> action, IScheduler scheduler)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return (first, second, third, fourth, fifth, sixth, seventh, eighth, ninth, tenth, eleventh, twelfth, thirteenth) =>
            {
                var subject = new AsyncSubject<Unit>(scheduler);
                scheduler.Schedule(() =>
                {
                    try
                    {
                        action(first, second, third, fourth, fifth, sixth, seventh, eighth, ninth, tenth, eleventh, twelfth, thirteenth);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(new Unit());
                    subject.OnCompleted();
                });
                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts the action into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, IObservable<Unit>> ToAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            return ToAsync(action, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Converts the action into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, IObservable<Unit>> ToAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> action, IScheduler scheduler)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return (first, second, third, fourth, fifth, sixth, seventh, eighth, ninth, tenth, eleventh, twelfth, thirteenth, fourteenth) =>
            {
                var subject = new AsyncSubject<Unit>(scheduler);
                scheduler.Schedule(() =>
                {
                    try
                    {
                        action(first, second, third, fourth, fifth, sixth, seventh, eighth, ninth, tenth, eleventh, twelfth, thirteenth, fourteenth);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(new Unit());
                    subject.OnCompleted();
                });
                return subject.AsObservable();
            };
        }

        /// <summary>
        /// Converts the action into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, IObservable<Unit>> ToAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            return ToAsync(action, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Converts the action into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, IObservable<Unit>> ToAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> action, IScheduler scheduler)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return (first, second, third, fourth, fifth, sixth, seventh, eighth, ninth, tenth, eleventh, twelfth, thirteenth, fourteenth, fifteenth) =>
            {
                var subject = new AsyncSubject<Unit>(scheduler);
                scheduler.Schedule(() =>
                {
                    try
                    {
                        action(first, second, third, fourth, fifth, sixth, seventh, eighth, ninth, tenth, eleventh, twelfth, thirteenth, fourteenth, fifteenth);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(new Unit());
                    subject.OnCompleted();
                });
                return subject.AsObservable();
            };
        }


        /// <summary>
        /// Converts the action into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, IObservable<Unit>> ToAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            return ToAsync(action, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Converts the action into an asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, IObservable<Unit>> ToAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> action, IScheduler scheduler)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return (first, second, third, fourth, fifth, sixth, seventh, eighth, ninth, tenth, eleventh, twelfth, thirteenth, fourteenth, fifteenth, sixteenth) =>
            {
                var subject = new AsyncSubject<Unit>(scheduler);
                scheduler.Schedule(() =>
                {
                    try
                    {
                        action(first, second, third, fourth, fifth, sixth, seventh, eighth, ninth, tenth, eleventh, twelfth, thirteenth, fourteenth, fifteenth, sixteenth);
                    }
                    catch (Exception exception)
                    {
                        subject.OnError(exception);
                        return;
                    }
                    subject.OnNext(new Unit());
                    subject.OnCompleted();
                });
                return subject.AsObservable();
            };
        }
#endif

        /// <summary>
        /// Invokes the function asynchronously.
        /// </summary>
        public static IObservable<TSource> Start<TSource>(Func<TSource> function)
        {
            if (function == null)
                throw new ArgumentNullException("function");

            return ToAsync(function)();
        }

        /// <summary>
        /// Invokes the function asynchronously.
        /// </summary>
        public static IObservable<TSource> Start<TSource>(Func<TSource> function, IScheduler scheduler)
        {
            if (function == null)
                throw new ArgumentNullException("function");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return ToAsync(function, scheduler)();
        }

        /// <summary>
        /// Invokes the action asynchronously.
        /// </summary>
        public static IObservable<Unit> Start(Action action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            return ToAsync(action, Scheduler.ThreadPool)();
        }

        /// <summary>
        /// Invokes the action asynchronously.
        /// </summary>
        public static IObservable<Unit> Start(Action action, IScheduler scheduler)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return ToAsync(action, scheduler)();
        }

        /// <summary>
        /// Converts a Begin/End invoke function pair into a asynchronous function.
        /// </summary>
        public static Func<IObservable<Unit>> FromAsyncPattern(Func<AsyncCallback, object, IAsyncResult> begin, Action<IAsyncResult> end)
        {
            if (begin == null)
                throw new ArgumentNullException("begin");
            if (end == null)
                throw new ArgumentNullException("end");

            return FromAsyncPattern(begin, iar =>
            {
                end(iar);
                return new Unit();
            });
        }

        /// <summary>
        /// Converts a Begin/End invoke function pair into a asynchronous function.
        /// </summary>
        public static Func<T1, IObservable<Unit>> FromAsyncPattern<T1>(Func<T1, AsyncCallback, object, IAsyncResult> begin, Action<IAsyncResult> end)
        {
            if (begin == null)
                throw new ArgumentNullException("begin");
            if (end == null)
                throw new ArgumentNullException("end");

            return FromAsyncPattern(begin, iar =>
            {
                end(iar);
                return new Unit();
            });
        }

        /// <summary>
        /// Converts a Begin/End invoke function pair into a asynchronous function.
        /// </summary>
        public static Func<T1, T2, IObservable<Unit>> FromAsyncPattern<T1, T2>(Func<T1, T2, AsyncCallback, object, IAsyncResult> begin, Action<IAsyncResult> end)
        {
            if (begin == null)
                throw new ArgumentNullException("begin");
            if (end == null)
                throw new ArgumentNullException("end");

            return FromAsyncPattern(begin, iar =>
            {
                end(iar);
                return new Unit();
            });
        }

#if DESKTOPCLR20 || DESKTOPCLR40

        /// <summary>
        /// Converts a Begin/End invoke function pair into a asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, IObservable<Unit>> FromAsyncPattern<T1, T2, T3>(Func<T1, T2, T3, AsyncCallback, object, IAsyncResult> begin, Action<IAsyncResult> end)
        {
            if (begin == null)
                throw new ArgumentNullException("begin");
            if (end == null)
                throw new ArgumentNullException("end");

            return FromAsyncPattern(begin, iar =>
            {
                end(iar);
                return new Unit();
            });
        }

        /// <summary>
        /// Converts a Begin/End invoke function pair into a asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, IObservable<Unit>> FromAsyncPattern<T1, T2, T3, T4>(Func<T1, T2, T3, T4, AsyncCallback, object, IAsyncResult> begin, Action<IAsyncResult> end)
        {
            if (begin == null)
                throw new ArgumentNullException("begin");
            if (end == null)
                throw new ArgumentNullException("end");

            return FromAsyncPattern(begin, iar =>
            {
                end(iar);
                return new Unit();
            });
        }

        /// <summary>
        /// Converts a Begin/End invoke function pair into a asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, IObservable<Unit>> FromAsyncPattern<T1, T2, T3, T4, T5>(Func<T1, T2, T3, T4, T5, AsyncCallback, object, IAsyncResult> begin, Action<IAsyncResult> end)
        {
            if (begin == null)
                throw new ArgumentNullException("begin");
            if (end == null)
                throw new ArgumentNullException("end");

            return FromAsyncPattern(begin, iar =>
            {
                end(iar);
                return new Unit();
            });
        }

        /// <summary>
        /// Converts a Begin/End invoke function pair into a asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, IObservable<Unit>> FromAsyncPattern<T1, T2, T3, T4, T5, T6>(Func<T1, T2, T3, T4, T5, T6, AsyncCallback, object, IAsyncResult> begin, Action<IAsyncResult> end)
        {
            if (begin == null)
                throw new ArgumentNullException("begin");
            if (end == null)
                throw new ArgumentNullException("end");

            return FromAsyncPattern(begin, iar =>
            {
                end(iar);
                return new Unit();
            });
        }

        /// <summary>
        /// Converts a Begin/End invoke function pair into a asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, IObservable<Unit>> FromAsyncPattern<T1, T2, T3, T4, T5, T6, T7>(Func<T1, T2, T3, T4, T5, T6, T7, AsyncCallback, object, IAsyncResult> begin, Action<IAsyncResult> end)
        {
            if (begin == null)
                throw new ArgumentNullException("begin");
            if (end == null)
                throw new ArgumentNullException("end");

            return FromAsyncPattern(begin, iar =>
            {
                end(iar);
                return new Unit();
            });
        }

        /// <summary>
        /// Converts a Begin/End invoke function pair into a asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, IObservable<Unit>> FromAsyncPattern<T1, T2, T3, T4, T5, T6, T7, T8>(Func<T1, T2, T3, T4, T5, T6, T7, T8, AsyncCallback, object, IAsyncResult> begin, Action<IAsyncResult> end)
        {
            if (begin == null)
                throw new ArgumentNullException("begin");
            if (end == null)
                throw new ArgumentNullException("end");

            return FromAsyncPattern(begin, iar =>
            {
                end(iar);
                return new Unit();
            });
        }

        /// <summary>
        /// Converts a Begin/End invoke function pair into a asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, IObservable<Unit>> FromAsyncPattern<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, AsyncCallback, object, IAsyncResult> begin, Action<IAsyncResult> end)
        {
            if (begin == null)
                throw new ArgumentNullException("begin");
            if (end == null)
                throw new ArgumentNullException("end");

            return FromAsyncPattern(begin, iar =>
            {
                end(iar);
                return new Unit();
            });
        }

	    /// <summary>
	    /// Converts a Begin/End invoke function pair into a asynchronous function.
	    /// </summary>
	    public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, IObservable<Unit>> FromAsyncPattern<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, AsyncCallback, object, IAsyncResult> begin)
	    {
	        return FromAsyncPattern<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(begin, default(Action<IAsyncResult>));
	    }

	    /// <summary>
        /// Converts a Begin/End invoke function pair into a asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, IObservable<Unit>> FromAsyncPattern<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, AsyncCallback, object, IAsyncResult> begin, Action<IAsyncResult> end)
        {
            if (begin == null)
                throw new ArgumentNullException("begin");
            if (end == null)
                throw new ArgumentNullException("end");

            return FromAsyncPattern(begin, iar =>
            {
                end(iar);
                return new Unit();
            });
        }

        /// <summary>
        /// Converts a Begin/End invoke function pair into a asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, IObservable<Unit>> FromAsyncPattern<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, AsyncCallback, object, IAsyncResult> begin, Action<IAsyncResult> end)
        {
            if (begin == null)
                throw new ArgumentNullException("begin");
            if (end == null)
                throw new ArgumentNullException("end");

            return FromAsyncPattern(begin, iar =>
            {
                end(iar);
                return new Unit();
            });
        }

        /// <summary>
        /// Converts a Begin/End invoke function pair into a asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, IObservable<Unit>> FromAsyncPattern<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, AsyncCallback, object, IAsyncResult> begin, Action<IAsyncResult> end)
        {
            if (begin == null)
                throw new ArgumentNullException("begin");
            if (end == null)
                throw new ArgumentNullException("end");

            return FromAsyncPattern(begin, iar =>
            {
                end(iar);
                return new Unit();
            });
        }

        /// <summary>
        /// Converts a Begin/End invoke function pair into a asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, IObservable<Unit>> FromAsyncPattern<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, AsyncCallback, object, IAsyncResult> begin, Action<IAsyncResult> end)
        {
            if (begin == null)
                throw new ArgumentNullException("begin");
            if (end == null)
                throw new ArgumentNullException("end");

            return FromAsyncPattern(begin, iar =>
            {
                end(iar);
                return new Unit();
            });
        }

        /// <summary>
        /// Converts a Begin/End invoke function pair into a asynchronous function.
        /// </summary>
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, IObservable<Unit>> FromAsyncPattern<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, AsyncCallback, object, IAsyncResult> begin, Action<IAsyncResult> end)
        {
            if (begin == null)
                throw new ArgumentNullException("begin");
            if (end == null)
                throw new ArgumentNullException("end");

            return FromAsyncPattern(begin, iar =>
            {
                end(iar);
                return new Unit();
            });
        }
#endif
    }
}
