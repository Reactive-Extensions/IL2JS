namespace System
{
    public class ArgumentNullException : ArgumentException
    {
        public ArgumentNullException()
            : base("argument null exception")
        {
        }

        public ArgumentNullException(string paramName)
            : base("argument null exception", paramName)
        {
        }

        public ArgumentNullException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public ArgumentNullException(string paramName, string message)
            : base(paramName, message)
        {
        }
    }
}
