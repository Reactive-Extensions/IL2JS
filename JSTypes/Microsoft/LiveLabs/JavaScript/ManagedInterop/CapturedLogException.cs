using System;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop
{
    public class CapturedLogException : Exception
    {
        public string Log { get; private set; }

        public CapturedLogException()
            : base("captured log exception")
        {
            Log = "<no log>";
        }

        public CapturedLogException(string message)
            : base("message")
        {
            Log = "<no log>";
        }

        public CapturedLogException(string message, Exception innerException)
            : base(message, innerException)
        {
            Log = "<no log>";
        }

        public CapturedLogException(Runtime runtime)
        {
            Log = runtime.CurrentLog;
        }

        public override string Message
        {
            get
            {
                return Log;
            }
        }
    }
}