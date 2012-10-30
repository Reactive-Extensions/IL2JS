//
// Export constructor so runtime can throw exeption
//

using Microsoft.LiveLabs.JavaScript.Interop;

namespace System.Reflection
{
    public class TargetException : Exception
    {
        [Export("function(root, f) { root.TargetException = f; }", PassRootAsArgument = true)]
        internal TargetException() : base("invalid target of invocation")
        {
        }

        internal TargetException(string message) : base(message)
        {
        }

        internal TargetException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
