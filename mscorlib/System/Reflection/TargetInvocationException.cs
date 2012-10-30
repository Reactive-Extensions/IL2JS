namespace System.Reflection
{
    public sealed class TargetInvocationException : Exception
    {
        public TargetInvocationException(Exception inner) : base("target invocation exception", inner)
        {
        }

        public TargetInvocationException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
