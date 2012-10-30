// <copyright file="Extensions.cs" company="Microsoft">
// Copyright © 2010 Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Csa.SharedObjects.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;

    /// <summary>
    /// LinqExtensions class.  Contains LINQ related extensions.
    /// </summary>
    public static class LinqExtensions
    {
        /// <summary>
        /// Extends IEnumerable&lt;T&gt; to have a ForEach method.
        /// </summary>
        /// <typeparam name="T">The type of the IEnumerable&lt;T&gt;.</typeparam>
        /// <param name="enumeration">The IEnumerable&lt;T&gt; being extended.</param>
        /// <param name="action">The Action&lt;T&gt; to execute.</param>
        public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T> action)
        {
            foreach (T element in enumeration)
            {
                action(element);
            }
        }
    }

    /// <summary>
    /// BinaryReaderExtensions class. Contains extensions to the BinaryReader type.
    /// </summary>
    public static class BinaryReaderExtensions
    {
        public static Guid ReadGuid(this BinaryReader reader)
        {
            byte[] data = reader.ReadBytes(16);
            return new Guid(data);
        }

        public static void ReadInts(this BinaryReader reader, ICollection<int> ints)
        {
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                ints.Add(reader.ReadInt32());
            }
        }

        public static void ReadLongs(this BinaryReader reader, ICollection<long> longs)
        {
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                longs.Add(reader.ReadInt64());
            }
        }
    }

    /// <summary>
    /// BinaryReaderExtensions class. Contains extensions to the BinaryReader type.
    /// </summary>
    public static class BinaryWriterExtensions
    {
        public static void Write(this BinaryWriter writer, Guid guid)
        {
            byte[] data = guid.ToByteArray();
            writer.Write(data);
        }

        public static void WriteInts(this BinaryWriter writer, IEnumerable<int> list)
        {
            int count = list.Count();
            writer.Write(count);
            foreach (int i in list)
            {
                writer.Write(i);
            }
        }

        public static void WriteLongs(this BinaryWriter writer, IEnumerable<long> list)
        {
            int count = list.Count();
            writer.Write(count);
            foreach (int i in list)
            {
                writer.Write(i);
            }
        }
    }
}
