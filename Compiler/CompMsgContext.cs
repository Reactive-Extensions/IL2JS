//
// Context for a user message
//

using System;
using Microsoft.LiveLabs.Extras;
using CST = Microsoft.LiveLabs.CST;

namespace Microsoft.LiveLabs.JavaScript.IL2JS
{
    public static class CompMsgContext
    {
        public static MessageContext TraceFile(Location loc)
        {
            return new MessageContext(null, loc, sb => { });
        }
    }
}
