//
// Export constructor so runtime can throw exeption
//

using Microsoft.LiveLabs.JavaScript.Interop;

namespace System
{
    public class NotSupportedException : SystemException
    {
        [Export("function(root, f) { root.NotSupportedException = f; }", PassRootAsArgument = true)]
        public NotSupportedException() : base("not supported exception")
        {
        }

        [Export("function(root, f) { root.NotSupportedExceptionWithMessage = f; }", PassRootAsArgument = true)]
        public NotSupportedException(string message)
            : base(message)
        {
        }

        public NotSupportedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}