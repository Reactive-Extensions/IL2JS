//
// Export constructor so runtime can throw exeption
//

using Microsoft.LiveLabs.JavaScript.Interop;

namespace System
{
    public sealed class IndexOutOfRangeException : SystemException
    {
        [Export("function(root, f) { root.IndexOutOfRangeException = f; }", PassRootAsArgument = true)]
        public IndexOutOfRangeException()
            : base("index out of range exception")
        {
        }

        public IndexOutOfRangeException(string message)
            : base(message)
        {
        }

        public IndexOutOfRangeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}