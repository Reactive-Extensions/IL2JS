using System;
using System.IO;
using Newtonsoft.Json.Utilities;
using System.Diagnostics;

namespace Newtonsoft.Json
{
    public class JsonTextWriter : JsonWriter
    {
        // Fields
        private int _indentation;
        private char _indentChar;
        private char _quoteChar;
        private bool _quoteName;
        private readonly TextWriter _writer;

        // Methods
        public JsonTextWriter(TextWriter textWriter)
        {
            if (textWriter == null)
            {
                throw new ArgumentNullException("textWriter");
            }
            this._writer = textWriter;
            this._quoteChar = '"';
            this._quoteName = true;
            this._indentChar = ' ';
            this._indentation = 2;
        }

        public override void Close()
        {
            base.Close();
            this._writer.Close();
        }

        public override void Flush()
        {
            this._writer.Flush();
        }

        public override void WriteComment(string text)
        {
            base.WriteComment(text);
            this._writer.Write("/*");
            this._writer.Write(text);
            this._writer.Write("*/");
        }

        protected override void WriteEnd(JsonToken token)
        {
            switch (token)
            {
                case JsonToken.EndObject:
                    this._writer.Write("}");
                    return;

                case JsonToken.EndArray:
                    this._writer.Write("]");
                    return;

                case JsonToken.EndConstructor:
                    this._writer.Write(")");
                    return;
            }
            throw new Exception("Invalid JsonToken: " + token);
        }

        protected override void WriteIndent()
        {
            if (base.Formatting == Formatting.Indented)
            {
                this._writer.Write(Environment.NewLine);
                int num = base.Top * this._indentation;
                for (int i = 0; i < num; i++)
                {
                    this._writer.Write(this._indentChar);
                }
            }
        }

        protected override void WriteIndentSpace()
        {
            this._writer.Write(' ');
        }

        public override void WriteNull()
        {
            base.WriteNull();
            this.WriteValueInternal(JsonConvert.Null, JsonToken.Null);
        }

        public override void WritePropertyName(string name)
        {
            base.WritePropertyName(name);
            JavaScriptUtils.WriteEscapedJavaScriptString(this._writer, name, this._quoteChar, this._quoteName);
            this._writer.Write(':');
        }

        public override void WriteRaw(string json)
        {
            base.WriteRaw(json);
            this._writer.Write(json);
        }

        public override void WriteStartArray()
        {
            base.WriteStartArray();
            this._writer.Write("[");
        }

        public override void WriteStartConstructor(string name)
        {
            base.WriteStartConstructor(name);
            this._writer.Write("new ");
            this._writer.Write(name);
            this._writer.Write("(");
        }

        public override void WriteStartObject()
        {
            base.WriteStartObject();
            this._writer.Write("{");
        }

        public override void WriteUndefined()
        {
            base.WriteUndefined();
            this.WriteValueInternal(JsonConvert.Undefined, JsonToken.Undefined);
        }

        public override void WriteValue(bool value)
        {
            base.WriteValue(value);
            this.WriteValueInternal(JsonConvert.ToString(value), JsonToken.Boolean);
        }

        public override void WriteValue(byte value)
        {
            base.WriteValue(value);
            this.WriteValueInternal(JsonConvert.ToString(value), JsonToken.Integer);
        }

        public override void WriteValue(char value)
        {
            base.WriteValue(value);
            this.WriteValueInternal(JsonConvert.ToString(value), JsonToken.Integer);
        }

        //public override void WriteValue(DateTime value)
        //{
        //    base.WriteValue(value);
        //    JsonConvert.WriteDateTimeString(this._writer, value);
        //}

        //public override void WriteValue(DateTimeOffset value)
        //{
        //    base.WriteValue(value);
        //    this.WriteValueInternal(JsonConvert.ToString(value), JsonToken.Date);
        //}

