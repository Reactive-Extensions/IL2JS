using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Microsoft.Csa.SharedObjects
{
    public class BinaryPayloadWriter : IPayloadWriter
    {
        BinaryWriter writer;

        public BinaryPayloadWriter(Stream stream)
        {
            this.writer = new BinaryWriter(stream);
        }

        #region IPayloadWriter Members

        public void Write(string name, string value)
        {
            if (value == null)
            {
                value = string.Empty;
            }

            this.writer.Write(value);
        }

        public void Write(string name, bool value)
        {
            this.writer.Write(value);
        }

        public void Write(string name, byte value)
        {
            this.writer.Write(value);
        }

        public void Write(string name, short value)
        {
            this.writer.Write(value);
        }

        public void Write(string name, UInt16 value)
        {
            this.writer.Write(value);
        }

        public void Write(string name, int value)
        {
            this.writer.Write(value);
        }

        public void Write(string name, long value)
        {
            this.writer.Write(value);
        }

        public void Write(string name, double value)
        {
            this.writer.Write(value);
        }

        public void Write(string name, byte[] value)
        {
            this.writer.Write(value.Length);
            this.writer.Write(value);
        }

        public void Write(string name, Guid guid)
        {
            this.writer.Write(guid.ToByteArray());
        }

        public void Write(string name, Int32[] xs)
        {
            this.writer.Write(xs.Length);
            foreach (Int32 x in xs)
            {
                this.writer.Write(x);
            }
        }

        public void Write(string name, string[] xs)
        {
            this.writer.Write(xs.Length);
            foreach (string x in xs)
            {
                this.writer.Write(x);
            }
        }

        public void Write<T>(string name, IEnumerable<T> xs) where T : ISharedObjectSerializable
        {
            this.Write(name, xs, (_, x) => x.Serialize(this));
        }

        public void Write<T>(string name, IEnumerable<T> xs, Action<IPayloadWriter, T> writeFunc)
        {
            if (xs == null)
            {
                this.writer.Write(0);
            }
            else
            {
                this.writer.Write(xs.Count());
                foreach (T x in xs)
                {
                    writeFunc(this, x);
                }
            }
        }

        public void Write(string name, ISharedObjectSerializable value)
        {
            if (value != null)
            {
                this.writer.Write(true);
                value.Serialize(this);
            }
            else
            {
                this.writer.Write(false);
            }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
        }

        #endregion
    }
}
