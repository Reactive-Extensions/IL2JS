//
// Export constructor so runtime can throw exeption
//

using Microsoft.LiveLabs.JavaScript.Interop;

namespace System
{
    public class ArithmeticException : SystemException
    {
        [Export("function(root, f) { root.ArithmeticException = f; }", PassRootAsArgument = true)]
        public ArithmeticException()
            : base("arithmetic exception")
        {
        }

        public ArithmeticException(string message)
            : base(message)
        {
        }

        public ArithmeticException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
