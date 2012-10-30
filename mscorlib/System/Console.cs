using System.Text;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace System
{
    public static class Console
    {
        private static StringBuilder pending;

        static Console()
        {
            pending = new StringBuilder();
        }

        public static void WriteLine()
        {
            WriteLine("");
        }

        public static void WriteLine(char value)
        {
            WriteLine(new String(value, 1));
        }

        public static void WriteLine(char[] buffer)
        {
            WriteLine(new string(buffer));
        }

        public static void WriteLine(int value)
        {
            WriteLine(value.ToString());
        }

        public static void WriteLine(object value)
        {
            WriteLine(value.ToString());
        }

        public static void WriteLine(string format, object arg0)
        {
            WriteLine(String.Format(format, arg0));
        }

        public static void WriteLine(string format, object arg0, object arg1)
        {
            WriteLine(String.Format(format, arg0, arg1));
        }

        public static void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            WriteLine(String.Format(format, arg0, arg1, arg2));
        }

        public static void WriteLine(string format, params object[] arg)
        {
            WriteLine(String.Format(format, arg));
        }

        public static void WriteLine(string value)
        {
            pending.Append(value);
            PrimWriteLine(pending.ToString());
            pending = new StringBuilder();
        }

        public static void Write(string format, object arg0)
        {
            Write(String.Format(format, arg0));
        }

        public static void Write(string format, object arg0, object arg1)
        {
            Write(String.Format(format, arg0, arg1));
        }

        public static void Write(string format, object arg0, object arg1, object arg2)
        {
            Write(String.Format(format, arg0, arg1, arg2));
        }

        public static void Write(string format, object[] arg)
        {
            Write(String.Format(format, arg));
        }

        public static void Write(char value)
        {
            Write(new String(value, 1));
        }

        public static void Write(char[] buffer)
        {
            Write(new String(buffer));
        }

        public static void Write(int value)
        {
            Write(value.ToString());
        }

        public static void Write(object value)
        {
            Write(value.ToString());
        }

        public static void Write(string value)
        {
            pending.Append(value);
        }

        [Import("function(root, value) { root.WriteLine(value); }", PassRootAsArgument = true)]
        extern private static void PrimWriteLine(string value);
    }
}
