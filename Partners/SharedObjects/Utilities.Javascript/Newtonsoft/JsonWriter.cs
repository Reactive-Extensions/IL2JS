using System;
using System.Collections.Generic;

namespace Newtonsoft.Json
{
    public abstract class JsonWriter : IDisposable
    {
        // Fields
        private State _currentState;
        private Formatting _formatting;
        private readonly List<JTokenType> _stack = new List<JTokenType>(8);
        private int _top;
        private static readonly State[][] stateArray;

        // Methods
        static JsonWriter()
        {
            State[][] stateArray = new State[8][];
            stateArray[0] = new State[] { State.Error, State.Error, State.Error, State.Error, State.Error, State.Error, State.Error, State.Error, State.Error, State.Error };
            stateArray[1] = new State[] { State.ObjectStart, State.ObjectStart, State.Error, State.Error, State.ObjectStart, State.ObjectStart, State.ObjectStart, State.ObjectStart, State.Error, State.Error };
            stateArray[2] = new State[] { State.ArrayStart, State.ArrayStart, State.Error, State.Error, State.ArrayStart, State.ArrayStart, State.ArrayStart, State.ArrayStart, State.Error, State.Error };
            stateArray[3] = new State[] { State.ConstructorStart, State.ConstructorStart, State.Error, State.Error, State.ConstructorStart, State.ConstructorStart, State.ConstructorStart, State.ConstructorStart, State.Error, State.Error };
            stateArray[4] = new State[] { State.Property, State.Error, State.Property, State.Property, State.Error, State.Error, State.Error, State.Error, State.Error, State.Error };
            State[] stateArray7 = new State[10];
            stateArray7[1] = State.Property;
            stateArray7[2] = State.ObjectStart;
            stateArray7[3] = State.Object;
            stateArray7[4] = State.ArrayStart;
            stateArray7[5] = State.Array;
            stateArray7[6] = State.Constructor;
            stateArray7[7] = State.Constructor;
            stateArray7[8] = State.Error;
            stateArray7[9] = State.Error;
            stateArray[5] = stateArray7;
            State[] stateArray8 = new State[10];
            stateArray8[1] = State.Property;
            stateArray8[2] = State.ObjectStart;
            stateArray8[3] = State.Object;
            stateArray8[4] = State.ArrayStart;
            stateArray8[5] = State.Array;
            stateArray8[6] = State.Constructor;
            stateArray8[7] = State.Constructor;
            stateArray8[8] = State.Error;
            stateArray8[9] = State.Error;
            stateArray[6] = stateArray8;
            State[] stateArray9 = new State[10];
            stateArray9[1] = State.Object;
            stateArray9[2] = State.Error;
            stateArray9[3] = State.Error;
            stateArray9[4] = State.Array;
            stateArray9[5] = State.Array;
            stateArray9[6] = State.Constructor;
            stateArray9[7] = State.Constructor;
            stateArray9[8] = State.Error;
            stateArray9[9] = State.Error;
            stateArray[7] = stateArray9;
            JsonWriter.stateArray = stateArray;
        }

        public JsonWriter()
        {
            this._stack.Add(JTokenType.None);
            this._currentState = State.Start;
            this._formatting = Formatting.None;
        }

        internal void AutoComplete(JsonToken tokenBeingWritten)
        {
            int num;
            switch (tokenBeingWritten)
            {
                case JsonToken.Integer:
                case JsonToken.Float:
                case JsonToken.String:
                case JsonToken.Boolean:
                case JsonToken.Null:
                case JsonToken.Undefined:
                case JsonToken.Date:
                case JsonToken.Bytes:
                    num = 7;
                    break;

                default:
                    num = (int)tokenBeingWritten;
                    break;
            }
            State state = stateArray[num][(int)this._currentState];
            if (state == State.Error)
            {
                throw new Exception("Token {0} in state {1} would result in an invalid JavaScript object.");
            }
            if ((((this._currentState == State.Object) || (this._currentState == State.Array)) || (this._currentState == State.Constructor)) && (tokenBeingWritten != JsonToken.Comment))
            {
                this.WriteValueDelimiter();
            }
            else if ((this._currentState == State.Property) && (this._formatting == Formatting.Indented))
            {
                this.WriteIndentSpace();
            }
            WriteState writeState = this.WriteState;
            if (((tokenBeingWritten == JsonToken.PropertyName) && (writeState != WriteState.Start)) || ((writeState == WriteState.Array) || (writeState == WriteState.Constructor)))
            {
                this.WriteIndent();
            }
            this._currentState = state;
        }

