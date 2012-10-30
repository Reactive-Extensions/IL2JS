namespace System
{
    public class FormatException : SystemException
    {
        public FormatException() : base("format exception")
        {
        }

        public FormatException(string message) : base(message)
        {
        }

        public FormatException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
