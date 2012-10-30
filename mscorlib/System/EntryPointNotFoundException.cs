namespace System
{
    public class EntryPointNotFoundException : TypeLoadException
    {
        public EntryPointNotFoundException()
            : base("entry point not found exception")
        {
        }

        public EntryPointNotFoundException(string message)
            : base(message)
        {
        }

        public EntryPointNotFoundException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
