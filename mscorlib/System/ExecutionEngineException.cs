namespace System
{
    public sealed class ExecutionEngineException : SystemException
    {
        internal ExecutionEngineException() : base("execution engine exception")
        {
        }

        internal ExecutionEngineException(string message) : base(message)
        {
        }
    }
}
