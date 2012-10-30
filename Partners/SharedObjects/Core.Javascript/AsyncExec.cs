using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace Microsoft.Csa.SharedObjects
{
    /// <summary>
    /// Provides static methods to simplify the implementation of the asynchronous begin/end pattern on top of a synchronous method or lambda.
    /// </summary>
    internal static class AsyncExec
    {
        private abstract class AsyncDelegate : IAsyncResult
        {
            private AsyncCallback asyncCallback;
            private object asyncState;
            private object owner;
            private IAsyncResult innerAsyncResult;

            protected AsyncDelegate(AsyncCallback asyncCallback, object asyncState, object owner)
            {
                this.owner = owner;
                this.asyncCallback = asyncCallback;
                this.asyncState = asyncState;
            }

            protected IAsyncResult Begin(Func<AsyncCallback, object, IAsyncResult> beginFunc)
            {
                this.innerAsyncResult = beginFunc(OuterAsyncCallback, asyncState);
                return this;
            }

            protected void End(object owner, Action<IAsyncResult> endAction)
            {
                VerifyOwner(owner);
                endAction(innerAsyncResult);
            }

            protected T End<T>(object owner, Func<IAsyncResult, T> endFunc)
            {
                VerifyOwner(owner);
                return endFunc(innerAsyncResult);
            }

            private void OuterAsyncCallback(IAsyncResult asyncResult)
            {
                Debug.Assert(asyncResult == innerAsyncResult);
                if (asyncCallback != null)
                {
                    asyncCallback(this);
                }
            }

            private void VerifyOwner(object owner)
            {
                if (owner != this.owner)
                {
                    throw new ArgumentException("Owner does not match async result", "owner");
                }
            }

            #region IAsyncResult implementation

            public object AsyncState
            {
                get { return innerAsyncResult.AsyncState; }
            }

            public WaitHandle AsyncWaitHandle
            {
                get { return innerAsyncResult.AsyncWaitHandle; }
            }

            public bool CompletedSynchronously
            {
                get { return innerAsyncResult.CompletedSynchronously; }
            }

            public bool IsCompleted
            {
                get { return innerAsyncResult.IsCompleted; }
            }

            #endregion
        }

        private class AsyncAction : AsyncDelegate
        {
            private Action action;

            public AsyncAction(Action action, AsyncCallback asyncCallback, object asyncState, object owner) :
                base(asyncCallback, asyncState, owner)
            {
                this.action = action;
            }

            public IAsyncResult Begin()
            {
                return Begin(action.BeginInvoke);
            }

            public void End(object owner)
            {
                End(owner, action.EndInvoke);
            }
        }

        private class AsyncFunc<T> : AsyncDelegate
        {
            private Func<T> func;

            public AsyncFunc(Func<T> func, AsyncCallback asyncCallback, object asyncState, object owner) :
                base(asyncCallback, asyncState, owner)
            {
                this.func = func;
            }

            public IAsyncResult Begin()
            {
                return Begin(func.BeginInvoke);
            }

            public T End(object owner)
            {
                return End<T>(owner, func.EndInvoke);
            }
        }

        /// <summary>
        /// Begins an asynchronous operation to execute a method or lambda that does not return a value.
        /// </summary>
        /// <param name="action">The method or lambda to execute asynchronously.</param>
        /// <param name="asyncCallback">A callback method that is called when the asynchronous operation completes.</param>
        /// <param name="asyncState">A user-defined object that contains information about the asynchronous operation.</param>
        /// <param name="owner">An optional object that represents the owner of the asynchronous operation.</param>
        /// <returns>An object of type IAsyncResult that identifies the asynchronous operation.</returns>
        public static IAsyncResult Begin(Action action, AsyncCallback asyncCallback, object asyncState, object owner = null)
        {
            return new AsyncAction(action, asyncCallback, asyncState, owner).Begin();
        }

        /// <summary>
        /// Ends an asynchronous operation to execute a method or lambda that does not return a value.
        /// </summary>
        /// <param name="asyncResult">An object of type IAsyncResult that identifies the asynchronous operation.</param>
        /// <param name="owner">An optional object that represents the owner of the asynchronous operation.</param>
        public static void End(IAsyncResult asyncResult, object owner = null)
        {
            GetDelegate<AsyncAction>(asyncResult).End(owner);
        }

        /// <summary>
        /// Begins an asynchronous operation to execute a method or lambda that returns a value.
        /// </summary>
        /// <typeparam name="T">The type of value that the operation will return.</typeparam>
        /// <param name="func">The method or lambda to execute asynchronously.</param>
        /// <param name="asyncCallback">A callback method that is called when the asynchronous operation completes.</param>
        /// <param name="asyncState">A user-defined object that contains information about the asynchronous operation.</param>
        /// <param name="owner">An optional object that represents the owner of the asynchronous operation.</param>
        /// <returns>An object of type IAsyncResult that identifies the asynchronous operation.</returns>
        public static IAsyncResult Begin<T>(Func<T> func, AsyncCallback asyncCallback, object asyncState, object owner = null)
        {
            return new AsyncFunc<T>(func, asyncCallback, asyncState, owner).Begin();
        }

        /// <summary>
        /// Ends an asynchronous operation to execute a method or lambda that returns a value.
        /// </summary>
        /// <typeparam name="T">The type of value that the operation will return.</typeparam>
        /// <param name="asyncResult">An object of type IAsyncResult that identifies the asynchronous operation.</param>
        /// <param name="owner">An optional object that represents the owner of the asynchronous operation.</param>
        /// <returns>The return value from the asynchronous operation.</returns>
        public static T End<T>(IAsyncResult asyncResult, object owner = null)
        {
            return GetDelegate<AsyncFunc<T>>(asyncResult).End(owner);
        }

        private static T GetDelegate<T>(IAsyncResult asyncResult) where T : AsyncDelegate
        {
            var asyncDelegate = asyncResult as T;
            if (asyncDelegate == null)
            {
                throw new ArgumentException("Invalid async result", "asyncResult");
            }

            return asyncDelegate;
        }
    }
}
