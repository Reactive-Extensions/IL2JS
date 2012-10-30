using System;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace System.Threading
{
    public sealed class Timer : IDisposable
    {
        [Import("function(callback, msec) { return window.setInterval(callback, msec); }")]
        extern private static int SetInterval(Action callback, int msec);

        [Import("function(id) { window.clearInterval(id); }")]
        extern private static void ClearInterval(int id);

        private const uint MAX_SUPPORTED_TIMEOUT = 0xfffffffe;
        private const int INFINITE_TIMEOUT = -1;

        private int dueTime; // Time to wait before first execution
        private int periodTime; // Time to wait for subsequent executions
        private TimerCallback callback;
        private object state;
        private bool firedOnce = false;
        private bool isDisposed;
        private int intervalId;

        public Timer(TimerCallback callback)
            : this(callback, null, (int)INFINITE_TIMEOUT, (int)INFINITE_TIMEOUT)
        {
        }

        public Timer(TimerCallback callback, object state, int dueTime, int period)
        {
            this.callback = callback;
            this.state = state;

            Change(dueTime, period);
        }

        public Timer(TimerCallback callback, object state, long dueTime, long period)
            : this(callback, state, (int)dueTime, (int)period)
        {
        }

        public Timer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
            : this(callback, state, (int)dueTime.TotalMilliseconds, (int)period.TotalMilliseconds)
        {
        }

        public Timer(TimerCallback callback, object state, uint dueTime, uint period)
            : this(callback, state, (int)dueTime, (int)period)
        {
        }

        public bool Change(int dueTime, int period)
        {
            if(isDisposed)
            {
                return false;
            }

            firedOnce = false;

            this.dueTime = dueTime;
            this.periodTime = period;

            if (dueTime == INFINITE_TIMEOUT)
            {
                // No timer when dueTime is infinite
                return true;
            }

            intervalId = SetInterval(OnInterval, dueTime);
            return true;
        }

        public bool Change(long dueTime, long period)
        {
            return this.Change((int)dueTime, (int)period);
        }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            return this.Change((int)dueTime.TotalMilliseconds, (int)period.TotalMilliseconds);
        }

        public bool Change(uint dueTime, uint period)
        {
            return Change((int)dueTime, (int)period);
        }

        private void OnInterval()
        {
            ClearInterval(intervalId);
            intervalId = 0;

            if (!firedOnce)
            {
                // Due expired...now try for period
                if (periodTime == INFINITE_TIMEOUT)
                {
                    // Do nothing...period timeout is infinite
                }
                else
                {
                    intervalId = SetInterval(OnInterval, periodTime);
                }
                firedOnce = true;
            }
            else
            {
                // If already firedOnce...period timer is in play and no need to adjust
            }

            // Fire callback
            if (callback != null)
            {
                callback(state);
            }
        }

        public void Dispose()
        {
            ClearInterval(intervalId);
        }

        public bool Dispose(WaitHandle notifyObject)
        {
            Dispose();
            return false;
        }
    }
}
