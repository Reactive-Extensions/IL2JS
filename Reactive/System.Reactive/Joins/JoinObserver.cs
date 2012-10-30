using System;
using System.Collections.Generic;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Collections.Generic;
using System.Diagnostics;
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

namespace
#if WM7
Microsoft.Windows.Phone
#endif
Reactive.Joins
{
    internal interface IJoinObserver : IDisposable
    {
        void Subscribe(object gate);
        void Dequeue();
    }

    internal sealed class JoinObserver<T> : AbstractObserver<Notification<T>>, IJoinObserver
    {
        private object gate;
        private readonly IObservable<T> source;
        private readonly Action<Exception> onError;
        private bool initialized;
        private List<ActivePlan> activePlans;
        public Queue<Notification<T>> Queue { get; private set; }
        private readonly MutableDisposable subscription;
        private bool isDisposed;

        public JoinObserver(IObservable<T> source, Action<Exception> onError)
        {
            this.source = source;
            this.onError = onError;
            Queue = new Queue<Notification<T>>();
            subscription = new MutableDisposable();
            activePlans = new List<ActivePlan>();
        }

        public void AddActivePlan(ActivePlan activePlan)
        {
            activePlans.Add(activePlan);
        }

        public void Subscribe(object gate)
        {
            this.gate = gate;
            initialized = true;
            subscription.Disposable = source.Materialize().Subscribe(this);
        }

        public void Dequeue()
        {
            Queue.Dequeue();
        }

        protected override void Next(Notification<T> n)
        {
            Debug.Assert(initialized);

            lock (gate)
            {
                if (!isDisposed)
                {
                    if (n.Kind == NotificationKind.OnError)
                    {
                        onError(n.Exception);
                        return;
                    }

                    Queue.Enqueue(n);
                    foreach (var activePlan in activePlans.ToArray())
                        activePlan.Match();
                }
            }
        }

        protected override void Error(Exception exception)
        {
        }

        protected override void Completed()
        {
        }

        internal void RemoveActivePlan(ActivePlan activePlan)
        {
            activePlans.Remove(activePlan);
            if (activePlans.Count == 0)
                Dispose();
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                subscription.Dispose();
            }
        }
    }
}