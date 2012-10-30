namespace System.Reflection
{
    public sealed class TargetParameterCountException : Exception
    {
        public TargetParameterCountException()
            : base("target parameter count exception")
        {
        }

        public TargetParameterCountException(string message)
            : base(message)
        {
        }

        public TargetParameterCountException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
