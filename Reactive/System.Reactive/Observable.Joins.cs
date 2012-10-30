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
Reactive.Joins;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Linq;
using System.Text;
using System.Diagnostics;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Disposables;

namespace
#if WM7
Microsoft.Windows.Phone
#endif
Reactive.Linq
{
	public static partial class Observable
	{

        /// <summary>
        /// Matches when both observable sequences have an available value.
        /// </summary>
        public static Pattern<TLeft, TRight> And<TLeft, TRight>(this IObservable<TLeft> left, IObservable<TRight> right)
        {
            if (left == null)
                throw new ArgumentNullException("left");

            if (right == null)
                throw new ArgumentNullException("right");

            return new Pattern<TLeft, TRight>(left, right);
        }

        /// <summary>
        /// Matches when the observable sequence has an available value and projects the value.
        /// </summary>
        public static Plan<TResult> Then<TSource, TResult>(this IObservable<TSource> source, Func<TSource, TResult> selector)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (selector == null)
                throw new ArgumentNullException("selector");

            return new Pattern<TSource>(source).Then(selector);
        }

        /// <summary>
        /// Joins together the results from several patterns.
        /// </summary>
        public static IObservable<TResult> Join<TResult>(params Plan<TResult>[] plans)
        {
            if (plans == null)
                throw new ArgumentNullException("plans");

            return Join((IEnumerable<Plan<TResult>>)plans);
        }

        /// <summary>
        /// Joins together the results from several patterns.
        /// </summary>
        public static IObservable<TResult> Join<TResult>(this IEnumerable<Plan<TResult>> plans)
        {
            if (plans == null)
                throw new ArgumentNullException("plans");

            return new AnonymousObservable<TResult>(observer =>
            {
                var externalSubscriptions = new Dictionary<object, IJoinObserver>();
                var gate = new object();
                var activePlans = new List<ActivePlan>();
                var outObserver = Observer.Create<TResult>(observer.OnNext,
                    exception =>
                    {
                        foreach (var po in externalSubscriptions.Values)
                        {
                            po.Dispose();
                        }
                        observer.OnError(exception);
                    },
                    observer.OnCompleted);
                try
                {
                    foreach (var plan in plans)
                        activePlans.Add(plan.Activate(externalSubscriptions, outObserver,
                                                      activePlan =>
                                                      {
                                                          activePlans.Remove(activePlan);
                                                          if (activePlans.Count == 0)
                                                              outObserver.OnCompleted();
                                                      }));
                }
                catch (Exception e)
                {
                    return Throw<TResult>(e).Subscribe(observer);
                }

                var group = new CompositeDisposable();
                foreach (var joinObserver in externalSubscriptions.Values)
                {
                    joinObserver.Subscribe(gate);
                    group.Add(joinObserver);
                }

                return group;
            });
        }
    }
}
