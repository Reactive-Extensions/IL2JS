using Microsoft.LiveLabs.JavaScript.IL2JS;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace System
{
    [UsedType(true)]
    [Runtime(true)]
    internal class DelegateAsyncResult : IAsyncResult
    {
        private readonly bool isAsync;
        private readonly object asyncState;
        private bool completed;
        private object result;
        private Exception exception;

        [Export("function(root, f) { root.DelegateAsyncResult = f; }", PassRootAsArgument = true)]
        internal DelegateAsyncResult(bool isAsync, object asyncState)
        {
            this.isAsync = isAsync;
            this.asyncState = asyncState;
        }

        public bool IsCompleted
        {
            get { return completed; }
        }

        public object AsyncState
        {
            get { return asyncState; }
        }

        public bool CompletedSynchronously
        {
            get { return completed && !isAsync; }
        }

        [NoInterop(true)]
        [Export("function(root, f) { root.SetDelegateAsyncResult = f; }", PassRootAsArgument = true)]
        internal static void SetResult(DelegateAsyncResult inst, object result)
        {
            inst.completed = true;
            inst.result = result;
        }

        [NoInterop(true)]
        [Export("function(root, f) { root.SetDelegateAsyncException = f; }", PassRootAsArgument = true)]
        internal static void SetException(DelegateAsyncResult inst, Exception exception)
        {
            inst.completed = true;
            inst.exception = exception;
        }

        [NoInterop(true)]
        [Export("function(root, f) { root.GetDelegateAsyncResult = f; }", PassRootAsArgument = true)]
        internal static object GetResult(DelegateAsyncResult inst)
        {
            if (inst.exception != null)
                throw inst.exception;
            else
                return inst.result;
        }

        // PROVISIONAL
        public Threading.WaitHandle AsyncWaitHandle
        {
            get { throw new NotSupportedException(); }
        }
    }
}