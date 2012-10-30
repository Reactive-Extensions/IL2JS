////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////

using System.Text;
namespace System.IO
{
    public abstract class TextWriter : IDisposable
    {
        private const string InitialNewLine = "\r\n";

        // Methods
        protected TextWriter() { }
        public virtual void Close() { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public virtual void Flush() { }

        public virtual void Write(bool value)
        {
            this.Write(value ? "True" : "False");
        }

        public virtual void Write(char[] buffer) {}
        public virtual void Write(char value) {}
        public virtual void Write(decimal value)
        {
            this.Write(value.ToString());
        }

        public virtual void Write(double value)
        {
            this.Write(value.ToString());
        }

        public virtual void Write(int value)
        {
            this.Write(value.ToString());
        }

        public virtual void Write(long value)
        {
            this.Write(value.ToString());
        }

        public virtual void Write(object value)
        {
            if (value != null)
            {
                this.Write(value.ToString());
            }
        }

        public virtual void Write(float value)
        {
            this.Write(value.ToString());
        }

        public virtual void Write(string value) { throw new NotImplementedException(); }

        public virtual void Write(uint value)
        {
            this.Write(value.ToString());
        }

        public virtual void Write(ulong value)
        {
            this.Write(value.ToString());
        }

        public virtual void Write(string format, object arg0)
        {
            this.Write(string.Format(format, new object[] { arg0 }));
        }

        public virtual void Write(string format, params object[] arg)
        {
            this.Write(string.Format(format, arg));
        }

        public virtual void Write(string format, object arg0, object arg1)
        {
            this.Write(string.Format(format, new object[] { arg0, arg1 }));
        }

        public virtual void Write(char[] buffer, int index, int count) { throw new NotImplementedException();}

        internal virtual void Write(string format, object arg0, object arg1, object arg2)
        {
            this.Write(string.Format(format, new object[] { arg0, arg1, arg2 }));
        }

        public virtual void WriteLine()
        {
            this.Write(TextWriter.InitialNewLine);
        }

        public virtual void WriteLine(bool value)
        {
            this.Write(value);
            this.WriteLine();
        }

        public virtual void WriteLine(char[] buffer)
        {
            this.Write(buffer);
            this.WriteLine();
        }

        public virtual void WriteLine(char value)
        {
            this.Write(value);
            this.WriteLine();
        }

        public virtual void WriteLine(decimal value)
        {
            this.Write(value);
            this.WriteLine();
        }

        public virtual void WriteLine(double value)
        {
            this.Write(value);
            this.WriteLine();
        }

        public virtual void WriteLine(int value)
        {
            this.Write(value);
            this.WriteLine();
        }

        public virtual void WriteLine(long value)
        {
            this.Write(value);
            this.WriteLine();
        }

        public virtual void WriteLine(object value)
        {
            if (value == null)
            {
                this.WriteLine();
            }
            else
            {
                this.WriteLine(value.ToString());
            }
        }

        public virtual void WriteLine(float value)
        {
            this.Write(value);
            this.WriteLine();
        }

        public virtual void WriteLine(string value)
        {
            if (value == null)
            {
                this.WriteLine();
            }
            else
            {
                this.Write(value);
                this.WriteLine();
            }
        }

        public virtual void WriteLine(uint value)
        {
            this.Write(value);
            this.WriteLine();
        }

        public virtual void WriteLine(ulong value)
        {
            this.Write(value);
            this.WriteLine();
        }

        public virtual void WriteLine(string format, object arg0)
        {
            this.WriteLine(string.Format(format, new object[] { arg0 }));
        }

        public virtual void WriteLine(string format, params object[] arg)
        {
            this.WriteLine(string.Format(format, arg));
        }

        public virtual void WriteLine(string format, object arg0, object arg1)
        {
            this.WriteLine(string.Format(format, new object[] { arg0, arg1 }));
        }

        public virtual void WriteLine(char[] buffer, int index, int count)
        {
            this.Write(buffer, index, count);
            this.WriteLine();
        }

        internal virtual void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            this.WriteLine(string.Format(format, new object[] { arg0, arg1, arg2 }));
        }

        // Properties
        public abstract Encoding Encoding { get; }
    }
}
 

