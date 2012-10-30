using System;

namespace System
{
    /// <summary>
    /// A dummy class that implements GetResourceString. Environment.GetResourceString(string) is an internal
    /// method in mscorlib.dll.
    /// </summary>
    internal static class Environment2
    {
        internal static string GetResourceString(string key)
        {
            // A special case, so that at least AggregateException.ToString() works properly.
            if (key == "AggregateException_ToString") return "{0}{1}---> (Inner Exception #{2}) {3}{4}{5}";

            return key;
        }
    }
}
