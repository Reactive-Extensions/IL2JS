//
// Our own type for representing (x, y) offsets
//

namespace Microsoft.LiveLabs.Html
{
    public class OffsetPosition
    {
        public OffsetPosition(int left, int top)
        {
            Left = left;
            Top = top;
        }

        public int Left
        {
            get;
            private set;
        }

        public int Top
        {
            get;
            private set;
        }
    }
}