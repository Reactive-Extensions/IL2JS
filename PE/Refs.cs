using System;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.PE
{
    public struct FileOffset
    {
        // Offset relative to start of file
        public uint Offset;

        public void Read(BlobReader reader)
        {
            Offset = reader.ReadUInt32();
        }

        public void Write(BlobWriter writer)
        {
            writer.WriteUInt32(Offset);
        }
 
        public void Append(ReaderContext ctxt, string label)
        {
            ctxt.Tracer.AppendLine(String.Format("{0}: {1:x8} (file)", label, Offset));
        }
    }

    public struct MetadataOffset
    {
        // Offset relative to start of MetadataHeader
        public uint Offset;

        public void Read(BlobReader reader)
        {
            Offset = reader.ReadUInt32();
        }

        public void Write(BlobWriter writer)
        {
            writer.WriteUInt32(Offset);
        }

        public void Append(ReaderContext ctxt, string label)
        {
            ctxt.Tracer.AppendLine(String.Format("{0}: {1:x8} (metadata)", label, Offset));
        }
     }

    public struct RVA
    {
        // Address in virtual memory *after* all segments have been loaded, relative to image base
        public uint Address;

        public void Read(BlobReader reader)
        {
            Address = reader.ReadUInt32();
        }

        public void Write(BlobWriter writer)
        {
            writer.WriteUInt32(Address);
        }

        public void Append(ReaderContext ctxt, string label)
        {
            ctxt.AppendRVA(label, Address);
        }
    }

    public struct RVA<T>
    {
        // Address in virtual memory *after* all segments have been loaded, relative to image base
        public uint Address;
        public T Value;

        public void Read(BlobReader reader)
        {
            Address = reader.ReadUInt32();
        }

        public BlobReader GetReaderNonNull(ReaderContext ctxt)
        {
            if (Address == 0)
                throw new PEException("unexpected null address");
            else
                return ctxt.GetRVAReader(Address);
        }

        public BlobReader GetReader(ReaderContext ctxt)
        {
            if (Address == 0)
                return null;
            else
                return ctxt.GetRVAReader(Address);
        }

        public BlobWriter Alloc(WriterContext ctxt, Section section)
        {
            var writer = ctxt.GetSectionWriter(section);
            Address = writer.Offset;
            return writer;
        }

        public void Fixup(WriterContext ctxt, Section section)
        {
            Address = ctxt.FixupRVA(Address, section);
        }

        public void Write(BlobWriter writer)
        {
            writer.WriteUInt32(Address);
        }

        public void Append(ReaderContext ctxt, string label)
        {
            ctxt.AppendRVA(label, Address);
        }
    }

    public struct SizedRVA<T>
    {
        // Address in virtual memory *after* all segments have been loaded, relative to image base
        public uint Address;
        // Size in bytes of data at address
        public uint Size;
        public T Value;

        public void Read(BlobReader reader)
        {
            Address = reader.ReadUInt32();
            Size = reader.ReadUInt32();
        }

        public BlobReader GetReader(ReaderContext ctxt)
        {
            if (Address == 0)
                return null;
            else
                return ctxt.GetRVAReader(Address, Size);
        }

        public BlobReader GetReaderNonNull(ReaderContext ctxt)
        {
            if (Address == 0)
                throw new PEException("unexpeceted null address");
            else
                return ctxt.GetRVAReader(Address, Size);
        }

        public byte[] GetBytes(ReaderContext ctxt)
        {
            if (Address == 0)
                return null;
            else
                return ctxt.GetRVAReader(Address, Size).ReadBytes();
        }

        public BlobWriter Alloc(WriterContext ctxt, Section section)
        {
            var writer = ctxt.GetSectionWriter(section);
            Address = writer.Offset;
            return writer;
        }

        public void Fixup(WriterContext ctxt, Section section)
        {
            Address = ctxt.FixupRVA(Address, section);
        }

        public void Write(BlobWriter writer)
        {
            writer.WriteUInt32(Address);
            writer.WriteUInt32(Size);
        }

        public void Append(ReaderContext ctxt, string label)
        {
            ctxt.AppendRVA(label, Address, Size);
        }
    }

    public struct AliasedSizedRVA
    {
        // Address in virtual memory *after* all segments have been loaded, relative to image base
        public uint Address;
        // Size in bytes of data at address
        public uint Size;

        public void Read(BlobReader reader)
        {
            Address = reader.ReadUInt32();
            Size = reader.ReadUInt32();
        }

        public void Write(BlobWriter writer)
        {
            writer.WriteUInt32(Address);
            writer.WriteUInt32(Size);
        }


        public void Append(ReaderContext ctxt, string label)
        {
            ctxt.AppendRVA(label, Address, Size);
        }
    }

    public struct BlobRef
    {
        // Relative to start of blob heap
        public uint Offset;
        [CanBeNull]
        public byte[] Value;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            Offset = ctxt.Tables.IsBlobStreamBig ? reader.ReadUInt32() : reader.ReadUInt16();
            Value = null;
        }

        public void ResolveIndexes(ReaderContext ctxt)
        {
            if (Offset == 0)
                Value = null;
            else
            {
                var reader = ctxt.GetBlobReader();
                reader.Offset = Offset;
                if (reader.AtEndOfBlob)
                    throw new PEException("invalid blob offset");
                var size = reader.ReadCompressedUInt32();
                Value = reader.ReadBytes(size);
            }
        }

        public bool IsNull { get { return Value == null; } }

        public void PersistIndexes(WriterContext ctxt)
        {
            if (Value == null)
                Offset = 0;
            else
            {
                var writer = ctxt.GetBlobWriter();
                Offset = writer.Offset;
                writer.WriteCompressedUInt32((uint)Value.Length);
                writer.WriteBytes(Value);
            }
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            if (ctxt.Tables.IsBlobStreamBig)
                writer.WriteUInt32(Offset);
            else
                writer.WriteUInt16((ushort)Offset);
        }

        public override string ToString()
        {
            return Offset.ToString("x8");
        }
    }

    public struct StringRef
    {
        // Relative to start of string heap
        public uint Offset;
        [CanBeNull]
        public string Value;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            Offset = ctxt.Tables.IsStringStreamBig ? reader.ReadUInt32() : reader.ReadUInt16();
            Value = null;
        }

        public void ResolveIndexes(ReaderContext ctxt)
        {
            if (Offset == 0)
                Value = null;
            else
            {
                var value = default(string);
                if (!ctxt.OffsetToStringCache.TryGetValue(Offset, out value))
                {
                    var reader = ctxt.GetStringsReader();
                    reader.Offset = Offset;
                    if (reader.AtEndOfBlob)
                        throw new PEException("invalid string offset");
                    value = reader.ReadUTF8ZeroTerminatedString();
                    ctxt.OffsetToStringCache.Add(Offset, value);
                }
                Value = value;
            }
        }

        public bool IsNull { get { return Value == null; } }

        public void PersistIndexes(WriterContext ctxt)
        {
            if (Value == null)
                Offset = 0;
            else
            {
                var offset = default(uint);
                if (!ctxt.StringToOffsetCache.TryGetValue(Value, out offset))
                {
                    var writer = ctxt.GetStringsWriter();
                    offset = writer.Offset;
                    writer.WriteUTF8ZeroTerminatedString(Value);
                    ctxt.StringToOffsetCache.Add(Value, offset);
                }
                Offset = offset;
            }
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            if (ctxt.Tables.IsStringStreamBig)
                writer.WriteUInt32(Offset);
            else
                writer.WriteUInt16((ushort)Offset);
        }

        public override string ToString()
        {
            return Offset.ToString("x8");
        }
    }

    public struct UserStringRef
    {
        // Relative to body of user string heap
        public uint Offset;
        public string Value;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            // UNDOCUMENTED: Top 8 bits are 0x70. Why?
            Offset = reader.ReadUInt32() & 0xffffff;
            Value = null;
        }

        public void ResolveIndexes(ReaderContext ctxt)
        {
            if (Offset == 0)
                Value = null;
            else
            {
                var value = default(string);
                if (!ctxt.OffsetToUserStringCache.TryGetValue(Offset, out value))
                {
                    var reader = ctxt.GetUserStringsReader();
                    reader.Offset = Offset;
                    if (reader.AtEndOfBlob)
                        throw new PEException("invalid user string offset");
                    value = reader.ReadUTF16SizedStringWithEncodingHint();
                    ctxt.OffsetToUserStringCache.Add(Offset, value);
                }
                Value = value;
            }
        }

        public bool IsNull { get { return Value == null; } }

        public void PersistIndexes(WriterContext ctxt)
        {
            if (Value == null)
                Offset = 0;
            else
            {
                var offset = default(uint);
                if (!ctxt.UserStringToOffsetCache.TryGetValue(Value, out offset))
                {
                    var writer = ctxt.GetUserStringsWriter();
                    offset = writer.Offset;
                    writer.WriteUTF16SizedStringWithEncodingHint(Value);
                    ctxt.UserStringToOffsetCache.Add(Value, offset);
                }
                Offset = offset;
            }
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            writer.WriteUInt32(Offset);
        }

        public override string ToString()
        {
            return Offset.ToString("x8");
        }
    }

    public struct GuidRef
    {
        private const uint GuidSize = 16;

        // Base-1 index into GUID heap (an array of GUIDs)
        public uint Index;
        public Guid Value;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            Index = ctxt.Tables.IsGuidStreamBig ? reader.ReadUInt32() : reader.ReadUInt16();
            Value = Guid.Empty;
        }

        public void ResolveIndexes(ReaderContext ctxt)
        {
            if (Index == 0)
                Value = Guid.Empty;
            else
            {
                var reader = ctxt.GetGuidReader();
                reader.Offset = (Index - 1) * GuidSize;
                if (reader.AtEndOfBlob)
                    throw new PEException("invalid guid offset");
                Value = new Guid(reader.ReadBytes(GuidSize));
            }
        }

        public bool IsNull { get { return Value.Equals(Guid.Empty); } }

        public void PersistIndexes(WriterContext ctxt)
        {
            if (Value.Equals(Guid.Empty))
                Index = 0;
            else
            {
                var writer = ctxt.GetGuidWriter();
                Index = (writer.Offset / GuidSize) + 1;
                writer.WriteBytes(Value.ToByteArray());
            }
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            if (ctxt.Tables.IsGuidStreamBig)
                writer.WriteUInt32(Index);
            else
                writer.WriteUInt16((ushort)Index);
        }

        public override string ToString()
        {
            return Index.ToString("x8");
        }
    }

    public struct ConstantRef
    {
        // Relative to start of blob heap
        public uint Offset;
        [CanBeNull]
        public object Value;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            Offset = ctxt.Tables.IsBlobStreamBig ? reader.ReadUInt32() : reader.ReadUInt16();
            Value = null;
        }

        public bool IsNull { get { return Value == null; } }

        public void ResolveIndexes(ReaderContext ctxt, TypeSigTag tag)
        {
            if (Offset == 0)
                Value = null;
            else
            {
                var reader = ctxt.GetBlobReader();
                reader.Offset = Offset;
                Action<int> check = expected =>
                                      {
                                          var actual = reader.ReadCompressedUInt32();
                                          if (actual != expected)
                                              throw new PEException("mismatched constant and type");
                                      };
                switch (tag)
                {
                case TypeSigTag.BOOLEAN:
                    check(1);
                    Value = reader.ReadByte() == 0 ? false : true;
                    break;
                case TypeSigTag.CHAR:
                    check(2);
                    Value = (char)reader.ReadUInt16();
                    break;
                case TypeSigTag.I1:
                    check(1);
                    Value = reader.ReadSByte();
                    break;
                case TypeSigTag.U1:
                    check(1);
                    Value = reader.ReadByte();
                    break;
                case TypeSigTag.I2:
                    check(2);
                    Value = reader.ReadInt16();
                    break;
                case TypeSigTag.U2:
                    check(2);
                    Value = reader.ReadUInt16();
                    break;
                case TypeSigTag.I4:
                    check(4);
                    Value = reader.ReadInt32();
                    break;
                case TypeSigTag.U4:
                    check(4);
                    Value = reader.ReadUInt32();
                    break;
                case TypeSigTag.I8:
                    check(8);
                    Value = reader.ReadInt64();
                    break;
                case TypeSigTag.U8:
                    check(8);
                    Value = reader.ReadUInt64();
                    break;
                case TypeSigTag.R4:
                    check(4);
                    Value = reader.ReadSingle();
                    break;
                case TypeSigTag.R8:
                    check(8);
                    Value = reader.ReadDouble();
                    break;
                case TypeSigTag.STRING:
                    Value = reader.ReadUTF16SizedString();
                    break;
                case TypeSigTag.CLASS:
                    check(4);
                    var zero = reader.ReadUInt32();
                    if (zero != 0)
                        throw new PEException("expected zero for constant of class type");
                    Value = null;
                    break;
                default:
                    throw new PEException("unexpected constant tag");
                }
            }
        }

        public void PersistIndexes(WriterContext ctxt, TypeSigTag tag)
        {
            if (Value == null)
                Offset = 0;
            else
            {
                var writer = ctxt.GetBlobWriter();
                Offset = writer.Offset;
                switch (tag)
                {
                case TypeSigTag.BOOLEAN:
                    {
                        var b = (bool)Value;
                        writer.WriteCompressedUInt32(1);
                        writer.WriteByte(b ? (byte)0 : (byte)1);
                        break;
                    }
                case TypeSigTag.CHAR:
                    {
                        var c = (char)Value;
                        writer.WriteCompressedUInt32(2);
                        writer.WriteUInt16((ushort)c);
                        break;
                    }
                case TypeSigTag.I1:
                    {
                        var i = (sbyte)Value;
                        writer.WriteCompressedUInt32(1);
                        writer.WriteSByte(i);
                        break;
                    }
                case TypeSigTag.U1:
                    {
                        var i = (byte)Value;
                        writer.WriteCompressedUInt32(1);
                        writer.WriteByte(i);
                        break;
                    }
                case TypeSigTag.I2:
                    {
                        var i = (short)Value;
                        writer.WriteCompressedUInt32(2);
                        writer.WriteInt16(i);
                        break;
                    }
                case TypeSigTag.U2:
                    {
                        var i = (ushort)Value;
                        writer.WriteCompressedUInt32(2);
                        writer.WriteUInt16(i);
                        break;
                    }
                case TypeSigTag.I4:
                    {
                        var i = (int)Value;
                        writer.WriteCompressedUInt32(4);
                        writer.WriteInt32(i);
                        break;
                    }
                case TypeSigTag.U4:
                    {
                        var i = (uint)Value;
                        writer.WriteCompressedUInt32(4);
                        writer.WriteUInt32(i);
                        break;
                    }
                case TypeSigTag.I8:
                    {
                        var i = (long)Value;
                        writer.WriteCompressedUInt32(8);
                        writer.WriteInt64(i);
                        break;
                    }
                case TypeSigTag.U8:
                    {
                        var i = (ulong)Value;
                        writer.WriteCompressedUInt32(8);
                        writer.WriteUInt64(i);
                        break;
                    }
                case TypeSigTag.R4:
                    {
                        var f = (float)Value;
                        writer.WriteCompressedUInt32(4);
                        writer.WriteSingle(f);
                        break;
                    }
                case TypeSigTag.R8:
                    {
                        var d = (double)Value;
                        writer.WriteCompressedUInt32(8);
                        writer.WriteDouble(d);
                        break;
                    }
                case TypeSigTag.STRING:
                    {
                        var s = (string)Value;
                        writer.WriteUTF16SizedString(s);
                        break;
                    }
                case TypeSigTag.CLASS:
                    {
                        Offset = 0;
                        writer.WriteCompressedUInt32(4);
                        writer.WriteUInt32(0);
                        break;
                    }
                default:
                    throw new PEException("unexpected constant type");
                }
            }
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            if (ctxt.Tables.IsBlobStreamBig)
                writer.WriteUInt32(Offset);
            else
                writer.WriteUInt16((ushort)Offset);
        }

        public override string ToString()
        {
            return Offset.ToString("x8");
        }
    }

    public struct SigRef<T> where T : Signature
    {
        // Relative to start of blob heap
        public uint Offset;
        public T Value;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            Offset = ctxt.Tables.IsBlobStreamBig ? reader.ReadUInt32() : reader.ReadUInt16();
            Value = default(T);
        }

        public bool IsNull { get { return Value == null; } }

        public void ResolveIndexes(ReaderContext ctxt, Func<ReaderContext, BlobReader, T> f)
        {
            if (Offset == 0)
                Value = null;
            else
            {
                var sig = default(Signature);
                if (ctxt.OffsetToSignatureCache.TryGetValue(Offset, out sig))
                    Value = (T)sig;
                else
                {
                    var blobReader = ctxt.GetBlobReader();
                    blobReader.Offset = Offset;
                    if (blobReader.AtEndOfBlob)
                        throw new PEException("invalid blob offset for signature");
                    var size = blobReader.ReadCompressedUInt32();
                    // reader with offset zero at start of signature
                    var sigReader = new BlobReader(blobReader, blobReader.Offset, blobReader.Offset + size);
                    Value = f(ctxt, sigReader);
                    if (Value != null)
                    {
                        ctxt.OffsetToSignatureCache.Add(Offset, Value);
                        Value.ResolveIndexes(ctxt);
                    }
                }
            }
        }

        public void PersistIndexes(WriterContext ctxt)
        {
            if (Value == null)
                Offset = 0;
            else
            {
                var offset = default(uint);
                if (!ctxt.SignatureToOffsetCache.TryGetValue(Value, out offset))
                {
                    Value.PersistIndexes(ctxt);
                    var sigWriter = new BlobWriter();
                    Value.Write(ctxt, sigWriter);
                    
                    var writer = ctxt.GetBlobWriter();
                    offset = writer.Offset;
                    writer.WriteCompressedUInt32(sigWriter.Offset);
                    writer.WriteContents(sigWriter);
                    ctxt.SignatureToOffsetCache.Add(Value, offset);
                }
                Offset = offset;
            }
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            if (Offset == 0)
                throw new PEException("no index has been allocated");
            if (ctxt.Tables.IsBlobStreamBig)
                writer.WriteUInt32(Offset);
            else
                writer.WriteUInt16((ushort)Offset);
        }

        public override string ToString()
        {
            return Offset.ToString("x8");
        }
    }

    public struct TypeDefOrRefRef
    {
        public uint CodedIndex;
        [CanBeNull]
        public Row Value;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            CodedIndex = ctxt.Tables.TypeDefOrRefIsBig ? reader.ReadUInt32() : reader.ReadUInt16();
            Value = null;
        }

        public void ResolveIndexes(ReaderContext ctxt)
        {
            if (CodedIndex == 0)
                Value = null;
            else
            {
                var table = CodedIndex & 0x3;
                var index = (int)(CodedIndex >> 2);
                switch (table)
                {
                case 0:
                    Value = ctxt.Tables.TypeDefTable[index];
                    break;
                case 1:
                    Value = ctxt.Tables.TypeRefTable[index];
                    break;
                case 2:
                    Value = ctxt.Tables.TypeSpecTable[index];
                    break;
                default:
                    throw new PEException("invalid TypeDefOrRef coded index");
                }
            }
        }

        public bool IsNull { get { return Value == null; } }

        public void PersistIndexes(WriterContext ctxt)
        {
            if (Value == null)
                CodedIndex = 0;
            else
            {
                switch (Value.Tag)
                {
                case TableTag.TypeDef:
                    CodedIndex = (uint)Value.Index << 2;
                    break;
                case TableTag.TypeRef:
                    CodedIndex = (uint)Value.Index << 2 | 1;
                    break;
                case TableTag.TypeSpec:
                    CodedIndex = (uint)Value.Index << 2 | 2;
                    break;
                default:
                    throw new PEException("invalid TypeDefOrRef row");
                }
            }
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            if (ctxt.Tables.TypeDefOrRefIsBig)
                writer.WriteUInt32(CodedIndex);
            else
                writer.WriteUInt16((ushort)CodedIndex);
        }

        public override string ToString()
        {
            return CodedIndex.ToString("x8");
        }
    }

    public struct HasConstantRef
    {
        public uint CodedIndex;
        [CanBeNull]
        public Row Value;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            CodedIndex = ctxt.Tables.HasConstantIsBig ? reader.ReadUInt32() : reader.ReadUInt16();
            Value = null;
        }

        public void ResolveIndexes(ReaderContext ctxt)
        {
            if (CodedIndex == 0)
                Value = null;
            else
            {
                var table = CodedIndex & 0x3;
                var index = (int)(CodedIndex >> 2);
                switch (table)
                {
                case 0:
                    Value = ctxt.Tables.FieldTable[index];
                    break;
                case 1:
                    Value = ctxt.Tables.ParamTable[index];
                    break;
                case 2:
                    Value = ctxt.Tables.PropertyTable[index];
                    break;
                default:
                    throw new PEException("invalid HasConstant coded index");
                }
            }
        }

        public bool IsNull { get { return Value == null; } }

        public void PersistIndexes(WriterContext ctxt)
        {
            if (Value == null)
                CodedIndex = 0;
            else
            {
                switch (Value.Tag)
                {
                case TableTag.Field:
                    CodedIndex = (uint)Value.Index << 2;
                    break;
                case TableTag.Param:
                    CodedIndex = (uint)Value.Index << 2 | 1;
                    break;
                case TableTag.Property:
                    CodedIndex = (uint)Value.Index << 2 | 2;
                    break;
                default:
                    throw new PEException("invalid HasConstant row");
                }
            }
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            if (ctxt.Tables.HasConstantIsBig)
                writer.WriteUInt32(CodedIndex);
            else
                writer.WriteUInt16((ushort)CodedIndex);
        }

        public override string ToString()
        {
            return CodedIndex.ToString("x8");
        }
    }

    public struct HasCustomAttributeRef
    {
        public uint CodedIndex;
        [CanBeNull]
        public Row Value;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            CodedIndex = ctxt.Tables.HasCustomAttributeIsBig ? reader.ReadUInt32() : reader.ReadUInt16();
            Value = null;
        }

        public void ResolveIndexes(ReaderContext ctxt)
        {
            if (CodedIndex == 0)
                Value = null;
            else
            {
                var table = CodedIndex & 0x1f;
                var index = (int)(CodedIndex >> 5);
                switch (table)
                {
                    case 0:
                        Value = ctxt.Tables.MethodDefTable[index];
                        break;
                    case 1:
                        Value = ctxt.Tables.FieldTable[index];
                        break;
                    case 2:
                        Value = ctxt.Tables.TypeRefTable[index];
                        break;
                    case 3:
                        Value = ctxt.Tables.TypeDefTable[index];
                        break;
                    case 4:
                        Value = ctxt.Tables.ParamTable[index];
                        break;
                    case 5:
                        Value = ctxt.Tables.InterfaceImplTable[index];
                        break;
                    case 6:
                        Value = ctxt.Tables.MemberRefTable[index];
                        break;
                    case 7:
                        Value = ctxt.Tables.ModuleTable[index];
                        break;
                    case 8:
                        Value = ctxt.Tables.DeclSecurityTable[index];
                        break;
                    case 9:
                        Value = ctxt.Tables.PropertyTable[index];
                        break;
                    case 10:
                        Value = ctxt.Tables.EventTable[index];
                        break;
                    case 11:
                        Value = ctxt.Tables.StandAloneSigTable[index];
                        break;
                    case 12:
                        Value = ctxt.Tables.ModuleRefTable[index];
                        break;
                    case 13:
                        Value = ctxt.Tables.TypeSpecTable[index];
                        break;
                    case 14:
                        Value = ctxt.Tables.AssemblyTable[index];
                        break;
                    case 15:
                        Value = ctxt.Tables.AssemblyRefTable[index];
                        break;
                    case 16:
                        Value = ctxt.Tables.FileTable[index];
                        break;
                    case 17:
                        Value = ctxt.Tables.ExportedTypeTable[index];
                        break;
                    case 18:
                        Value = ctxt.Tables.ManifestResourceTable[index];
                        break;
                    default:
                        throw new PEException("invalid HasCustomAttribute coded index");
                }
            }
        }

        public bool IsNull { get { return Value == null; } }

        public void PersistIndexes(WriterContext ctxt)
        {
            if (Value == null)
                CodedIndex = 0;
            else
            {
                switch (Value.Tag)
                {
                    case TableTag.MethodDef:
                        CodedIndex = (uint)Value.Index << 5;
                        break;
                    case TableTag.Field:
                        CodedIndex = (uint)Value.Index << 5 | 1;
                        break;
                    case TableTag.TypeRef:
                        CodedIndex = (uint)Value.Index << 5 | 2;
                        break;
                    case TableTag.TypeDef:
                        CodedIndex = (uint)Value.Index << 5 | 3;
                        break;
                    case TableTag.Param:
                        CodedIndex = (uint)Value.Index << 5 | 4;
                        break;
                    case TableTag.InterfaceImpl:
                        CodedIndex = (uint)Value.Index << 5 | 5;
                        break;
                    case TableTag.MemberRef:
                        CodedIndex = (uint)Value.Index << 5 | 6;
                        break;
                    case TableTag.Module:
                        CodedIndex = (uint)Value.Index << 5 | 7;
                        break;
                    case TableTag.DeclSecurity:
                        CodedIndex = (uint)Value.Index << 5 | 8;
                        break;
                    case TableTag.Property:
                        CodedIndex = (uint)Value.Index << 5 | 9;
                        break;
                    case TableTag.Event:
                        CodedIndex = (uint)Value.Index << 5 | 10;
                        break;
                    case TableTag.StandAloneSig:
                        CodedIndex = (uint)Value.Index << 5 | 11;
                        break;
                    case TableTag.ModuleRef:
                        CodedIndex = (uint)Value.Index << 5 | 12;
                        break;
                    case TableTag.TypeSpec:
                        CodedIndex = (uint)Value.Index << 5 | 13;
                        break;
                    case TableTag.Assembly:
                        CodedIndex = (uint)Value.Index << 5 | 14;
                        break;
                    case TableTag.AssemblyRef:
                        CodedIndex = (uint)Value.Index << 5 | 15;
                        break;
                    case TableTag.File:
                        CodedIndex = (uint)Value.Index << 5 | 16;
                        break;
                    case TableTag.ExportedType:
                        CodedIndex = (uint)Value.Index << 5 | 17;
                        break;
                    case TableTag.ManifestResource:
                        CodedIndex = (uint)Value.Index << 5 | 18;
                        break;
                    default:
                        throw new PEException("invalid HasCustomAttribute row");
                }
            }
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            if (ctxt.Tables.HasCustomAttributeIsBig)
                writer.WriteUInt32(CodedIndex);
            else
                writer.WriteUInt16((ushort)CodedIndex);
        }

        public override string ToString()
        {
            return CodedIndex.ToString("x8");
        }
    }

    public struct HasFieldMarshalRef
    {
        public uint CodedIndex;
        [CanBeNull]
        public Row Value;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            CodedIndex = ctxt.Tables.HasFieldMarshalIsBig ? reader.ReadUInt32() : reader.ReadUInt16();
            Value = null;
        }

        public void ResolveIndexes(ReaderContext ctxt)
        {
            if (CodedIndex == 0)
                Value = null;
            else
            {
                var table = CodedIndex & 0x1;
                var index = (int)(CodedIndex >> 1);
                switch (table)
                {
                    case 0:
                        Value = ctxt.Tables.FieldTable[index];
                        break;
                    case 1:
                        Value = ctxt.Tables.ParamTable[index];
                        break;
                    default:
                        throw new PEException("invalid HasFieldMarshall coded index");
                }
            }
        }

        public bool IsNull { get { return Value == null; } }

        public void PersistIndexes(WriterContext ctxt)
        {
            if (Value == null)
                CodedIndex = 0;
            else
            {
                switch (Value.Tag)
                {
                    case TableTag.Field:
                        CodedIndex = (uint)Value.Index << 1;
                        break;
                    case TableTag.Param:
                        CodedIndex = (uint)Value.Index << 1 | 1;
                        break;
                    default:
                        throw new PEException("invalid HasFieldMarshall row");
                }
            }
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            if (ctxt.Tables.HasFieldMarshalIsBig)
                writer.WriteUInt32(CodedIndex);
            else
                writer.WriteUInt16((ushort)CodedIndex);
        }

        public override string ToString()
        {
            return CodedIndex.ToString("x8");
        }
    }

    public struct HasDeclSecurityRef
    {
        public uint CodedIndex;
        [CanBeNull]
        public Row Value;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            CodedIndex = ctxt.Tables.HasDeclSecurityIsBig ? reader.ReadUInt32() : reader.ReadUInt16();
            Value = null;
        }

        public void ResolveIndexes(ReaderContext ctxt)
        {
            if (CodedIndex == 0)
                Value = null;
            else
            {
                var table = CodedIndex & 0x3;
                var index = (int)(CodedIndex >> 2);
                switch (table)
                {
                case 0:
                    Value = ctxt.Tables.TypeDefTable[index];
                    break;
                case 1:
                    Value = ctxt.Tables.MethodDefTable[index];
                    break;
                case 2:
                    Value = ctxt.Tables.AssemblyTable[index];
                    break;
                default:
                    throw new PEException("invalid HasDeclSecurity coded index");
                }
            }
        }

        public bool IsNull { get { return Value == null; } }

        public void PersistIndexes(WriterContext ctxt)
        {
            if (Value == null)
                CodedIndex = 0;
            else
            {
                switch (Value.Tag)
                {
                case TableTag.TypeDef:
                    CodedIndex = (uint)Value.Index << 2;
                    break;
                case TableTag.MethodDef:
                    CodedIndex = (uint)Value.Index << 2 | 1;
                    break;
                case TableTag.Assembly:
                    CodedIndex = (uint)Value.Index << 2 | 2;
                    break;
                default:
                    throw new PEException("Invalid HasDeclSecurity row");
                }
            }
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            if (ctxt.Tables.HasDeclSecurityIsBig)
                writer.WriteUInt32(CodedIndex);
            else
                writer.WriteUInt16((ushort)CodedIndex);
        }

        public override string ToString()
        {
            return CodedIndex.ToString("x8");
        }
    }

    public struct MemberRefParentRef
    {
        public uint CodedIndex;
        [CanBeNull]
        public Row Value;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            CodedIndex = ctxt.Tables.MemberRefParentIsBig ? reader.ReadUInt32() : reader.ReadUInt16();
            Value = null;
        }

        public void ResolveIndexes(ReaderContext ctxt)
        {
            if (CodedIndex == 0)
                Value = null;
            else
            {
                var table = CodedIndex & 0x7;
                var index = (int)(CodedIndex >> 3);
                switch (table)
                {
                case 0:
                    Value = ctxt.Tables.TypeDefTable[index];
                    break;
                case 1:
                    Value = ctxt.Tables.TypeRefTable[index];
                    break;
                case 2:
                    Value = ctxt.Tables.ModuleRefTable[index];
                    break;
                case 3:
                    Value = ctxt.Tables.MethodDefTable[index];
                    break;
                case 4:
                    Value = ctxt.Tables.TypeSpecTable[index];
                    break;
                default:
                    throw new PEException("invalid MemberRefParent coded index");
                }
            }
        }

        public bool IsNull { get { return Value == null; } }

        public void PersistIndexes(WriterContext ctxt)
        {
            if (Value == null)
                CodedIndex = 0;
            else
            {
                switch (Value.Tag)
                {
                case TableTag.TypeDef:
                    CodedIndex = (uint)Value.Index << 3;
                    break;
                case TableTag.TypeRef:
                    CodedIndex = (uint)Value.Index << 3 | 1;
                    break;
                case TableTag.ModuleRef:
                    CodedIndex = (uint)Value.Index << 3 | 2;
                    break;
                case TableTag.MethodDef:
                    CodedIndex = (uint)Value.Index << 3 | 3;
                    break;
                case TableTag.TypeSpec:
                    CodedIndex = (uint)Value.Index << 3 | 4;
                    break;
                default:
                    throw new PEException("Invalid MemberRefParent row");
                }
            }
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            if (ctxt.Tables.MemberRefParentIsBig)
                writer.WriteUInt32(CodedIndex);
            else
                writer.WriteUInt16((ushort)CodedIndex);
        }

        public override string ToString()
        {
            return CodedIndex.ToString("x8");
        }
    }

    public struct HasSemanticsRef
    {
        public uint CodedIndex;
        [CanBeNull]
        public Row Value;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            CodedIndex = ctxt.Tables.HasSemanticsIsBig ? reader.ReadUInt32() : reader.ReadUInt16();
            Value = null;
        }

        public void ResolveIndexes(ReaderContext ctxt)
        {
            if (CodedIndex == 0)
                Value = null;
            else
            {
                var table = CodedIndex & 0x1;
                var index = (int)(CodedIndex >> 1);
                switch (table)
                {
                    case 0:
                        Value = ctxt.Tables.EventTable[index]; break;
                    case 1:
                        Value = ctxt.Tables.PropertyTable[index]; break;
                    default:
                        throw new PEException("invalid HasSemantics coded index");
                }
            }
        }

        public bool IsNull { get { return Value == null; } }

        public void PersistIndexes(WriterContext ctxt)
        {
            if (Value == null)
                CodedIndex = 0;
            else
            {
                switch (Value.Tag)
                {
                    case TableTag.Event:
                        CodedIndex = (uint)Value.Index << 1;
                        break;
                    case TableTag.Property:
                        CodedIndex = (uint)Value.Index << 1 | 1;
                        break;
                    default:
                        throw new PEException("invalid HasSemantics row");
                }
            }
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            if (ctxt.Tables.HasSemanticsIsBig)
                writer.WriteUInt32(CodedIndex);
            else
                writer.WriteUInt16((ushort)CodedIndex);
        }

        public override string ToString()
        {
            return CodedIndex.ToString("x8");
        }
    }


    public struct MethodDefOrRefRef
    {
        public uint CodedIndex;
        [CanBeNull]
        public Row Value;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            CodedIndex = ctxt.Tables.MethodDefOrRefIsBig ? reader.ReadUInt32() : reader.ReadUInt16();
            Value = null;
        }

        public void ResolveIndexes(ReaderContext ctxt)
        {
            if (CodedIndex == 0)
                Value = null;
            else
            {
                var table = CodedIndex & 0x1;
                var index = (int)(CodedIndex >> 1);
                switch (table)
                {
                case 0:
                    Value = ctxt.Tables.MethodDefTable[index];
                    break;
                case 1:
                    Value = ctxt.Tables.MemberRefTable[index];
                    break;
                default:
                    throw new PEException("invalid MethodDefOrRef coded index");
                }
            }
        }

        public bool IsNull { get { return Value == null; } }

        public void PersistIndexes(WriterContext ctxt)
        {
            if (Value == null)
                CodedIndex = 0;
            else
            {
                switch (Value.Tag)
                {
                case TableTag.MethodDef:
                    CodedIndex = (uint)Value.Index << 1;
                    break;
                case TableTag.MemberRef:
                    CodedIndex = (uint)Value.Index << 1 | 1;
                    break;
                default:
                    throw new PEException("invalid MethodDefOrRef row");
                }
            }
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            if (ctxt.Tables.MethodDefOrRefIsBig)
                writer.WriteUInt32(CodedIndex);
            else
                writer.WriteUInt16((ushort)CodedIndex);
        }

        public override string ToString()
        {
            return CodedIndex.ToString("x8");
        }
    }

    public struct MemberForwardedRef
    {
        public uint CodedIndex;
        [CanBeNull]
        public Row Value;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            CodedIndex = ctxt.Tables.MemberForwardedIsBig ? reader.ReadUInt32() : reader.ReadUInt16();
            Value = null;
        }

        public void ResolveIndexes(ReaderContext ctxt)
        {
            if (CodedIndex == 0)
                Value = null;
            else
            {
                var table = CodedIndex & 0x1;
                var index = (int)(CodedIndex >> 1);
                switch (table)
                {
                    case 0:
                        Value = ctxt.Tables.FieldTable[index];
                        break;
                    case 1:
                        Value = ctxt.Tables.MethodDefTable[index];
                        break;
                    default:
                        throw new PEException("invalid MemberForwarded coded index");
                }
            }
        }

        public bool IsNull { get { return Value == null; } }

        public void PersistIndexes(WriterContext ctxt)
        {
            if (Value == null)
                CodedIndex = 0;
            else
            {
                switch (Value.Tag)
                {
                    case TableTag.Field:
                        CodedIndex = (uint)Value.Index << 1;
                        break;
                    case TableTag.MethodDef:
                        CodedIndex = (uint)Value.Index << 1 | 1;
                        break;
                    default:
                        throw new PEException("invalid MemberForwarded row");
                }
            }
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            if (ctxt.Tables.MemberForwardedIsBig)
                writer.WriteUInt32(CodedIndex);
            else
                writer.WriteUInt16((ushort)CodedIndex);
        }

        public override string ToString()
        {
            return CodedIndex.ToString("x8");
        }
    }

    public struct ImplementationRef
    {
        public uint CodedIndex;
        [CanBeNull]
        public Row Value;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            CodedIndex = ctxt.Tables.ImplementationIsBig ? reader.ReadUInt32() : reader.ReadUInt16();
            Value = null;
        }

        public void ResolveIndexes(ReaderContext ctxt)
        {
            if (CodedIndex == 0)
                Value = null;
            else
            {
                var table = CodedIndex & 0x3;
                var index = (int)(CodedIndex >> 2);
                switch (table)
                {
                case 0:
                    Value = ctxt.Tables.FileTable[index];
                    break;
                case 1:
                    Value = ctxt.Tables.AssemblyRefTable[index];
                    break;
                case 2:
                    Value = ctxt.Tables.ExportedTypeTable[index];
                    break;
                default:
                    throw new PEException("invalid Implementation coded index");
                }
            }
        }

        public bool IsNull { get { return Value == null; } }

        public void PersistIndexes(WriterContext ctxt)
        {
            if (Value == null)
                CodedIndex = 0;
            else
            {
                switch (Value.Tag)
                {
                case TableTag.File:
                    CodedIndex = (uint)Value.Index << 2;
                    break;
                case TableTag.AssemblyRef:
                    CodedIndex = (uint)Value.Index << 2 | 1;
                    break;
                case TableTag.ExportedType:
                    CodedIndex = (uint)Value.Index << 2 | 2;
                    break;
                default:
                    throw new PEException("invalid Implementation row");
                }
            }
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            if (ctxt.Tables.ImplementationIsBig)
                writer.WriteUInt32(CodedIndex);
            else
                writer.WriteUInt16((ushort)CodedIndex);
        }

        public override string ToString()
        {
            return CodedIndex.ToString("x8");
        }
    }

    public struct CustomAttributeTypeRef
    {
        public uint CodedIndex;
        [CanBeNull]
        public Row Value;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            CodedIndex = ctxt.Tables.CustomAttributeTypeIsBig ? reader.ReadUInt32() : reader.ReadUInt16();
            Value = null;
        }

        public void ResolveIndexes(ReaderContext ctxt)
        {
            if (CodedIndex == 0)
                Value = null;
            else
            {
                var table = CodedIndex & 0x7;
                var index = (int)(CodedIndex >> 3);
                switch (table)
                {
                case 0:
                case 1:
                case 4:
                    throw new PEException("unused CustomAttributeType coded index");
                case 2:
                    Value = ctxt.Tables.MethodDefTable[index];
                    break;
                case 3:
                    Value = ctxt.Tables.MemberRefTable[index];
                    break;
                default:
                    throw new PEException("invalid CustomAttributeType coded index");
                }
            }
        }

        public bool IsNull { get { return Value == null; } }

        public void PersistIndexes(WriterContext ctxt)
        {
            if (Value == null)
                CodedIndex = 0;
            else
            {
                switch (Value.Tag)
                {
                case TableTag.MethodDef:
                    CodedIndex = (uint)Value.Index << 3 | 2;
                    break;
                case TableTag.MemberRef:
                    CodedIndex = (uint)Value.Index << 3 | 3;
                    break;
                default:
                    throw new PEException("invalid CustomAttributeType row");
                }
            }
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            if (ctxt.Tables.CustomAttributeTypeIsBig)
                writer.WriteUInt32(CodedIndex);
            else
                writer.WriteUInt16((ushort)CodedIndex);
        }

        public override string ToString()
        {
            return CodedIndex.ToString("x8");
        }
    }

    public struct ResolutionScopeRef
    {
        public uint CodedIndex;
        [CanBeNull]
        public Row Value;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            CodedIndex = ctxt.Tables.ResolutionScopeIsBig ? reader.ReadUInt32() : reader.ReadUInt16();
            Value = null;
        }

        public void ResolveIndexes(ReaderContext ctxt)
        {
            if (CodedIndex == 0)
                Value = null;
            else
            {
                var table = CodedIndex & 0x3;
                var index = (int)(CodedIndex >> 2);
                switch (table)
                {
                case 0:
                    Value = ctxt.Tables.ModuleTable[index];
                    break;
                case 1:
                    Value = ctxt.Tables.ModuleRefTable[index];
                    break;
                case 2:
                    Value = ctxt.Tables.AssemblyRefTable[index];
                    break;
                case 3:
                    Value = ctxt.Tables.TypeRefTable[index];
                    break;
                default:
                    throw new PEException("invalid ResolutionScope coded index tag");
                }
            }
        }

        public bool IsNull { get { return Value == null; } }

        public void PersistIndexes(WriterContext ctxt)
        {
            if (Value == null)
                CodedIndex = 0;
            else
            {
                switch (Value.Tag)
                {
                case TableTag.Module:
                    CodedIndex = (uint)Value.Index << 2;
                    break;
                case TableTag.ModuleRef:
                    CodedIndex = (uint)Value.Index << 2 | 1;
                    break;
                case TableTag.AssemblyRef:
                    CodedIndex = (uint)Value.Index << 2 | 2;
                    break;
                case TableTag.TypeRef:
                    CodedIndex = (uint)Value.Index << 2 | 3;
                    break;
                default:
                    throw new PEException("invalid ResolutionScope row");
                }
            }
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            if (ctxt.Tables.ResolutionScopeIsBig)
                writer.WriteUInt32(CodedIndex);
            else
                writer.WriteUInt16((ushort)CodedIndex);
        }

        public override string ToString()
        {
            return CodedIndex.ToString("x8");
        }
    }

    public struct TypeOrMethodDefRef
    {
        public uint CodedIndex;
        [CanBeNull]
        public Row Value;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            CodedIndex = ctxt.Tables.TypeOrMethodDefIsBig ? reader.ReadUInt32() : reader.ReadUInt16();
            Value = null;
        }

        public void ResolveIndexes(ReaderContext ctxt)
        {
            if (CodedIndex == 0)
                Value = null;
            else
            {
                var table = CodedIndex & 0x1;
                var index = (int)(CodedIndex >> 1);
                switch (table)
                {
                case 0:
                    Value = ctxt.Tables.TypeDefTable[index];
                    break;
                case 1:
                    Value = ctxt.Tables.MethodDefTable[index];
                    break;
                default:
                    throw new PEException("invalid TypeOrMethodDef coded index");
                }
            }
        }

        public bool IsNull { get { return Value == null; } }

        public void PersistIndexes(WriterContext ctxt)
        {
            if (Value == null)
                CodedIndex = 0;
            else
            {
                switch (Value.Tag)
                {
                case TableTag.TypeDef:
                    CodedIndex = (uint)Value.Index << 1;
                    break;
                case TableTag.MethodDef:
                    CodedIndex = (uint)Value.Index << 1 | 1;
                    break;
                default:
                    throw new PEException("invalid TypeOrMethodDef row");
                }
            }
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            if (ctxt.Tables.TypeOrMethodDefIsBig)
                writer.WriteUInt32(CodedIndex);
            else
                writer.WriteUInt16((ushort)CodedIndex);
        }

        public override string ToString()
        {
            return CodedIndex.ToString("x8");
        }
    }

    public struct AssemblyRefRef
    {
        public uint CodedIndex;
        [CanBeNull]
        public AssemblyRefRow Value;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            CodedIndex = ctxt.Tables.AssemblyRefIsBig ? reader.ReadUInt32() : reader.ReadUInt16();
            Value = null;
        }

        public void ResolveIndexes(ReaderContext ctxt)
        {
            Value = CodedIndex == 0 ? null : ctxt.Tables.AssemblyRefTable[(int)CodedIndex];
        }

        public bool IsNull { get { return Value == null; } }

        public void PersistIndexes(WriterContext ctxt)
        {
            CodedIndex = Value == null ? 0 : (uint)Value.Index;
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            if (ctxt.Tables.AssemblyRefIsBig)
                writer.WriteUInt32(CodedIndex);
            else
                writer.WriteUInt16((ushort)CodedIndex);
        }

        public override string ToString()
        {
            return CodedIndex.ToString("x8");
        }
    }

    public struct TypeDefRef
    {
        public uint CodedIndex;
        [CanBeNull]
        public TypeDefRow Value;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            CodedIndex = ctxt.Tables.TypeDefIsBig ? reader.ReadUInt32() : reader.ReadUInt16();
            Value = null;
        }

        public void ResolveIndexes(ReaderContext ctxt)
        {
            Value = CodedIndex == 0 ? null : ctxt.Tables.TypeDefTable[(int)CodedIndex];
        }

        public bool IsNull { get { return Value == null; } }

        public void PersistIndexes(WriterContext ctxt)
        {
            CodedIndex = Value == null ? 0 : (uint)Value.Index;
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            if (ctxt.Tables.TypeDefIsBig)
                writer.WriteUInt32(CodedIndex);
            else
                writer.WriteUInt16((ushort)CodedIndex);
        }

        public override string ToString()
        {
            return CodedIndex.ToString("x8");
        }
    }

    public struct EventRef
    {
        public uint CodedIndex;
        [CanBeNull]
        public EventRow Value;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            CodedIndex = ctxt.Tables.EventIsBig ? reader.ReadUInt32() : reader.ReadUInt16();
            Value = null;
        }

        public void ResolveIndexes(ReaderContext ctxt)
        {
            Value = CodedIndex == 0 ? null : ctxt.Tables.EventTable[(int)CodedIndex];
        }

        public bool IsNull { get { return Value == null; } }

        public void PersistIndexes(WriterContext ctxt)
        {
            CodedIndex = Value == null ? 0 : (uint)Value.Index;
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            if (ctxt.Tables.EventIsBig)
                writer.WriteUInt32(CodedIndex);
            else
                writer.WriteUInt16((ushort)CodedIndex);
        }

        public override string ToString()
        {
            return CodedIndex.ToString("x8");
        }
    }

    public struct FieldRef
    {
        public uint CodedIndex;
        [CanBeNull]
        public FieldRow Value;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            CodedIndex = ctxt.Tables.FieldIsBig ? reader.ReadUInt32() : reader.ReadUInt16();
            Value = null;
        }

        public void ResolveIndexes(ReaderContext ctxt)
        {
            Value = CodedIndex == 0 ? null : ctxt.Tables.FieldTable[(int)CodedIndex];
        }

        public bool IsNull { get { return Value == null; } }

        public void PersistIndexes(WriterContext ctxt)
        {
            CodedIndex = Value == null ? 0 : (uint)Value.Index;
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            if (ctxt.Tables.FieldIsBig)
                writer.WriteUInt32(CodedIndex);
            else
                writer.WriteUInt16((ushort)CodedIndex);
        }

        public override string ToString()
        {
            return CodedIndex.ToString("x8");
        }
    }

    public struct GenericParamRef
    {
        public uint CodedIndex;
        [CanBeNull]
        public GenericParamRow Value;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            CodedIndex = ctxt.Tables.GenericParamIsBig ? reader.ReadUInt32() : reader.ReadUInt16();
            Value = null;
        }

        public void ResolveIndexes(ReaderContext ctxt)
        {
            Value = CodedIndex == 0 ? null : ctxt.Tables.GenericParamTable[(int)CodedIndex];
        }

        public bool IsNull { get { return Value == null; } }

        public void PersistIndexes(WriterContext ctxt)
        {
            CodedIndex = Value == null ? 0 : (uint)Value.Index;
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            if (ctxt.Tables.GenericParamIsBig)
                writer.WriteUInt32(CodedIndex);
            else
                writer.WriteUInt16((ushort)CodedIndex);
        }

        public override string ToString()
        {
            return CodedIndex.ToString("x8");
        }
    }

    public struct ModuleRefRef
    {
        public uint CodedIndex;
        [CanBeNull]
        public ModuleRefRow Value;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            CodedIndex = ctxt.Tables.ModuleRefIsBig ? reader.ReadUInt32() : reader.ReadUInt16();
            Value = null;
        }

        public void ResolveIndexes(ReaderContext ctxt)
        {
            Value = CodedIndex == 0 ? null : ctxt.Tables.ModuleRefTable[(int)CodedIndex];
        }

        public bool IsNull { get { return Value == null; } }

        public void PersistIndexes(WriterContext ctxt)
        {
            CodedIndex = Value == null ? 0 : (uint)Value.Index;
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            if (ctxt.Tables.ModuleRefIsBig)
                writer.WriteUInt32(CodedIndex);
            else
                writer.WriteUInt16((ushort)CodedIndex);
        }

        public override string ToString()
        {
            return CodedIndex.ToString("x8");
        }
    }

    public struct ParamRef
    {
        public uint CodedIndex;
        [CanBeNull]
        public ParamRow Value;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            CodedIndex = ctxt.Tables.ParamIsBig ? reader.ReadUInt32() : reader.ReadUInt16();
            Value = null;
        }

        public void ResolveIndexes(ReaderContext ctxt)
        {
            Value = CodedIndex == 0 ? null : ctxt.Tables.ParamTable[(int)CodedIndex];
        }

        public bool IsNull { get { return Value == null; } }

        public void PersistIndexes(WriterContext ctxt)
        {
            CodedIndex = Value == null ? 0 : (uint)Value.Index;
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            if (ctxt.Tables.ParamIsBig)
                writer.WriteUInt32(CodedIndex);
            else
                writer.WriteUInt16((ushort)CodedIndex);
        }

        public override string ToString()
        {
            return CodedIndex.ToString("x8");
        }
    }

    public struct MethodDefRef
    {
        public uint CodedIndex;
        [CanBeNull]
        public MethodDefRow Value;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            CodedIndex = ctxt.Tables.MethodDefIsBig ? reader.ReadUInt32() : reader.ReadUInt16();
            Value = null;
        }

        public void ResolveIndexes(ReaderContext ctxt)
        {
            Value = CodedIndex == 0 ? null : ctxt.Tables.MethodDefTable[(int)CodedIndex];
        }

        public bool IsNull { get { return Value == null; } }

        public void PersistIndexes(WriterContext ctxt)
        {
            CodedIndex = Value == null ? 0 : (uint)Value.Index;
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            if (ctxt.Tables.MethodDefIsBig)
                writer.WriteUInt32(CodedIndex);
            else
                writer.WriteUInt16((ushort)CodedIndex);
        }

        public override string ToString()
        {
            return CodedIndex.ToString("x8");
        }
    }

    public struct PropertyRef
    {
        public uint CodedIndex;
        [CanBeNull]
        public PropertyRow Value;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            CodedIndex = ctxt.Tables.PropertyIsBig ? reader.ReadUInt32() : reader.ReadUInt16();
            Value = null;
        }

        public void ResolveIndexes(ReaderContext ctxt)
        {
            Value = CodedIndex == 0 ? null : ctxt.Tables.PropertyTable[(int)CodedIndex];
        }

        public bool IsNull { get { return Value == null; } }

        public void PersistIndexes(WriterContext ctxt)
        {
            CodedIndex = Value == null ? 0 : (uint)Value.Index;
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            if (ctxt.Tables.PropertyIsBig)
                writer.WriteUInt32(CodedIndex);
            else
                writer.WriteUInt16((ushort)CodedIndex);
        }

        public override string ToString()
        {
            return CodedIndex.ToString("x8");
        }
    }

    public struct EventMapRef
    {
        public uint CodedIndex;
        [CanBeNull]
        public EventMapRow Value;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            CodedIndex = ctxt.Tables.EventMapIsBig ? reader.ReadUInt32() : reader.ReadUInt16();
            Value = null;
        }

        public void ResolveIndexes(ReaderContext ctxt)
        {
            Value = CodedIndex == 0 ? null : ctxt.Tables.EventMapTable[(int)CodedIndex];
        }

        public bool IsNull { get { return Value == null; } }

        public void PersistIndexes(WriterContext ctxt)
        {
            CodedIndex = Value == null ? 0 : (uint)Value.Index;
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            if (ctxt.Tables.EventMapIsBig)
                writer.WriteUInt32(CodedIndex);
            else
                writer.WriteUInt16((ushort)CodedIndex);
        }

        public override string ToString()
        {
            return CodedIndex.ToString("x8");
        }
    }

    public struct PropertyMapRef
    {
        public uint CodedIndex;
        [CanBeNull]
        public PropertyMapRow Value;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            CodedIndex = ctxt.Tables.PropertyMapIsBig ? reader.ReadUInt32() : reader.ReadUInt16();
            Value = null;
        }

        public void ResolveIndexes(ReaderContext ctxt)
        {
            Value = CodedIndex == 0 ? null : ctxt.Tables.PropertyMapTable[(int)CodedIndex];
        }

        public bool IsNull { get { return Value == null; } }

        public void PersistIndexes(WriterContext ctxt)
        {
            CodedIndex = Value == null ? 0 : (uint)Value.Index;
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            if (ctxt.Tables.PropertyMapIsBig)
                writer.WriteUInt32(CodedIndex);
            else
                writer.WriteUInt16((ushort)CodedIndex);
        }

        public override string ToString()
        {
            return CodedIndex.ToString("x8");
        }
    }

    public struct TokenRef
    {
        public uint CodedIndex;
        [CanBeNull]
        public Row Value;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            CodedIndex = reader.ReadUInt32();
            Value = null;
        }

        public void ResolveIndexes(ReaderContext ctxt)
        {
            if (CodedIndex == 0)
                Value = null;
            else
            {
                var table = (TableTag)(CodedIndex >> 24);
                var index = (int)(CodedIndex & 0xffffff);
                switch (table)
                {
                case TableTag.Assembly:
                    Value = ctxt.Tables.AssemblyTable[index];
                    break;
                case TableTag.AssemblyOS:
                    Value = ctxt.Tables.AssemblyOSTable[index];
                    break;
                case TableTag.AssemblyProcessor:
                    Value = ctxt.Tables.AssemblyProcessorTable[index];
                    break;
                case TableTag.AssemblyRef:
                    Value = ctxt.Tables.AssemblyRefTable[index];
                    break;
                case TableTag.AssemblyRefOS:
                    Value = ctxt.Tables.AssemblyRefOSTable[index];
                    break;
                case TableTag.AssemblyRefProcessor:
                    Value = ctxt.Tables.AssemblyRefProcessorTable[index];
                    break;
                case TableTag.ClassLayout:
                    Value = ctxt.Tables.ClassLayoutTable[index];
                    break;
                case TableTag.Constant:
                    Value = ctxt.Tables.ConstantTable[index];
                    break;
                case TableTag.CustomAttribute:
                    Value = ctxt.Tables.CustomAttributeTable[index];
                    break;
                case TableTag.DeclSecurity:
                    Value = ctxt.Tables.DeclSecurityTable[index];
                    break;
                case TableTag.EventMap:
                    Value = ctxt.Tables.EventMapTable[index];
                    break;
                case TableTag.Event:
                    Value = ctxt.Tables.EventTable[index];
                    break;
                case TableTag.ExportedType:
                    Value = ctxt.Tables.ExportedTypeTable[index];
                    break;
                case TableTag.Field:
                    Value = ctxt.Tables.FieldTable[index];
                    break;
                case TableTag.FieldLayout:
                    Value = ctxt.Tables.FieldLayoutTable[index];
                    break;
                case TableTag.FieldMarshal:
                    Value = ctxt.Tables.FieldMarshalTable[index];
                    break;
                case TableTag.FieldRVA:
                    Value = ctxt.Tables.FieldRVATable[index];
                    break;
                case TableTag.File:
                    Value = ctxt.Tables.FileTable[index];
                    break;
                case TableTag.GenericParam:
                    Value = ctxt.Tables.GenericParamTable[index];
                    break;
                case TableTag.GenericParamConstraint:
                    Value = ctxt.Tables.GenericParamConstraintTable[index];
                    break;
                case TableTag.ImplMap:
                    Value = ctxt.Tables.ImplMapTable[index];
                    break;
                case TableTag.InterfaceImpl:
                    Value = ctxt.Tables.InterfaceImplTable[index];
                    break;
                case TableTag.ManifestResource:
                    Value = ctxt.Tables.ManifestResourceTable[index];
                    break;
                case TableTag.MemberRef:
                    Value = ctxt.Tables.MemberRefTable[index];
                    break;
                case TableTag.MethodDef:
                    Value = ctxt.Tables.MethodDefTable[index];
                    break;
                case TableTag.MethodImpl:
                    Value = ctxt.Tables.MethodImplTable[index];
                    break;
                case TableTag.MethodSemantics:
                    Value = ctxt.Tables.MethodSemanticsTable[index];
                    break;
                case TableTag.MethodSpec:
                    Value = ctxt.Tables.MethodSpecTable[index];
                    break;
                case TableTag.Module:
                    Value = ctxt.Tables.ModuleTable[index];
                    break;
                case TableTag.ModuleRef:
                    Value = ctxt.Tables.ModuleRefTable[index];
                    break;
                case TableTag.NestedClass:
                    Value = ctxt.Tables.NestedClassTable[index];
                    break;
                case TableTag.Param:
                    Value = ctxt.Tables.ParamTable[index];
                    break;
                case TableTag.Property:
                    Value = ctxt.Tables.PropertyTable[index];
                    break;
                case TableTag.PropertyMap:
                    Value = ctxt.Tables.PropertyMapTable[index];
                    break;
                case TableTag.StandAloneSig:
                    Value = ctxt.Tables.StandAloneSigTable[index];
                    break;
                case TableTag.TypeDef:
                    Value = ctxt.Tables.TypeDefTable[index];
                    break;
                case TableTag.TypeRef:
                    Value = ctxt.Tables.TypeRefTable[index];
                    break;
                case TableTag.TypeSpec:
                    Value = ctxt.Tables.TypeSpecTable[index];
                    break;
                default:
                    throw new PEException("invalid metadata token table");
                }
            }
        }

        public bool IsNull { get { return Value == null; } }

        public void PersistIndexes(WriterContext ctxt)
        {
            if (Value == null)
                CodedIndex = 0;
            else
            {
                var table = (uint)Value.Tag;
                var index = (uint)Value.Index;
                CodedIndex = table << 24 | (index & 0xffffff);
            }
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            writer.WriteUInt32(CodedIndex);
        }

        public override string ToString()
        {
            return CodedIndex.ToString("x8");
        }
    }

    public struct TypeDefOrRefVarLenRef
    {
        public uint CodedIndex;
        [CanBeNull]
        public Row Value;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            CodedIndex = reader.ReadCompressedUInt32();
            Value = null;
        }

        public void ResolveIndexes(ReaderContext ctxt)
        {
            if (CodedIndex == 0)
                Value = null;
            else
            {
                var table = CodedIndex & 0x3;
                var index = (int)(CodedIndex >> 2);
                switch (table)
                {
                case 0:
                    Value = ctxt.Tables.TypeDefTable[index];
                    break;
                case 1:
                    Value = ctxt.Tables.TypeRefTable[index];
                    break;
                case 2:
                    Value = ctxt.Tables.TypeSpecTable[index];
                    break;
                default:
                    throw new PEException("invalid TypeDefOrRef coded index");
                }
            }
        }

        public bool IsNull { get { return Value == null; } }

        public void PersistIndexes(WriterContext ctxt)
        {
            CodedIndex = Value == null ? 0 : (uint)Value.Index;
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            writer.WriteCompressedUInt32(CodedIndex);
        }

        public override string ToString()
        {
            return CodedIndex.ToString("x8");
        }
    }

    public struct EventListRef
    {
        // Some encoding of a base-1 index into a table. List continues until end or start of rows for next parent record
        public uint FirstCodedIndex;
        public IImSeq<EventRow> Value;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            FirstCodedIndex = ctxt.Tables.EventIsBig ? reader.ReadUInt32() : reader.ReadUInt16();
            Value = null;
        }

        public void ResolveIndexes(ReaderContext ctxt, EventMapRow parentRow)
        {
            var rows = new Seq<EventRow>();
            if (FirstCodedIndex > 0)
            {
                var limit = (uint)ctxt.Tables.EventTable.Count + 1;
                var parentIndex = parentRow.Index;
                while (++parentIndex <= ctxt.Tables.EventMapTable.Count)
                {
                    var nextIndex = ctxt.Tables.EventMapTable[parentIndex].EventList.FirstCodedIndex;
                    if (nextIndex > 0)
                    {
                        limit = nextIndex;
                        break;
                    }
                }
                var index = FirstCodedIndex;
                while (index < limit)
                    rows.Add(ctxt.Tables.EventTable[(int)index++]);
            }
            Value = rows;
        }

        public bool IsNull { get { return Value.Count == 0; } }

        public void PersistIndexes(WriterContext ctxt, EventMapRow parentRow)
        {
            if (Value.Count == 0)
            {
                var parentIndex = parentRow.Index;
                while (++parentIndex <= ctxt.Tables.EventMapTable.Count)
                {
                    var nextList = ctxt.Tables.EventMapTable[parentIndex].EventList.Value;
                    if (nextList.Count > 0)
                    {
                        FirstCodedIndex = (uint)nextList[0].Index;
                        return;
                    }
                }
                FirstCodedIndex = (uint)ctxt.Tables.EventTable.Count + 1;
            }
            else
                FirstCodedIndex = (uint)Value[0].Index;
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            if (ctxt.Tables.EventIsBig)
                writer.WriteUInt32(FirstCodedIndex);
            else
                writer.WriteUInt16((ushort)FirstCodedIndex);
        }

        public override string ToString()
        {
            return FirstCodedIndex.ToString("x8");
        }
    }

    public struct PropertyListRef
    {
        // Some encoding of a base-1 index into a table. List continues until end or start of rows for next parent record
        public uint FirstCodedIndex;
        public IImSeq<PropertyRow> Value;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            FirstCodedIndex = ctxt.Tables.PropertyIsBig ? reader.ReadUInt32() : reader.ReadUInt16();
            Value = null;
        }

        public void ResolveIndexes(ReaderContext ctxt, PropertyMapRow parentRow)
        {
            var rows = new Seq<PropertyRow>();
            if (FirstCodedIndex > 0)
            {
                var limit = (uint)ctxt.Tables.PropertyTable.Count + 1;
                var parentIndex = parentRow.Index;
                while (++parentIndex <= ctxt.Tables.PropertyMapTable.Count)
                {
                    var nextIndex = ctxt.Tables.PropertyMapTable[parentIndex].PropertyList.FirstCodedIndex;
                    if (nextIndex > 0)
                    {
                        limit = nextIndex;
                        break;
                    }
                }
                var index = FirstCodedIndex;
                while (index < limit)
                    rows.Add(ctxt.Tables.PropertyTable[(int)index++]);
            }
            Value = rows;
        }

        public bool IsNull { get { return Value.Count == 0; } }

        public void PersistIndexes(WriterContext ctxt, PropertyMapRow parentRow)
        {
            if (Value.Count == 0)
            {
                var parentIndex = parentRow.Index;
                while (++parentIndex <= ctxt.Tables.PropertyMapTable.Count)
                {
                    var nextList = ctxt.Tables.PropertyMapTable[parentIndex].PropertyList.Value;
                    if (nextList.Count > 0)
                    {
                        FirstCodedIndex = (uint)nextList[0].Index;
                        return;
                    }
                }
                FirstCodedIndex = (uint)ctxt.Tables.PropertyTable.Count + 1;
            }
            else
                FirstCodedIndex = (uint)Value[0].Index;
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            if (ctxt.Tables.PropertyIsBig)
                writer.WriteUInt32(FirstCodedIndex);
            else
                writer.WriteUInt16((ushort)FirstCodedIndex);
        }

        public override string ToString()
        {
            return FirstCodedIndex.ToString("x8");
        }
    }

    public struct ParamListRef
    {
        // Some encoding of a base-1 index into a table. List continues until end or start of rows for next parent record
        public uint FirstCodedIndex;
        public IImSeq<ParamRow> Value;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            FirstCodedIndex = ctxt.Tables.ParamIsBig ? reader.ReadUInt32() : reader.ReadUInt16();
            Value = null;
        }

        public void ResolveIndexes(ReaderContext ctxt, MethodDefRow parentRow)
        {
            var rows = new Seq<ParamRow>();
            if (FirstCodedIndex > 0)
            {
                var limit = (uint)ctxt.Tables.ParamTable.Count + 1;
                var parentIndex = parentRow.Index;
                while (++parentIndex <= ctxt.Tables.MethodDefTable.Count)
                {
                    var nextIndex = ctxt.Tables.MethodDefTable[parentIndex].ParamList.FirstCodedIndex;
                    if (nextIndex > 0)
                    {
                        limit = nextIndex;
                        break;
                    }
                }
                var index = FirstCodedIndex;
                while (index < limit)
                    rows.Add(ctxt.Tables.ParamTable[(int)index++]);
            }
            Value = rows;
        }

        public bool IsNull { get { return Value.Count == 0; } }

        public void PersistIndexes(WriterContext ctxt, MethodDefRow parentRow)
        {
            if (Value.Count == 0)
            {
                var parentIndex = parentRow.Index;
                while (++parentIndex <= ctxt.Tables.MethodDefTable.Count)
                {
                    var nextList = ctxt.Tables.MethodDefTable[parentIndex].ParamList.Value;
                    if (nextList.Count > 0)
                    {
                        FirstCodedIndex = (uint)nextList[0].Index;
                        return;
                    }
                }
                FirstCodedIndex = (uint)ctxt.Tables.ParamTable.Count + 1;
            }
            else
                FirstCodedIndex = (uint)Value[0].Index;
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            if (ctxt.Tables.ParamIsBig)
                writer.WriteUInt32(FirstCodedIndex);
            else
                writer.WriteUInt16((ushort)FirstCodedIndex);
        }

        public override string ToString()
        {
            return FirstCodedIndex.ToString("x8");
        }
    }

    public struct FieldListRef
    {
        // Some encoding of a base-1 index into a table. List continues until end or start of rows for next parent record
        public uint FirstCodedIndex;
        public IImSeq<FieldRow> Value;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            FirstCodedIndex = ctxt.Tables.FieldIsBig ? reader.ReadUInt32() : reader.ReadUInt16();
            Value = null;
        }

        public void ResolveIndexes(ReaderContext ctxt, TypeDefRow parentRow)
        {
            var rows = new Seq<FieldRow>();
            if (FirstCodedIndex > 0)
            {
                var limit = (uint)ctxt.Tables.FieldTable.Count + 1;
                var parentIndex = parentRow.Index;
                while (++parentIndex <= ctxt.Tables.TypeDefTable.Count)
                {
                    var nextIndex = ctxt.Tables.TypeDefTable[parentIndex].FieldList.FirstCodedIndex;
                    if (nextIndex > 0)
                    {
                        limit = nextIndex;
                        break;
                    }
                }
                var index = FirstCodedIndex;
                while (index < limit)
                    rows.Add(ctxt.Tables.FieldTable[(int)index++]);
            }
            Value = rows;
        }

        public bool IsNull { get { return Value.Count == 0; } }

        public void PersistIndexes(WriterContext ctxt, TypeDefRow parentRow)
        {
            if (Value.Count == 0)
            {
                var parentIndex = parentRow.Index;
                while (++parentIndex <= ctxt.Tables.TypeDefTable.Count)
                {
                    var nextList = ctxt.Tables.TypeDefTable[parentIndex].FieldList.Value;
                    if (nextList.Count > 0)
                    {
                        FirstCodedIndex = (uint)nextList[0].Index;
                        return;
                    }
                }
                FirstCodedIndex = (uint)ctxt.Tables.FieldTable.Count + 1;
            }
            else
                FirstCodedIndex = (uint)Value[0].Index;
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            if (ctxt.Tables.FieldIsBig)
                writer.WriteUInt32(FirstCodedIndex);
            else
                writer.WriteUInt16((ushort)FirstCodedIndex);
        }

        public override string ToString()
        {
            return FirstCodedIndex.ToString("x8");
        }
    }

    public struct MethodDefListRef
    {
        // Some encoding of a base-1 index into a table. List continues until end or start of rows for next parent record
        public uint FirstCodedIndex;
        public IImSeq<MethodDefRow> Value;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            FirstCodedIndex = ctxt.Tables.MethodDefIsBig ? reader.ReadUInt32() : reader.ReadUInt16();
            Value = null;
        }

        public void ResolveIndexes(ReaderContext ctxt, TypeDefRow parentRow)
        {
            var rows = new Seq<MethodDefRow>();
            if (FirstCodedIndex > 0)
            {
                var limit = (uint)ctxt.Tables.MethodDefTable.Count + 1;
                var parentIndex = parentRow.Index;
                while (++parentIndex <= ctxt.Tables.TypeDefTable.Count)
                {
                    var nextIndex = ctxt.Tables.TypeDefTable[parentIndex].MethodList.FirstCodedIndex;
                    if (nextIndex > 0)
                    {
                        limit = nextIndex;
                        break;
                    }
                }
                var index = FirstCodedIndex;
                while (index < limit)
                    rows.Add(ctxt.Tables.MethodDefTable[(int)index++]);
            }
            Value = rows;
        }

        public bool IsNull { get { return Value.Count == 0; } }

        public void PersistIndexes(WriterContext ctxt, TypeDefRow parentRow)
        {
            if (Value.Count == 0)
            {
                var parentIndex = parentRow.Index;
                while (++parentIndex <= ctxt.Tables.TypeDefTable.Count)
                {
                    var nextList = ctxt.Tables.TypeDefTable[parentIndex].MethodList.Value;
                    if (nextList.Count > 0)
                    {
                        FirstCodedIndex = (uint)nextList[0].Index;
                        return;
                    }
                }
                FirstCodedIndex = (uint)ctxt.Tables.MethodDefTable.Count + 1;
            }
            else
                FirstCodedIndex = (uint)Value[0].Index;
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            if (ctxt.Tables.MethodDefIsBig)
                writer.WriteUInt32(FirstCodedIndex);
            else
                writer.WriteUInt16((ushort)FirstCodedIndex);
        }

        public override string ToString()
        {
            return FirstCodedIndex.ToString("x8");
        }
    }
}