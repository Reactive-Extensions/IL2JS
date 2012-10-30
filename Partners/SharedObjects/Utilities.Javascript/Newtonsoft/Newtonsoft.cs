using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using Microsoft.LiveLabs.JavaScript;
using Microsoft.LiveLabs.JavaScript.Interop;

#if IL2JS

namespace Newtonsoft.Json
{
    public class JsonReader : IDisposable
    {
        private JSObject obj;

        protected JsonReader(string json)
        {
            //Console.WriteLine(json);
            obj = Parse(json);
        }

        public JSObject ParsedObject
        {
            get
            {
                return this.obj;
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
        }

        #endregion

        [Import(@"function(text) { return JSON.parse(text); }")]
        extern public static JSObject Parse(string text);
    }

    public class JsonTextReader : JsonReader
    {
        public JsonTextReader(StreamReader sr) : base(sr.ReadToEnd())
        {
        }

        public JsonTextReader(string json)
            : base(json)
        {

        }
    }
    
    public class JsonSerializer
    {
        public object Deserialize(StreamReader reader, Type type)
        {
            var json = new JsonTextReader(reader);
            if (type == typeof(string))
            {
                return json.ParsedObject.To<string>();
            }
            else if (type == typeof(Int32))
            {
                return json.ParsedObject.To<Int32>();
            }
            else
            {
                throw new NotSupportedException("Cannot deserialize this type");
            }
        }

        public void Serialize(StreamWriter writer, object obj)
        {
            var txt = new JsonTextWriter(writer);
            txt.WriteObject(obj);
        }
    }
}

namespace Newtonsoft.Json.Converters
{
    public class JavascriptDateTimeConverter
    {
    }
}

namespace Newtonsoft.Json.Linq
{
    [Import]
    public class JToken : JSObject
    {
        public static object ReadFrom(JsonReader reader)
        {
            // TODO: must work out if its an array or not
            if (reader.ParsedObject is JSArray)
            {
                return new JArray(reader.ParsedObject as JSArray);
            }
            else
            {
                return new JObject(reader.ParsedObject);
            }
        }

        public T Value<T>(string name)
        {
            return HasField(name) ? GetField<T>(name) : default(T);
        }

        public T Value<T>()
        {
            return To<T>();
        }

        public bool HasValues
        {
            get
            {
                return GetEnumerator() != null;
            }
        }

        public new JToken this[string name]
        {
            get
            {
                return HasField(name) ? new JObject(base[name]) : null;
            }
        }
    }

    public class JArray : JToken, IList<JToken>
    {
        // BUSTED. Probably need to derive from JObject

        [Import(@"function(arr) { return arr; }")]
        extern public JArray(JSArray array);

        //public JArray(JToken tok) : base()
        //{
        //}
        #region IList<JToken> Members

        public int IndexOf(JToken item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, JToken item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public JToken this[int index]
        {
            get
            {
                return new JObject(this.To<JSArray>()[index]);
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region ICollection<JToken> Members

        public void Add(JToken item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(JToken item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(JToken[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsReadOnly
        {
            get { throw new NotImplementedException(); }
        }

        public bool Remove(JToken item)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IEnumerable<JToken> Members

        public IEnumerator<JToken> GetEnumerator()
        {
            JSArray a = this.To<JSArray>();
            foreach (JSObject obj in a)
            {
                yield return new JObject(obj);
            }
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            JSArray a = this.To<JSArray>();
            foreach (JSObject obj in a)
            {
                yield return new JObject(obj);
            }
        }

        #endregion
    }

    public class JObject : JToken
    {
        [Import(@"function(obj) { return obj; }")]
        extern public JObject(JSObject obj);

        public bool TryGetValue(string name, out JToken tok)
        {
            if (HasField(name))
            {
                tok = this[name];
                return true;
            }
            else
            {
                tok = null;
                return false;
            }
        }
    }
}

#endif

