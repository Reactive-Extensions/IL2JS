//
// Export constructor so runtime can throw exeption
//

using Microsoft.LiveLabs.JavaScript.Interop;

namespace System
{
    public class ArgumentException : SystemException
    {
        private string paramName;

        [Export("function(root, f) { root.ArgumentException = f; }", PassRootAsArgument = true)]
        public ArgumentException()
            : base("argument exception")
        {
        }

        public ArgumentException(string message) : base(message)
        {
        }

        public ArgumentException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public ArgumentException(string message, string paramName) : base(message)
        {
            this.paramName = paramName;
        }

        public override string Message
        {
            get
            {
                var message = base.Message;
                if (!string.IsNullOrEmpty(paramName))
                    return message + Environment.NewLine + "param: " + paramName;
                else
                    return message;
            }
        }
    }
}