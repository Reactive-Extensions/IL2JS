using Microsoft.LiveLabs.JavaScript.IL2JS;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace System
{
    [UsedType(true)]
    [Reflection(ReflectionLevel.Names)]
    public class Exception
    {
        private readonly string message;
        private readonly Exception innerException;
        private string className;

        public Exception()
        {
        }

        public Exception(string message)
        {
            this.message = message;
        }

        public Exception(string message, Exception innerException)
        {
            this.message = message;
            this.innerException = innerException;
        }

        [Export("function(root, f) { root.GetExceptionMessage = f; }", PassRootAsArgument = true)]
        private static string GetExceptionMessage(Exception inst)
        {
            return inst.message;
        }

        public virtual string Message
        {
            get { return message; }
        }

        public virtual Exception InnerException { get { return innerException; } }

        private string GetClassName()
        {
            if (className == null)
                className = GetType().Name;
            return className;
        }

        public virtual Exception GetBaseException()
        {
            var inner = InnerException;
            var current = this;
            while (inner != null)
            {
                current = inner;
                inner = inner.InnerException;
            }
            return current;
        }

        public override string ToString()
        {
            var result = default(string);
            if (string.IsNullOrEmpty(message))
                result = GetClassName();
            else
                result = GetClassName() + ": " + message;
            if (innerException != null)
                result = result + " ---> " + innerException.ToString() + Environment.NewLine + "   " +
                         "--- End of inner exception stack trace ---";
            return result;
        }

        // NOTE: Must retain since compiled code may refer to it
        public new Type GetType()
        {
            return base.GetType();
        }
    }
}
