//
// Metadata tables (S22)
//

using System;
using System.Collections.Generic;
using System.Security.Permissions;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.PE
{
    public abstract class Row
    {
        public int Index;

        public override bool Equals(object other)
        {
            var otherRow = other as Row;
            return otherRow != null && Tag == otherRow.Tag && Index == otherRow.Index;
        }

        public override int GetHashCode()
        {
            var res = (uint)Tag << 3;
            res ^= (uint)Index >> 7;
            res ^= (uint)Index << 25;
            return (int)res;
        }

        public abstract TableTag Tag { get; }

        public abstract void Read(ReaderContext ctxt, BlobReader reader);

        public abstract void Deref(ReaderContext ctxt);

        public abstract void PersistIndexes(WriterContext ctxt);

        public abstract void Write(WriterContext ctxt, BlobWriter writer);
    }

    public interface IHasNumRows
    {
        int NumRows { get; }
    }

    public abstract class Table<T> : ISeq<T>, IHasNumRows where T : Row, new()
    {
        private int numRows;
        [NotNull]
        private Seq<T> rows;

        protected Table(int numRows)
        {
            this.numRows = numRows;
            rows = new Seq<T>(numRows);
        }

        public int NumRows
        {
            get { return numRows; }
        }

        public abstract TableTag Tag { get; }

        public T this[int index]
        {
            get { return rows[index - 1]; }
            set { rows[index - 1] = value; }
        }

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            for (var i = 0; i < numRows; i++)
            {
                var row = new T { Index = i + 1 };
                row.Read(ctxt, reader);
                rows.Add(row);
            }
        }

        public virtual void ResolveIndexes(ReaderContext ctxt)
        {
            foreach (var row in rows)
                row.Deref(ctxt);
        }

        public virtual void PersistIndexes(WriterContext ctxt)
        {
            foreach (var row in rows)
                row.PersistIndexes(ctxt);
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            foreach (var item in this)
                item.Write(ctxt, writer);
        }

        public void Add(T item)
        {
            rows.Add(item);
        }

        public void Clear()
        {
            rows.Clear();
        }

        public bool Contains(T item)
        {
            return rows.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            rows.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return rows.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            return rows.Remove(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return rows.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return rows.IndexOf(item) + 1;
        }

        public void Insert(int index, T item)
        {
            rows.Insert(index - 1, item);
        }

        public void RemoveAt(int index)
        {
            rows.RemoveAt(index - 1);
        }
    }

    // S22.2
    public class AssemblyRow : Row
    {
        public AssemblyHashAlgorithm HashAlgId;
        public ushort MajorVersion;
        public ushort MinorVersion;
        public ushort BuildNumber;
        public ushort RevisionNumber;
        public AssemblyFlags Flags;
        public BlobRef PublicKey;
        public StringRef Name;
        public StringRef Culture;

        public override void Read(ReaderContext ctxt, BlobReader reader)
        {
            HashAlgId = (AssemblyHashAlgorithm)reader.ReadUInt32();
            MajorVersion = reader.ReadUInt16();
            MinorVersion = reader.ReadUInt16();
            BuildNumber = reader.ReadUInt16();
            RevisionNumber = reader.ReadUInt16();
            Flags = (AssemblyFlags)reader.ReadUInt32();
            PublicKey.Read(ctxt, reader);
            Name.Read(ctxt, reader);
            Culture.Read(ctxt, reader);
        }

        public override TableTag Tag
        {
            get { return TableTag.Assembly; }
        }

        public override void Deref(ReaderContext ctxt)
        {
            PublicKey.ResolveIndexes(ctxt);
            Name.ResolveIndexes(ctxt);
            Culture.ResolveIndexes(ctxt);
        }

        public override void PersistIndexes(WriterContext ctxt)
        {
            PublicKey.PersistIndexes(ctxt);
            Name.PersistIndexes(ctxt);
            Culture.PersistIndexes(ctxt);
        }

        public override void Write(WriterContext ctxt, BlobWriter writer)
        {
            writer.WriteUInt32((uint)HashAlgId);
            writer.WriteUInt16(MajorVersion);
            writer.WriteUInt16(MinorVersion);
            writer.WriteUInt16(BuildNumber);
            writer.WriteUInt16(RevisionNumber);
            writer.WriteUInt32((uint)Flags);
            PublicKey.Write(ctxt, writer);
            Name.Write(ctxt, writer);
            Culture.Write(ctxt, writer);
        }
    }

    public class AssemblyTable : Table<AssemblyRow>
    {
        public AssemblyTable(int numRows)
            : base(numRows)
        {
        }

        public override TableTag Tag
        {
            get { return TableTag.Assembly; }
        }
    }

    // S22.3
    public class AssemblyOSRow : Row
    {
        public uint OSPlatformID;
        public uint OSMajorVersion;
        public uint OSMinorVersion;

        public override void Read(ReaderContext ctxt, BlobReader reader)
        {
            OSPlatformID = reader.ReadUInt32();
            OSMajorVersion = reader.ReadUInt32();
            OSMinorVersion = reader.ReadUInt32();
        }

        public override TableTag Tag
        {
            get { return TableTag.AssemblyOS; }
        }

        public override void Deref(ReaderContext ctxt)
        {
        }

        public override void PersistIndexes(WriterContext ctxt)
        {
        }

        public override void Write(WriterContext ctxt, BlobWriter writer)
        {
            writer.WriteUInt32(OSPlatformID);
            writer.WriteUInt32(OSMajorVersion);
            writer.WriteUInt32(OSMinorVersion);
        }
    }

    public class AssemblyOSTable : Table<AssemblyOSRow>
    {
        public AssemblyOSTable(int numRows)
            : base(numRows)
        {
        }

        public override TableTag Tag
        {
            get { return TableTag.AssemblyOS; }
        }
    }

    // S22.4
    public class AssemblyProcessorRow : Row
    {
        public uint Processor;

        public override void Read(ReaderContext ctxt, BlobReader reader)
        {
            Processor = reader.ReadUInt32();
        }

        public override TableTag Tag
        {
            get { return TableTag.AssemblyProcessor; }
        }

        public override void Deref(ReaderContext ctxt)
        {
        }

        public override void PersistIndexes(WriterContext ctxt)
        {
        }

        public override void Write(WriterContext ctxt, BlobWriter writer)
        {
            writer.WriteUInt32(Processor);
        }
    }

    public class AssemblyProcessorTable : Table<AssemblyProcessorRow>
    {
        public AssemblyProcessorTable(int numRows)
            : base(numRows)
        {
        }

        public override TableTag Tag
        {
            get { return TableTag.AssemblyProcessor; }
        }
    }

    // S22.5
    public class AssemblyRefRow : Row
    {
        public ushort MajorVersion;
        public ushort MinorVersion;
        public ushort BuildNumber;
        public ushort RevisionNumber;
        public AssemblyFlags Flags;
        public BlobRef PublicKeyOrToken;
        public StringRef Name;
        public StringRef Culture;
        public BlobRef HashValue;

        public override void Read(ReaderContext ctxt, BlobReader reader)
        {
            MajorVersion = reader.ReadUInt16();
            MinorVersion = reader.ReadUInt16();
            BuildNumber = reader.ReadUInt16();
            RevisionNumber = reader.ReadUInt16();
            Flags = (AssemblyFlags)reader.ReadUInt32();
            PublicKeyOrToken.Read(ctxt, reader);
            Name.Read(ctxt, reader);
            Culture.Read(ctxt, reader);
            HashValue.Read(ctxt, reader);
        }

        public override TableTag Tag
        {
            get { return TableTag.AssemblyRef; }
        }

        public override void Deref(ReaderContext ctxt)
        {
            PublicKeyOrToken.ResolveIndexes(ctxt);
            Name.ResolveIndexes(ctxt);
            Culture.ResolveIndexes(ctxt);
            HashValue.ResolveIndexes(ctxt);
        }

        public override void PersistIndexes(WriterContext ctxt)
        {
            PublicKeyOrToken.PersistIndexes(ctxt);
            Name.PersistIndexes(ctxt);
            Culture.PersistIndexes(ctxt);
            HashValue.PersistIndexes(ctxt);
        }

        public override void Write(WriterContext ctxt, BlobWriter writer)
        {
            writer.WriteUInt16(MajorVersion);
            writer.WriteUInt16(MinorVersion);
            writer.WriteUInt16(BuildNumber);
            writer.WriteUInt16(RevisionNumber);
            writer.WriteUInt32((uint)Flags);
            PublicKeyOrToken.Write(ctxt, writer);
            Name.Write(ctxt, writer);
            Culture.Write(ctxt, writer);
            HashValue.Write(ctxt, writer);
        }
    }

    public class AssemblyRefTable : Table<AssemblyRefRow>
    {
        public AssemblyRefTable(int numRows)
            : base(numRows)
        {
        }

        public override TableTag Tag
        {
            get { return TableTag.AssemblyRef; }
        }
    }

    // S22.6
    public class AssemblyRefOSRow : Row
    {
        public uint OSPlatformID;
        public uint OSMajorVersion;
        public uint OSMinorVersion;
        public AssemblyRefRef AssemblyRef;

        public override void Read(ReaderContext ctxt, BlobReader reader)
        {
            OSPlatformID = reader.ReadUInt32();
            OSMajorVersion = reader.ReadUInt32();
            OSMinorVersion = reader.ReadUInt32();
            AssemblyRef.Read(ctxt, reader);
        }

        public override TableTag Tag
        {
            get { return TableTag.AssemblyRefOS; }
        }

        public override void Deref(ReaderContext ctxt)
        {
            AssemblyRef.ResolveIndexes(ctxt);
        }

        public override void PersistIndexes(WriterContext ctxt)
        {
            AssemblyRef.PersistIndexes(ctxt);
        }

        public override void Write(WriterContext ctxt, BlobWriter writer)
        {
            writer.WriteUInt32(OSPlatformID);
            writer.WriteUInt32(OSMajorVersion);
            writer.WriteUInt32(OSMinorVersion);
            AssemblyRef.Write(ctxt, writer);
        }
    }

    public class AssemblyRefOSTable : Table<AssemblyRefOSRow>
    {
        public AssemblyRefOSTable(int numRows)
            : base(numRows)
        {
        }

        public override TableTag Tag
        {
            get { return TableTag.AssemblyRefOS; }
        }
    }

    // S22.7
    public class AssemblyRefProcessorRow : Row
    {
        public uint Processor;
        public AssemblyRefRef AssemblyRef;

        public override void Read(ReaderContext ctxt, BlobReader reader)
        {
            Processor = reader.ReadUInt32();
            AssemblyRef.Read(ctxt, reader);
        }

        public override TableTag Tag
        {
            get { return TableTag.AssemblyRefProcessor; }
        }

        public override void Deref(ReaderContext ctxt)
        {
            AssemblyRef.ResolveIndexes(ctxt);
        }

        public override void PersistIndexes(WriterContext ctxt)
        {
            AssemblyRef.PersistIndexes(ctxt);
        }

        public override void Write(WriterContext ctxt, BlobWriter writer)
        {
            writer.WriteUInt32(Processor);
            AssemblyRef.Write(ctxt, writer);
        }
    }

    public class AssemblyRefProcessorTable : Table<AssemblyRefProcessorRow>
    {
        public AssemblyRefProcessorTable(int numRows)
            : base(numRows)
        {
        }

        public override TableTag Tag
        {
            get { return TableTag.AssemblyRefProcessor; }
        }
    }

    // S22.8
    public class ClassLayoutRow : Row
    {
        public ushort PackingSize;
        public uint ClassSize;
        public TypeDefRef Parent;

        public override void Read(ReaderContext ctxt, BlobReader reader)
        {
            PackingSize = reader.ReadUInt16();
            ClassSize = reader.ReadUInt32();
            Parent.Read(ctxt, reader);
        }

        public override TableTag Tag
        {
            get { return TableTag.ClassLayout; }
        }

        public override void Deref(ReaderContext ctxt)
        {
            Parent.ResolveIndexes(ctxt);
        }

        public override void PersistIndexes(WriterContext ctxt)
        {
            Parent.PersistIndexes(ctxt);
        }

        public override void Write(WriterContext ctxt, BlobWriter writer)
        {
            writer.WriteUInt16(PackingSize);
            writer.WriteUInt32(ClassSize);
            Parent.Write(ctxt, writer);
        }
    }

    public class ClassLayoutTable : Table<ClassLayoutRow>
    {
        public ClassLayoutTable(int numRows)
            : base(numRows)
        {
        }

        public override TableTag Tag
        {
            get { return TableTag.ClassLayout; }
        }
    }

    // S22.9
    public class ConstantRow : Row
    {
        public TypeSigTag Type;
        public HasConstantRef Parent; // ParamRow, FieldRow or PropertyRow
        public ConstantRef Value;
        public const byte padding = 0x00;

        public override void Read(ReaderContext ctxt, BlobReader reader)
        {
            Type = (TypeSigTag)reader.ReadByte();
            var actualPadding = reader.ReadByte();
            if (actualPadding != padding)
                throw new PEException("invalid constant row");
            Parent.Read(ctxt, reader);
            Value.Read(ctxt, reader);
        }

        public override TableTag Tag
        {
            get { return TableTag.Constant; }
        }

        public override void Deref(ReaderContext ctxt)
        {
            Parent.ResolveIndexes(ctxt);
            Value.ResolveIndexes(ctxt, Type);
        }

        public override void PersistIndexes(WriterContext ctxt)
        {
            Parent.PersistIndexes(ctxt);
            Value.PersistIndexes(ctxt, Type);
        }

        public override void Write(WriterContext ctxt, BlobWriter writer)
        {
            writer.WriteByte((byte)Type);
            writer.WriteByte(padding);
            Parent.Write(ctxt, writer);
            Value.Write(ctxt, writer);
        }
    }

    public class ConstantTable : Table<ConstantRow>
    {
        public ConstantTable(int numRows)
            : base(numRows)
        {
        }

        public override TableTag Tag
        {
            get { return TableTag.Constant; }
        }
    }

    // S22.10
    public class CustomAttributeRow : Row
    {
        public HasCustomAttributeRef Parent; // any row except CustomAttributeRow
        public CustomAttributeTypeRef Type; // MethodDefRow or MemberRefRow
        public BlobRef Value; // must defer parsing till higher level types constructed

        public override void Read(ReaderContext ctxt, BlobReader reader)
        {
            Parent.Read(ctxt, reader);
            Type.Read(ctxt, reader);
            Value.Read(ctxt, reader);
        }

        public override TableTag Tag
        {
            get { return TableTag.CustomAttribute; }
        }

        public override void Deref(ReaderContext ctxt)
        {
            Parent.ResolveIndexes(ctxt);
            Type.ResolveIndexes(ctxt);
            Value.ResolveIndexes(ctxt);
        }

        public override void PersistIndexes(WriterContext ctxt)
        {
            Parent.PersistIndexes(ctxt);
            Type.PersistIndexes(ctxt);
            Value.PersistIndexes(ctxt);
        }

        public override void Write(WriterContext ctxt, BlobWriter writer)
        {
            Parent.Write(ctxt, writer);
            Type.Write(ctxt, writer);
            Value.Write(ctxt, writer);
        }

        public CustomAttributeSignature GetCustomAttribute(IImSeq<CustomAttributePropertyType> fixedArgTypes, Func<string, CustomAttributePropertyType> resolveType)
        {
            var reader = new BlobReader(Value.Value);
            var res = new CustomAttributeSignature();
            res.Read(fixedArgTypes, reader, resolveType);
            return res;
        }
    }

    public class CustomAttributeTable : Table<CustomAttributeRow>
    {
        public CustomAttributeTable(int numRows)
            : base(numRows)
        {
        }

        public override TableTag Tag
        {
            get { return TableTag.CustomAttribute; }
        }
    }

    // S22.11
    public class DeclSecurityRow : Row
    {
        public SecurityAction Action;
        public HasDeclSecurityRef Parent; // TypeDefRow, MethodDefRow or AssemblyRow
        public BlobRef PermissionSet;

        public override void Read(ReaderContext ctxt, BlobReader reader)
        {
            Action = (SecurityAction)reader.ReadUInt16();
            Parent.Read(ctxt, reader);
            PermissionSet.Read(ctxt, reader);
        }

        public override TableTag Tag
        {
            get { return TableTag.DeclSecurity; }
        }

        public override void Deref(ReaderContext ctxt)
        {
            Parent.ResolveIndexes(ctxt);
            PermissionSet.ResolveIndexes(ctxt);
        }

        public override void PersistIndexes(WriterContext ctxt)
        {
            Parent.PersistIndexes(ctxt);
            PermissionSet.PersistIndexes(ctxt);
        }

        public override void Write(WriterContext ctxt, BlobWriter writer)
        {
            writer.WriteUInt16((ushort)Action);
            Parent.Write(ctxt, writer);
            PermissionSet.Write(ctxt, writer);
        }
    }

    public class DeclSecurityTable : Table<DeclSecurityRow>
    {
        public DeclSecurityTable(int numRows)
            : base(numRows)
        {
        }

        public override TableTag Tag
        {
            get { return TableTag.DeclSecurity; }
        }
    }


    // S22.12
    public class EventMapRow : Row
    {
        public TypeDefRef Parent;
        public EventListRef EventList;

        public override void Read(ReaderContext ctxt, BlobReader reader)
        {
            Parent.Read(ctxt, reader);
            EventList.Read(ctxt, reader);
        }

        public override TableTag Tag
        {
            get { return TableTag.EventMap; }
        }

        public override void Deref(ReaderContext ctxt)
        {
            Parent.ResolveIndexes(ctxt);
            EventList.ResolveIndexes(ctxt, this);
        }

        public override void PersistIndexes(WriterContext ctxt)
        {
            Parent.PersistIndexes(ctxt);
            EventList.PersistIndexes(ctxt, this);
        }

        public override void Write(WriterContext ctxt, BlobWriter writer)
        {
            Parent.Write(ctxt, writer);
            EventList.Write(ctxt, writer);
        }
    }

    public class EventMapTable : Table<EventMapRow>
    {
        public EventMapTable(int numRows)
            : base(numRows)
        {
        }

        public override TableTag Tag
        {
            get { return TableTag.EventMap; }
        }
    }

    // S22.13
    // TODO: Events must be sorted
    public class EventRow : Row
    {
        public EventAttributes EventFlags;
        public StringRef Name;
        public TypeDefOrRefRef EventType; // TypeDefRow, TypeRefRow, or TypeSpecRow

        public override void Read(ReaderContext ctxt, BlobReader reader)
        {
            EventFlags = (EventAttributes)reader.ReadUInt16();
            Name.Read(ctxt, reader);
            EventType.Read(ctxt, reader);
        }

        public override TableTag Tag
        {
            get { return TableTag.Event; }
        }

        public override void Deref(ReaderContext ctxt)
        {
            Name.ResolveIndexes(ctxt);
            EventType.ResolveIndexes(ctxt);
        }

        public override void PersistIndexes(WriterContext ctxt)
        {
            Name.PersistIndexes(ctxt);
            EventType.PersistIndexes(ctxt);
        }

        public override void Write(WriterContext ctxt, BlobWriter writer)
        {
            writer.WriteUInt16((ushort)EventFlags);
            Name.Write(ctxt, writer);
            EventType.Write(ctxt, writer);
        }
    }

    public class EventTable : Table<EventRow>
    {
        public EventTable(int numRows)
            : base(numRows)
        {
        }

        public override TableTag Tag
        {
            get { return TableTag.Event; }
        }
    }

    // S22.14
    public class ExportedTypeRow : Row
    {
        public TypeAttributes Flags;
        public uint TypeDefId;
        public StringRef TypeName;
        public StringRef TypeNamespace;
        public ImplementationRef Implementation; // FileRow or ExportedTypeRow

        public override void Read(ReaderContext ctxt, BlobReader reader)
        {
            Flags = (TypeAttributes)reader.ReadUInt32();
            TypeDefId = reader.ReadUInt32();
            TypeName.Read(ctxt, reader);
            TypeNamespace.Read(ctxt, reader);
            Implementation.Read(ctxt, reader);
        }

        public override TableTag Tag
        {
            get { return TableTag.ExportedType; }
        }

        public override void Deref(ReaderContext ctxt)
        {
            TypeName.ResolveIndexes(ctxt);
            TypeNamespace.ResolveIndexes(ctxt);
            Implementation.ResolveIndexes(ctxt);
        }

        public override void PersistIndexes(WriterContext ctxt)
        {
            TypeName.PersistIndexes(ctxt);
            TypeNamespace.PersistIndexes(ctxt);
            Implementation.PersistIndexes(ctxt);
        }

        public override void Write(WriterContext ctxt, BlobWriter writer)
        {
            writer.WriteUInt32((uint)Flags);
            writer.WriteUInt32(TypeDefId);
            TypeName.Write(ctxt, writer);
            TypeNamespace.Write(ctxt, writer);
            Implementation.Write(ctxt, writer);
        }

    }

    public class ExportedTypeTable : Table<ExportedTypeRow>
    {
        public ExportedTypeTable(int numRows)
            : base(numRows)
        {
        }

        public override TableTag Tag
        {
            get { return TableTag.ExportedType; }
        }
    }

    // S22.15
    public class FieldRow : Row
    {
        public FieldAttributes Flags;
        public StringRef Name;
        public SigRef<FieldMemberSig> Signature;

        public override void Read(ReaderContext ctxt, BlobReader reader)
        {
            Flags = (FieldAttributes)reader.ReadUInt16();
            Name.Read(ctxt, reader);
            Signature.Read(ctxt, reader);
        }

        public override TableTag Tag
        {
            get { return TableTag.Field; }
        }

        public override void Deref(ReaderContext ctxt)
        {
            Name.ResolveIndexes(ctxt);
            Signature.ResolveIndexes(ctxt, FieldMemberSig.ReadField);
        }

        public override void PersistIndexes(WriterContext ctxt)
        {
            Name.PersistIndexes(ctxt);
            Signature.PersistIndexes(ctxt);
        }

        public override void Write(WriterContext ctxt, BlobWriter writer)
        {
            writer.WriteUInt16((ushort)Flags);
            Name.Write(ctxt, writer);
            Signature.Write(ctxt, writer);
        }
    }

    public class FieldTable : Table<FieldRow>
    {
        public FieldTable(int numRows)
            : base(numRows)
        {
        }

        public override TableTag Tag
        {
            get { return TableTag.Field; }
        }
    }

    // S22.16
    public class FieldLayoutRow : Row
    {
        public uint Offset;
        public FieldRef Field;

        public override void Read(ReaderContext ctxt, BlobReader reader)
        {
            Offset = reader.ReadUInt32();
            Field.Read(ctxt, reader);
        }

        public override TableTag Tag
        {
            get { return TableTag.FieldLayout; }
        }

        public override void Deref(ReaderContext ctxt)
        {
            Field.ResolveIndexes(ctxt);
        }

        public override void PersistIndexes(WriterContext ctxt)
        {
            Field.PersistIndexes(ctxt);
        }

        public override void Write(WriterContext ctxt, BlobWriter writer)
        {
            writer.WriteUInt32(Offset);
            Field.Write(ctxt, writer);
        }
    }

    public class FieldLayoutTable : Table<FieldLayoutRow>
    {
        public FieldLayoutTable(int numRows)
            : base(numRows)
        {
        }

        public override TableTag Tag
        {
            get { return TableTag.FieldLayout; }
        }
    }

    // S22.17
    public class FieldMarshalRow : Row
    {
        public HasFieldMarshalRef Parent; // FieldRow or ParamRow
        public BlobRef NativeType; // TODO: should be SigRef<MarshalSpecSig>

        public override void Read(ReaderContext ctxt, BlobReader reader)
        {
            Parent.Read(ctxt, reader);
            NativeType.Read(ctxt, reader);
        }

        public override TableTag Tag
        {
            get { return TableTag.FieldMarshal; }
        }

        public override void Deref(ReaderContext ctxt)
        {
            Parent.ResolveIndexes(ctxt);
            NativeType.ResolveIndexes(ctxt);
        }

        public override void PersistIndexes(WriterContext ctxt)
        {
            Parent.PersistIndexes(ctxt);
            NativeType.PersistIndexes(ctxt);
        }

        public override void Write(WriterContext ctxt, BlobWriter writer)
        {
            Parent.Write(ctxt, writer);
            NativeType.Write(ctxt, writer);
        }
    }

    public class FieldMarshalTable : Table<FieldMarshalRow>
    {
        public FieldMarshalTable(int numRows)
            : base(numRows)
        {
        }

        public override TableTag Tag
        {
            get { return TableTag.FieldMarshal; }
        }
    }

    // S22.18
    public class FieldRVARow : Row
    {
        public RVA<byte[]> Data;
        public FieldRef Field;

        public override void Read(ReaderContext ctxt, BlobReader reader)
        {
            Data.Read(reader);
            Field.Read(ctxt, reader);
        }

        public override TableTag Tag
        {
            get { return TableTag.FieldRVA; }
        }

        public override void Deref(ReaderContext ctxt)
        {
            Field.ResolveIndexes(ctxt);
        }

        public void GetData(ReaderContext ctxt)
        {
            // This can't be done in ResolveIndexes since we need all the tables to have been read and resolved
            var size = Field.Value.Signature.Value.Type.Type.ByteSize(ctxt.Tables);
            Data.Value = Data.GetReader(ctxt).ReadBytes(size);
        }

        public override void PersistIndexes(WriterContext ctxt)
        {
            Field.PersistIndexes(ctxt);
        }

        public override void Write(WriterContext ctxt, BlobWriter writer)
        {
            Data.Write(writer);
            Field.Write(ctxt, writer);
        }
    }

    public class FieldRVATable : Table<FieldRVARow>
    {
        public FieldRVATable(int numRows)
            : base(numRows)
        {
        }

        public override TableTag Tag
        {
            get { return TableTag.FieldRVA; }
        }
    }

    // S22.19
    public class FileRow : Row
    {
        public FileAttributes Flags;
        public StringRef Name;
        public BlobRef HashValue;

        public override void Read(ReaderContext ctxt, BlobReader reader)
        {
            Flags = (FileAttributes)reader.ReadUInt32();
            Name.Read(ctxt, reader);
            HashValue.Read(ctxt, reader);
        }

        public override TableTag Tag
        {
            get { return TableTag.File; }
        }

        public override void Deref(ReaderContext ctxt)
        {
            Name.ResolveIndexes(ctxt);
            HashValue.ResolveIndexes(ctxt);
        }

        public override void PersistIndexes(WriterContext ctxt)
        {
            Name.PersistIndexes(ctxt);
            HashValue.PersistIndexes(ctxt);
        }

        public override void Write(WriterContext ctxt, BlobWriter writer)
        {
            writer.WriteUInt32((uint)Flags);
            Name.Write(ctxt, writer);
            HashValue.Write(ctxt, writer);
        }
    }

    public class FileTable : Table<FileRow>
    {
        public FileTable(int numRows)
            : base(numRows)
        {
        }

        public override TableTag Tag
        {
            get { return TableTag.File; }
        }
    }

    // S22.20
    public class GenericParamRow : Row
    {
        public ushort Number;
        public GenericParamAttributes Flags;
        public TypeOrMethodDefRef Owner; // TypeDefRow or MethodDefRow
        public StringRef Name;

        public override void Read(ReaderContext ctxt, BlobReader reader)
        {
            Number = reader.ReadUInt16();
            Flags = (GenericParamAttributes)reader.ReadUInt16();
            Owner.Read(ctxt, reader);
            Name.Read(ctxt, reader);
        }

        public override TableTag Tag
        {
            get { return TableTag.GenericParam; }
        }

        public override void Deref(ReaderContext ctxt)
        {
            Name.ResolveIndexes(ctxt);
            Owner.ResolveIndexes(ctxt);
        }

        public override void PersistIndexes(WriterContext ctxt)
        {
            Name.PersistIndexes(ctxt);
            Owner.PersistIndexes(ctxt);
        }

        public override void Write(WriterContext ctxt, BlobWriter writer)
        {
            writer.WriteUInt16(Number);
            writer.WriteUInt16((ushort)Flags);
            Owner.Write(ctxt, writer);
            Name.Write(ctxt, writer);
        }
    }

    public class GenericParamTable : Table<GenericParamRow>
    {
        public GenericParamTable(int numRows)
            : base(numRows)
        {
        }

        public override TableTag Tag
        {
            get { return TableTag.GenericParam; }
        }
    }

    // S22.21
    public class GenericParamConstraintRow : Row
    {
        public GenericParamRef Owner;
        public TypeDefOrRefRef Constraint; // TypeRefRow, TypeRefRow or TypeSpecRow

        public override void Read(ReaderContext ctxt, BlobReader reader)
        {
            Owner.Read(ctxt, reader);
            Constraint.Read(ctxt, reader);
        }

        public override TableTag Tag
        {
            get { return TableTag.GenericParamConstraint; }
        }

        public override void Deref(ReaderContext ctxt)
        {
            Owner.ResolveIndexes(ctxt);
            Constraint.ResolveIndexes(ctxt);
        }

        public override void PersistIndexes(WriterContext ctxt)
        {
            Owner.PersistIndexes(ctxt);
            Constraint.PersistIndexes(ctxt);
        }

        public override void Write(WriterContext ctxt, BlobWriter writer)
        {
            Owner.Write(ctxt, writer);
            Constraint.Write(ctxt, writer);
        }
    }

    public class GenericParamConstraintTable : Table<GenericParamConstraintRow>
    {
        public GenericParamConstraintTable(int numRows)
            : base(numRows)
        {
        }

        public override TableTag Tag
        {
            get { return TableTag.GenericParamConstraint; }
        }
    }

    // S22.22
    public class ImplMapRow : Row
    {
        public PInvokeAttributes MappingFlags;
        public MemberForwardedRef MemberForwarded; // FieldRow or MethodDefRow
        public StringRef ImportName;
        public ModuleRefRef ImportScope;

        public override void Read(ReaderContext ctxt, BlobReader reader)
        {
            MappingFlags = (PInvokeAttributes)reader.ReadUInt16();
            MemberForwarded.Read(ctxt, reader);
            ImportName.Read(ctxt, reader);
            ImportScope.Read(ctxt, reader);
        }

        public override TableTag Tag
        {
            get { return TableTag.ImplMap; }
        }

        public override void Deref(ReaderContext ctxt)
        {
            MemberForwarded.ResolveIndexes(ctxt);
            ImportName.ResolveIndexes(ctxt);
            ImportScope.ResolveIndexes(ctxt);
        }

        public override void PersistIndexes(WriterContext ctxt)
        {
            MemberForwarded.PersistIndexes(ctxt);
            ImportName.PersistIndexes(ctxt);
            ImportScope.PersistIndexes(ctxt);
        }

        public override void Write(WriterContext ctxt, BlobWriter writer)
        {
            writer.WriteUInt16((ushort)MappingFlags);
            MemberForwarded.Write(ctxt, writer);
            ImportName.Write(ctxt, writer);
            ImportScope.Write(ctxt, writer);
        }
    }

    public class ImplMapTable : Table<ImplMapRow>
    {
        public ImplMapTable(int numRows)
            : base(numRows)
        {
        }

        public override TableTag Tag
        {
            get { return TableTag.ImplMap; }
        }
    }

    // S22.23
    public class InterfaceImplRow : Row
    {
        public TypeDefRef Class;
        public TypeDefOrRefRef Interface; // TypeDefRow, TypeRefRow or TypeSpecRow

        public override void Read(ReaderContext ctxt, BlobReader reader)
        {
            Class.Read(ctxt, reader);
            Interface.Read(ctxt, reader);
        }

        public override TableTag Tag
        {
            get { return TableTag.InterfaceImpl; }
        }

        public override void Deref(ReaderContext ctxt)
        {
            Class.ResolveIndexes(ctxt);
            Interface.ResolveIndexes(ctxt);
        }

        public override void PersistIndexes(WriterContext ctxt)
        {
            Class.PersistIndexes(ctxt);
            Interface.PersistIndexes(ctxt);
        }

        public override void Write(WriterContext ctxt, BlobWriter writer)
        {
            Class.Write(ctxt, writer);
            Interface.Write(ctxt, writer);
        }
    }

    public class InterfaceImplTable : Table<InterfaceImplRow>
    {
        public InterfaceImplTable(int numRows)
            : base(numRows)
        {
        }

        public override TableTag Tag
        {
            get { return TableTag.InterfaceImpl; }
        }
    }

    // S22.24
    public class ManifestResourceRow : Row
    {
        public uint Offset;
        public ManifestResourceAttributes Flags;
        public StringRef Name;
        public ImplementationRef Implementation; // FileRow, AssemblyRefRow or NULL

        public override void Read(ReaderContext ctxt, BlobReader reader)
        {
            Offset = reader.ReadUInt32();
            Flags = (ManifestResourceAttributes)reader.ReadUInt32();
            Name.Read(ctxt, reader);
            Implementation.Read(ctxt, reader);
        }

        public override TableTag Tag
        {
            get { return TableTag.ManifestResource; }
        }

        public override void Deref(ReaderContext ctxt)
        {
            Name.ResolveIndexes(ctxt);
            Implementation.ResolveIndexes(ctxt);
        }

        public override void PersistIndexes(WriterContext ctxt)
        {
            Implementation.PersistIndexes(ctxt);
            Name.PersistIndexes(ctxt);
        }

        public override void Write(WriterContext ctxt, BlobWriter writer)
        {
            writer.WriteUInt32(Offset);
            writer.WriteUInt32((uint)Flags);
            Name.Write(ctxt, writer);
            Implementation.Write(ctxt, writer);
        }
    }

    public class ManifestResourceTable : Table<ManifestResourceRow>
    {
        public ManifestResourceTable(int numRows)
            : base(numRows)
        {
        }

        public override TableTag Tag
        {
            get { return TableTag.ManifestResource; }
        }
    }

    // S22.25
    public class MemberRefRow : Row
    {
        public MemberRefParentRef Class; // MethodDefRow, ModuleRefRow, TypeDefRow, TypeRefRow or TypeSpecRow
        public StringRef Name;
        public SigRef<MemberSig> Signature;

        public override void Read(ReaderContext ctxt, BlobReader reader)
        {
            Class.Read(ctxt, reader);
            Name.Read(ctxt, reader);
            Signature.Read(ctxt, reader);
        }

        public override TableTag Tag
        {
            get { return TableTag.MemberRef; }
        }

        public override void Deref(ReaderContext ctxt)
        {
            Class.ResolveIndexes(ctxt);
            Name.ResolveIndexes(ctxt);
            Signature.ResolveIndexes(ctxt, MemberSig.Read);
        }

        public override void PersistIndexes(WriterContext ctxt)
        {
            Class.PersistIndexes(ctxt);
            Name.PersistIndexes(ctxt);
            Signature.PersistIndexes(ctxt);
        }

        public override void Write(WriterContext ctxt, BlobWriter writer)
        {
            Class.Write(ctxt, writer);
            Name.Write(ctxt, writer);
            Signature.Write(ctxt, writer);
        }
    }

    public class MemberRefTable : Table<MemberRefRow>
    {
        public MemberRefTable(int numRows)
            : base(numRows)
        {
        }

        public override TableTag Tag
        {
            get { return TableTag.MemberRef; }
        }
    }

    // S22.26
    public class MethodDefRow : Row
    {
        public RVA<MethodBody> Body;
        public MethodImplAttributes ImplFlags;
        public MethodAttributes Flags;
        public StringRef Name;
        public SigRef<MethodMemberSig> Signature;
        public ParamListRef ParamList;

        public override void Read(ReaderContext ctxt, BlobReader reader)
        {
            Body.Read(reader);
            ImplFlags = (MethodImplAttributes)reader.ReadUInt16();
            Flags = (MethodAttributes)reader.ReadUInt16();
            Name.Read(ctxt, reader);
            Signature.Read(ctxt, reader);
            ParamList.Read(ctxt, reader);
        }

        public override TableTag Tag
        {
            get { return TableTag.MethodDef; }
        }

        public override void Deref(ReaderContext ctxt)
        {
            Name.ResolveIndexes(ctxt);
            Signature.ResolveIndexes(ctxt, MethodMemberSig.ReadMethod);
            ParamList.ResolveIndexes(ctxt, this);
        }

        public void GetData(ReaderContext ctxt, Func<OpCode, Row, object> resolveRow)
        {
            // Can't do this in ResolveIndexes since need all tables to have been read and resolved first
            if (Body.Address > 0)
            {
                var reader = Body.GetReader(ctxt);
                Body.Value = new MethodBody();
                Body.Value.Read(ctxt, reader, resolveRow);
            }
        }

        public override void PersistIndexes(WriterContext ctxt)
        {
            Name.PersistIndexes(ctxt);
            Signature.PersistIndexes(ctxt);
            ParamList.PersistIndexes(ctxt, this);
        }

        public override void Write(WriterContext ctxt, BlobWriter writer)
        {
            Body.Write(writer);
            writer.WriteUInt16((ushort)ImplFlags);
            writer.WriteUInt16((ushort)Flags);
            Name.Write(ctxt, writer);
            Signature.Write(ctxt, writer);
            ParamList.Write(ctxt, writer);
        }
    }

    public class MethodDefTable : Table<MethodDefRow>
    {
        public MethodDefTable(int numRows)
            : base(numRows)
        {
        }

        public override TableTag Tag
        {
            get { return TableTag.MethodDef; }
        }
    }

    // S22.27
    public class MethodImplRow : Row
    {
        public TypeDefRef Class;
        public MethodDefOrRefRef MethodBody; // MethodDefRow or MemberRefRow
        public MethodDefOrRefRef MethodDeclaration; // MethodDefRow or MemberRefRow

        public override void Read(ReaderContext ctxt, BlobReader reader)
        {
            Class.Read(ctxt, reader);
            MethodBody.Read(ctxt, reader);
            MethodDeclaration.Read(ctxt, reader);
        }

        public override TableTag Tag
        {
            get { return TableTag.MethodImpl; }
        }

        public override void Deref(ReaderContext ctxt)
        {
            Class.ResolveIndexes(ctxt);
            MethodBody.ResolveIndexes(ctxt);
            MethodDeclaration.ResolveIndexes(ctxt);
        }

        public override void PersistIndexes(WriterContext ctxt)
        {
            Class.PersistIndexes(ctxt);
            MethodBody.PersistIndexes(ctxt);
            MethodDeclaration.PersistIndexes(ctxt);
        }

        public override void Write(WriterContext ctxt, BlobWriter writer)
        {
            Class.Write(ctxt, writer);
            MethodBody.Write(ctxt, writer);
            MethodDeclaration.Write(ctxt, writer);
        }
    }

    public class MethodImplTable : Table<MethodImplRow>
    {
        public MethodImplTable(int numRows)
            : base(numRows)
        {
        }

        public override TableTag Tag
        {
            get { return TableTag.MethodImpl; }
        }
    }

    // S22.28
    public class MethodSemanticsRow : Row
    {
        public MethodSemanticsAttributes Semantics;
        public MethodDefRef Method;
        public HasSemanticsRef Association; // EventRow or PropertyRow

        public override void Read(ReaderContext ctxt, BlobReader reader)
        {
            Semantics = (MethodSemanticsAttributes)reader.ReadUInt16();
            Method.Read(ctxt, reader);
            Association.Read(ctxt, reader);
        }

        public override TableTag Tag
        {
            get { return TableTag.MethodSemantics; }
        }

        public override void Deref(ReaderContext ctxt)
        {
            Method.ResolveIndexes(ctxt);
            Association.ResolveIndexes(ctxt);
        }

        public override void PersistIndexes(WriterContext ctxt)
        {
            Method.PersistIndexes(ctxt);
            Association.PersistIndexes(ctxt);
        }

        public override void Write(WriterContext ctxt, BlobWriter writer)
        {
            writer.WriteUInt16((ushort)Semantics);
            Method.Write(ctxt, writer);
            Association.Write(ctxt, writer);
        }
    }

    public class MethodSemanticsTable : Table<MethodSemanticsRow>
    {
        public MethodSemanticsTable(int numRows)
            : base(numRows)
        {
        }

        public override TableTag Tag
        {
            get { return TableTag.MethodSemantics; }
        }
    }

    // S22.29
    public class MethodSpecRow : Row
    {
        public MethodDefOrRefRef Method; // MethodDefRow or MemberRefRow
        public SigRef<MethodSpecMemberSig> Instantiation;

        public override void Read(ReaderContext ctxt, BlobReader reader)
        {
            Method.Read(ctxt, reader);
            Instantiation.Read(ctxt, reader);
        }

        public override TableTag Tag
        {
            get { return TableTag.MethodSpec; }
        }

        public override void Deref(ReaderContext ctxt)
        {
            Method.ResolveIndexes(ctxt);
            Instantiation.ResolveIndexes(ctxt, MethodSpecMemberSig.ReadMethodSpec);
        }

        public override void PersistIndexes(WriterContext ctxt)
        {
            Method.PersistIndexes(ctxt);
            Instantiation.PersistIndexes(ctxt);
        }

        public override void Write(WriterContext ctxt, BlobWriter writer)
        {
            Method.Write(ctxt, writer);
            Instantiation.Write(ctxt, writer);
        }
    }

    public class MethodSpecTable : Table<MethodSpecRow>
    {
        public MethodSpecTable(int numRows)
            : base(numRows)
        {
        }

        public override TableTag Tag
        {
            get { return TableTag.MethodSpec; }
        }
    }

    // S22.30
    public class ModuleRow : Row
    {
        public ushort Generation;
        public StringRef Name;
        public GuidRef Mvid;
        public GuidRef EncId;
        public GuidRef EncBaseId;

        public override void Read(ReaderContext ctxt, BlobReader reader)
        {
            Generation = reader.ReadUInt16();
            Name.Read(ctxt, reader);
            Mvid.Read(ctxt, reader);
            EncId.Read(ctxt, reader);
            EncBaseId.Read(ctxt, reader);
        }

        public override TableTag Tag
        {
            get { return TableTag.Module; }
        }

        public override void Deref(ReaderContext ctxt)
        {
            Name.ResolveIndexes(ctxt);
            Mvid.ResolveIndexes(ctxt);
            EncId.ResolveIndexes(ctxt);
            EncBaseId.ResolveIndexes(ctxt);
        }

        public override void PersistIndexes(WriterContext ctxt)
        {
            Name.PersistIndexes(ctxt);
            Mvid.PersistIndexes(ctxt);
            EncId.PersistIndexes(ctxt);
            EncBaseId.PersistIndexes(ctxt);
        }

        public override void Write(WriterContext ctxt, BlobWriter writer)
        {
            writer.WriteUInt16(Generation);
            Name.Write(ctxt, writer);
            Mvid.Write(ctxt, writer);
            EncId.Write(ctxt, writer);
            EncBaseId.Write(ctxt, writer);
        }
    }

    public class ModuleTable : Table<ModuleRow>
    {
        public ModuleTable(int numRows)
            : base(numRows)
        {
        }

        public override TableTag Tag
        {
            get { return TableTag.Module; }
        }
    }

    // S22.31
    public class ModuleRefRow : Row
    {
        public StringRef Name;

        public override void Read(ReaderContext ctxt, BlobReader reader)
        {
            Name.Read(ctxt, reader);
        }

        public override TableTag Tag
        {
            get { return TableTag.ModuleRef; }
        }

        public override void Deref(ReaderContext ctxt)
        {
            Name.ResolveIndexes(ctxt);
        }

        public override void PersistIndexes(WriterContext ctxt)
        {
            Name.PersistIndexes(ctxt);
        }

        public override void Write(WriterContext ctxt, BlobWriter writer)
        {
            Name.Write(ctxt, writer);
        }
    }

    public class ModuleRefTable : Table<ModuleRefRow>
    {
        public ModuleRefTable(int numRows)
            : base(numRows)
        {
        }

        public override TableTag Tag
        {
            get { return TableTag.ModuleRef; }
        }
    }

    // S22.32
    public class NestedClassRow : Row
    {
        public TypeDefRef NestedClass;
        public TypeDefRef EnclosingClass;


        public override void Read(ReaderContext ctxt, BlobReader reader)
        {
            NestedClass.Read(ctxt, reader);
            EnclosingClass.Read(ctxt, reader);
        }

        public override TableTag Tag
        {
            get { return TableTag.NestedClass; }
        }

        public override void Deref(ReaderContext ctxt)
        {
            NestedClass.ResolveIndexes(ctxt);
            EnclosingClass.ResolveIndexes(ctxt);
        }

        public override void PersistIndexes(WriterContext ctxt)
        {
            NestedClass.PersistIndexes(ctxt);
            EnclosingClass.PersistIndexes(ctxt);
        }

        public override void Write(WriterContext ctxt, BlobWriter writer)
        {
            NestedClass.Write(ctxt, writer);
            EnclosingClass.Write(ctxt, writer);
        }
    }

    public class NestedClassTable : Table<NestedClassRow>
    {
        public NestedClassTable(int numRows)
            : base(numRows)
        {
        }

        public override TableTag Tag
        {
            get { return TableTag.NestedClass; }
        }
    }


    // S22.33
    public class ParamRow : Row
    {
        public ParamAttributes Flags;
        public ushort Sequence;
        public StringRef Name;

        public override void Read(ReaderContext ctxt, BlobReader reader)
        {
            Flags = (ParamAttributes)reader.ReadUInt16();
            Sequence = reader.ReadUInt16();
            Name.Read(ctxt, reader);
        }

        public override TableTag Tag
        {
            get { return TableTag.Param; }
        }

        public override void Deref(ReaderContext ctxt)
        {
            Name.ResolveIndexes(ctxt);
        }

        public override void PersistIndexes(WriterContext ctxt)
        {
            Name.PersistIndexes(ctxt);
        }

        public override void Write(WriterContext ctxt, BlobWriter writer)
        {
            writer.WriteUInt16((ushort)Flags);
            writer.WriteUInt16(Sequence);
            Name.Write(ctxt, writer);
        }
    }

    public class ParamTable : Table<ParamRow>
    {
        public ParamTable(int numRows)
            : base(numRows)
        {
        }

        public override TableTag Tag
        {
            get { return TableTag.Param; }
        }
    }

    // S22.34
    public class PropertyRow : Row
    {
        public PropertyAttributes Flags;
        public StringRef Name;
        public SigRef<PropertyMemberSig> Type;

        public override void Read(ReaderContext ctxt, BlobReader reader)
        {
            Flags = (PropertyAttributes)reader.ReadUInt16();
            Name.Read(ctxt, reader);
            Type.Read(ctxt, reader);
        }

        public override TableTag Tag
        {
            get { return TableTag.Property; }
        }

        public override void Deref(ReaderContext ctxt)
        {
            Name.ResolveIndexes(ctxt);
            Type.ResolveIndexes(ctxt, PropertyMemberSig.ReadProperty);
        }


        public override void PersistIndexes(WriterContext ctxt)
        {
            Name.PersistIndexes(ctxt);
            Type.PersistIndexes(ctxt);
        }

        public override void Write(WriterContext ctxt, BlobWriter writer)
        {
            writer.WriteUInt16((ushort)Flags);
            Name.Write(ctxt, writer);
            Type.Write(ctxt, writer);
        }
    }

    public class PropertyTable : Table<PropertyRow>
    {
        public PropertyTable(int numRows)
            : base(numRows)
        {
        }

        public override TableTag Tag
        {
            get { return TableTag.Property; }
        }
    }

    // S22.35
    public class PropertyMapRow : Row
    {
        public TypeDefRef Parent;
        public PropertyListRef PropertyList;

        public override void Read(ReaderContext ctxt, BlobReader reader)
        {
            Parent.Read(ctxt, reader);
            PropertyList.Read(ctxt, reader);
        }

        public override TableTag Tag
        {
            get { return TableTag.PropertyMap; }
        }

        public override void Deref(ReaderContext ctxt)
        {
            Parent.ResolveIndexes(ctxt);
            PropertyList.ResolveIndexes(ctxt, this);
        }
    
        public override void PersistIndexes(WriterContext ctxt)
        {
            Parent.PersistIndexes(ctxt);
            PropertyList.PersistIndexes(ctxt, this);
        }

        public override void Write(WriterContext ctxt, BlobWriter writer)
        {
            Parent.Write(ctxt, writer);
            PropertyList.Write(ctxt, writer);
        }
    }

    public class PropertyMapTable : Table<PropertyMapRow>
    {
        public PropertyMapTable(int numRows)
            : base(numRows)
        {
        }

        public override TableTag Tag
        {
            get { return TableTag.PropertyMap; }
        }
    }

    // S22.36
    public class StandAloneSigRow : Row
    {
        public SigRef<MemberSig> Signature;

        public override void Read(ReaderContext ctxt, BlobReader reader)
        {
            Signature.Read(ctxt, reader);
        }

        public override TableTag Tag
        {
            get { return TableTag.StandAloneSig; }
        }

        public override void Deref(ReaderContext ctxt)
        {
            Signature.ResolveIndexes(ctxt, MemberSig.Read);
        }

        public override void PersistIndexes(WriterContext ctxt)
        {
            Signature.PersistIndexes(ctxt);
        }

        public override void Write(WriterContext ctxt, BlobWriter writer)
        {
            Signature.Write(ctxt, writer);
        }
    }

    public class StandAloneSigTable : Table<StandAloneSigRow>
    {
        public StandAloneSigTable(int numRows)
            : base(numRows)
        {
        }

        public override TableTag Tag
        {
            get { return TableTag.StandAloneSig; }
        }
    }

    // S22.37
    public class TypeDefRow : Row
    {
        public TypeAttributes Flags;
        public StringRef TypeName;
        public StringRef TypeNamespace;
        public TypeDefOrRefRef Extends; // TypeDefRow, TypeRefRow, TypeSpecRow or NULL
        public FieldListRef FieldList;
        public MethodDefListRef MethodList;

        public override void Read(ReaderContext ctxt, BlobReader reader)
        {
            Flags = (TypeAttributes)reader.ReadUInt32();
            TypeName.Read(ctxt, reader);
            TypeNamespace.Read(ctxt, reader);
            Extends.Read(ctxt, reader);
            FieldList.Read(ctxt, reader);
            MethodList.Read(ctxt, reader);
        }

        public override TableTag Tag
        {
            get { return TableTag.TypeDef; }
        }

        public override void Deref(ReaderContext ctxt)
        {
            TypeName.ResolveIndexes(ctxt);
            TypeNamespace.ResolveIndexes(ctxt);
            Extends.ResolveIndexes(ctxt);
            FieldList.ResolveIndexes(ctxt, this);
            MethodList.ResolveIndexes(ctxt,  this);
        }

        public override void PersistIndexes(WriterContext ctxt)
        {
            TypeName.PersistIndexes(ctxt);
            TypeNamespace.PersistIndexes(ctxt);
            Extends.PersistIndexes(ctxt);
            FieldList.PersistIndexes(ctxt, this);
            MethodList.PersistIndexes(ctxt, this);
        }

        public override void Write(WriterContext ctxt, BlobWriter writer)
        {
            writer.WriteUInt32((uint)Flags);
            TypeName.Write(ctxt, writer);
            TypeNamespace.Write(ctxt, writer);
            Extends.Write(ctxt, writer);
            FieldList.Write(ctxt, writer);
            MethodList.Write(ctxt, writer);
        }
    }

    public class TypeDefTable : Table<TypeDefRow>
    {
        public TypeDefTable(int numRows)
            : base(numRows)
        {
        }

        public override TableTag Tag
        {
            get { return TableTag.TypeDef; }
        }
    }

    // S22.38
    public class TypeRefRow : Row
    {
        public ResolutionScopeRef ResolutionScope; // ModuleRow, ModuleRefRow, AssemblyRefRow, TypeRefRow or NULL
        public StringRef TypeName;
        public StringRef TypeNamespace;

        public override void Read(ReaderContext ctxt, BlobReader reader)
        {
            ResolutionScope.Read(ctxt, reader);
            TypeName.Read(ctxt, reader);
            TypeNamespace.Read(ctxt, reader);
        }

        public override TableTag Tag
        {
            get { return TableTag.TypeRef; }
        }

        public override void Deref(ReaderContext ctxt)
        {
            ResolutionScope.ResolveIndexes(ctxt);
            TypeName.ResolveIndexes(ctxt);
            TypeNamespace.ResolveIndexes(ctxt);
        }

        public override void PersistIndexes(WriterContext ctxt)
        {
            ResolutionScope.PersistIndexes(ctxt);
            TypeName.PersistIndexes(ctxt);
            TypeNamespace.PersistIndexes(ctxt);
        }

        public override void Write(WriterContext ctxt, BlobWriter writer)
        {
            ResolutionScope.Write(ctxt, writer);
            TypeName.Write(ctxt, writer);
            TypeNamespace.Write(ctxt, writer);
        }
    }

    public class TypeRefTable : Table<TypeRefRow>
    {
        public TypeRefTable(int numRows)
            : base(numRows)
        {
        }

        public override TableTag Tag
        {
            get { return TableTag.TypeRef; }
        }
    }

    // S22.39
    public class TypeSpecRow : Row
    {
        public SigRef<TypeSig> Signature;

        public override void Read(ReaderContext ctxt, BlobReader reader)
        {
            Signature.Read(ctxt, reader);
        }

        public override TableTag Tag
        {
            get { return TableTag.TypeSpec; }
        }

        public override void Deref(ReaderContext ctxt)
        {
            Signature.ResolveIndexes(ctxt, TypeSig.Read);
        }

        public override void PersistIndexes(WriterContext ctxt)
        {
            Signature.PersistIndexes(ctxt);
        }

        public override void Write(WriterContext ctxt, BlobWriter writer)
        {
            Signature.Write(ctxt, writer);
        }
    }

    public class TypeSpecTable : Table<TypeSpecRow>
    {
        public TypeSpecTable(int numRows)
            : base(numRows)
        {
        }

        public override TableTag Tag
        {
            get { return TableTag.TypeSpec; }
        }
    }
}