//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;

    /// <summary>
    /// A queue that supports multiple producer and single consumer pattern.
    /// Consumer must be invoked single threaded. It is possible to invoke the
    /// consumer on a different thread.
    /// If consumer cannot process a work, all new work items are queued until
    /// continue work method is called.
    /// </summary>
    sealed class SerializedWorker<T> where T : class
    {
        static readonly WaitCallback onWorkCallback = OnWorkCallback;
        readonly Func<T, bool> workFunc;    // return true if work is completed
        readonly Action<T> abortCallback;
        readonly bool dispatchOnDifferentThread;
        readonly ConcurrentQueue<T> pendingWork;
        volatile bool disposed;
        int count;
        int working;

        public SerializedWorker(Func<T, bool> workFunc, Action<T> abortCallback, bool dispatchOnDifferentThread)
        {
            this.workFunc = workFunc;
            this.abortCallback = abortCallback;
            this.dispatchOnDifferentThread = dispatchOnDifferentThread;
            this.pendingWork = new ConcurrentQueue<T>();
        }

        public void DoWork(T work)
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            this.pendingWork.Enqueue(work);
            if (Interlocked.Increment(ref this.count) == 1)
            {
                if (this.dispatchOnDifferentThread)
                {
                    ThreadPool.QueueUserWorkItem(onWorkCallback, this);
                }
                else
                {
                    this.DoWorkInternal();
                }
            }
        }

        public void ContinueWork()
        {
            if (this.disposed)
            {
                return;
            }

            this.DoWorkInternal();
        }

        public void Abort()
        {
            this.disposed = true;
            if (this.abortCallback != null)
            {
                do
                {
                    T work = null;
                    if (this.pendingWork.TryDequeue(out work))
                    {
                        this.abortCallback(work);
                    }
                }
                while (Interlocked.Decrement(ref this.count) > 0);
            }
        }

        static void OnWorkCallback(object state)
        {
            var thisPtr = (SerializedWorker<T>)state;
            thisPtr.DoWorkInternal();
        }

        void DoWorkInternal()
        {
            if (Interlocked.Increment(ref this.working) > 1)
            {
                return;
            }

            // This loop drains all work requests (1 from DoWork and * from ContinueWork)
            do
            {
                // This loop drains all work items from the queue. We should not stop
                // if TryPeek returns false because new work could be queued after that.
                do
                {
                    T work = null;
                    if (this.pendingWork.TryPeek(out work))
                    {
                        if (this.workFunc(work))
                        {
                            // work completed so remove it from the queue
                            if (!this.pendingWork.TryDequeue(out work))
                            {
                                // the worker is disposed
                                return;
                            }
                        }
                        else
                        {
                            // work cannot be completed at this time.
                            // new work will be queued, work will be 
                            // resumed when ContinueWork is called
                            break;
                        }
                    }
                }
                while (Interlocked.Decrement(ref this.count) > 0);
            }
            while (Interlocked.Decrement(ref this.working) > 0);
        }
    }
}