        private void AutoCompleteAll()
        {
            while (this._top > 0)
            {
                this.WriteEnd();
            }
        }

        private void AutoCompleteClose(JsonToken tokenBeingClosed)
        {
            int num = 0;
            for (int i = 0; i < this._top; i++)
            {
                int num3 = this._top - i;
                if (((JTokenType)this._stack[num3]) == this.GetTypeForCloseToken(tokenBeingClosed))
                {
                    num = i + 1;
                    break;
                }
            }
            if (num == 0)
            {
                throw new Exception("No token to close.");
            }
            for (int j = 0; j < num; j++)
            {
                JsonToken closeTokenForType = this.GetCloseTokenForType(this.Pop());
                if ((this._currentState != State.ObjectStart) && (this._currentState != State.ArrayStart))
                {
                    this.WriteIndent();
                }
                this.WriteEnd(closeTokenForType);
            }
            JTokenType type = this.Peek();
            switch (type)
            {
                case JTokenType.None:
                    this._currentState = State.Start;
                    return;

                case JTokenType.Object:
                    this._currentState = State.Object;
                    return;

                case JTokenType.Array:
                    this._currentState = State.Array;
                    return;

                case JTokenType.Constructor:
                    this._currentState = State.Array;
                    return;
            }
            throw new Exception("Unknown JsonType: " + type);
        }

        public virtual void Close()
        {
            this.AutoCompleteAll();
        }

        private void Dispose(bool disposing)
        {
            if (this.WriteState != WriteState.Closed)
            {
                this.Close();
            }
        }

        public abstract void Flush();
        private JsonToken GetCloseTokenForType(JTokenType type)
        {
            switch (type)
            {
                case JTokenType.Object:
                    return JsonToken.EndObject;

                case JTokenType.Array:
                    return JsonToken.EndArray;

                case JTokenType.Constructor:
                    return JsonToken.EndConstructor;
            }
            throw new Exception("No close token for type: " + type);
        }

        private JTokenType GetTypeForCloseToken(JsonToken token)
        {
            switch (token)
            {
                case JsonToken.EndObject:
                    return JTokenType.Object;

                case JsonToken.EndArray:
                    return JTokenType.Array;

                case JsonToken.EndConstructor:
                    return JTokenType.Constructor;
            }
            throw new Exception("No type for token: " + token);
        }

        private bool IsEndToken(JsonToken token)
        {
            switch (token)
            {
                case JsonToken.EndObject:
                case JsonToken.EndArray:
                case JsonToken.EndConstructor:
                    return true;
            }
            return false;
        }

        private bool IsStartToken(JsonToken token)
        {
            switch (token)
            {
                case JsonToken.StartObject:
                case JsonToken.StartArray:
                case JsonToken.StartConstructor:
                    return true;
            }
            return false;
        }

        private JTokenType Peek()
        {
            return this._stack[this._top];
        }

        private JTokenType Pop()
        {
            JTokenType type = this.Peek();
            this._top--;
            return type;
        }

        private void Push(JTokenType value)
        {
            this._top++;
            if (this._stack.Count <= this._top)
            {
                this._stack.Add(value);
            }
            else
            {
                this._stack[this._top] = value;
            }
        }

        void IDisposable.Dispose()
        {
            this.Dispose(true);
        }

        public virtual void WriteComment(string text)
        {
            this.AutoComplete(JsonToken.Comment);
        }

