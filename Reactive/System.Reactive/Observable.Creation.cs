using System;
using System.Collections.Generic;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Collections.Generic;
using System.Reflection;
using System.Globalization;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Disposables;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Concurrency;
using System.Linq;

namespace
#if WM7
Microsoft.Windows.Phone
#endif
Reactive.Linq
{
    public static partial class Observable
    {
        /// <summary>
        /// Returns a non-terminating observable sequence.
        /// </summary>
        public static IObservable<TValue> Never<TValue>()
        {
            return new AnonymousObservable<TValue>(observer => Disposable.Empty);
        }

        /// <summary>
        /// Returns an empty observable sequence.
        /// </summary>
        public static IObservable<TValue> Empty<TValue>()
        {
            return Empty<TValue>(Scheduler.CurrentThread);
        }

        /// <summary>
        /// Returns an empty observable sequence.
        /// </summary>
        public static IObservable<TValue> Empty<TValue>(IScheduler scheduler)
        {
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return new AnonymousObservable<TValue>(observer => scheduler.Schedule(observer.OnCompleted));
        }

        /// <summary>
        /// Returns an observable sequence that contains a single value.
        /// </summary>
        public static IObservable<TValue> Return<TValue>(TValue value)
        {
            return Return(value, Scheduler.CurrentThread);
        }

        /// <summary>
        /// Returns an observable sequence that contains a single value.
        /// </summary>
        public static IObservable<TValue> Return<TValue>(TValue value, IScheduler scheduler)
        {
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return new AnonymousObservable<TValue>(observer => scheduler.Schedule(() =>
                {
                    observer.OnNext(value);
                    observer.OnCompleted();
                }));
        }

        /// <summary>
        /// Returns an observable sequence that terminates with an exception.
        /// </summary>
        public static IObservable<TValue> Throw<TValue>(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException("exception");

            return Throw<TValue>(exception, Scheduler.CurrentThread);
        }

        /// <summary>
        /// Returns an observable sequence that terminates with an exception.
        /// </summary>
        public static IObservable<TValue> Throw<TValue>(Exception exception, IScheduler scheduler)
        {
            if (exception == null)
                throw new ArgumentNullException("exception");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return new AnonymousObservable<TValue>(observer => scheduler.Schedule(() => observer.OnError(exception)));
        }

        /// <summary>
        /// Subscribes an observer to an enumerable sequence.  Returns an object that can be used to unsubscribe the observer from the enumerable.
        /// </summary>
        public static IDisposable Subscribe<TSource>(this IEnumerable<TSource> source, IObserver<TSource> observer)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (observer == null)
                throw new ArgumentNullException("observer");

            return source.Subscribe(observer, Scheduler.CurrentThread);
        }

        /// <summary>
        /// Subscribes an observer to an enumerable sequence.  Returns an object that can be used to unsubscribe the observer from the enumerable.
        /// </summary>
        public static IDisposable Subscribe<TSource>(this IEnumerable<TSource> source, IObserver<TSource> observer, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (observer == null)
                throw new ArgumentNullException("observer");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            var e = source.GetEnumerator();
            var flag = new BooleanDisposable();

            scheduler.Schedule(self =>
            {
                var hasNext = false;
                var ex = default(Exception);
                var current = default(TSource);

                if (flag.IsDisposed)
                {
                    e.Dispose();
                    return;
                }

                try
                {
                    hasNext = e.MoveNext();
                    if (hasNext)
                        current = e.Current;
                }
                catch (Exception exception)
                {
                    ex = exception;
                }

                if (!hasNext || ex != null)
                {
                    e.Dispose();
                }

                if (ex != null)
                {
                    observer.OnError(ex);
                    return;
                }

                if (!hasNext)
                {
                    observer.OnCompleted();
                    return;
                }

                observer.OnNext(current);
                self();
            });

            return flag;
        }

        /// <summary>
        /// Returns an observable sequence that contains the values of the underlying .NET event.
        /// </summary>
        public static IObservable<IEvent<TEventArgs>> FromEvent<TDelegate, TEventArgs>(Func<EventHandler<TEventArgs>, TDelegate> conversion, Action<TDelegate> addHandler, Action<TDelegate> removeHandler) where TEventArgs : EventArgs
        {
            if (conversion == null)
                throw new ArgumentNullException("conversion");
            if (addHandler == null)
                throw new ArgumentNullException("addHandler");
            if (removeHandler == null)
                throw new ArgumentNullException("removeHandler");

            return new AnonymousObservable<IEvent<TEventArgs>>(observer =>
            {
                var handler = conversion((sender, eventArgs) => observer.OnNext(Event.Create(sender, eventArgs)));
                addHandler(handler);
                return Disposable.Create(() => removeHandler(handler));
            });
        }

        /// <summary>
        /// Returns an observable sequence that contains the values of the underlying .NET event.
        /// </summary>
        public static IObservable<IEvent<TEventArgs>> FromEvent<TEventArgs>(Action<EventHandler<TEventArgs>> addHandler, Action<EventHandler<TEventArgs>> removeHandler) where TEventArgs : EventArgs
        {
            if (addHandler == null)
                throw new ArgumentNullException("addHandler");
            if (removeHandler == null)
                throw new ArgumentNullException("removeHandler");

            return FromEvent<EventHandler<TEventArgs>, TEventArgs>(
                handler => handler,
                addHandler,
                removeHandler);
        }

        /// <summary>
        /// Returns an observable sequence that contains the values of the underlying .NET event.
        /// </summary>
        public static IObservable<IEvent<TEventArgs>> FromEvent<TEventArgs>(object target, string eventName) where TEventArgs : EventArgs
        {
            if (target == null)
                throw new ArgumentNullException("target");
            if (eventName == null)
                throw new ArgumentNullException("eventName");

            var e = target.GetType().GetEvent(eventName, BindingFlags.Public | BindingFlags.Instance);

            if (e == null)
#if IL2JS
                throw new InvalidOperationException(string.Format("Could not find event '{0}' on object of type '{1}'.", eventName, target.GetType().FullName));
#else
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Could not find event '{0}' on object of type '{1}'.", eventName, target.GetType().FullName));
#endif

            var addMethod = e.GetAddMethod();
            var removeMethod = e.GetRemoveMethod();

            if (addMethod == null)
                throw new InvalidOperationException("Event is missing the add method.");
            if (removeMethod == null)
                throw new InvalidOperationException("Event is missing the remove method.");

            var ps = addMethod.GetParameters();
            if (ps.Length != 1)
                throw new InvalidOperationException("Add method should take 1 parameter.");

            var delegateType = ps[0].ParameterType;

            var invokeMethod = delegateType.GetMethod("Invoke");

            var parameters = invokeMethod.GetParameters();

            if (parameters.Length == 2
                && parameters[0].ParameterType.Equals(typeof(object))
                && typeof(TEventArgs).Equals(parameters[1].ParameterType)
                && invokeMethod.ReturnType == typeof(void))
            {
                return new AnonymousObservable<IEvent<TEventArgs>>(observer =>
                {
                    EventHandler<TEventArgs> handler = (sender, eventArgs) => observer.OnNext(Event.Create(sender, eventArgs));
                    var d = Delegate.CreateDelegate(delegateType, handler, "Invoke");
                    addMethod.Invoke(target, new object[] { d });
                    return Disposable.Create(() => removeMethod.Invoke(target, new object[] { d }));
                });
            }

            throw new InvalidOperationException("The event delegate must be of the form void Handler(object, T) where T : EventArgs.");
        }

        /// <summary>
        /// Generates an observable sequence by iterating a state from an initial state until
        /// the condition fails.
        /// </summary>
        public static IObservable<TResult> Generate<TState, TResult>(TState initialState, Func<TState, bool> condition, Func<TState, TResult> resultSelector, Func<TState, TState> iterate, IScheduler scheduler)
        {
            if (condition == null)
                throw new ArgumentNullException("condition");
            if (resultSelector == null)
                throw new ArgumentNullException("resultSelector");
            if (iterate == null)
                throw new ArgumentNullException("iterate");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return new AnonymousObservable<TResult>(observer =>
                {
                    var state = initialState;
                    var first = true;
                    return scheduler.Schedule(self =>
                        {
                            var hasResult = false;
                            var result = default(TResult);
                            try
                            {
                                if (first)
                                    first = false;
                                else
                                    state = iterate(state);
                                hasResult = condition(state);
                                if (hasResult)
                                    result = resultSelector(state);
                            }
                            catch (Exception exception)
                            {
                                observer.OnError(exception);
                                return;
                            }

                            if (hasResult)
                            {
                                observer.OnNext(result);
                                self();
                            }
                            else
                                observer.OnCompleted();
                        });
                });
        }

        /// <summary>
        /// Generates an observable sequence by iterating a state from an initial state until
        /// the condition fails.
        /// </summary>
        public static IObservable<TResult> Generate<TState, TResult>(TState initialState, Func<TState, bool> condition, Func<TState, TResult> resultSelector, Func<TState, TState> iterate)
        {
            if (condition == null)
                throw new ArgumentNullException("condition");
            if (resultSelector == null)
                throw new ArgumentNullException("resultSelector");
            if (iterate == null)
                throw new ArgumentNullException("iterate");

            return Generate(initialState, condition, resultSelector, iterate, Scheduler.CurrentThread);
        }


        /// <summary>
        /// Returns an observable sequence that invokes the observableFactory function whenever a new observer subscribes.
        /// </summary>
        public static IObservable<TValue> Defer<TValue>(Func<IObservable<TValue>> observableFactory)
        {
            if (observableFactory == null)
                throw new ArgumentNullException("observableFactory");

            return new AnonymousObservable<TValue>(observer =>
            {
                IObservable<TValue> result;
                try
                {
                    result = observableFactory();
                }
                catch (Exception exception)
                {
                    return Throw<TValue>(exception).Subscribe(observer);
                }

                return result.Subscribe(observer);
            });
        }


        /// <summary>
        /// Retrieves resource from resourceSelector for use in resourceUsage and disposes 
        /// the resource once the resulting observable sequence terminates.
        /// </summary>
        public static IObservable<TSource> Using<TSource, TResource>(Func<TResource> resourceSelector, Func<TResource, IObservable<TSource>> resourceUsage) where TResource : IDisposable
        {
            if (resourceSelector == null)
                throw new ArgumentNullException("resourceSelector");
            if (resourceUsage == null)
                throw new ArgumentNullException("resourceUsage");

            return new AnonymousObservable<TSource>(observer =>
            {
                var source = default(IObservable<TSource>);
                var disposable = Disposable.Empty;
                try
                {
                    var resource = resourceSelector();
                    if (resource != null)
                        disposable = resource;
                    source = resourceUsage(resource);
                }
                catch (Exception exception)
                {
                    return new CompositeDisposable(Throw<TSource>(exception).Subscribe(observer), disposable);
                }

                return new CompositeDisposable(source.Subscribe(observer), disposable);
            });
        }

        /// <summary>
        /// Converts an enumerable sequence to an observable sequence.
        /// </summary>
        public static IObservable<TSource> ToObservable<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.ToObservable(Scheduler.CurrentThread);
        }

        /// <summary>
        /// Converts an enumerable sequence to an observable sequence.
        /// </summary>
        public static IObservable<TSource> ToObservable<TSource>(this IEnumerable<TSource> source, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return new AnonymousObservable<TSource>(observer => source.Subscribe(observer, scheduler));
        }

        /// <summary>
        /// Creates an observable sequence from the subscribe implementation.
        /// </summary>
        public static IObservable<TSource> CreateWithDisposable<TSource>(Func<IObserver<TSource>, IDisposable> subscribe)
        {
            if (subscribe == null)
                throw new ArgumentNullException("subscribe");

            return new AnonymousObservable<TSource>(subscribe);
        }


        /// <summary>
        /// Creates an observable sequence from the subscribe implementation.
        /// </summary>
        public static IObservable<TSource> Create<TSource>(Func<IObserver<TSource>, Action> subscribe)
        {
            if (subscribe == null)
                throw new ArgumentNullException("subscribe");

            return CreateWithDisposable<TSource>(o => Disposable.Create(subscribe(o)));
        }

        /// <summary>
        /// Generates an observable sequence of integral numbers within a specified range.
        /// </summary>
        public static IObservable<int> Range(int start, int count)
        {
            var max = ((long)start) + count - 1;
            if (count < 0 || max > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            return Range(start, count, Scheduler.CurrentThread);
        }

        /// <summary>
        /// Generates an observable sequence of integral numbers within a specified range.
        /// </summary>
        public static IObservable<int> Range(int start, int count, IScheduler scheduler)
        {
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");
            var max = ((long)start) + count - 1;
            if (count < 0 || max > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            return Generate(start, x => x <= max, x => x, x => x + 1, scheduler);
        }

        static IEnumerable<T> RepeatInfinite<T>(T value)
        {
            while (true)
                yield return value;
        }

        /// <summary>
        /// Repeats the observable sequence indefinitely.
        /// </summary>
        public static IObservable<TSource> Repeat<TSource>(this IObservable<TSource> source, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return RepeatInfinite(source).Concat(scheduler);
        }

        /// <summary>
        /// Repeats the observable sequence repeatCount times.
        /// </summary>
        public static IObservable<TSource> Repeat<TSource>(this IObservable<TSource> source, int repeatCount, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (repeatCount < 0)
                throw new ArgumentOutOfRangeException("repeatCount");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return Enumerable.Repeat(source, repeatCount).Concat(scheduler);
        }

        /// <summary>
        /// Repeats the source observable sequence until it successfully terminates.
        /// </summary>
        public static IObservable<TSource> Retry<TSource>(this IObservable<TSource> source, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return RepeatInfinite(source).Catch(scheduler);
        }

        /// <summary>
        /// Repeats the source observable sequence the retryCount times or until it successfully terminates.
        /// </summary>
        public static IObservable<TSource> Retry<TSource>(this IObservable<TSource> source, int retryCount, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (retryCount < 0)
                throw new ArgumentOutOfRangeException("retryCount");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return Enumerable.Repeat(source, retryCount).Catch(scheduler);
        }

        /// <summary>
        /// Generates an observable sequence that contains one repeated value.
        /// </summary>
        public static IObservable<TValue> Repeat<TValue>(TValue value, IScheduler scheduler)
        {
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return Return(value).Repeat(scheduler);
        }

        /// <summary>
        /// Generates an observable sequence that contains one repeated value.
        /// </summary>
        public static IObservable<TValue> Repeat<TValue>(TValue value, int repeatCount, IScheduler scheduler)
        {
            if (repeatCount < 0)
                throw new ArgumentOutOfRangeException("repeatCount");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return Return(value).Repeat(repeatCount, scheduler);
        }


        /// <summary>
        /// Repeats the observable sequence indefinitely.
        /// </summary>
        public static IObservable<TSource> Repeat<TSource>(this IObservable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Repeat(Scheduler.Immediate);
        }

        /// <summary>
        /// Repeats the observable sequence repeatCount times.
        /// </summary>
        public static IObservable<TSource> Repeat<TSource>(this IObservable<TSource> source, int repeatCount)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (repeatCount < 0)
                throw new ArgumentOutOfRangeException("repeatCount");

            return source.Repeat(repeatCount, Scheduler.Immediate);
        }

        /// <summary>
        /// Repeats the source observable sequence until it successfully terminates.
        /// </summary>
        public static IObservable<TSource> Retry<TSource>(this IObservable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Retry(Scheduler.Immediate);
        }

        /// <summary>
        /// Repeats the source observable sequence the retryCount times or until it successfully terminates.
        /// </summary>
        public static IObservable<TSource> Retry<TSource>(this IObservable<TSource> source, int retryCount)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (retryCount < 0)
                throw new ArgumentOutOfRangeException("retryCount");

            return source.Retry(retryCount, Scheduler.Immediate);
        }

        /// <summary>
        /// Generates an observable sequence that contains one repeated value.
        /// </summary>
        public static IObservable<TValue> Repeat<TValue>(TValue value)
        {
            return Repeat(value, Scheduler.CurrentThread);
        }

        /// <summary>
        /// Generates an observable sequence that contains one repeated value.
        /// </summary>
        public static IObservable<TValue> Repeat<TValue>(TValue value, int repeatCount)
        {
            if (repeatCount < 0)
                throw new ArgumentOutOfRangeException("repeatCount");

            return Repeat(value, repeatCount, Scheduler.CurrentThread);
        }
    }
}
