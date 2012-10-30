////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////

using Microsoft.LiveLabs.JavaScript.IL2JS;
using System.Text;

namespace System.IO
{
    /// <summary>
    /// Parses URI formats.
    /// </summary>
    public class MemoryStream : Stream
    {
        internal StringBuilder StringBuilder { get; private set; }

        public MemoryStream() 
        {
            this.StringBuilder = new StringBuilder();
        }

        public MemoryStream(byte[] buffer) { throw new NotSupportedException(); }
        public MemoryStream(byte[] buffer, bool writable) { throw new NotSupportedException(); }
        public MemoryStream(byte[] buffer, int index, int count) { throw new NotSupportedException(); }
        public MemoryStream(byte[] buffer, int index, int count, bool writeable) { throw new NotSupportedException(); }
        public MemoryStream(byte[] buffer, int index, int count, bool writeable, bool publiclyVisible) { throw new NotSupportedException(); }

        public virtual byte[] ToArray() { throw new NotSupportedException(); }

        public override void Close() { }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            // NOOOP...We don't really keep track of position here
            return 0;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead
        {
            get { throw new NotImplementedException(); }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { throw new NotImplementedException(); }
        }

        public override long Length
        {
            get { throw new NotImplementedException(); }
        }

        public override long Position
        {
            get
            {
                return 0;
            }
            set
            {
                // NO OP
            }
        }
    }
}
