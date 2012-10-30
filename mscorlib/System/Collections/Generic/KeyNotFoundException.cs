namespace System.Collections.Generic
{
    public class KeyNotFoundException : SystemException
    {
        public KeyNotFoundException() : base("key not found exception")
        {
        }

        public KeyNotFoundException(string message)
            : base(message)
        {
        }

        public KeyNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
