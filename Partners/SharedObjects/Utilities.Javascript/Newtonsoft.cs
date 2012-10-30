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
    }

    public class JsonWriter : IDisposable
    {
        // Tried to do this with enum but hit problems with Stack of enum.
        private const int State_Empty = 0;
        private const int State_Object = 1;
        private const int State_ObjectBody = 2;
        private const int State_Array = 3;
        private const int State_ArrayBody = 4;
        private const int State_Value = 5;

        private enum State
        {
            Empty,
            Object,
            ObjectBody,
            Array,
            ArrayBody,
            Value
        }

        private StreamWriter sw;
        //private Stack<State> states;
        private Stack<int> states;
        public JsonWriter(StreamWriter writer)
        {
            sw = writer;
            states = new Stack<int>();
            states.Push(State_Empty);
        }

        private void OnWriteValue()
        {
            switch (states.Peek())
            {
                case State_Object:
                    states.Push(State_ObjectBody);
                    break;
                case State_ObjectBody:
                    break;
                case State_Array:
                    states.Push(State_ArrayBody);
                    break;
                case State_ArrayBody:
                    sw.Write(",");
                    break;
                default:
                    throw new Exception("Invalid Json writer state");
            }
        }

        public void WritePropertyName(string name)
        {
            int curState = states.Peek();
            if (curState == State_ObjectBody)
            {
                sw.Write(",");
            }
            sw.Write("\"" + name + "\":");
        }

        // TODO: need special case for DateTime ??
        public void WriteValue<T>(T val)
        {
            OnWriteValue();
            sw.Write(val.ToString());
        }

        public void WriteValue(string val)
        {
            OnWriteValue();
            sw.Write("\"" + val + "\"");
        }

        public void WriteStartObject()
        {
            states.Push(State_Object);
            sw.Write("{");
        }

        public void WriteEndObject()
        {
            // It is possible that we wrote out a completely empty object like this: {}
            // and never entered the ObjectBody state. We have to pop back to before the Object state

            int oldState = states.Pop();
            if (oldState == State_ObjectBody)
                states.Pop();

            sw.Write("}");
        }

        public void WriteStartArray()
        {
            states.Push(State_Array);
            sw.Write("[");
        }

        public void WriteEndArray()
        {
            int oldState = states.Pop();
            if (oldState == State_ArrayBody)
                states.Pop();
            sw.Write("]");
        }

        public void Flush()
        {
            sw.Flush();
        }

        #region IDisposable Members

        public void Dispose()
        {
            Flush();
        }

        #endregion
    }

    public class JsonTextWriter : JsonWriter
    {
        public JsonTextWriter(StreamWriter writer) : base(writer) { }
    }

    public class JsonSerializer
    {
        public object Deserialize(StreamReader reader, Type type)
        {
            return null;
        }

        public void Serialize(StreamWriter writer, object obj)
        {

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
        public static JToken ReadFrom(JsonReader reader)
        {
            // TODO: must work out if its an array or not
            return new JObject(reader.ParsedObject);
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
        public JArray(JToken tok) : base()
        {
        }
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

