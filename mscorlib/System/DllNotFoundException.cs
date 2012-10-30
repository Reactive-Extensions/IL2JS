namespace System
{
    public class DllNotFoundException : TypeLoadException
    {
        public DllNotFoundException() : base("dll not found exception")
        {
        }

        public DllNotFoundException(string message) : base(message)
        {
        }

        public DllNotFoundException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
