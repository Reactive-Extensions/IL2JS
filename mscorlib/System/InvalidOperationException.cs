//
// Export constructor so runtime can throw exeption
//

using Microsoft.LiveLabs.JavaScript.Interop;

namespace System
{
    public class InvalidOperationException : SystemException
    {
        [Export("function(root, f) { root.InvalidOperationException = f; }", PassRootAsArgument = true)]
        public InvalidOperationException() : base("invalid operation exception")
        {
        }

        [Export("function(root, f) { root.InvalidOperationExceptionWithMessage = f; }", PassRootAsArgument = true)]
        public InvalidOperationException(string message) : base(message)
        {
        }

        public InvalidOperationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}