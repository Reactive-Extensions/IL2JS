////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////

using System;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace System.Windows.Threading
{
    public class DispatcherTimer
    {
        // Fields
        private bool isEnabled;
        private TimeSpan interval;
        private int intervalId;

        // Events
        public event EventHandler Tick;

        // Methods
        public DispatcherTimer()
        {
            Interval = new TimeSpan(0L);
        }

        [Import("function(callback, msec) { return window.setInterval(callback, msec); }")]
        extern private static int SetInterval(Action callback, int msec);

        [Import("function(id) { window.clearInterval(id); }")]
        extern private static void ClearInterval(int id);

        public void Start()
        {
            isEnabled = true;
            intervalId = SetInterval(OnInterval, (int)interval.TotalMilliseconds);
        }

        public void Stop()
        {
            if (isEnabled)
            {
                ClearInterval(intervalId);
                intervalId = 0;
                isEnabled = false;
            }
        }

        private void OnInterval()
        {
            if (Tick != null)
            {
                Tick(this, EventArgs.Empty);
            }
        }

        // Properties
        public TimeSpan Interval
        {
            get
            {
                return interval;
            }
            set
            {
                if(interval.TotalMilliseconds > int.MaxValue || interval.TotalMilliseconds < 0)
                {
                    throw new ArgumentOutOfRangeException();
                }
                interval = value;
            }
        }

        public bool IsEnabled
        {
            get
            {
                return isEnabled;
            }
        }
    }
}
