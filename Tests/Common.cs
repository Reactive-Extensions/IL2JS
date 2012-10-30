using System;
#if IL2JS || SILVERLIGHT || WSH
using Microsoft.LiveLabs.JavaScript.Interop;
#endif

namespace Microsoft.LiveLabs.JavaScript.Tests
{
    public static class TestLogger
    {
#if SILVERLIGHT
        [Import(@"function(message) {
                      var tt = document.createElement('tt');
                      tt.innerText = message.replace(new RegExp('/</g'), '&lt;');
                      var div = document.createElement('div');
                      div.appendChild(tt);
                      document.body.appendChild(div);
                  }")]
        extern public static void Log(string message);
#else
        public static void Log(string message)
        {
            Console.WriteLine(message);
        }
#endif

        public static void Log(int message)
        {
            Log(message.ToString());
        }

        public static void Log(double message)
        {
            Log(message.ToString());
        }

        public static void Log(bool message)
        {
            Log(message.ToString());
        }

        public static void LogException(Exception e)
        {
            Log("Exception: " + e.GetType().FullName);
        }

        public static void Assert(bool b)
        {
            if (!b)
            {
                throw new InvalidOperationException("assertion failed");
            }
        }
    }
}
