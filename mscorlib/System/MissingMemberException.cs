namespace System
{
    public class MissingMemberException : MemberAccessException
    {
        public MissingMemberException() : base("missing member exception")
        {
        }

        public MissingMemberException(string message) : base(message)
        {
        }

        public MissingMemberException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
