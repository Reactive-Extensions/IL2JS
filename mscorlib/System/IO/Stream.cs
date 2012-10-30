////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////

using Microsoft.LiveLabs.JavaScript.IL2JS;

namespace System.IO
{
    public abstract class Stream : IDisposable
    {
        // Methods
        protected Stream()
        {
        }

        public virtual IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) { throw new NotSupportedException(); }
        public virtual IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) { throw new NotSupportedException(); }

        public virtual void Close()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void CopyTo(Stream destination) { throw new NotSupportedException(); }

        public void CopyTo(Stream destination, int bufferSize) { throw new NotSupportedException(); }


        public void Dispose()
        {
            this.Close();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }

        public virtual int EndRead(IAsyncResult asyncResult) { throw new NotSupportedException(); }

        public virtual void EndWrite(IAsyncResult asyncResult) { throw new NotSupportedException(); }
        public abstract void Flush();

        protected virtual void ObjectInvariant()
        {
        }

        public abstract int Read(byte[] buffer, int offset, int count);
        public virtual int ReadByte() { throw new NotSupportedException(); }

        public abstract long Seek(long offset, SeekOrigin origin);
        public abstract void SetLength(long value);
        public abstract void Write(byte[] buffer, int offset, int count);
        public virtual void WriteByte(byte value) { throw new NotSupportedException(); }

        // Properties
        public abstract bool CanRead { get; }

        public abstract bool CanSeek { get; }

        public virtual bool CanTimeout
        {
            get
            {
                return false;
            }
        }

        public abstract bool CanWrite { get; }

        public abstract long Length { get; }

        public abstract long Position { get; set; }

        public virtual int ReadTimeout
        {
            get
        {
            throw new InvalidOperationException("TimeoutsNotSupported");
        }
            set
        {
            throw new InvalidOperationException("TimeoutsNotSupported");
        }
        }

        public virtual int WriteTimeout
        {
            get
        {
            throw new InvalidOperationException("TimeoutsNotSupported");
        }
            set
        {
            throw new InvalidOperationException("TimeoutsNotSupported");
        }
        }
    }
}
