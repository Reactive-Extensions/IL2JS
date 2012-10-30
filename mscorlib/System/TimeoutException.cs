namespace System
{
    public class TimeoutException : SystemException
    {
        public TimeoutException()
            : base("timeout exception")
        {
        }

        public TimeoutException(string message)
            : base(message)
        {
        }

        public TimeoutException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
