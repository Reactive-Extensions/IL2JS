namespace System
{
    public class MemberAccessException : SystemException
    {
        public MemberAccessException()
            : base("member access exception")
        {
        }

        public MemberAccessException(string message)
            : base(message)
        {
        }

        public MemberAccessException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
