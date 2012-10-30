using System;
using System.Text;

namespace Microsoft.LiveLabs.PE
{
    public class BlobWriter
    {
        private byte[] data;
        // Following offsets are w.r.t. above data array
        private readonly uint baseOffset; // Which byte corresponds to external offset 0
        private uint currOffset; // Next byte to write
        private uint dataLimit;  // First byte which has not yet been written to
        private readonly bool limited;    // True if following limit applies
        private readonly uint writeLimit; // First byte beyond end of writable area
        // Current writer offset, relative to baseOffset
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
            get { return limited && currOffset >= writeLimit; }
        }

        private void Prepare(uint length)
        {
            var newOffset = currOffset + length;
            if (limited && newOffset > writeLimit)
                throw new PEException("trying to write beyond end of stream");
            if (newOffset > (uint)data.Length) {
                var newLength = (uint)data.Length * 2;
                if (limited && newLength > writeLimit)
                    newLength = writeLimit;
                var newData = new byte[newLength];
                Array.Copy(data, newData, (int)dataLimit);
                data = newData;
            }
            if (newOffset > dataLimit)
                dataLimit = currOffset + length;
        }

        public BlobWriter(byte[] data, uint baseOffset, bool limited, uint writeLimit)
        {
            this.data = data;
            this.baseOffset = baseOffset;
            currOffset = baseOffset;
            this.dataLimit = baseOffset;
            this.limited = limited;
            this.writeLimit = writeLimit;
        }

        public BlobWriter(uint writeLimit) : this(new byte[writeLimit], 0, true, writeLimit)
        {
        }

        public BlobWriter() : this(new byte[256], 0, false, 0)
        {
        }

        public void WriteByte(byte value)
        {
            Prepare(1);
            data[currOffset++] = value;
        }

        public void WriteSByte(sbyte value)
        {
            WriteByte((byte)value);
        }

        public void WriteUInt16(ushort value)
        {
            Prepare(2);
            data[currOffset++] = (byte)(value & 0xff);
            data[currOffset++] = (byte)(value >> 8);
        }

        public void WriteInt16(short value)
        {
            WriteUInt16((ushort)value);
        }

        public void WriteUInt24(uint value)
        {
            Prepare(3);
            data[currOffset++] = (byte)(value & 0xff);
            data[currOffset++] = (byte)((value >> 8) & 0xff);
            data[currOffset++] = (byte)(value >> 16);
        }

        public void WriteUInt32(uint value)
        {
            Prepare(4);
            data[currOffset++] = (byte)(value & 0xff);
            data[currOffset++] = (byte)((value >> 8) & 0xff);
            data[currOffset++] = (byte)((value >> 16) & 0xff);
            data[currOffset++] = (byte)(value >> 24);
        }

        public void WriteInt32(int value)
        {
            WriteUInt32((uint)value);
        }

        public void WriteUInt64(ulong value)
        {
            Prepare(8);
            data[currOffset++] = (byte)(value & 0xff);
            data[currOffset++] = (byte)((value >> 8) & 0xff);
            data[currOffset++] = (byte)((value >> 16) & 0xff);
            data[currOffset++] = (byte)((value >> 24) & 0xff);
            data[currOffset++] = (byte)((value >> 32) & 0xff);
            data[currOffset++] = (byte)((value >> 40) & 0xff);
            data[currOffset++] = (byte)((value >> 48) & 0xff);
            data[currOffset++] = (byte)(value >> 56);
        }

        public void WriteInt64(long value)
        {
            WriteUInt64((ulong)value);
        }

        public void WriteBytes(byte[] bytes, uint length)
        {
            Prepare(length);
            Array.Copy(bytes, 0, data, currOffset, length);
            currOffset += length;
        }

        public void WriteContents(BlobWriter writer)
        {
            var length = writer.dataLimit - writer.baseOffset;
            Prepare(length);
            Array.Copy(writer.data, writer.baseOffset, data, currOffset, length);
            currOffset += length;
        }

        public void WriteBytes(byte[] bytes)
        {
            WriteBytes(bytes, (uint)bytes.Length);
        }

        public void WriteSingle(float value)
        {
            WriteBytes(BitConverter.GetBytes(value));
        }

        public void WriteDouble(double value)
        {
            WriteBytes(BitConverter.GetBytes(value));
        }

