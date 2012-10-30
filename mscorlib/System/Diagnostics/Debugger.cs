//
// A re-implementation of Debugger for the JavaScript runtime.
//

using Microsoft.LiveLabs.JavaScript.Interop;

namespace System.Diagnostics
{
    public static class Debugger
    {
        [Import("function() { debugger; }")]
        public extern static void Break();

        public static bool IsLogging()
        {
            return true;
        }

        [Import("function() { debugger; }")]
        public extern static void Launch();

        public static void Log(int level, string category, string message)
        {
            Console.WriteLine(level + ": " + category + ": " + message);
        }
    }
}