//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;

    /// <summary>
    /// Multiple is not an AMQP data type. It is a property of a list field
    /// that affects the encoding of the associated data type.
    /// </summary>
    sealed class Multiple<T>
    {
        List<T> value;

        public Multiple()
        {
            this.value = new List<T>();
        }

        public Multiple(IList<T> value)
        {
            this.value = new List<T>(value);
        }

        public void Add(T item)
        {
            this.value.Add(item);
        }

        public bool Contains(T item)
        {
            return this.value.Contains(item);
        }

        public static int GetEncodeSize(Multiple<T> multiple)
        {
            if (multiple == null)
            {
                return FixedWidth.NullEncoded;
            }
            else if (multiple.value.Count == 1)
            {
                return AmqpEncoding.GetObjectEncodeSize(multiple.value[0]);
            }
            else
            {
                return ArrayEncoding.GetEncodeSize(multiple.value.ToArray());
            }
        }

        public static void Encode(Multiple<T> multiple, ByteBuffer buffer)
        {
            if (multiple == null)
            {
                AmqpEncoding.EncodeNull(buffer);
            }
            else if (multiple.value.Count == 1)
            {
                AmqpEncoding.EncodeObject(multiple.value[0], buffer);
            }
            else
            {
                ArrayEncoding.Encode(multiple.value.ToArray(), buffer);
            }
        }

        public static Multiple<T> Decode(ByteBuffer buffer)
        {
            object value = AmqpEncoding.DecodeObject(buffer);
            if (value == null)
            {
                return null;
            }
            else if (value is T)
            {
                Multiple<T> multiple = new Multiple<T>();
                multiple.Add((T)value);
                return multiple;
            }
            else if (value.GetType().IsArray)
            {
                Multiple<T> multiple = new Multiple<T>((T[])value);
                return multiple;
            }
            else
            {
                throw new AmqpException(AmqpError.InvalidField);
            }
        }

        public static IList<T> Intersect(Multiple<T> multiple1, Multiple<T> multiple2)
        {
            List<T> list = new List<T>();
            if (multiple1 == null || multiple2 == null)
            {
                return list;
            }

            foreach (T t1 in multiple1.value)
            {
                if (multiple2.value.Contains(t1))
                {
                    list.Add(t1);
                }
            }

            return list;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("[");
            bool firstItem = true;
            foreach (object item in this.value)
            {
                if (!firstItem)
                {
                    sb.Append(',');
                }

                sb.Append(item.ToString());
                firstItem = false;
            }

            sb.Append(']');
            return sb.ToString();
        }
    }
}
