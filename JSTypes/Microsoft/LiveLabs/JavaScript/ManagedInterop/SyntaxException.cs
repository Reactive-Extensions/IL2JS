using System;

namespace Microsoft.LiveLabs.JavaScript.JST
{

    public class SyntaxException : Exception
    {
        public SyntaxException(string dummy, string context, string description) :
            base("Invalid interop exchange syntax: " + context + (description == null ? "" : (": " + description)))
        {
        }
    }
}
