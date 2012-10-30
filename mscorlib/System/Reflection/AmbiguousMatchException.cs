namespace System.Reflection
{
    public sealed class AmbiguousMatchException : SystemException
    {
        public AmbiguousMatchException()
            : base("ambiguous match exception")
        {
        }

        public AmbiguousMatchException(string message)
            : base(message)
        {
        }

        public AmbiguousMatchException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
