namespace System
{
    internal sealed class InsufficientMemoryException : OutOfMemoryException
    {
        public InsufficientMemoryException() : base("insufficient memory exception")
        {
        }
    }
}