        //private void WriteConstructorDate(JsonReader reader)
        //{
        //    if (!reader.Read())
        //    {
        //        throw new Exception("Unexpected end while reading date constructor.");
        //    }
        //    if (reader.TokenType != JsonToken.Integer)
        //    {
        //        throw new Exception("Unexpected token while reading date constructor. Expected Integer, got " + reader.TokenType);
        //    }
        //    long javaScriptTicks = (long)reader.Value;
        //    DateTime time = JsonConvert.ConvertJavaScriptTicksToDateTime(javaScriptTicks);
        //    if (!reader.Read())
        //    {
        //        throw new Exception("Unexpected end while reading date constructor.");
        //    }
        //    if (reader.TokenType != JsonToken.EndConstructor)
        //    {
        //        throw new Exception("Unexpected token while reading date constructor. Expected EndConstructor, got " + reader.TokenType);
        //    }
        //    this.WriteValue(time);
        //}

        public void WriteEnd()
        {
            this.WriteEnd(this.Peek());
        }

        protected virtual void WriteEnd(JsonToken token)
        {
        }

        private void WriteEnd(JTokenType type)
        {
            switch (type)
            {
                case JTokenType.Object:
                    this.WriteEndObject();
                    return;

                case JTokenType.Array:
                    this.WriteEndArray();
                    return;

                case JTokenType.Constructor:
                    this.WriteEndConstructor();
                    return;
            }
            throw new Exception("Unexpected type when writing end: " + type);
        }

        public void WriteEndArray()
        {
            this.AutoCompleteClose(JsonToken.EndArray);
        }

        public void WriteEndConstructor()
        {
            this.AutoCompleteClose(JsonToken.EndConstructor);
        }

        public void WriteEndObject()
        {
            this.AutoCompleteClose(JsonToken.EndObject);
        }

        protected virtual void WriteIndent()
        {
        }

        protected virtual void WriteIndentSpace()
        {
        }

        public virtual void WriteNull()
        {
            this.AutoComplete(JsonToken.Null);
        }

        public virtual void WritePropertyName(string name)
        {
            this.AutoComplete(JsonToken.PropertyName);
        }

        public virtual void WriteRaw(string json)
        {
        }

        public virtual void WriteRawValue(string json)
        {
            this.AutoComplete(JsonToken.Undefined);
            this.WriteRaw(json);
        }

        public virtual void WriteStartArray()
        {
            this.AutoComplete(JsonToken.StartArray);
            this.Push(JTokenType.Array);
        }

        public virtual void WriteStartConstructor(string name)
        {
            this.AutoComplete(JsonToken.StartConstructor);
            this.Push(JTokenType.Constructor);
        }

        public virtual void WriteStartObject()
        {
            this.AutoComplete(JsonToken.StartObject);
            this.Push(JTokenType.Object);
        }

        //public void WriteToken(JsonReader reader)
        //{
        //    int depth;
        //    ValidationUtils.ArgumentNotNull(reader, "reader");
        //    if (reader.TokenType == JsonToken.None)
        //    {
        //        depth = -1;
        //    }
        //    else if (!this.IsStartToken(reader.TokenType))
        //    {
        //        depth = reader.Depth + 1;
        //    }
        //    else
        //    {
        //        depth = reader.Depth;
        //    }
        //    this.WriteToken(reader, depth);
        //}

        //internal void WriteToken(JsonReader reader, int initialDepth)
        //{
        //    do
        //    {
        //        switch (reader.TokenType)
        //        {
        //            case JsonToken.None:
        //                break;

        //            case JsonToken.StartObject:
        //                this.WriteStartObject();
        //                break;

        //            case JsonToken.StartArray:
        //                this.WriteStartArray();
        //                break;

        //            case JsonToken.StartConstructor:
        //                if (string.Compare(reader.Value.ToString(), "Date", StringComparison.Ordinal) != 0)
        //                {
        //                    this.WriteStartConstructor(reader.Value.ToString());
        //                    break;
        //                }
        //                this.WriteConstructorDate(reader);
        //                break;

        //            case JsonToken.PropertyName:
        //                this.WritePropertyName(reader.Value.ToString());
        //                break;

