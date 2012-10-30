namespace System
{
    public sealed class StackOverflowException : SystemException
    {
        public StackOverflowException() : base("stack overflow exception")
        {
        }

        public StackOverflowException(string message) : base(message)
        {
        }

        public StackOverflowException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
