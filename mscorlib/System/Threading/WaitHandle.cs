////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////

namespace System.Threading
{
    public abstract class WaitHandle : IDisposable
    {
        protected bool isSet;
        protected EventResetMode resetMode;

        // Methods
        protected WaitHandle()
        {
            isSet = false;
        }

        public virtual void Close()
        {
        }

        public void Dispose()
        {         
        }

        public virtual bool WaitOne()
        {
            return this.WaitOne(-1, false);
        }

        public virtual bool WaitOne(int millisecondsTimeout)
        {
            return this.WaitOne(millisecondsTimeout, false);
        }

        public virtual bool WaitOne(TimeSpan timeout)
        {
            return this.WaitOne(timeout, false);
        }

        public virtual bool WaitOne(int millisecondsTimeout, bool exitContext)
        {
            if (millisecondsTimeout < -1)
            {
                throw new ArgumentOutOfRangeException("millisecondsTimeout");
            }
            return this.WaitOne((long)millisecondsTimeout, exitContext);
        }

        private bool WaitOne(long timeout, bool exitContext)
        {
            if(isSet)
            {
                if (resetMode == EventResetMode.AutoReset)
                {
                    isSet = false;
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public virtual bool WaitOne(TimeSpan timeout, bool exitContext)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if ((-1L > totalMilliseconds) || (0x7fffffffL < totalMilliseconds))
            {
                throw new ArgumentOutOfRangeException("timeout");
            }
            return this.WaitOne(totalMilliseconds, exitContext);
        }

        // Properties
        public virtual IntPtr Handle
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }
    }
}
