using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;

namespace Microsoft.Csa.SharedObjects
{
    public class JsonPayloadWriter : IPayloadWriter
    {
        private JsonWriter writer;

        public JsonPayloadWriter(Stream stream)
        {
            this.writer = new JsonTextWriter(new StreamWriter(stream));
        }

        public JsonPayloadWriter(StreamWriter sw)
        {
            this.writer = new JsonTextWriter(sw);
        }

        #region IPayloadWriter Members

        public void Write(string name, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                this.writer.WritePropertyName(name);
                this.writer.WriteValue(value);
            }
        }

        public void Write(string name, bool value)
        {
            if (value)
            {
                this.writer.WritePropertyName(name);
                this.writer.WriteValue(value);
            }
        }

        public void Write(string name, byte value)
        {
            if (value != 0)
            {
                this.writer.WritePropertyName(name);
                this.writer.WriteValue(value);
            }
        }

        public void Write(string name, short value)
        {
            if (value != 0)
            {
                this.writer.WritePropertyName(name);
                this.writer.WriteValue(value);
            }
        }

        public void Write(string name, UInt16 value)
        {
            if (value != 0)
            {
                this.writer.WritePropertyName(name);
                this.writer.WriteValue(value);
            }
        }

        public void Write(string name, int value)
        {
            if (value != 0)
            {
                this.writer.WritePropertyName(name);
                this.writer.WriteValue(value);
            }
        }

        public void Write(string name, long value)
        {
            if (value != 0)
            {
                this.writer.WritePropertyName(name);
                this.writer.WriteValue(value);
            }
        }

        public void Write(string name, double value)
        {
            if (value != 0.0)
            {
                this.writer.WritePropertyName(name);
                this.writer.WriteValue(value);
            }
        }

        public void Write(string name, byte[] value)
        {
            this.writer.WritePropertyName(name);
            this.writer.WriteValue(Convert.ToBase64String(value));
        }

        public void Write(string name, Guid guid)
        {
            this.writer.WritePropertyName(name);
            this.writer.WriteValue(guid.ToString());
        }

        public void Write(string name, Int32[] xs)
        {
            WriteArrayCore(name, xs, (_, o) => this.writer.WriteValue(o), false);
        }

        public void Write(string name, string[] xs)
        {
            WriteArrayCore(name, xs, (_, o) => this.writer.WriteValue(o), false);
        }

        public void Write<T>(string name, IEnumerable<T> xs) where T : ISharedObjectSerializable
        {
            this.Write(name, xs, (_, x) => x.Serialize(this));
        }

        public void Write<T>(string name, IEnumerable<T> xs, Action<IPayloadWriter, T> writeFunc)
        {
            WriteArrayCore(name, xs, writeFunc, true);
        }

        public void Write(string name, ISharedObjectSerializable value)
        {
            if (value != null)
            {
                if (!string.IsNullOrEmpty(name))
                {
                    this.writer.WritePropertyName(name);
                }
                this.writer.WriteStartObject();
                value.Serialize(this);
                this.writer.WriteEndObject();
            }
        }

        #endregion

        private void WriteArrayCore<T>(string name, IEnumerable<T> xs, Action<IPayloadWriter, T> writeFunc, bool isObject)
        {
            if (!string.IsNullOrEmpty(name))
            {
                this.writer.WritePropertyName(name);
            }
            
            this.writer.WriteStartArray();

            // call write function for each item in the list
            foreach (var x in xs)
            {
                // write object wrapper if T is a complex object. If not,
                // this routine works for scalar types as well
                if (isObject)
                {
                    this.writer.WriteStartObject();
                }

                writeFunc(this, x);

                if (isObject)
                {
                    this.writer.WriteEndObject();
                }
            }
            
            this.writer.WriteEndArray();
        }

        #region IDisposable Members

        public void Dispose()
        {
            this.writer.Flush();
        }

        #endregion
    }
}
