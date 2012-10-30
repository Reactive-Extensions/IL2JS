using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Csa.SharedObjects
{
    public class BinaryPayloadReader : IPayloadReader
    {
        BinaryReader reader;
        public BinaryPayloadReader(Stream stream)
        {
            this.reader = new BinaryReader(stream);
        }

        #region IPayloadReader Members

        public string ReadString(string name)
        {
            return this.reader.ReadString();
        }

        public bool ReadBoolean(string name)
        {
            return this.reader.ReadBoolean();
        }

        public byte ReadByte(string name)
        {
            return this.reader.ReadByte();
        }

        public short ReadInt16(string name)
        {
            return this.reader.ReadInt16();
        }

        public UInt16 ReadUInt16(string name)
        {
            return this.reader.ReadUInt16();
        }

        public int ReadInt32(string name)
        {
            return this.reader.ReadInt32();
        }

        public long ReadInt64(string name)
        {
            return this.reader.ReadInt64();
        }

        public byte[] ReadBytes(string name)
        {
            int length = this.reader.ReadInt32();
            return this.reader.ReadBytes(length);
        }

        public Guid ReadGuid(string name)
        {
            return new Guid(this.reader.ReadBytes(16));
        }

        public Int32[] ReadIntArray(string name)
        {
            int length = this.reader.ReadInt32();
            Int32[] xs = new Int32[length];
            for (int i = 0; i < length; ++i)
            {
                xs[i] = this.reader.ReadInt32();
            }
            return xs;
        }

        public string[] ReadStringArray(string name)
        {
            int length = this.reader.ReadInt32();
            string[] xs = new string[length];
            for (int i = 0; i < length; ++i)
            {
                xs[i] = this.reader.ReadString();
            }
            return xs;
        }

        public void ReadList(string name, Action<IPayloadReader> readFunc)
        {
            int count = this.reader.ReadInt32();
            for (int i = 0; i < count; ++i)
            {
                readFunc(this);
            }
        }

        public List<T> ReadList<T>(string name) where T : ISharedObjectSerializable, new()
        {
            return this.ReadList(name, _ => { 
                T item = new T(); 
                item.Deserialize(this); 
                return item; });
        }

        public List<T> ReadList<T>(string name, Func<IPayloadReader, T> readFunc)
        {
            int count = this.reader.ReadInt32();
            if (count == 0)
            {
                return null;
            }
            List<T> items = new List<T>();
            for (int i = 0; i < count; ++i)
            {
                T item = readFunc(this);
                items.Add(item);
            }
            return items;
        }

        public T ReadObject<T>(string name, Func<IPayloadReader, T> readFunc)
        {
            bool hasValue = this.reader.ReadBoolean();
            return hasValue ? readFunc(this) : default(T);
        }

        public T ReadObject<T>(string name) where T : ISharedObjectSerializable, new()
        {
            return (T)ReadObject(name, _ =>
            {
                T t = new T();
                t.Deserialize(this);
                return t;
            });
        }

        public T ReadObject<T>(string name, ReadObjectOption option) where T : ISharedObjectSerializable, new()
        {
            T value = this.ReadObject<T>(name);
            if (value == null && option == ReadObjectOption.Create)
            {
                return new T();
            }
            else
            {
                return value;
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
