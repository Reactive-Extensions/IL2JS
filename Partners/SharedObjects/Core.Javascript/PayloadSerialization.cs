using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Microsoft.Csa.SharedObjects
{
    public enum PayloadFormat { Binary, JSON };
    public enum ReadObjectOption { Default, Create };

    public interface IPayloadWriter : IDisposable
    {
        void Write(string name, string value);
        void Write(string name, bool value);
        void Write(string name, byte value);
        void Write(string name, Int16 value);
        void Write(string name, UInt16 value);
        void Write(string name, Int32 value);
        void Write(string name, Int64 value);
        void Write(string name, byte[] value);
        void Write(string name, Guid guid);

        void Write(string name, Int32[] xs);
        void Write(string name, string[] xs);

        // More advanced scenarios

        // Serialize an IEnumerable when each element is serializable
        void Write<T>(string name, IEnumerable<T> xs) where T : ISharedObjectSerializable;

        // Serialize an IEnumerable by providing a function to write each element
        void Write<T>(string name, IEnumerable<T> xs, Action<IPayloadWriter, T> writeFunc);

        // Write out a child object
        void Write(string name, ISharedObjectSerializable value);
    }

    public interface IPayloadReader : IDisposable
    {
        string ReadString(string name);
        bool ReadBoolean(string name);
        byte ReadByte(string name);
        Int16 ReadInt16(string name);
        UInt16 ReadUInt16(string name);
        Int32 ReadInt32(string name);
        Int64 ReadInt64(string name);
        byte[] ReadBytes(string name);
        Guid ReadGuid(string name);
        Int32[] ReadIntArray(string name);
        string[] ReadStringArray(string name);

        void ReadList(string name, Action<IPayloadReader> readFunc);
        List<T> ReadList<T>(string name) where T : ISharedObjectSerializable, new();
        List<T> ReadList<T>(string name, Func<IPayloadReader, T> readFunc);

        T ReadObject<T>(string name, Func<IPayloadReader, T> readFunc);
        T ReadObject<T>(string name) where T : ISharedObjectSerializable, new();
        T ReadObject<T>(string name, ReadObjectOption option) where T : ISharedObjectSerializable, new();
    }

    public interface ISharedObjectSerializable
    {
        void Serialize(IPayloadWriter writer);
        void Deserialize(IPayloadReader reader);
    }
}
