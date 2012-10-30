namespace System
{
    public class DivideByZeroException : ArithmeticException
    {
        public DivideByZeroException() : base("divide by zero exception")
        {
        }

        public DivideByZeroException(string message) : base(message)
        {
        }

        public DivideByZeroException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
