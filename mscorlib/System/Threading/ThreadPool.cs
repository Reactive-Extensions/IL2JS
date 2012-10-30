
namespace System.Threading
{
	public class ThreadPool
	{
        //public static bool BindHandle(SafeHandle osHandle) { throw new NotSupportedException(); }

        public static void GetMaxThreads(out int workerThreads, out int completionPortThreads) { throw new NotSupportedException(); }
        public static void GetMinThreads(out int workerThreads, out int completionPortThreads) { throw new NotSupportedException(); }
        public static bool QueueUserWorkItem(WaitCallback callBack) { throw new NotSupportedException(); }
        public static bool QueueUserWorkItem(WaitCallback callBack, object state) { throw new NotSupportedException(); }
        public static RegisteredWaitHandle RegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, object state, int millisecondsTimeOutInterval, bool executeOnlyOnce)
        {
            return RegisterWaitForSingleObject(waitObject, callBack, state, (long)millisecondsTimeOutInterval, executeOnlyOnce);
        }
        public static RegisteredWaitHandle RegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, object state, long millisecondsTimeOutInterval, bool executeOnlyOnce)
        {
            if (executeOnlyOnce != true)
            {
                throw new NotSupportedException("executeOnlyOnce must be true");
            }

            int tickTime = (int)Math.Min(100, millisecondsTimeOutInterval);

            int totalTimeElapsed = 0;
            Timer timer = null;

            timer = new Timer((s) =>
            {
                // Timer will fire every due period...if total time exceeds millisecondsTimeoutInterval then we call the timeoutcallback
                // otherwise we wait...if at any point the waitObject is signaled...than we can abort.
                if (waitObject.WaitOne(0))
                {
                    // Signal is set so no timeout occured
                    timer.Dispose();
                    return;
                }
                else if (totalTimeElapsed > millisecondsTimeOutInterval)
                {
                    // Timeout has occured
                    timer.Dispose();
                    callBack(state, true);
                }
                else
                {
                    totalTimeElapsed += tickTime;
                }
            }, null, tickTime, tickTime);

            return null;
        }

        public static RegisteredWaitHandle RegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, object state, TimeSpan timeout, bool executeOnlyOnce)
        {
            return RegisterWaitForSingleObject(waitObject, callBack, state, (long)timeout.TotalMilliseconds, executeOnlyOnce);
        }
        public static RegisteredWaitHandle RegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, object state, uint millisecondsTimeOutInterval, bool executeOnlyOnce)
        {
            return RegisterWaitForSingleObject(waitObject, callBack, state, (long)millisecondsTimeOutInterval, executeOnlyOnce);
        }
        public static bool SetMaxThreads(int workerThreads, int completionPortThreads) { throw new NotSupportedException(); }
        public static bool SetMinThreads(int workerThreads, int completionPortThreads) { throw new NotSupportedException(); }
        //public static bool UnsafeQueueNativeOverlapped(NativeOverlapped* overlapped);
	}
}
