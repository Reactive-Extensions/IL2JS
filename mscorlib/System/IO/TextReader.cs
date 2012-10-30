////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////

namespace System.IO
{
    public abstract class TextReader : IDisposable
    {
        protected TextReader()
        {
        }

        public virtual void Close()
        {
            this.Dispose(true);
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public virtual int Peek()
        {
            return -1;
        }

        public virtual int Read()
        {
            return -1;
        }

        public virtual int Read(char[] buffer, int index, int count)
        {
            throw new NotImplementedException();
        }

        public virtual int ReadBlock(char[] buffer, int index, int count)
        {
            throw new NotImplementedException();
        }

        public virtual string ReadLine()
        {
            throw new NotImplementedException();
        }

        public virtual string ReadToEnd()
        {
            throw new NotImplementedException();
        }
    }
}
