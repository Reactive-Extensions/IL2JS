using System;
using System.Text;

namespace Microsoft.LiveLabs.PE
{
    public class BlobReader
    {
        private readonly byte[] data;
        // Following offsets are w.r.t. above data array
        private readonly uint baseOffset; // Which byte corresponds to external offset 0
        private uint currOffset; // Next byte to read
        private readonly uint dataLimit;  // First byte beyond valid data
        private readonly uint readLimit;  // First byte beyond readable data
        // NOTE: Bytes between dataLimit and readLimit are implicitly zero.
        // NOTE: readLimit may be larger than length of data array

        public BlobReader(byte[] data, uint baseOffset, uint dataLimit, uint readLimit)
        {
            this.data = data;
            this.baseOffset = baseOffset;
            currOffset = baseOffset;
            this.dataLimit = dataLimit;
            this.readLimit = readLimit;
        }


        public BlobReader(byte[] data, uint baseOffset)
            : this(data, baseOffset, (uint)data.Length, (uint)data.Length)
        {
        }

        public BlobReader(byte[] data)
            : this(data, 0, (uint)data.Length, (uint)data.Length)
        {
        }

         public BlobReader(BlobReader outer, uint offset)
            : this(outer.data, outer.baseOffset + offset, outer.dataLimit, outer.readLimit)
        {
        }

         public BlobReader(BlobReader outer, uint offset, uint dataLimit)
             : this(outer.data, outer.baseOffset + offset, outer.baseOffset + dataLimit, outer.baseOffset + dataLimit)
         {
         }

         public BlobReader(BlobReader outer, uint offset, uint dataLimit, uint readLimit)
             : this(outer.data, outer.baseOffset + offset, outer.baseOffset + dataLimit, outer.baseOffset + readLimit)
         {
         }

        // Current reader offset, relative to baseOffset
        public uint Offset
        {
            get
            {
                return currOffset - baseOffset;
            }
            set
            {
                currOffset = baseOffset + value;
            }
        }

        public bool AtEndOfBlob
        {
            get { return currOffset >= readLimit; }
        }

        public uint RemainingBytes
        {
            get { return readLimit - currOffset; }
        }

        public byte[] ReadBytes(uint length)
        {
            if (currOffset + length > readLimit)
                throw new PEException("attempting to read beyond end of stream");
            var bytes = new byte[length];
            if (currOffset >= dataLimit)
            {
                Array.Clear(bytes, 0, (int)length);
                currOffset += length;
            }
            else if (currOffset + length <= dataLimit)
            {
                Array.Copy(data, currOffset, bytes, 0, (int)length);
                currOffset += length;
            }
            else
            {
                for (var i = 0; i < length; i++)
                {
                    bytes[i] = currOffset < dataLimit ? data[currOffset] : (byte)0;
                    currOffset++;
                }
            }
            return bytes;
        }

        public byte[] ReadBytes()
        {
            return ReadBytes(RemainingBytes);
        }

        public byte ReadByte()
        {
            if (currOffset + 1 <= dataLimit)
                return data[currOffset++];
            else if (currOffset >= dataLimit && currOffset + 1 <= readLimit)
            {
                currOffset++;
                return 0;
            }
            else
            {
                var d = ReadBytes(1);
                return d[0];
            }
        }

        public sbyte ReadSByte()
        {
            return (sbyte)ReadByte();
        }

        public ushort ReadUInt16()
        {
            if (currOffset + 2 <= dataLimit)
                return (ushort)((uint)data[currOffset++] | ((uint)data[currOffset++] << 8));
            else if (currOffset >= dataLimit && currOffset + 2 <= readLimit)
            {
                currOffset += 2;
                return 0;
            }
            else
            {
                var d = ReadBytes(2);
                return (ushort)((uint)d[0] | ((uint)d[1] << 8));
            }
        }

        public short ReadInt16()
        {
            return (short)ReadUInt16();
        }

        public uint ReadUInt24()
        {
            if (currOffset + 3 <= dataLimit)
                return (uint)data[currOffset++] | ((uint)(data[currOffset++]) << 8) |
                       ((uint)(data[currOffset++]) << 16);
            else if (currOffset >= dataLimit && currOffset + 3 <= readLimit)
            {
                currOffset += 3;
                return 0;
            }
            else
            {
                var d = ReadBytes(3);
                return (uint)d[0] | ((uint)(d[1]) << 8) | ((uint)(d[2]) << 16);
            }
        }

        public uint ReadUInt32()
        {
            if (currOffset + 4 <= dataLimit)
                return (uint)data[currOffset++] | ((uint)data[currOffset++] << 8) | ((uint)data[currOffset++] << 16) |
                       ((uint)data[currOffset++] << 24);
            else if (currOffset >= dataLimit && currOffset + 4 <= readLimit)
            {
                currOffset += 4;
                return 0;
            }
            else
            {
                var d = ReadBytes(4);
                return (uint)d[currOffset++] | ((uint)d[currOffset++] << 8) | ((uint)d[currOffset++] << 16) |
                       ((uint)d[currOffset++] << 24);
            }
        }

        public int ReadInt32()
        {
            return (int)ReadUInt32();
        }

