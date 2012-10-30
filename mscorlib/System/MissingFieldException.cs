namespace System
{
    public class MissingFieldException : MissingMemberException
    {
        public MissingFieldException()
            : base("missing field exception")
        {
        }

        public MissingFieldException(string message)
            : base(message)
        {
        }

        public MissingFieldException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
        
