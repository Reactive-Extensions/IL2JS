//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
    abstract class EncodingBase
    {
        FormatCode formatCode;

        protected EncodingBase(FormatCode formatCode)
        {
            this.formatCode = formatCode;
        }

        public FormatCode FormatCode 
        {
            get { return this.formatCode; }
        }

        public abstract int GetObjectEncodeSize(object value, bool arrayEncoding);

        public abstract void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer);

        public abstract object DecodeObject(ByteBuffer buffer, FormatCode formatCode);

        public static void VerifyFormatCode(FormatCode formatCode, FormatCode expected, int offset)
        {
            if (formatCode != expected)
            {
                throw AmqpEncoding.GetInvalidFormatCodeException(formatCode, offset);
            }
        }

        public static void VerifyFormatCode(FormatCode formatCode, int offset, params FormatCode[] expected)
        {
            bool valid = false;
            foreach (FormatCode code in expected)
            {
                if (formatCode == code)
                {
                    valid = true;
                    break;
                }
            }

            if (!valid)
            {
                throw AmqpEncoding.GetInvalidFormatCodeException(formatCode, offset);
            }
        }
    }
}
