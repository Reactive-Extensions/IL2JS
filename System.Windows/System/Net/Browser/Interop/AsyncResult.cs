////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////


// <copyright file="AsyncResult.cs" company="Microsoft">
// From Jeffrey Richters MSDN Article on Async programming
// </copyright>
namespace System.Net.Interop
{
    using System;

    /// <summary>
    /// From Jeffrey Richters MSDN Article on Async programming
    /// </summary>
    internal class AsyncResult<TResult> : AsyncResultNoResult
    {
        // Field set when operation completes
        private TResult result = default(TResult);

        public AsyncResult(AsyncCallback asyncCallback, object state) :
            base(asyncCallback, state)
        {
        }

        public void SetAsCompleted(TResult result, bool completedSynchronously)
        {
            // Save the asynchronous operation's result
            this.result = result;

            // Tell the base class that the operation completed 
            // sucessfully (no exception)
            base.SetAsCompleted(null, completedSynchronously);
        }

        public new TResult EndInvoke()
        {
            base.EndInvoke(); // Wait until operation has completed 
            return this.result;  // Return the result (if above didn't throw)
        }
    }
}
