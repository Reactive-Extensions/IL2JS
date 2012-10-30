using Microsoft.LiveLabs.JavaScript.Interop;

namespace System.Diagnostics
{
    public static class Debug
    {
        public static void Assert(bool condition)
        {
            if (!condition)
                throw new InvalidOperationException();
        }

        public static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        public static void Assert(bool condition, string message, string detailMessage)
        {
            if (!condition)
                throw new InvalidOperationException(message + ": " + detailMessage);
        }

        public static void Assert(bool condition, string message, string detailMessageFormat, params object[] args)
        {
            if (!condition)
            {
                string detail = string.Format(detailMessageFormat, args);
                throw new InvalidOperationException(message + ": " + detail);
            }
        }

        public static void WriteLine(object value)
        {
            if(value == null)
            {
                return;
            }
            WriteLine(value.ToString());
        }

        [Import(@"function(root, value){ root.WriteLine(value); }", PassRootAsArgument = true)]
        extern public static void WriteLine(string value);

        public static void WriteLine(string format, Object[] args)
        {
            WriteLine(string.Format(format, args));
        }
    }
}