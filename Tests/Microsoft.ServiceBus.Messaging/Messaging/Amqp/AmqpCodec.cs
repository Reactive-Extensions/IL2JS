//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
    using Microsoft.ServiceBus.Messaging.Amqp.Framing;
    using Microsoft.ServiceBus.Messaging.Amqp.Sasl;
    using Microsoft.ServiceBus.Messaging.Amqp.Transaction;

    /// <summary>
    /// Provides a single place to encode and decode AMQP premitives
    /// Encode and decode AMQP framing types.
    /// </summary>
    static class AmqpCodec
    {
        static Dictionary<string, Func<AmqpDescribed>> knownTypesByName;
        static Dictionary<ulong, Func<AmqpDescribed>> knownTypesByCode;

        static AmqpCodec()
        {
            knownTypesByName = new Dictionary<string, Func<AmqpDescribed>>()
            {
                // performatives
                { Open.Name, () => new Open() },
                { Close.Name, () => new Close() },
                { Begin.Name, () => new Begin() },
                { End.Name, () => new End() },
                { Attach.Name, () => new Attach() },
                { Detach.Name, () => new Detach() },
                { Transfer.Name, () => new Transfer() },
                { Disposition.Name, () => new Disposition() },
                { Flow.Name, () => new Flow() },

                // transaction performatives and types
                { Coordinator.Name, () => { return new Coordinator(); } },
                { Declare.Name, () => { return new Declare(); } },
                { Declared.Name, () => { return new Declared(); } },
                { Discharge.Name, () => { return new Discharge(); } },
                { TransactionalState.Name, () => { return new TransactionalState(); }},

                // sasl performatives
                { SaslMechanisms.Name, () => new SaslMechanisms() },
                { SaslInit.Name, () => new SaslInit() },
                { SaslChallenge.Name, () => new SaslChallenge() },
                { SaslResponse.Name, () => new SaslResponse() },
                { SaslOutcome.Name, () => new SaslOutcome() },

                // definitions
                { Error.Name, () => new Error() },
                { Source.Name, () => new Source() },
                { Target.Name, () => new Target() },
                { Received.Name, () => new Received() },
                { Accepted.Name, () => new Accepted() },
                { Released.Name, () => new Released() },
                { Rejected.Name, () => new Rejected() },
                { Modified.Name, () => new Modified() },
                { DeleteOnClose.Name, () => new DeleteOnClose() },
                { DeleteOnNoLinks.Name, () => new DeleteOnNoLinks() },
                { DeleteOnNoMessages.Name, () => new DeleteOnNoMessages() },
                { DeleteOnNoLinksOrMessages.Name, () => new DeleteOnNoLinksOrMessages() },
            };

            knownTypesByCode = new Dictionary<ulong, Func<AmqpDescribed>>()
            {
                // frame bodies
                { Open.Code, () => new Open() },
                { Close.Code, () => new Close() },
                { Begin.Code, () => new Begin() },
                { End.Code, () => new End() },
                { Attach.Code, () => new Attach() },
                { Detach.Code, () => new Detach() },
                { Transfer.Code, () => new Transfer() },
                { Disposition.Code, () => new Disposition() },
                { Flow.Code, () => new Flow() },

                // transaction performatives and types
                { Coordinator.Code, () => { return new Coordinator(); } },
                { Declare.Code, () => { return new Declare(); } },
                { Discharge.Code, () => { return new Discharge(); } },
                { Declared.Code, () => { return new Declared(); } },
                { TransactionalState.Code, () => { return new TransactionalState(); }},

                // sasl frames
                { SaslMechanisms.Code, () => new SaslMechanisms() },
                { SaslInit.Code, () => new SaslInit() },
                { SaslChallenge.Code, () => new SaslChallenge() },
                { SaslResponse.Code, () => new SaslResponse() },
                { SaslOutcome.Code, () => new SaslOutcome() },

                // definitions
                { Error.Code, () => new Error() },
                { Source.Code, () => new Source() },
                { Target.Code, () => new Target() },
                { Received.Code, () => new Received() },
                { Accepted.Code, () => new Accepted() },
                { Released.Code, () => new Released() },
                { Rejected.Code, () => new Rejected() },
                { Modified.Code, () => new Modified() },
                { DeleteOnClose.Code, () => new DeleteOnClose() },
                { DeleteOnNoLinks.Code, () => new DeleteOnNoLinks() },
                { DeleteOnNoMessages.Code, () => new DeleteOnNoMessages() },
                { DeleteOnNoLinksOrMessages.Code, () => new DeleteOnNoLinksOrMessages() },
            };
        }

        public static int MinimumFrameDecodeSize
        {
            get { return Frame.HeaderSize; }
        }

        public static int GetFrameSize(ByteBuffer buffer)
        {
            return (int)AmqpBitConverter.ReadUInt(buffer.Buffer, buffer.Offset, FixedWidth.UInt);
        }

        public static void RegisterKnownTypes(string name, ulong code, Func<AmqpDescribed> ctor)
        {
            lock (knownTypesByCode)
            {
                knownTypesByName.Add(name, ctor);
                knownTypesByCode.Add(code, ctor);
            }
        }

        //// get encode size methods

        public static int GetBooleanEncodeSize(bool? value)
        {
            return BooleanEncoding.GetEncodeSize(value);
        }

        public static int GetUByteEncodeSize(byte? value)
        {
            return UByteEncoding.GetEncodeSize(value);
        }

        public static int GetUShortEncodeSize(ushort? value)
        {
            return UShortEncoding.GetEncodeSize(value);
        }

        public static int GetUIntEncodeSize(uint? value)
        {
            return UIntEncoding.GetEncodeSize(value);
        }

        public static int GetULongEncodeSize(ulong? value)
        {
            return ULongEncoding.GetEncodeSize(value);
        }

        public static int GetByteEncodeSize(sbyte? value)
        {
            return ByteEncoding.GetEncodeSize(value);
        }

        public static int GetShortEncodeSize(short? value)
        {
            return ShortEncoding.GetEncodeSize(value);
        }

        public static int GetIntEncodeSize(int? value)
        {
            return IntEncoding.GetEncodeSize(value);
        }

        public static int GetLongEncodeSize(long? value)
        {
            return LongEncoding.GetEncodeSize(value);
        }

        public static int GetFloatEncodeSize(float? value)
        {
            return FloatEncoding.GetEncodeSize(value);
        }

        public static int GetDoubleEncodeSize(double? value)
        {
            return DoubleEncoding.GetEncodeSize(value);
        }

        public static int GetCharEncodeSize(char? value)
        {
            return CharEncoding.GetEncodeSize(value);
        }

        public static int GetTimeStampEncodeSize(DateTime? value)
        {
            return TimeStampEncoding.GetEncodeSize(value);
        }

        public static int GetUuidEncodeSize(Guid? value)
        {
            return UuidEncoding.GetEncodeSize(value);
        }

        public static int GetBinaryEncodeSize(ArraySegment<byte> value)
        {
            return BinaryEncoding.GetEncodeSize(value);
        }

        public static int GetSymbolEncodeSize(AmqpSymbol value)
        {
            return SymbolEncoding.GetEncodeSize(value);
        }

        public static int GetStringEncodeSize(string value)
        {
            return StringEncoding.GetEncodeSize(value);
        }

        public static int GetListEncodeSize(IList value)
        {
            return ListEncoding.GetEncodeSize(value);
        }

        public static int GetMapEncodeSize(AmqpMap value)
        {
            return MapEncoding.GetEncodeSize(value);
        }

        public static int GetArrayEncodeSize<T>(T[] value)
        {
            return ArrayEncoding.GetEncodeSize(value);
        }

        public static int GetSerializableEncodeSize(IAmqpSerializable value)
        {
            if (value == null)
            {
                return FixedWidth.NullEncoded;
            }
            else
            {
                return value.EncodeSize;
            }
        }

        public static int GetMultipleEncodeSize<T>(Multiple<T> value)
        {
            return Multiple<T>.GetEncodeSize(value);
        }

        public static int GetObjectEncodeSize(object value)
        {
            return AmqpEncoding.GetObjectEncodeSize(value);
        }

        //// encode methods

        public static void EncodeBoolean(bool? data, ByteBuffer buffer)
        {
            BooleanEncoding.Encode(data, buffer);
        }

        public static void EncodeUByte(byte? data, ByteBuffer buffer)
        {
            UByteEncoding.Encode(data, buffer);
        }

        public static void EncodeUShort(ushort? data, ByteBuffer buffer)
        {
            UShortEncoding.Encode(data, buffer);
        }

        public static void EncodeUInt(uint? data, ByteBuffer buffer)
        {
            UIntEncoding.Encode(data, buffer);
        }

        public static void EncodeULong(ulong? data, ByteBuffer buffer)
        {
            ULongEncoding.Encode(data, buffer);
        }

        public static void EncodeByte(sbyte? data, ByteBuffer buffer)
        {
            ByteEncoding.Encode(data, buffer);
        }

        public static void EncodeShort(short? data, ByteBuffer buffer)
        {
            ShortEncoding.Encode(data, buffer);
        }

        public static void EncodeInt(int? data, ByteBuffer buffer)
        {
            IntEncoding.Encode(data, buffer);
        }

        public static void EncodeLong(long? data, ByteBuffer buffer)
        {
            LongEncoding.Encode(data, buffer);
        }

        public static void EncodeChar(char? data, ByteBuffer buffer)
        {
            CharEncoding.Encode(data, buffer);
        }

        public static void EncodeFloat(float? data, ByteBuffer buffer)
        {
            FloatEncoding.Encode(data, buffer);
        }

        public static void EncodeDouble(double? data, ByteBuffer buffer)
        {
            DoubleEncoding.Encode(data, buffer);
        }

        public static void EncodeTimeStamp(DateTime? data, ByteBuffer buffer)
        {
            TimeStampEncoding.Encode(data, buffer);
        }

        public static void EncodeUuid(Guid? data, ByteBuffer buffer)
        {
            UuidEncoding.Encode(data, buffer);
        }

        public static void EncodeBinary(ArraySegment<byte> data, ByteBuffer buffer)
        {
            BinaryEncoding.Encode(data, buffer);
        }

        public static void EncodeString(string data, ByteBuffer buffer)
        {
            StringEncoding.Encode(data, buffer);
        }

        public static void EncodeSymbol(AmqpSymbol data, ByteBuffer buffer)
        {
            SymbolEncoding.Encode(data, buffer);
        }

        public static void EncodeList(IList data, ByteBuffer buffer)
        {
            ListEncoding.Encode(data, buffer);
        }

        public static void EncodeMap(AmqpMap data, ByteBuffer buffer)
        {
            MapEncoding.Encode(data, buffer);
        }

        public static void EncodeArray<T>(T[] data, ByteBuffer buffer)
        {
            ArrayEncoding.Encode(data, buffer);
        }

        public static void EncodeSerializable(IAmqpSerializable data, ByteBuffer buffer)
        {
            if (data == null)
            {
                AmqpEncoding.EncodeNull(buffer);
            }
            else
            {
                data.Encode(buffer);
            }
        }

        public static void EncodeMultiple<T>(Multiple<T> data, ByteBuffer buffer)
        {
            Multiple<T>.Encode(data, buffer);
        }

        public static void EncodeObject(object data, ByteBuffer buffer)
        {
            AmqpEncoding.EncodeObject(data, buffer);
        }

        //// decode methods

        public static bool? DecodeBoolean(ByteBuffer buffer)
        {
            return BooleanEncoding.Decode(buffer, 0);
        }

        public static byte? DecodeUByte(ByteBuffer buffer)
        {
            return UByteEncoding.Decode(buffer, 0);
        }

        public static ushort? DecodeUShort(ByteBuffer buffer)
        {
            return UShortEncoding.Decode(buffer, 0);
        }

        public static uint? DecodeUInt(ByteBuffer buffer)
        {
            return UIntEncoding.Decode(buffer, 0);
        }

        public static ulong? DecodeULong(ByteBuffer buffer)
        {
            return ULongEncoding.Decode(buffer, 0);
        }

        public static sbyte? DecodeByte(ByteBuffer buffer)
        {
            return ByteEncoding.Decode(buffer, 0);
        }

        public static short? DecodeShort(ByteBuffer buffer)
        {
            return ShortEncoding.Decode(buffer, 0);
        }

        public static int? DecodeInt(ByteBuffer buffer)
        {
            return IntEncoding.Decode(buffer, 0);
        }

        public static long? DecodeLong(ByteBuffer buffer)
        {
            return LongEncoding.Decode(buffer, 0);
        }

        public static float? DecodeFloat(ByteBuffer buffer)
        {
            return FloatEncoding.Decode(buffer, 0);
        }

        public static double? DecodeDouble(ByteBuffer buffer)
        {
            return DoubleEncoding.Decode(buffer, 0);
        }

        public static char? DecodeChar(ByteBuffer buffer)
        {
            return CharEncoding.Decode(buffer, 0);
        }

        public static DateTime? DecodeTimeStamp(ByteBuffer buffer)
        {
            return TimeStampEncoding.Decode(buffer, 0);
        }

        public static Guid? DecodeUuid(ByteBuffer buffer)
        {
            return UuidEncoding.Decode(buffer, 0);
        }

        public static ArraySegment<byte> DecodeBinary(ByteBuffer buffer)
        {
            return BinaryEncoding.Decode(buffer, 0);
        }

        public static string DecodeString(ByteBuffer buffer)
        {
            return StringEncoding.Decode(buffer, 0);
        }

        public static AmqpSymbol DecodeSymbol(ByteBuffer buffer)
        {
            return SymbolEncoding.Decode(buffer, 0);
        }

        public static IList DecodeList(ByteBuffer buffer)
        {
            return ListEncoding.Decode(buffer, 0);
        }

        public static AmqpMap DecodeMap(ByteBuffer buffer)
        {
            return MapEncoding.Decode(buffer, 0);
        }

        public static T DecodeMap<T>(ByteBuffer buffer) where T : RestrictedMap, new()
        {
            AmqpMap map = MapEncoding.Decode(buffer, 0);
            T restrictedMap = null;
            if (map != null)
            {
                restrictedMap = new T();
                restrictedMap.SetMap(map);
            }

            return restrictedMap;
        }

        public static T[] DecodeArray<T>(ByteBuffer buffer)
        {
            return ArrayEncoding.Decode<T>(buffer, 0);
        }

        public static Multiple<T> DecodeMultiple<T>(ByteBuffer buffer)
        {
            return Multiple<T>.Decode(buffer);
        }

        public static object DecodeObject(ByteBuffer buffer)
        {
            FormatCode formatCode = AmqpEncoding.ReadFormatCode(buffer);
            if (formatCode == FormatCode.Null)
            {
                return null;
            }
            else if (formatCode == FormatCode.Described)
            {
                object descriptor = AmqpCodec.DecodeObject(buffer);
                Func<AmqpDescribed> knownTypeCtor = null;
                if (descriptor is AmqpSymbol)
                {
                    knownTypesByName.TryGetValue(((AmqpSymbol)descriptor).Value, out knownTypeCtor);
                }
                else if (descriptor is ulong)
                {
                    knownTypesByCode.TryGetValue((ulong)descriptor, out knownTypeCtor);
                }

                if (knownTypeCtor != null)
                {
                    AmqpDescribed amqpDescribed = knownTypeCtor();
                    amqpDescribed.DecodeValue(buffer);
                    return amqpDescribed;
                }
                else
                {
                    object value = AmqpCodec.DecodeObject(buffer);
                    return new DescribedType(descriptor, value);
                }
            }
            else
            {
                return AmqpEncoding.DecodeObject(buffer, formatCode);
            }
        }

        public static AmqpDescribed DecodeAmqpDescribed(ByteBuffer buffer)
        {
            return DecodeAmqpDescribed(buffer, knownTypesByName, knownTypesByCode);
        }

        public static AmqpDescribed DecodeAmqpDescribed(
            ByteBuffer buffer,
            Dictionary<string, Func<AmqpDescribed>> byName,
            Dictionary<ulong, Func<AmqpDescribed>> byCode)
        {
            AmqpDescribed value = CreateAmqpDescribed(buffer, byName, byCode);
            if (value != null)
            {
                value.DecodeValue(buffer);
            }

            return value;
        }

        public static AmqpDescribed CreateAmqpDescribed(ByteBuffer buffer)
        {
            return CreateAmqpDescribed(buffer, knownTypesByName, knownTypesByCode);
        }

        public static AmqpDescribed CreateAmqpDescribed(
            ByteBuffer buffer,
            Dictionary<string, Func<AmqpDescribed>> byName,
            Dictionary<ulong, Func<AmqpDescribed>> byCode)
        {
            FormatCode formatCode = AmqpEncoding.ReadFormatCode(buffer);
            if (formatCode == FormatCode.Null)
            {
                return null;
            }

            EncodingBase.VerifyFormatCode(formatCode, FormatCode.Described, buffer.Offset);

            Func<AmqpDescribed> knownTypeCtor = null;
            formatCode = AmqpEncoding.ReadFormatCode(buffer);
            if (formatCode == FormatCode.Symbol8 || formatCode == FormatCode.Symbol32)
            {
                AmqpSymbol name = SymbolEncoding.Decode(buffer, formatCode);
                byName.TryGetValue(name.Value, out knownTypeCtor);
            }
            else if (formatCode == FormatCode.ULong0 || formatCode == FormatCode.ULong || formatCode == FormatCode.SmallULong)
            {
                ulong code = ULongEncoding.Decode(buffer, formatCode).Value;
                byCode.TryGetValue(code, out knownTypeCtor);
            }

            if (knownTypeCtor == null)
            {
                throw AmqpEncoding.GetEncodingException("unknown code");
            }

            AmqpDescribed value = knownTypeCtor();

            return value;
        }

        public static T DecodeKnownType<T>(ByteBuffer buffer) where T : class, IAmqpSerializable, new()
        {
            FormatCode formatCode = AmqpEncoding.ReadFormatCode(buffer);
            if (formatCode == FormatCode.Null)
            {
                return null;
            }

            T value = new T();
            value.Decode(buffer);
            return value;
        }
    }
}
