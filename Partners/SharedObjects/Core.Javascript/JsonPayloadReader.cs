using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Linq;
using Microsoft.LiveLabs.JavaScript;

namespace Microsoft.Csa.SharedObjects
{
    public class JsonPayloadReader : IPayloadReader
    {
        //JToken jsonToken;
        JSObject jsObject;

        public JsonPayloadReader(string json)
        {
            using (JsonReader reader = new JsonTextReader(json))
            {
                this.jsObject = reader.ParsedObject;
                //this.jsonToken = JToken.ReadFrom(reader) as JToken;
            }
        }

        public JsonPayloadReader(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);

            using (StreamReader sw = new StreamReader(stream))
            using (JsonReader reader = new JsonTextReader(sw))
            {
                this.jsObject = reader.ParsedObject;
                //this.jsonToken = JToken.ReadFrom(reader) as JToken; //JObject.Parse(sw.ReadToEnd());
            }
        }
        
        private JsonPayloadReader(JObject obj)
        {
            //this.jsonToken = obj;
        }

        private JsonPayloadReader(JSObject obj)
        {
            this.jsObject = obj;
        }

        #region IPayloadReader Members

        public string ReadString(string name)
        {
            string value = jsObject.GetField<String>(name);
            return (value != null) ? value : string.Empty;
        }

        public bool ReadBoolean(string name)
        {
            return jsObject.GetField<bool>(name);
        }

        public byte ReadByte(string name)
        {
            return jsObject.GetField<byte>(name);
        }

        public short ReadInt16(string name)
        {
            return jsObject.GetField<Int16>(name);
        }

        public UInt16 ReadUInt16(string name)
        {
            return jsObject.GetField<UInt16>(name);
        }

        public int ReadInt32(string name)
        {
            return jsObject.GetField<Int32>(name);
        }

        public long ReadInt64(string name)
        {
            return jsObject.GetField<Int64>(name);
        }

        public byte[] ReadBytes(string name)
        {
            string encoded = this.ReadString(name);
            return Convert.FromBase64String(encoded);
        }

        public Guid ReadGuid(string name)
        {
            return new Guid(ReadString(name));
        }

        private JSArray ReadArrayToken(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return (JSArray)this.jsObject;
            }

            return (JSArray)this.jsObject[name];
        }

        public void ReadList(string name, Action<IPayloadReader> readFunc)
        {
            JSArray array = ReadArrayToken(name);

            foreach (var tok in array)
            {
                JSObject jobj = tok as JSObject;
                if (jobj != null)
                {
                    // Create an object that can read from this token:
                    JsonPayloadReader reader = new JsonPayloadReader(jobj);
                    readFunc(reader);
                }
            }
        }

        public Int32[] ReadIntArray(string name)
        {
            JSArray array = ReadArrayToken(name);
            IEnumerable<JSObject> e = array;
            //return e.Select(tok => tok .Value<Int32>()).ToArray();
            return null;
        }

        public string[] ReadStringArray(string name)
        {
            //JArray array = ReadArrayToken(name);
            //IEnumerable<JToken> e = array;
            //return e.Select(tok => tok.Value<String>()).ToArray();
            return null;
        }

        public List<T> ReadList<T>(string name) where T : ISharedObjectSerializable, new()
        {
            return this.ReadList(name, reader => { 
                T item = new T(); 
                item.Deserialize(reader); 
                return item; });
        }

        public List<T> ReadList<T>(string name, Func<IPayloadReader, T> readFunc)
        {
            List<T> items = new List<T>();
            ReadList(name, reader =>
                {
                    T item = readFunc(reader);
                    items.Add(item);
                });
            return items;
        }

        public T ReadObject<T>(string name, Func<IPayloadReader, T> readFunc)
        {
            if (string.IsNullOrEmpty(name))
            {
                return readFunc(this);
            }

            JSObject o = (JSObject)this.jsObject[name];
            return (!o.IsUndefined) ? readFunc(new JsonPayloadReader(o)) : default(T);
        }
        
        public T ReadObject<T>(string name) where T : ISharedObjectSerializable, new()
        {
            JsonPayloadReader reader = this;

            if (!string.IsNullOrEmpty(name))
            {
                JSObject tok = this.jsObject[name];
                if(tok == null)
                {
                    return default(T);
                }
                reader = new JsonPayloadReader(tok);
            }

            T value = new T();
            // Deserialize using a reader based on the sub-object
            value.Deserialize(reader);
            return value;
        }

        public T ReadObject<T>(string name, ReadObjectOption option) where T : ISharedObjectSerializable, new()
        {
            T value = this.ReadObject<T>(name);
            if (value == null && option == ReadObjectOption.Create)
            {
                return new T();
            }
            else
            {
                return value;
            }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
        }

        #endregion
    }
}
