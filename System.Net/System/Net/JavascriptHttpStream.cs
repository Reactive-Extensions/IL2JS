using System;
using System.Net;
using System.IO;
using System.Text;

namespace System.Net
{
	public class JavascriptHttpStream : MemoryStream
	{
        private StringBuilder sb;
        private bool opened = false;
        private Action<Stream> closedCallback;

        public JavascriptHttpStream(Action<Stream> onClosed)
        {
            this.opened = true;
            this.closedCallback = onClosed;
            this.sb = new StringBuilder();
        }

        public override void Close()
        {
            if (!opened)
            {
                return;
            }

            this.opened = false;
            if (this.closedCallback != null)            
            {
                closedCallback(this);
            }
        }

        public void Write(string content)
        {
            sb.Append(content);
        }

        public override string ToString()
        {
            return sb.ToString();
        }
	}
}