        public void WriteObject(object value)
        {
            if (value is string)
            {
                WriteValue(value as string);
            }
            else if (value is bool)
            {
                WriteValue((bool)value);
            }
            else if (value is Int32)
            {
                WriteValue((Int32)value);
            }
            else if (value is byte)
            {
                WriteValue((byte)value);
            }
            else
            {
                throw new NotSupportedException("WriteValue does not support this type");
            }
        }

        public override void WriteValue(decimal value)
        {
            base.WriteValue(value);
            this.WriteValueInternal(JsonConvert.ToString(value), JsonToken.Float);
        }

        public override void WriteValue(double value)
        {
            base.WriteValue(value);
            this.WriteValueInternal(JsonConvert.ToString(value), JsonToken.Float);
        }

        public override void WriteValue(short value)
        {
            base.WriteValue(value);
            this.WriteValueInternal(value, JsonToken.Integer);
        }

        public override void WriteValue(int value)
        {
            base.WriteValue(value);
            this.WriteValueInternal(value, JsonToken.Integer);
        }

        public override void WriteValue(long value)
        {
            base.WriteValue(value);
            this.WriteValueInternal(value, JsonToken.Integer);
        }

        public override void WriteValue(sbyte value)
        {
            base.WriteValue(value);
            this.WriteValueInternal(value, JsonToken.Integer);
        }

        public override void WriteValue(float value)
        {
            base.WriteValue(value);
            this.WriteValueInternal(value, JsonToken.Float);
        }

        public override void WriteValue(string value)
        {
            base.WriteValue(value);
            if (value == null)
            {
                this.WriteValueInternal(JsonConvert.Null, JsonToken.Null);
            }
            else
            {
                JavaScriptUtils.WriteEscapedJavaScriptString(this._writer, value, this._quoteChar, true);
            }
        }

        public override void WriteValue(ushort value)
        {
            base.WriteValue(value);
            this.WriteValueInternal(JsonConvert.ToString(value), JsonToken.Integer);
        }

        public override void WriteValue(uint value)
        {
            base.WriteValue(value);
            this.WriteValueInternal(JsonConvert.ToString(value), JsonToken.Integer);
        }

        public override void WriteValue(ulong value)
        {
            base.WriteValue(value);
            this.WriteValueInternal(JsonConvert.ToString(value), JsonToken.Integer);
        }

        //public override void WriteValue(byte[] value)
        //{
        //    base.WriteValue(value);
        //    if (value != null)
        //    {
        //        this._writer.Write(this._quoteChar);
        //        this.Base64Encoder.Encode(value, 0, value.Length);
        //        this.Base64Encoder.Flush();
        //        this._writer.Write(this._quoteChar);
        //    }
        //}

        protected override void WriteValueDelimiter()
        {
            this._writer.Write(',');
        }

        private void WriteValueInternal(object value, JsonToken token)
        {
            this._writer.Write(value.ToString());
        }

        private void WriteValueInternal(string value, JsonToken token)
        {
            this._writer.Write(value);
        }

        public override void WriteWhitespace(string ws)
        {
            base.WriteWhitespace(ws);
            this._writer.Write(ws);
        }

        //// Properties
        //private Base64Encoder Base64Encoder
        //{
        //    get
        //    {
        //        if (this._base64Encoder == null)
        //        {
        //            this._base64Encoder = new Base64Encoder(this._writer);
        //        }
        //        return this._base64Encoder;
        //    }
        //}

        public int Indentation
        {
            get
            {
                return this._indentation;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("Indentation value must be greater than 0.");
                }
                this._indentation = value;
            }
        }

        public char IndentChar
        {
            get
            {
                return this._indentChar;
            }
            set
            {
                this._indentChar = value;
            }
        }

        public char QuoteChar
        {
            get
            {
                return this._quoteChar;
            }
            set
            {
                if ((value != '"') && (value != '\''))
                {
                    throw new ArgumentException("Invalid JavaScript string quote character. Valid quote characters are ' and \".");
                }
                this._quoteChar = value;
            }
        }

        public bool QuoteName
        {
            get
            {
                return this._quoteName;
            }
            set
            {
                this._quoteName = value;
            }
        }
    }
}
