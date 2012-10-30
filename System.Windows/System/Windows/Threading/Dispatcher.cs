namespace System.Windows.Threading
{
    using Microsoft.LiveLabs.JavaScript.IL2JS;

    public sealed class Dispatcher
    {
        public DispatcherOperation BeginInvoke(System.Action a)
        {
            throw new Exception("Not supported");
        }

        public DispatcherOperation BeginInvoke(Delegate d, params object[] args)
        {
            throw new Exception("Not supported");
        }

        /// <summary>
        /// Determines whether the calling thread is the thread associated with this
        //  System.Windows.Threading.Dispatcher
        /// </summary>
        /// <returns></returns>
        public bool CheckAccess()
        {
            return true;
        }

        /// <summary>
        /// Determines whether the calling thread has access to this System.Windows.Threading.Dispatcher.
        /// </summary>
        public void VerifyAccess()
        {
            // NO OP
        }
    }
}
