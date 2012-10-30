//
// Base type for user-implemented pages
//

namespace Microsoft.LiveLabs.Html
{
    public abstract class Page
    {
        protected Page() { }

        public Document Document
        {
            get { return Browser.Document; }
        }

        public Window Window
        {
            get { return Browser.Window; }
        }
    }
}