using System;
using System.IO;
using System.Text;

namespace System.IO
{
	internal class JScriptStream : MemoryStream
	{
        private StringBuilder sb;
        private bool opened = false;

        internal JScriptStream()
        {
            this.opened = true;            
            this.sb = new StringBuilder();
        }

        public override void Close()
        {
            if (!opened)
            {
                return;
            }
            this.opened = false;
        }

        internal void Write(string content)
        {
            sb.Append(content);
        }

        internal string Read()
        {
            return sb.ToString();
        }
	}
}