        //            case JsonToken.Comment:
        //                this.WriteComment(reader.Value.ToString());
        //                break;

        //            case JsonToken.Raw:
        //                this.WriteRawValue((string)reader.Value);
        //                break;

        //            case JsonToken.Integer:
        //                this.WriteValue((long)reader.Value);
        //                break;

        //            case JsonToken.Float:
        //                this.WriteValue((double)reader.Value);
        //                break;

        //            case JsonToken.String:
        //                this.WriteValue(reader.Value.ToString());
        //                break;

        //            case JsonToken.Boolean:
        //                this.WriteValue((bool)reader.Value);
        //                break;

        //            case JsonToken.Null:
        //                this.WriteNull();
        //                break;

        //            case JsonToken.Undefined:
        //                this.WriteUndefined();
        //                break;

        //            case JsonToken.EndObject:
        //                this.WriteEndObject();
        //                break;

        //            case JsonToken.EndArray:
        //                this.WriteEndArray();
        //                break;

        //            case JsonToken.EndConstructor:
        //                this.WriteEndConstructor();
        //                break;

        //            case JsonToken.Date:
        //                this.WriteValue((DateTime)reader.Value);
        //                break;

        //            case JsonToken.Bytes:
        //                this.WriteValue((byte[])reader.Value);
        //                break;

        //            default:
        //                throw MiscellaneousUtils.CreateArgumentOutOfRangeException("TokenType", reader.TokenType, "Unexpected token type.");
        //        }
        //    }
        //    while (((initialDepth - 1) < (reader.Depth - (this.IsEndToken(reader.TokenType) ? 1 : 0))) && reader.Read());
        //}

        public virtual void WriteUndefined()
        {
            this.AutoComplete(JsonToken.Undefined);
        }

        public virtual void WriteValue(bool value)
        {
            this.AutoComplete(JsonToken.Boolean);
        }

        public virtual void WriteValue(byte value)
        {
            this.AutoComplete(JsonToken.Integer);
        }

        public virtual void WriteValue(char value)
        {
            this.AutoComplete(JsonToken.String);
        }

        public virtual void WriteValue(DateTime value)
        {
            this.AutoComplete(JsonToken.Date);
        }

        public virtual void WriteValue(DateTimeOffset value)
        {
            this.AutoComplete(JsonToken.Date);
        }

        public virtual void WriteValue(decimal value)
        {
            this.AutoComplete(JsonToken.Float);
        }

        public virtual void WriteValue(double value)
        {
            this.AutoComplete(JsonToken.Float);
        }

        public virtual void WriteValue(short value)
        {
            this.AutoComplete(JsonToken.Integer);
        }

        public virtual void WriteValue(int value)
        {
            this.AutoComplete(JsonToken.Integer);
        }

        public virtual void WriteValue(bool? value)
        {
            if (!value.HasValue)
            {
                this.WriteNull();
            }
            else
            {
                this.WriteValue(value.Value);
            }
        }

        public virtual void WriteValue(long value)
        {
            this.AutoComplete(JsonToken.Integer);
        }

        public virtual void WriteValue(byte? value)
        {
            byte? nullable = value;
            int? nullable3 = nullable.HasValue ? new int?(nullable.GetValueOrDefault()) : null;
            if (!nullable3.HasValue)
            {
                this.WriteNull();
            }
            else
            {
                this.WriteValue(value.Value);
            }
        }

        public virtual void WriteValue(char? value)
        {
            char? nullable = value;
            int? nullable3 = nullable.HasValue ? new int?(nullable.GetValueOrDefault()) : null;
            if (!nullable3.HasValue)
            {
                this.WriteNull();
            }
            else
            {
                this.WriteValue(value.Value);
            }
        }

        public virtual void WriteValue(DateTime? value)
        {
            if (!value.HasValue)
            {
                this.WriteNull();
            }
            else
            {
                this.WriteValue(value.Value);
            }
        }

