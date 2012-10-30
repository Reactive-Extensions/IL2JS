//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Serialization
{
    using System;
    using System.IO;
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;

    abstract class SerializableType
    {
        SerializableType()
        {
        }

        public void WriteObject(Stream stream, object graph)
        {
            using (ByteBuffer buffer = ByteBuffer.Wrap(512))
            {
                this.OnWriteObject(buffer, graph);
                stream.Write(buffer.Buffer, buffer.Offset, buffer.Length);
            }
        }

        public object ReadObject(Stream stream)
        {
            using (ByteBuffer buffer = ByteBuffer.Wrap(stream))
            {
                return this.OnReadObject(buffer);
            }
        }

        public static SerializableType Create(Type type)
        {
            return new PremitiveType(type);
        }

        public static SerializableType Create(Type type, string descriptorName, ulong? descriptorCode, SerialiableMember[] members)
        {
            return new CompositeType(type, descriptorName, descriptorCode, members);
        }

        protected abstract void OnWriteObject(ByteBuffer buffer, object graph);

        protected abstract object OnReadObject(ByteBuffer buffer);

        sealed class PremitiveType : SerializableType
        {
            readonly EncodingBase encoder;

            public PremitiveType(Type type)
            {
                this.encoder = AmqpEncoding.GetEncoding(type);
            }

            protected override void OnWriteObject(ByteBuffer buffer, object value)
            {
                this.encoder.EncodeObject(value, false, buffer);
            }

            protected override object OnReadObject(ByteBuffer buffer)
            {
                return this.encoder.DecodeObject(buffer, 0);
            }
        }

        sealed class CompositeType : SerializableType
        {
            readonly Type type;
            readonly AmqpSymbol descriptorName;
            readonly ulong? descriptorCode;
            readonly SerialiableMember[] members;

            public CompositeType(Type type, string descriptorName, ulong? descriptorCode, SerialiableMember[] members)
            {
                this.type = type;
                this.descriptorName = descriptorName;
                this.descriptorCode = descriptorCode;
                this.members = members;
            }

            protected override void OnWriteObject(ByteBuffer buffer, object graph)
            {
                if (graph == null)
                {
                    AmqpEncoding.EncodeNull(buffer);
                    return;
                }

                AmqpBitConverter.WriteUByte(buffer, (byte)FormatCode.Described);
                if (this.descriptorName.Value != null)
                {
                    SymbolEncoding.Encode(this.descriptorName, buffer);
                }
                else
                {
                    ULongEncoding.Encode(this.descriptorCode, buffer);
                }

                if (this.members.Length == 0)
                {
                    AmqpBitConverter.WriteUByte(buffer, (byte)FormatCode.List0);
                    return;
                }

                AmqpBitConverter.WriteUByte(buffer, (byte)FormatCode.List32);
                int endCopy = buffer.End;               // remember the current position
                AmqpBitConverter.WriteUInt(buffer, 0);  // reserve space for list size
                AmqpBitConverter.WriteUInt(buffer, (uint)this.members.Length);

                for (int i = 0; i < this.members.Length; ++i)
                {
                    object memberValue = this.members[i].Accessor.ReadObject(graph);
                    this.members[i].Type.OnWriteObject(buffer, memberValue);
                }

                // write the correct size
                AmqpBitConverter.WriteUInt(buffer.Buffer, endCopy, (uint)(buffer.End - endCopy - FixedWidth.UInt));
            }

            protected override object OnReadObject(ByteBuffer buffer)
            {
                object container = Activator.CreateInstance(this.type);
                FormatCode formatCode = AmqpEncoding.ReadFormatCode(buffer);    // FormatCode.Described
                if (formatCode != FormatCode.Described)
                {
                    throw new AmqpException(AmqpError.InvalidField, "format-code");
                }

                bool validDescriptor = false;
                formatCode = AmqpEncoding.ReadFormatCode(buffer);
                if (formatCode == FormatCode.ULong || formatCode == FormatCode.SmallULong)
                {
                    ulong code = AmqpBitConverter.ReadULong(buffer);
                    validDescriptor = this.descriptorCode == null || code == this.descriptorCode.Value;
                }
                else if (formatCode == FormatCode.Symbol8 || formatCode == FormatCode.Symbol32)
                {
                    AmqpSymbol symbol = SymbolEncoding.Decode(buffer, formatCode);
                    validDescriptor = this.descriptorName.Value == null || symbol.Equals(this.descriptorName);
                }

                if (!validDescriptor)
                {
                    throw new AmqpException(AmqpError.InvalidField, "descriptor");
                }

                formatCode = AmqpEncoding.ReadFormatCode(buffer);    // FormatCode.List
                if (formatCode == FormatCode.List0)
                {
                    return container;
                }

                int size = 0;
                int count = 0;
                AmqpEncoding.ReadSizeAndCount(buffer, formatCode, FormatCode.List8, FormatCode.List32, out size, out count);

                // prefetch bytes from the stream
                buffer.EnsureLength(size - FixedWidth.UInt);
                for (int i = 0; i < count; ++i)
                {
                    object value = this.members[i].Type.OnReadObject(buffer);
                    this.members[i].Accessor.SetObject(container, value);
                }

                return container;
            }
        }
    }
}
