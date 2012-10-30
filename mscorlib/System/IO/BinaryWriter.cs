////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////

using Microsoft.LiveLabs.JavaScript.IL2JS;
using System.Text;

namespace System.IO
{
    /// Writes primitive types in binary to a stream and supports writing strings in a specific encoding.
    public class BinaryWriter : IDisposable
    {
        public static readonly BinaryWriter Null;
        protected Stream OutStream;
       
        /// <summary>
        /// Initializes a new instance of the System.IO.BinaryWriter class based on the
        ///  supplied stream and using UTF-8 as the encoding for strings.
        /// </summary>
        /// <param name="output"></param>
        public BinaryWriter(Stream output)
        {
            this.OutStream = output;
        }

        public BinaryWriter(Stream output, Encoding encoding)
        {
            this.OutStream = output;
        }

        public virtual void Close()
        {
            this.OutStream.Close();
        }

        public void Dispose()
        {
            this.Close();
        }

        public virtual void Write(bool value) { throw new NotSupportedException(); }
        public virtual void Write(byte value) { throw new NotSupportedException(); }
        public virtual void Write(byte[] buffer) { throw new NotSupportedException(); }
        public virtual void Write(char ch) { throw new NotSupportedException(); }
        public virtual void Write(char[] chars) { throw new NotSupportedException(); }
        public virtual void Write(double value) { throw new NotSupportedException(); }
        public virtual void Write(float value) { throw new NotSupportedException(); }
        public virtual void Write(int value) { throw new NotSupportedException(); }
        public virtual void Write(long value) { throw new NotSupportedException(); }
        public virtual void Write(sbyte value) { throw new NotSupportedException(); }
        public virtual void Write(short value) { throw new NotSupportedException(); }
        public virtual void Write(string value) { throw new NotSupportedException(); }
        public virtual void Write(uint value) { throw new NotSupportedException(); }
        public virtual void Write(ulong value) { throw new NotSupportedException(); }
        public virtual void Write(ushort value) { throw new NotSupportedException(); }
        public virtual void Write(byte[] buffer, int index, int count) { throw new NotSupportedException(); }        
        public virtual void Write(char[] chars, int index, int count) { throw new NotSupportedException(); }
        protected void Write7BitEncodedInt(int value) { throw new NotSupportedException(); }

        public virtual Stream BaseStream { get { return this.OutStream; } }
    }
}
