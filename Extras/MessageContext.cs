//
// Context for a user message
//

using System;
using System.Text;

namespace Microsoft.LiveLabs.Extras
{
    public class MessageContext
    {
        public readonly MessageContext Parent;
        public readonly Location Loc;
        private readonly Action<StringBuilder> append;

        public MessageContext(MessageContext parent, Location loc, Action<StringBuilder> append)
        {
            Parent = parent;
            Loc = loc;
            this.append = append;
        }

        public Location BestLoc
        {
            get
            {
                if (Loc != null)
                    return Loc;
                else if (Parent != null)
                    return Parent.BestLoc;
                else
                    return null;
            }
        }

        public void Append(StringBuilder sb)
        {
            if (Parent != null)
            {
                Parent.Append(sb);
                if (append != null)
                {
                    sb.Append('/');
                    append(sb);
                }
            }
            else if (append != null)
                append(sb);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            Append(sb);
            return sb.ToString();
        }
    }
}