        public virtual void WriteValue(DateTimeOffset? value)
        {
            if (!value.HasValue)
            {
                this.WriteNull();
            }
            else
            {
                this.WriteValue(value.Value);
            }
        }

        public virtual void WriteValue(decimal? value)
        {
            if (!value.HasValue)
            {
                this.WriteNull();
            }
            else
            {
                this.WriteValue(value.Value);
            }
        }

        public virtual void WriteValue(double? value)
        {
            if (!value.HasValue)
            {
                this.WriteNull();
            }
            else
            {
                this.WriteValue(value.Value);
            }
        }

        public virtual void WriteValue(short? value)
        {
            short? nullable = value;
            int? nullable3 = nullable.HasValue ? new int?(nullable.GetValueOrDefault()) : null;
            if (!nullable3.HasValue)
            {
                this.WriteNull();
            }
            else
            {
                this.WriteValue(value.Value);
            }
        }

        public virtual void WriteValue(int? value)
        {
            if (!value.HasValue)
            {
                this.WriteNull();
            }
            else
            {
                this.WriteValue(value.Value);
            }
        }

        public virtual void WriteValue(long? value)
        {
            if (!value.HasValue)
            {
                this.WriteNull();
            }
            else
            {
                this.WriteValue(value.Value);
            }
        }

        public virtual void WriteValue(sbyte? value)
        {
            sbyte? nullable = value;
            int? nullable3 = nullable.HasValue ? new int?(nullable.GetValueOrDefault()) : null;
            if (!nullable3.HasValue)
            {
                this.WriteNull();
            }
            else
            {
                this.WriteValue(value.Value);
            }
        }

        public virtual void WriteValue(float? value)
        {
            if (!value.HasValue)
            {
                this.WriteNull();
            }
            else
            {
                this.WriteValue(value.Value);
            }
        }

        public virtual void WriteValue(ushort? value)
        {
            ushort? nullable = value;
            int? nullable3 = nullable.HasValue ? new int?(nullable.GetValueOrDefault()) : null;
            if (!nullable3.HasValue)
            {
                this.WriteNull();
            }
            else
            {
                this.WriteValue(value.Value);
            }
        }

        public virtual void WriteValue(uint? value)
        {
            if (!value.HasValue)
            {
                this.WriteNull();
            }
            else
            {
                this.WriteValue(value.Value);
            }
        }

        public virtual void WriteValue(ulong? value)
        {
            if (!value.HasValue)
            {
                this.WriteNull();
            }
            else
            {
                this.WriteValue(value.Value);
            }
        }

        //public virtual void WriteValue(object value)
        //{
        //    if (value == null)
        //    {
        //        this.WriteNull();
        //        return;
        //    }
        //    if (value is IConvertible)
        //    {
        //        IConvertible convertible = value as IConvertible;
        //        switch (convertible.GetTypeCode())
        //        {
        //            case TypeCode.DBNull:
        //                this.WriteNull();
        //                return;

        //            case TypeCode.Boolean:
        //                this.WriteValue(convertible.ToBoolean(CultureInfo.InvariantCulture));
        //                return;

        //            case TypeCode.Char:
        //                this.WriteValue(convertible.ToChar(CultureInfo.InvariantCulture));
        //                return;

        //            case TypeCode.SByte:
        //                this.WriteValue(convertible.ToSByte(CultureInfo.InvariantCulture));
        //                return;

        //            case TypeCode.Byte:
        //                this.WriteValue(convertible.ToByte(CultureInfo.InvariantCulture));
        //                return;

        //            case TypeCode.Int16:
        //                this.WriteValue(convertible.ToInt16(CultureInfo.InvariantCulture));
        //                return;

        //            case TypeCode.UInt16:
        //                this.WriteValue(convertible.ToUInt16(CultureInfo.InvariantCulture));
        //                return;

        //            case TypeCode.Int32:
        //                this.WriteValue(convertible.ToInt32(CultureInfo.InvariantCulture));
        //                return;

        //            case TypeCode.UInt32:
        //                this.WriteValue(convertible.ToUInt32(CultureInfo.InvariantCulture));
        //                return;