        // S24.2.4
        public void WriteCompressedUInt32(uint value)
        {
            if (value <= 0x7f)
                WriteByte((byte)value);
            else if (value <= 0x3fff)
            {
                WriteByte((byte)((value >> 8) | 0x80));
                WriteByte((byte)(value & 0xff));
            }
            else if (value <= 0x1fffffff)
            {
                WriteByte((byte)((value >> 24) | 0xc0));
                WriteByte((byte)((value >> 16) & 0xff));
                WriteByte((byte)((value >> 8) & 0xff));
                WriteByte((byte)(value & 0xff));
            }
            else if (value == 0xffffffff)
                WriteByte(0xff);
            else
                throw new PEException("invalid value for 7-bit encoding");
        }


        public void WriteAsciiZeroPaddedString(string value, uint length)
        {
            if (value == null)
                Pad(length);
            else
            {
                var blob = Encoding.ASCII.GetBytes(value);
                if (blob.Length < length)
                {
                    WriteBytes(blob);
                    Pad(length - (uint)blob.Length);
                }
                else
                    WriteBytes(blob, length);
            }
        }

        public void WriteUTF8ZeroPaddedString(string value, uint length)
        {
            if (value == null)
                Pad(length);
            else
            {
                var blob = Encoding.UTF8.GetBytes(value);
                if (blob.Length < length)
                {
                    WriteBytes(blob);
                    Pad(length - (uint)blob.Length);
                }
                else
                    WriteBytes(blob, length);
            }
        }

        public void WriteAsciiZeroTerminatedString(string value, uint alignment)
        {
            if (value == null)
                throw new ArgumentNullException();
            var blob = Encoding.ASCII.GetBytes(value);
            WriteBytes(blob);
            WriteByte(0);
            var extra = (uint)(blob.Length + 1) % alignment;
            if (extra != 0)
                Pad(alignment - extra);
        }

        public void WriteUTF8SizedString(string value)
        {
            if (value == null)
                WriteCompressedUInt32(0xffffffff);
            else if (value.Length == 0)
                WriteCompressedUInt32(0);
            else
            {
                var bytes = Encoding.UTF8.GetBytes(value);
                var len = (uint)bytes.Length;
                WriteCompressedUInt32(len);
                WriteBytes(bytes);
            }
        }

        public void WriteUTF16SizedString(string value)
        {
            if (value == null)
                WriteCompressedUInt32(0xffffffff);
            else if (value.Length == 0)
                WriteCompressedUInt32(0);
            else
            {
                var bytes = Encoding.Unicode.GetBytes(value);
                var len = (uint)bytes.Length;
                WriteCompressedUInt32(len);
                WriteBytes(bytes);
            }
        }

        public void WriteUTF16SizedStringWithEncodingHint(string value)
        {
            if (value == null)
                WriteCompressedUInt32(0xffffffff);
            else if (value.Length == 0)
                WriteCompressedUInt32(0);
            else
            {
                var bytes = Encoding.Unicode.GetBytes(value);
                var len = (uint)bytes.Length + 1;
                var marker = (byte)0;
                foreach (var c in value)
                {
                    if (c >= 0x01 && c <= 0x08 || c >= 0x0e && c <= 0x1f || c == 0x27 || c == 0x2d || c == 0x7f || c > 0xff)
                        marker = 0x01;
                }
                WriteCompressedUInt32(len);
                WriteBytes(bytes);
                WriteByte(marker);
            }
        }

        public void WriteUTF8ZeroTerminatedString(string value)
        {
            if (value == null)
                throw new ArgumentNullException();
            WriteBytes(Encoding.UTF8.GetBytes(value));
            WriteByte(0);
        }

        public void WriteUTF8SizedZeroPaddedString(string value, uint alignment)
        {
            var blob = Encoding.UTF8.GetBytes(value);
            var over = (uint)blob.Length % alignment;
            var padding = over == 0 ? 0 : alignment - over;
            WriteUInt32((uint)blob.Length + padding);
            WriteBytes(blob);
            Pad(padding);
        }

        // Aligned w.r.t. underlying data, not current view
        public void Align(uint alignment)
        {
            while ((currOffset % alignment) != 0)
                WriteByte(0);
        }

        public void Pad(uint length)
        {
            for (var i = 0; i < length; i++)
                WriteByte(0);
        }

        public byte[] GetBlob()
        {
            if (baseOffset == 0 && dataLimit == data.Length)
                return data;

            var blob = new byte[dataLimit - baseOffset];
            Array.Copy(data, baseOffset, blob, 0, blob.Length);
            return blob;
        }

        public void FixupUInt32(uint offset, uint value)
        {
            var savedOffset = Offset;
            Offset = offset;
            WriteUInt32(value);
            Offset = savedOffset;
        }
    }
}
