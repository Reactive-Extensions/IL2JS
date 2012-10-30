//
// Export constructor so runtime can throw exeption
//

using Microsoft.LiveLabs.JavaScript.Interop;

namespace System
{
    public class InvalidCastException : SystemException
    {
        [Export("function(root, f) { root.InvalidCastException = f; }", PassRootAsArgument = true)]
        public InvalidCastException() : base("invalid cast exception")
        {
        }

        public InvalidCastException(string message) : base(message)
        {
        }

        public InvalidCastException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}