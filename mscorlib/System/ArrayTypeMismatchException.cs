//
// Export constructor so runtime can throw exeption
//

using Microsoft.LiveLabs.JavaScript.Interop;

namespace System
{
    public class ArrayTypeMismatchException : SystemException
    {
        [Export("function(root, f) { root.ArrayTypeMismatchException = f; }", PassRootAsArgument = true)]
        public ArrayTypeMismatchException()
            : base("array type mismatch exception")
        {
        }

        public ArrayTypeMismatchException(string message)
            : base(message)
        {
        }

        public ArrayTypeMismatchException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}