        public ulong ReadUInt64()
        {
            if (currOffset + 8 <= dataLimit)
                return (ulong)data[currOffset++] | ((ulong)data[currOffset++] << 8) |
                       ((ulong)data[currOffset++] << 16) | ((ulong)data[currOffset++] << 24) |
                       ((ulong)data[currOffset++] << 32) | ((ulong)data[currOffset++] << 40) |
                       ((ulong)data[currOffset++] << 48) | ((ulong)data[currOffset++] << 56);
            else if (currOffset >= dataLimit && currOffset + 8 <= readLimit)
            {
                currOffset += 8;
                return 0;
            }
            else
            {
                var d = ReadBytes(8);
                return (ulong)d[currOffset++] | ((ulong)d[currOffset++] << 8) | ((ulong)d[currOffset++] << 16) |
                       ((ulong)d[currOffset++] << 24) | ((ulong)d[currOffset++] << 32) |
                       ((ulong)d[currOffset++] << 40) | ((ulong)d[currOffset++] << 48) |
                       ((ulong)d[currOffset++] << 56);
            }
        }

        public long ReadInt64()
        {
            return (long)ReadUInt64();
        }

        public float ReadSingle()
        {
            if (currOffset + 4 <= dataLimit)
            {
                var value = BitConverter.ToSingle(data, (int)currOffset);
                currOffset += 4;
                return value;
            }
            else if (currOffset >= dataLimit && currOffset + 4 <= readLimit)
            {
                currOffset += 4;
                return 0.0f;
            }
            else
            {
                var d = ReadBytes(4);
                var value = BitConverter.ToSingle(d, 0);
                currOffset += 4;
                return value;
            }
        }

        public double ReadDouble()
        {
            if (currOffset + 8 <= dataLimit)
            {
                var value = BitConverter.ToDouble(data, (int)currOffset);
                currOffset += 8;
                return value;
            }
            else if (currOffset >= dataLimit && currOffset + 8 <= readLimit)
            {
                currOffset += 8;
                return 0.0;
            }
            else
            {
                var d = ReadBytes(8);
                var value = BitConverter.ToDouble(d, 0);
                currOffset += 8;
                return value;
            }
        }

        // S24.2.4, S23.2
        public uint ReadCompressedUInt32()
        {
            var headerByte = ReadByte();
            if ((headerByte & 0x80) == 0x00)
                return headerByte;
            else if ((headerByte & 0xc0) == 0x80)
                return ((uint)(headerByte & 0x3f) << 8) | ReadByte();
            else if ((headerByte & 0xe0) == 0xc0)
                return ((uint)(headerByte & 0x1f) << 24) | ((uint)ReadByte() << 16) | ((uint)ReadByte() << 8) | ReadByte();
            else if (headerByte == 0xFF)
                return 0xffffffff;
            else
                throw new PEException("invalid bytes for 7-bit encoding");
        }

        public string ReadAsciiZeroPaddedString(uint length)
        {
            return Encoding.ASCII.GetString(ReadBytes(length)).Trim('\0');
        }

        public string ReadUTF8ZeroPaddedString(uint length)
        {
            return Encoding.UTF8.GetString(ReadBytes(length)).Trim('\0');
        }

        private uint NextZero()
        {
            var i = currOffset;
            while (i < dataLimit && data[i] != 0)
                i++;
            if (i >= readLimit)
                throw new PEException("missing terminating zero for string");
            return i;
        }

        public string ReadAsciiZeroTerminatedString(uint alignment)
        {
            var blob = ReadBytes(NextZero() - currOffset);
            ReadByte();
            var extra = (uint)(blob.Length + 1) % alignment;
            if (extra != 0)
                Pad(alignment - extra);
            return Encoding.ASCII.GetString(blob);
        }

        // S24.2.4
        public string ReadUTF8SizedString()
        {
            var len = ReadCompressedUInt32();
            if (len == 0xffffffffu)
                return null;
            else if (len == 0)
                return "";
            else
            {
                var blob = ReadBytes(len);
                return new String(Encoding.UTF8.GetChars(blob));
            }
        }

        public string ReadUTF16SizedString()
        {
            var len = ReadCompressedUInt32();
            if (len == 0xffffffffu)
                return null;
            else if (len == 0)
                return "";
            else
            {
                if (len % 2 != 0)
                    throw new PEException("invalid UTF16 string length");
                var blob = ReadBytes(len);
                return new String(Encoding.Unicode.GetChars(blob));
            }
        }

        public string ReadUTF16SizedStringWithEncodingHint()
        {
            var len = ReadCompressedUInt32();
            if (len == 0xffffffffu)
                return null;
            else if (len == 0)
                return "";
            else
            {
                if (len % 2 == 0)
                    throw new PEException("invalid UTF16 string with encoding hint length");
                var blob = ReadBytes(len - 1u);
                var isUTF16 = ReadByte() > 0; // ignored
                return new String(Encoding.Unicode.GetChars(blob));
            }
        }

        public string ReadUTF8ZeroTerminatedString()
        {
            var blob = ReadBytes(NextZero() - currOffset);
            ReadByte();
            return Encoding.UTF8.GetString(blob);
        }

        public string ReadUTF8SizedZeroPaddedString(uint alignment)
        {
            var length = ReadUInt32();
            if (length % alignment != 0)
                throw new PEException("incorrectly aligned string");
            var blob = ReadBytes(length);
            var i = blob.Length - 1;
            var j = 0;
            while (i >= 0 && blob[i] == 0 && j < alignment)
            {
                i--;
                j++;
            }
            return Encoding.UTF8.GetString(blob, 0, i + 1);
        }

        // Aligned w.r.t. underlying data, not current view
        public void Align(int alignment)
        {
            while ((currOffset % alignment) != 0)
            {
                if (ReadByte() != 0)
                    throw new PEException("invalid alignment padding");
            }
        }

        public void Pad(uint length)
        {
            for (var i = 0; i < length; i++)
            {
                if (ReadByte() != 0)
                    throw new PEException("invalid padding");
            }
        }
    }
}