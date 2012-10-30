// <copyright file="AsyncResultNoResult.cs" company="Microsoft">
// From Jeffrey Richters MSDN Article on Async programming
// </copyright>
namespace Microsoft.Csa.SharedObjects.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// From Jeffrey Richters MSDN Article on Async programming
    /// </summary>
    public class AsyncResultNoResult : IAsyncResult
    {
        // Fields set at construction which never change while 
        // operation is pending
        private readonly AsyncCallback asyncCallback;
        private readonly object asyncState;

        // Fields set at construction which do change after 
        // operation completes
        private const int StatePending = 0;
        private const int StateCompletedSynchronously = 1;
        private const int StateCompletedAsynchronously = 2;
        private int completedState;

        // Field that may or may not get set depending on usage
        private ManualResetEvent asyncWaitHandle;

        // Fields set when operation completes
        private Exception exception;

        public AsyncResultNoResult(AsyncCallback asyncCallback, object state)
        {
            this.asyncCallback = asyncCallback;
            this.asyncState = state;
        }

        public void SetAsCompleted(
           Exception exception, bool completedSynchronously)
        {
            // Passing null for exception means no error occurred. 
            // This is the common case
            this.exception = exception;

            // The m_CompletedState field MUST be set prior calling the callback
            int state = completedSynchronously ? StateCompletedSynchronously : StateCompletedAsynchronously;
            int prevState = Interlocked.Exchange(ref this.completedState, state);
            if (prevState != StatePending)
            {
                throw new InvalidOperationException("You can set a result only once");
            }

            // If the event exists, set it
            if (this.asyncWaitHandle != null)
            {
                this.asyncWaitHandle.Set();
            }

            // If a callback method was set, call it
            if (this.asyncCallback != null)
            {
                this.asyncCallback(this);
            }
        }

        public void EndInvoke()
        {
            // This method assumes that only 1 thread calls EndInvoke 
            // for this object
            if (!this.IsCompleted)
            {
                // If the operation isn't done, wait for it
                this.AsyncWaitHandle.WaitOne();
                this.AsyncWaitHandle.Close();
                this.asyncWaitHandle = null;  // Allow early GC
            }

            // Operation is done: if an exception occured, throw it
            if (this.exception != null)
            {
                throw this.exception;
            }
        }

        #region Implementation of IAsyncResult
        public object AsyncState
        {
            get { return this.asyncState; }
        }

        public bool CompletedSynchronously
        {
            get
            {
#if SILVERLIGHT
                lock(this)
                {
                    return this.completedState == StateCompletedSynchronously;
                }
#else
                return Thread.VolatileRead(ref this.completedState) == StateCompletedSynchronously;
#endif
            }
        }

        public WaitHandle AsyncWaitHandle
        {
            get
            {
                if (this.asyncWaitHandle == null)
                {
                    bool done = this.IsCompleted;
                    ManualResetEvent mre = new ManualResetEvent(done);
                    if (Interlocked.CompareExchange(ref this.asyncWaitHandle, mre, null) != null)
                    {
                        // Another thread created this object's event; dispose 
                        // the event we just created
                        mre.Close();
                    }
                    else
                    {
                        if (!done && this.IsCompleted)
                        {
                            // If the operation wasn't done when we created 
                            // the event but now it is done, set the event
                            this.asyncWaitHandle.Set();
                        }
                    }
                }

                return this.asyncWaitHandle;
            }
        }

        public bool IsCompleted
        {
            get
            {
#if SILVERLIGHT
                lock(this)
                {
                    return this.completedState != StatePending;
                }
#else
                return Thread.VolatileRead(ref this.completedState) != StatePending;
#endif
            }
        }
        #endregion
    }
}
