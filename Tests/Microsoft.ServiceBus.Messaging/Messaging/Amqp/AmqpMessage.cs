//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.ServiceBus.Common;
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
    using Microsoft.ServiceBus.Messaging.Amqp.Framing;

    [Flags]
    enum SectionFlag
    {
        Header = 1,
        DeliveryAnnotations = 2,
        MessageAnnotations = 4,
        Properties = 8,
        ApplicationProperties = 16,
        Data = 32,
        AmqpSequence = 64,
        AmqpValue = 128,
        Footer = 256,
        All = Mutable | Immutable,
        Body = Data | AmqpSequence | AmqpValue,
        NonBody = All & ~Body,
        Mutable = Header | DeliveryAnnotations | MessageAnnotations | Footer,
        Immutable = Properties | ApplicationProperties | Body,
    }

    /// <summary>
    /// Implements the AMQP MESSAGE FORMAT 0 message.
    /// </summary>
    abstract class AmqpMessage : Delivery
    {
        Header header;
        DeliveryAnnotations deliveryAnnotations;
        MessageAnnotations messageAnnotations;
        Properties properties;
        ApplicationProperties applicationProperties;
        Footer footer;
        SectionFlag sectionFlags;

        public Header Header
        {
            get
            {
                this.EnsureInitialized<Header>(ref this.header, SectionFlag.Header);
                return this.header;
            }

            protected set
            {
                this.header = value;
                this.UpdateSectionFlag(value != null, SectionFlag.Header);
            }
        }

        public DeliveryAnnotations DeliveryAnnotations
        {
            get
            {
                EnsureInitialized<DeliveryAnnotations>(ref this.deliveryAnnotations, SectionFlag.DeliveryAnnotations);
                return this.deliveryAnnotations;
            }

            protected set
            {
                this.deliveryAnnotations = value;
                this.UpdateSectionFlag(value != null, SectionFlag.DeliveryAnnotations);
            }
        }

        public MessageAnnotations MessageAnnotations
        {
            get
            {
                EnsureInitialized<MessageAnnotations>(ref this.messageAnnotations, SectionFlag.MessageAnnotations);
                return this.messageAnnotations;
            }

            protected set
            {
                this.messageAnnotations = value;
                this.UpdateSectionFlag(value != null, SectionFlag.MessageAnnotations);
            }
        }

        public Properties Properties
        {
            get
            {
                EnsureInitialized<Properties>(ref this.properties, SectionFlag.Properties);
                return this.properties;
            }

            protected set
            {
                this.properties = value;
                this.UpdateSectionFlag(value != null, SectionFlag.Properties);
            }
        }

        public ApplicationProperties ApplicationProperties
        {
            get
            {
                EnsureInitialized<ApplicationProperties>(ref this.applicationProperties, SectionFlag.ApplicationProperties);
                return this.applicationProperties;
            }

            protected set
            {
                this.applicationProperties = value;
                this.UpdateSectionFlag(value != null, SectionFlag.ApplicationProperties);
            }
        }

        public virtual IEnumerable<Data> DataBody
        {
            get { throw new InvalidOperationException(); }
            set { throw new InvalidOperationException(); }
        }

        public virtual IEnumerable<AmqpSequence> SequenceBody
        {
            get { throw new InvalidOperationException(); }
            set { throw new InvalidOperationException(); }
        }

        public virtual AmqpValue ValueBody
        {
            get { throw new InvalidOperationException(); }
            set { throw new InvalidOperationException(); }
        }

        public virtual Stream BodyStream
        {
            get { throw new InvalidOperationException(); }
            set { throw new InvalidOperationException(); }
        }
        
        public Footer Footer
        {
            get
            {
                EnsureInitialized<Footer>(ref this.footer, SectionFlag.Footer);
                return this.footer;
            }

            protected set
            {
                this.footer = value;
                this.UpdateSectionFlag(value != null, SectionFlag.Footer);
            }
        }

        public SectionFlag Sections
        {
            get
            {
                this.Deserialize(SectionFlag.All);
                return this.sectionFlags;
            }
        }

        public SectionFlag BodyType
        {
            get
            {
                this.Deserialize(SectionFlag.All);
                return this.sectionFlags & SectionFlag.Body;
            }
        }

        public static AmqpMessage Create()
        {
            return new AmqpEmptyMessage();
        }

        public static AmqpMessage Create(Data data)
        {
            return Create(new Data[] { data });
        }

        public static AmqpMessage Create(IEnumerable<Data> dataList)
        {
            return new AmqpDataMessage(dataList);
        }

        public static AmqpMessage Create(AmqpValue value)
        {
            return new AmqpValueMessage(value);
        }

        public static AmqpMessage Create(IEnumerable<AmqpSequence> amqpSequence)
        {
            return new AmqpSequenceMessage(amqpSequence);
        }

        public static AmqpMessage Create(ArraySegment<byte> binaryData)
        {
            return Create(new Data[] { new Data() { Value = binaryData } });
        }

        public static AmqpMessage Create(Stream stream, bool ownStream, bool fullMessage = false)
        {
            if (fullMessage)
            {
                return new AmqpOutputStreamMessage(stream, ownStream);
            }
            else
            {
                return new AmqpBodyStreamMessage(stream, ownStream);
            }
        }

        public static AmqpMessage Create(ArraySegment<byte>[] bufferList)
        {
            return new AmqpInputStreamMessage(new BufferListStream(bufferList));
        }

        public AmqpMessage Clone()
        {
            AmqpMessage newMessage = null;
            if (this.BodyType == SectionFlag.Data)
            {
                newMessage = new AmqpDataMessage(this.DataBody);
            }
            else if (this.BodyType == SectionFlag.AmqpSequence)
            {
                newMessage = new AmqpSequenceMessage(this.SequenceBody);
            }
            else if (this.BodyType == SectionFlag.AmqpValue)
            {
                newMessage = new AmqpValueMessage(this.ValueBody);
            }
            else
            {
                throw new AmqpException(AmqpError.NotAllowed, SRClient.AmqpInvalidMessageBodyType);
            }

            return newMessage;
        }

        public void Modify(Modified modified)
        {
            // TODO: handle delivery failed and undeliverable here
            foreach(KeyValuePair<MapKey, object> pair in modified.MessageAnnotations)
            {
                this.MessageAnnotations.Map[pair.Key] = pair.Value;
            }
        }

        public virtual Stream ToStream()
        {
            throw new InvalidOperationException();
        }

        public virtual void Deserialize(SectionFlag desiredSections)
        {
        }

        protected virtual void EnsureInitialized<T>(ref T obj, SectionFlag section) where T : class, new()
        {
            if (AmqpMessage.EnsureInitialized(ref obj))
            {
                this.sectionFlags |= section;
            }
        }

        static bool EnsureInitialized<T>(ref T obj) where T : class, new()
        {
            if (obj == null)
            {
                obj = new T();
                return true;
            }
            else
            {
                return false;
            }
        }

        static ArraySegment<byte>[] ReadStream(Stream stream, int segmentSize, out int length)
        {
            length = 0;
            List<ArraySegment<byte>> buffers = new List<ArraySegment<byte>>();
            while (true)
            {
                byte[] buffer = new byte[segmentSize];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    break;
                }

                buffers.Add(new ArraySegment<byte>(buffer, 0, bytesRead));
                length += bytesRead;
            }

            return buffers.ToArray();
        }

        int GetSectionSize(IAmqpSerializable section)
        {
            return section == null ? 0 : section.EncodeSize;
        }

        void EncodeSection(ByteBuffer buffer, IAmqpSerializable section)
        {
            if (section != null)
            {
                section.Encode(buffer);
            }
        }

        void UpdateSectionFlag(bool set, SectionFlag flag)
        {
            if (set)
            {
                this.sectionFlags |= flag;
            }
            else
            {
                this.sectionFlags &= ~flag;
            }
        }

        abstract class AmqpBufferedMessage : AmqpMessage
        {
            BufferListStream bufferStream;
            bool initialized;

            protected override ArraySegment<byte>[] GetPayload(int payloadSize, out bool more)
            {
                if (!this.initialized)
                {
                    this.Initialize();
                    this.initialized = true;
                }

                return this.bufferStream.ReadBuffers(payloadSize, false, out more);
            }

            protected override void OnCompletePayload(int payloadSize)
            {
                long position = this.bufferStream.Position;
                this.bufferStream.Position = position + payloadSize;
            }

            protected virtual void OnInitialize()
            {
            }

            protected abstract int GetBodySize();

            protected abstract void EncodeBody(ByteBuffer buffer);

            protected virtual void AddCustomSegments(List<ArraySegment<byte>> segmentList)
            {
            }

            void Initialize()
            {
                this.OnInitialize();

                int encodeSize = this.GetSectionSize(this.header) +
                    this.GetSectionSize(this.deliveryAnnotations) +
                    this.GetSectionSize(this.messageAnnotations) +
                    this.GetSectionSize(this.properties) +
                    this.GetSectionSize(this.applicationProperties) +
                    this.GetBodySize() +
                    this.GetSectionSize(this.footer);

                List<ArraySegment<byte>> segmentList = new List<ArraySegment<byte>>(4);
                if (encodeSize == 0)
                {
                    this.AddCustomSegments(segmentList);
                }
                else
                {
                    ByteBuffer buffer = ByteBuffer.Wrap(new byte[encodeSize]);
                    int segmentOffset = 0;

                    this.EncodeSection(buffer, this.header);
                    this.EncodeSection(buffer, this.deliveryAnnotations);
                    this.EncodeSection(buffer, this.messageAnnotations);
                    this.EncodeSection(buffer, this.properties);
                    this.EncodeSection(buffer, this.applicationProperties);
                    if (buffer.Length > 0)
                    {
                        segmentList.Add(buffer.Array);
                    }

                    segmentOffset = buffer.Length;
                    this.EncodeBody(buffer);
                    int count = buffer.Length - segmentOffset;
                    if (count > 0)
                    {
                        segmentList.Add(new ArraySegment<byte>(buffer.Buffer, segmentOffset, count));
                    }

                    this.AddCustomSegments(segmentList);

                    if (this.footer != null)
                    {
                        segmentOffset = buffer.Length;
                        this.EncodeSection(buffer, this.footer);
                        segmentList.Add(new ArraySegment<byte>(buffer.Buffer, segmentOffset, buffer.Length - segmentOffset));
                    }
                }

                this.bufferStream = new BufferListStream(segmentList.ToArray());
            }
        }

        sealed class AmqpEmptyMessage : AmqpBufferedMessage
        {
            protected override int GetBodySize()
            {
                return 0;
            }

            protected override void EncodeBody(ByteBuffer buffer)
            {
            }
        }

        sealed class AmqpValueMessage : AmqpBufferedMessage
        {
            readonly AmqpValue value;

            public AmqpValueMessage(AmqpValue value)
            {
                this.value = value;
                this.sectionFlags |= SectionFlag.AmqpValue;
            }

            public override AmqpValue ValueBody
            {
                get { return this.value; }
            }

            protected override int GetBodySize()
            {
                return this.GetSectionSize(this.value);
            }

            protected override void EncodeBody(ByteBuffer buffer)
            {
                this.EncodeSection(buffer, this.value);
            }
        }

        sealed class AmqpDataMessage : AmqpBufferedMessage
        {
            readonly IEnumerable<Data> dataList;

            public AmqpDataMessage(IEnumerable<Data> dataList)
            {
                this.dataList = dataList;
                this.sectionFlags |= SectionFlag.Data;
            }

            public override IEnumerable<Data> DataBody
            {
                get { return this.dataList; }
            }

            protected override int GetBodySize()
            {
                return 0;
            }

            protected override void EncodeBody(ByteBuffer buffer)
            {
            }

            protected override void AddCustomSegments(List<ArraySegment<byte>> segmentList)
            {
                foreach (Data data in this.dataList)
                {
                    ArraySegment<byte> value = (ArraySegment<byte>)data.Value;
                    segmentList.Add(Data.GetEncodedPrefix(value.Count));
                    segmentList.Add(value);
                }
            }
        }

        sealed class AmqpSequenceMessage : AmqpBufferedMessage
        {
            readonly IEnumerable<AmqpSequence> sequence;

            public AmqpSequenceMessage(IEnumerable<AmqpSequence> sequence)
            {
                this.sequence = sequence;
                this.sectionFlags |= SectionFlag.AmqpSequence;
            }

            public override IEnumerable<AmqpSequence> SequenceBody
            {
                get { return this.sequence; }
            }

            protected override int GetBodySize()
            {
                int bodySize = 0;
                foreach (AmqpSequence seq in this.sequence)
                {
                    bodySize += this.GetSectionSize(seq);
                }

                return bodySize;
            }

            protected override void EncodeBody(ByteBuffer buffer)
            {
                foreach (AmqpSequence seq in this.sequence)
                {
                    this.EncodeSection(buffer, seq);
                }
            }
        }

        /// <summary>
        /// Wraps a stream in the message body. The data is sent in one or more Data sections.
        /// </summary>
        sealed class AmqpBodyStreamMessage : AmqpBufferedMessage
        {
            readonly Stream bodyStream;
            readonly bool ownStream;
            ArraySegment<byte>[] bodyData;
            int bodyLength;

            public AmqpBodyStreamMessage(Stream bodyStream, bool ownStream)
            {
                Fx.Assert(bodyStream != null, "The bodyStream argument should not be null.");
                this.sectionFlags |= SectionFlag.Data;
                this.bodyStream = bodyStream;
                this.ownStream = ownStream;
            }

            public override Stream BodyStream
            {
                get
                {
                    return new BufferListStream(this.bodyData);
                }

                set
                {
                    base.BodyStream = value;
                }
            }

            protected override void OnInitialize()
            {
                this.bodyData = AmqpMessage.ReadStream(this.bodyStream, 1024, out this.bodyLength);
                if (this.ownStream)
                {
                    this.bodyStream.Dispose();
                }
            }

            protected override int GetBodySize()
            {
                return 0;
            }

            protected override void EncodeBody(ByteBuffer buffer)
            {
            }

            protected override void AddCustomSegments(List<ArraySegment<byte>> segmentList)
            {
                if (this.bodyLength > 0)
                {
                    segmentList.Add(Data.GetEncodedPrefix(this.bodyLength));
                    segmentList.AddRange(this.bodyData);
                }
            }
        }

        /// <summary>
        /// The stream contains an entire AMQP message. When the stream is sent out,
        /// the mutable sections are updated.
        /// </summary>
        sealed class AmqpOutputStreamMessage : AmqpBufferedMessage
        {
            readonly Stream messageStream;
            readonly bool ownStream;
            ArraySegment<byte>[] buffers;

            public AmqpOutputStreamMessage(Stream messageStream, bool ownStream)
            {
                this.messageStream = messageStream;
                this.ownStream = ownStream;
            }

            protected override void OnInitialize()
            {
                // mask off immutable sections except footer
                this.properties = null;
                this.applicationProperties = null;
                this.footer = null;

                BufferListStream stream = this.messageStream as BufferListStream;
                if (stream != null && !this.ownStream)
                {
                    stream = (BufferListStream)stream.Clone();
                }
                else
                {
                    int length = 0;
                    ArraySegment<byte>[] buffers = AmqpMessage.ReadStream(this.messageStream, 512, out length);
                    stream = new BufferListStream(buffers);
                }

                AmqpMessageReader reader = new AmqpMessageReader(stream);
                AmqpMessage emptyMessage = AmqpMessage.Create();
                reader.ReadMessage(emptyMessage, SectionFlag.Header | SectionFlag.DeliveryAnnotations | SectionFlag.MessageAnnotations);
                this.UpdateHeader(emptyMessage.header);
                this.UpdateDeliveryAnnotations(emptyMessage.deliveryAnnotations);
                this.UpdateMessageAnnotations(emptyMessage.messageAnnotations);

                // read out the remaining buffers
                bool unused = false;
                this.buffers = stream.ReadBuffers(int.MaxValue, true, out unused);

                stream.Dispose();
                if (this.ownStream)
                {
                    this.messageStream.Dispose();
                }
            }

            protected override int GetBodySize()
            {
                return 0;
            }

            protected override void EncodeBody(ByteBuffer buffer)
            {
            }

            protected override void AddCustomSegments(List<ArraySegment<byte>> segmentList)
            {
                if (this.buffers != null && this.buffers.Length > 0)
                {
                    segmentList.AddRange(this.buffers);
                }
            }

            void UpdateHeader(Header header)
            {
                if (header != null)
                {
                    if (this.header == null)
                    {
                        this.Header = header;
                    }
                    else
                    {
                        // update this header only if it is null
                        this.header.Durable = this.header.Durable ?? header.Durable;
                        this.header.Priority = this.header.Priority ?? header.Priority;
                        this.header.Ttl = this.header.Ttl ?? header.Ttl;
                        this.header.FirstAcquirer = this.header.FirstAcquirer ?? header.FirstAcquirer;
                        this.header.DeliveryCount = this.header.DeliveryCount ?? header.DeliveryCount;
                    }
                }
            }

            void UpdateDeliveryAnnotations(DeliveryAnnotations deliveryAnnotations)
            {
                if (deliveryAnnotations != null)
                {
                    if (this.deliveryAnnotations == null)
                    {
                        this.DeliveryAnnotations = deliveryAnnotations;
                    }
                    else
                    {
                        foreach (KeyValuePair<MapKey, object> pair in this.deliveryAnnotations.Map)
                        {
                            deliveryAnnotations.Map[pair.Key] = pair.Value;
                        }

                        this.deliveryAnnotations = deliveryAnnotations;
                    }
                }
            }

            void UpdateMessageAnnotations(MessageAnnotations messageAnnotations)
            {
                if (messageAnnotations != null)
                {
                    if (this.messageAnnotations == null)
                    {
                        this.MessageAnnotations = messageAnnotations;
                    }
                    else
                    {
                        foreach (KeyValuePair<MapKey, object> pair in this.messageAnnotations.Map)
                        {
                            messageAnnotations.Map[pair.Key] = pair.Value;
                        }

                        this.messageAnnotations = messageAnnotations;
                    }
                }
            }
        }

        /// <summary>
        /// Used on the receiver side. The entire message is immutable (changes are discarded).
        /// </summary>
        sealed class AmqpInputStreamMessage : AmqpMessage
        {
            readonly BufferListStream bufferStream;
            bool deserialized;
            IEnumerable<Data> dataList;
            IEnumerable<AmqpSequence> sequenceList;
            AmqpValue amqpValue;
            Stream bodyStream;

            public AmqpInputStreamMessage(BufferListStream bufferStream)
            {
                this.bufferStream = bufferStream;
            }

            public override IEnumerable<Data> DataBody
            {
                get 
                {
                    this.Deserialize(SectionFlag.All);
                    return this.dataList; 
                }

                set
                {
                    this.dataList = value;
                    this.UpdateSectionFlag(value != null, SectionFlag.Data);
                }
            }

            public override IEnumerable<AmqpSequence> SequenceBody
            {
                get 
                {
                    this.Deserialize(SectionFlag.All);
                    return this.sequenceList;
                }

                set 
                {
                    this.sequenceList = value;
                    this.UpdateSectionFlag(value != null, SectionFlag.AmqpSequence);
                }
            }

            public override AmqpValue ValueBody
            {
                get
                {
                    this.Deserialize(SectionFlag.All);
                    return this.amqpValue;
                }
                
                set
                {
                    this.amqpValue = value;
                    this.UpdateSectionFlag(value != null, SectionFlag.AmqpValue);
                }
            }

            public override Stream BodyStream
            {
                get
                {
                    this.Deserialize(SectionFlag.All);
                    return this.bodyStream;
                }

                set 
                {
                    this.bodyStream = value;
                }
            }

            public override Stream ToStream()
            {
                return this.bufferStream;
            }

            public override void Deserialize(SectionFlag desiredSections)
            {
                if (!this.deserialized)
                {
                    BufferListStream stream = (BufferListStream)this.bufferStream.Clone();
                    AmqpMessageReader reader = new AmqpMessageReader(stream);
                    reader.ReadMessage(this, desiredSections);
                    stream.Dispose();
                    this.deserialized = true;
                }
            }

            protected override ArraySegment<byte>[] GetPayload(int payloadSize, out bool more)
            {
                throw new InvalidOperationException();
            }

            protected override void OnCompletePayload(int payloadSize)
            {
                throw new InvalidOperationException();
            }

            protected override void EnsureInitialized<T>(ref T obj, SectionFlag section)
            {
                this.Deserialize(SectionFlag.All);
            }
        }
        
        sealed class AmqpMessageReader
        {
            static Dictionary<string, ulong> sectionCodeByName = new Dictionary<string, ulong>()
            {
                { Header.Name, Header.Code },
                { DeliveryAnnotations.Name, DeliveryAnnotations.Code },
                { MessageAnnotations.Name, MessageAnnotations.Code },
                { Properties.Name, Properties.Code },
                { ApplicationProperties.Name, ApplicationProperties.Code },
                { Data.Name, Data.Code },
                { AmqpSequence.Name, AmqpSequence.Code },
                { AmqpValue.Name, AmqpValue.Code },
                { Footer.Name, Footer.Code },
            };

            static Action<AmqpMessageReader, AmqpMessage>[] sectionReaders = new Action<AmqpMessageReader, AmqpMessage>[]
            {
                ReadHeaderSection,
                ReadDeliveryAnnotationsSection,
                ReadMessageAnnotationsSection,
                ReadPropertiesSection,
                ReadApplicationPropertiesSection,
                ReadDataSection,
                ReadAmqpSequenceSection,
                ReadAmqpValueSection,
                ReadFooterSection,
            };

            readonly BufferListStream stream;
            List<Data> dataList;
            List<AmqpSequence> sequenceList;
            AmqpValue amqpValue;
            List<ArraySegment<byte>> bodyBuffers;

            public AmqpMessageReader(BufferListStream stream)
            {
                this.stream = stream;
            }

            public void ReadMessage(AmqpMessage message, SectionFlag sections)
            {
                while (this.ReadSection(message, sections));

                if ((sections & SectionFlag.Body) != 0)
                {
                    if (this.dataList != null)
                    {
                        message.DataBody = this.dataList;
                    }

                    if (this.sequenceList != null)
                    {
                        message.SequenceBody = this.sequenceList;
                    }

                    if (this.amqpValue != null)
                    {
                        message.ValueBody = this.amqpValue;
                    }

                    if (this.bodyBuffers != null)
                    {
                        message.BodyStream = new BufferListStream(this.bodyBuffers.ToArray());
                    }
                }
            }

            static void ReadHeaderSection(AmqpMessageReader reader, AmqpMessage message)
            {
                message.Header = ReadListSection<Header>(reader);
            }

            static void ReadDeliveryAnnotationsSection(AmqpMessageReader reader, AmqpMessage message)
            {
                message.DeliveryAnnotations = ReadMapSection<DeliveryAnnotations>(reader);
            }

            static void ReadMessageAnnotationsSection(AmqpMessageReader reader, AmqpMessage message)
            {
                message.MessageAnnotations = ReadMapSection<MessageAnnotations>(reader);
            }

            static void ReadPropertiesSection(AmqpMessageReader reader, AmqpMessage message)
            {
                message.Properties = ReadListSection<Properties>(reader);
            }

            static void ReadApplicationPropertiesSection(AmqpMessageReader reader, AmqpMessage message)
            {
                message.ApplicationProperties = ReadMapSection<ApplicationProperties>(reader);
            }

            static void ReadDataSection(AmqpMessageReader reader, AmqpMessage message)
            {
                FormatCode formatCode = reader.ReadFormatCode();
                Fx.Assert(formatCode == FormatCode.Binary8 || formatCode == FormatCode.Binary32, "Invalid binary format code");
                bool smallEncoding = formatCode == FormatCode.Binary8;
                int count = reader.ReadInt(smallEncoding);
                ArraySegment<byte> buffer = reader.ReadBytes(count);

                AmqpMessage.EnsureInitialized<List<Data>>(ref reader.dataList);
                reader.dataList.Add(new Data() { Value = buffer });

                reader.AddBodyBuffer(buffer);
            }

            static void ReadAmqpSequenceSection(AmqpMessageReader reader, AmqpMessage message)
            {
                AmqpMessage.EnsureInitialized<List<AmqpSequence>>(ref reader.sequenceList);
                reader.sequenceList.Add(ReadListSection<AmqpSequence>(reader, true));
            }

            static void ReadAmqpValueSection(AmqpMessageReader reader, AmqpMessage message)
            {
                ArraySegment<byte> buffer = reader.ReadBytes(int.MaxValue);
                ByteBuffer byteBuffer = ByteBuffer.Wrap(buffer);
                object value = AmqpCodec.DecodeObject(byteBuffer);
                reader.amqpValue = new AmqpValue() { Value = value };

                reader.AddBodyBuffer(buffer);

                // we didn't know the size and the buffer may include the footer
                if (byteBuffer.Length > 0)
                {
                    Footer footer = new Footer();
                    footer.Decode(byteBuffer);
                    message.Footer = footer;
                }
            }

            static void ReadFooterSection(AmqpMessageReader reader, AmqpMessage message)
            {
                message.Footer = ReadMapSection<Footer>(reader);
            }

            static T ReadListSection<T>(AmqpMessageReader reader, bool isBodySection = false) where T : DescribedList, new()
            {
                T section = new T();
                long position = reader.stream.Position;
                FormatCode formatCode = reader.ReadFormatCode();
                Fx.Assert(formatCode == FormatCode.List8 || formatCode == FormatCode.List0 || formatCode == FormatCode.List32, "Invalid list format code");
                if (formatCode == FormatCode.List0)
                {
                    return section;
                }

                bool smallEncoding = formatCode == FormatCode.List8;
                int size = reader.ReadInt(smallEncoding);
                int count = reader.ReadInt(smallEncoding);
                if (count == 0)
                {
                    return section;
                }

                long position2 = reader.stream.Position;

                ArraySegment<byte> bytes = reader.ReadBytes(size - (smallEncoding ? FixedWidth.UByte : FixedWidth.UInt));
                long position3 = reader.stream.Position;

                section.DecodeValue(ByteBuffer.Wrap(bytes), size, count);

                // Check if we are decoding the AMQP value body
                if (isBodySection)
                {
                    reader.stream.Position = position;
                    ArraySegment<byte> segment = reader.stream.ReadBytes((int)(position2 - position));
                    reader.stream.Position = position3;

                    reader.AddBodyBuffer(segment);
                    reader.AddBodyBuffer(bytes);
                }

                return section;
            }

            static T ReadMapSection<T>(AmqpMessageReader reader) where T : DescribedMap, new()
            {
                T section = new T();
                FormatCode formatCode = reader.ReadFormatCode();
                Fx.Assert(formatCode == FormatCode.Map8 || formatCode == FormatCode.Map32, "Invalid map format code");
                bool smallEncoding = formatCode == FormatCode.Map8;
                int size = reader.ReadInt(smallEncoding);
                int count = reader.ReadInt(smallEncoding);
                if (count > 0)
                {
                    ArraySegment<byte> bytes = reader.ReadBytes(size - (smallEncoding ? FixedWidth.UByte : FixedWidth.UInt));
                    section.DecodeValue(ByteBuffer.Wrap(bytes), size, count);
                }

                return section;
            }

            bool ReadSection(AmqpMessage message, SectionFlag sections)
            {
                long position = this.stream.Position;
                if (position == this.stream.Length)
                {
                    return false;
                }

                FormatCode formatCode = this.ReadFormatCode();
                if (formatCode != FormatCode.Described)
                {
                    throw AmqpEncoding.GetEncodingException("section.format-code");
                }

                ulong descriptorCode = this.ReadDescriptorCode();
                if (descriptorCode < Header.Code || descriptorCode > Footer.Code)
                {
                    throw AmqpEncoding.GetEncodingException("section.descriptor");
                }

                int sectionIndex = (int)(descriptorCode - Header.Code);
                SectionFlag sectionFlag = (SectionFlag)(1 << sectionIndex);
                if ((sectionFlag & sections) == 0)
                {
                    // The section we want to decode does not exist, so rollback to
                    // where we were.
                    this.stream.Position = position;
                    return false;
                }

                sectionReaders[sectionIndex](this, message);
                return true;
            }

            FormatCode ReadFormatCode()
            {
                int formatCode = this.stream.ReadByte();
                if ((formatCode & 0x0F) == 0x0F)
                {
                    formatCode = (formatCode << 8) + this.stream.ReadByte();
                }

                return (FormatCode)formatCode;
            }

            ulong ReadDescriptorCode()
            {
                FormatCode formatCode = this.ReadFormatCode();
                ulong descriptorCode = 0;
                if (formatCode == FormatCode.SmallULong)
                {
                    descriptorCode = (ulong)this.stream.ReadByte();
                }
                else if (formatCode == FormatCode.ULong)
                {
                    ArraySegment<byte> buffer = this.ReadBytes(FixedWidth.ULong);
                    descriptorCode = AmqpBitConverter.ReadULong(buffer.Array, buffer.Offset, FixedWidth.ULong);
                }
                else if (formatCode == FormatCode.Symbol8 || formatCode == FormatCode.Symbol32)
                {
                    int count = this.ReadInt(formatCode == FormatCode.Symbol8);
                    ArraySegment<byte> nameBuffer = this.ReadBytes(count);
                    string descriptorName = System.Text.Encoding.ASCII.GetString(nameBuffer.Array, nameBuffer.Offset, count);
                    sectionCodeByName.TryGetValue(descriptorName, out descriptorCode);
                }

                return descriptorCode;
            }

            int ReadInt(bool smallEncoding)
            {
                if (smallEncoding)
                {
                    return this.stream.ReadByte();
                }
                else
                {
                    ArraySegment<byte> buffer = this.ReadBytes(FixedWidth.UInt);
                    return(int)AmqpBitConverter.ReadUInt(buffer.Array, buffer.Offset, FixedWidth.UInt);
                }
            }

            ArraySegment<byte> ReadBytes(int count)
            {
                ArraySegment<byte> bytes = this.stream.ReadBytes(count);
                if (count != int.MaxValue && bytes.Count < count)
                {
                    throw AmqpEncoding.GetEncodingException("eof");
                }

                return bytes;
            }

            void AddBodyBuffer(ArraySegment<byte> buffer)
            {
                AmqpMessage.EnsureInitialized<List<ArraySegment<byte>>>(ref this.bodyBuffers);
                this.bodyBuffers.Add(buffer);
            }
        }
    }
}
