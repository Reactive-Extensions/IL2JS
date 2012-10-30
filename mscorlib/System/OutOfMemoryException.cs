namespace System
{
    public class OutOfMemoryException : SystemException
    {
        public OutOfMemoryException()
            : base("out of memory exception")
        {
        }

        public OutOfMemoryException(string message)
            : base(message)
        {
        }

        public OutOfMemoryException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
