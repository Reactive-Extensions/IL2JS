//
// A JavaScript syntax error
//

using System;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.JavaScript.JST
{

    public class SyntaxException : Exception
    {
        public Location Loc;
        public string Context;
        public string Details;

        public SyntaxException(Location loc, string context, string details)
        {
            Loc = loc;
            Context = context;
            Details = details;
        }

        public override string Message
        {
            get
            {
                return String.Format("{0}: syntax error in {1}: {2}", Loc, Context, Details);
            }
        }
    }

}