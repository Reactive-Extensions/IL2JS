////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////

using Microsoft.LiveLabs.JavaScript.IL2JS;
using System.Text;

namespace System.IO
{
    /// Writes primitive types in binary to a stream and supports writing strings in a specific encoding.
    public class BinaryReader : IDisposable
    {
        private Stream m_stream;

        public BinaryReader(Stream input) 
        {
            this.m_stream = input;
        }

        public BinaryReader(Stream input, Encoding encoding)
        {
            this.m_stream = input;
        }

        public virtual void Close()
        {
            this.m_stream.Close();
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            Stream stream = this.m_stream;
            this.m_stream = null;
            if (stream != null)
            {
                stream.Close();
            }
        }
        
        public virtual int PeekChar() { throw new NotSupportedException(); }
        public virtual int Read() { throw new NotSupportedException(); }
        public virtual int Read(byte[] buffer, int index, int count) { throw new NotSupportedException(); }
        public virtual int Read(char[] buffer, int index, int count) { throw new NotSupportedException(); }
        public virtual bool ReadBoolean() { throw new NotSupportedException(); }
        public virtual byte ReadByte() { throw new NotSupportedException(); }
        public virtual byte[] ReadBytes(int count) { throw new NotSupportedException(); }
        public virtual char ReadChar() { throw new NotSupportedException(); }
        public virtual char[] ReadChars(int count) { throw new NotSupportedException(); }
        public virtual decimal ReadDecimal() { throw new NotSupportedException(); }
        public virtual double ReadDouble() { throw new NotSupportedException(); }
        public virtual short ReadInt16() { throw new NotSupportedException(); }
        public virtual int ReadInt32() { throw new NotSupportedException(); }
        public virtual long ReadInt64() { throw new NotSupportedException(); }
        public virtual sbyte ReadSByte() { throw new NotSupportedException(); }
        public virtual float ReadSingle() { throw new NotSupportedException(); }
        public virtual string ReadString() { throw new NotSupportedException(); }
        public virtual ushort ReadUInt16() { throw new NotSupportedException(); }
        public virtual uint ReadUInt32() { throw new NotSupportedException(); }
        public virtual ulong ReadUInt64() { throw new NotSupportedException(); }

        public virtual Stream BaseStream { get { return this.m_stream; } }
    }
}
