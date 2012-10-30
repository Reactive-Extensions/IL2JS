namespace System
{
    public class MissingMethodException : MissingMemberException
    {
        public MissingMethodException() : base("missing method exception")
        {
        }

        public MissingMethodException(string message) : base(message)
        {
        }

        public MissingMethodException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
