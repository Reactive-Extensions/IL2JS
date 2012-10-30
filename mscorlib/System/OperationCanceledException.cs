namespace System
{
    internal class OperationCanceledException : SystemException
    {
        public OperationCanceledException() : base("operation canceled exception")
        {
        }
    }
}
