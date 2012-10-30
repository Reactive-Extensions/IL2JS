namespace System
{
    public class OverflowException : ArithmeticException
    {
        public OverflowException()
            : base("overflow exception")
        {
        }

        public OverflowException(string message)
            : base(message)
        {
        }

        public OverflowException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