        //            case TypeCode.Int64:
        //                this.WriteValue(convertible.ToInt64(CultureInfo.InvariantCulture));
        //                return;

        //            case TypeCode.UInt64:
        //                this.WriteValue(convertible.ToUInt64(CultureInfo.InvariantCulture));
        //                return;

        //            case TypeCode.Single:
        //                this.WriteValue(convertible.ToSingle(CultureInfo.InvariantCulture));
        //                return;

        //            case TypeCode.Double:
        //                this.WriteValue(convertible.ToDouble(CultureInfo.InvariantCulture));
        //                return;

        //            case TypeCode.Decimal:
        //                this.WriteValue(convertible.ToDecimal(CultureInfo.InvariantCulture));
        //                return;

        //            case TypeCode.DateTime:
        //                this.WriteValue(convertible.ToDateTime(CultureInfo.InvariantCulture));
        //                return;

        //            case (TypeCode.DateTime | TypeCode.Object):
        //                goto Label_01B3;

        //            case TypeCode.String:
        //                this.WriteValue(convertible.ToString(CultureInfo.InvariantCulture));
        //                return;
        //        }
        //    }
        //    else
        //    {
        //        if (value is DateTimeOffset)
        //        {
        //            this.WriteValue((DateTimeOffset)value);
        //            return;
        //        }
        //        if (value is byte[])
        //        {
        //            this.WriteValue((byte[])value);
        //            return;
        //        }
        //    }
        //Label_01B3: ;
        //    throw new ArgumentException("Unsupported type: {0}. Use the JsonSerializer class to get the object's JSON representation.".FormatWith(CultureInfo.InvariantCulture, new object[] { value.GetType() }));
        //}

        public virtual void WriteValue(sbyte value)
        {
            this.AutoComplete(JsonToken.Integer);
        }

        public virtual void WriteValue(float value)
        {
            this.AutoComplete(JsonToken.Float);
        }

        public virtual void WriteValue(byte[] value)
        {
            if (value == null)
            {
                this.WriteNull();
            }
            else
            {
                this.AutoComplete(JsonToken.Bytes);
            }
        }

        public virtual void WriteValue(string value)
        {
            this.AutoComplete(JsonToken.String);
        }

        public virtual void WriteValue(ushort value)
        {
            this.AutoComplete(JsonToken.Integer);
        }

        public virtual void WriteValue(uint value)
        {
            this.AutoComplete(JsonToken.Integer);
        }

        public virtual void WriteValue(ulong value)
        {
            this.AutoComplete(JsonToken.Integer);
        }

        protected virtual void WriteValueDelimiter()
        {
        }

        public virtual void WriteWhitespace(string ws)
        {
            //TODO SHOULD THIS DO SOMETHING
            //if ((ws != null) && !StringUtils.IsWhiteSpace(ws))
            //{
            //    throw new Exception("Only white space characters should be used.");
            //}
        }

        // Properties
        public Formatting Formatting
        {
            get
            {
                return this._formatting;
            }
            set
            {
                this._formatting = value;
            }
        }

        protected internal int Top
        {
            get
            {
                return this._top;
            }
        }

        public WriteState WriteState
        {
            get
            {
                switch (this._currentState)
                {
                    case State.Start:
                        return WriteState.Start;

                    case State.Property:
                        return WriteState.Property;

                    case State.ObjectStart:
                    case State.Object:
                        return WriteState.Object;

                    case State.ArrayStart:
                    case State.Array:
                        return WriteState.Array;

                    case State.ConstructorStart:
                    case State.Constructor:
                        return WriteState.Constructor;

                    case State.Closed:
                        return WriteState.Closed;

                    case State.Error:
                        return WriteState.Error;
                }
                throw new Exception("Invalid state: " + this._currentState);
            }
        }

        // Nested Types
        private enum State
        {
            Start,
            Property,
            ObjectStart,
            Object,
            ArrayStart,
            Array,
            ConstructorStart,
            Constructor,
            Bytes,
            Closed,
            Error
        }
    }
}
