namespace System
{
    public class SystemException : Exception
    {
        public SystemException() : base("system exception")
        {
        }

        public SystemException(string message)
            : base(message)
        {
        }

        public SystemException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
