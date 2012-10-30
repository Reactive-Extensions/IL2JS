﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReactiveTests.Mocks
{
    class MockEnumerable<T> : IEnumerable<T>
    {
        public readonly TestScheduler Scheduler;
        public readonly List<Subscription> Subscriptions = new List<Subscription>();

        IEnumerable<T> underlyingEnumerable;

        public MockEnumerable(TestScheduler scheduler, IEnumerable<T> underlyingEnumerable)
        {
            this.Scheduler = scheduler;
            this.underlyingEnumerable = underlyingEnumerable;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new MockEnumerator(Scheduler, Subscriptions, underlyingEnumerable.GetEnumerator());
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        class MockEnumerator : IEnumerator<T>
        {
            List<Subscription> subscriptions;
            IEnumerator<T> enumerator;
            TestScheduler scheduler;
            int index;
            bool disposed = false;

            public MockEnumerator(TestScheduler scheduler, List<Subscription> subscriptions, IEnumerator<T> enumerator)
            {
                this.subscriptions = subscriptions;
                this.enumerator = enumerator;
                this.scheduler = scheduler;

                index = subscriptions.Count;
                subscriptions.Add(new Subscription(scheduler.Ticks));
            }

            public T Current
            {
                get
                {
                    if (disposed)
                        throw new ObjectDisposedException("this");
                    return enumerator.Current; 
                }
            }

            public void Dispose()
            {
                if (!disposed)
                {
                    disposed = true;
                    enumerator.Dispose();
                    subscriptions[index] = new Subscription(subscriptions[index].Subscribe, scheduler.Ticks);
                }
            }

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                if (disposed)
                    throw new ObjectDisposedException("this");
                return enumerator.MoveNext();
            }

            public void Reset()
            {
                if (disposed)
                    throw new ObjectDisposedException("this");
                enumerator.Reset();
            }
        }

    }
}
