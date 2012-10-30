namespace System
{
    public class NotImplementedException : SystemException
    {
        public NotImplementedException() : base("not implemented exception")
        {
        }

        public NotImplementedException(string message) : base(message)
        {
        }

        public NotImplementedException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
