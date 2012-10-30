namespace System
{
    public class TypeLoadException : SystemException
    {
        public TypeLoadException()
            : base("type load exception")
        {
        }

        public TypeLoadException(string message)
            : base(message)
        {
        }

        public TypeLoadException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
