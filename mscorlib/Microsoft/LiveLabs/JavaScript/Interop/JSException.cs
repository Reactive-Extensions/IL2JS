//
// Exception to represent an unmanaged JavaScript exception
// NOTE: Appears in both IL2JS's mscorlib and CLR's JSTypes assemblies
//

using System;
#if IL2JS
using Microsoft.LiveLabs.JavaScript.IL2JS;
#endif

namespace Microsoft.LiveLabs.JavaScript.Interop
{
#if IL2JS
    [UsedType(true)]
#endif
    public class JSException : Exception
    {
        public JSObject UnderlyingException { get; private set; }

#if IL2JS
        [Export("function(root, f) { root.GetUnderlyingException = f; }", PassRootAsArgument = true)]
        private static JSObject GetUnderlyingException(JSException instance)
        {
            return instance.UnderlyingException;
        }
#endif

        private static string BestMessage(JSObject obj)
        {
            var err = obj as JSError;
            if (err != null)
                return err.Message;
            else
                return obj.ToString();
        }

#if IL2JS
        [Export("function(root, f) { root.JSException = f; }", PassRootAsArgument = true)]
#endif
        public JSException(JSObject underlyingException) : base("JavaScript execution exception: " + BestMessage(underlyingException))
        {
            UnderlyingException = underlyingException;
        }

        public JSException(JSObject underlyingException, Exception innerException)
            : base("JavaScript execution exception", innerException)
        {
            UnderlyingException = underlyingException;
        }

        public override string Message
        {
            get
            {
                if (UnderlyingException == null)
                    return base.Message;
                else
                    return "JavaScript execution exception: " + BestMessage(UnderlyingException);
            }
        }
    }
}