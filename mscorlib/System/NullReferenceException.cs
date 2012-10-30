//
// Export constructor so runtime can throw exeption
//

using Microsoft.LiveLabs.JavaScript.Interop;

namespace System
{
    public class NullReferenceException : SystemException
    {
        [Export("function(root, f) { root.NullReferenceException = f; }", PassRootAsArgument = true)]
        public NullReferenceException()
            : base("null reference exception")
        {
        }

        public NullReferenceException(string message)
            : base(message)
        {
        }

        public NullReferenceException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}