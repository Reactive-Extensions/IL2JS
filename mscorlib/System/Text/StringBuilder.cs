//
// A reimplementation of StringBuilder for the JavaScript runtime.
//

using Microsoft.LiveLabs.JavaScript.IL2JS;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace System.Text
{
    [Runtime(true)]
    public sealed class StringBuilder 
    {
        public StringBuilder()
        {
            Setup();
        }

        public StringBuilder(int capacity)
        {
            Setup();
        }

        public StringBuilder(string value)
        {
            Setup();
            Append(value);
        }

        public StringBuilder(string value, int capacity)
        {
            Setup();
            Append(value);
        }

        [Import("function(inst) { inst.Arr = []; }", PassInstanceAsArgument = true)]
        extern private void Setup();

        [Import(@"function(inst, value) { inst.Arr.push(value ? ""True"" : ""False""); return inst; }", PassInstanceAsArgument = true)]
        extern public StringBuilder Append(bool value);

        [Import("function(inst, value) { inst.Arr.push(value.toString()); return inst; }", PassInstanceAsArgument = true)]
        extern public StringBuilder Append(byte value);

        [Import("function(inst, value) { inst.Arr.push(String.fromCharCode(value)); return inst; }", PassInstanceAsArgument = true)]
        extern public StringBuilder Append(char value);

        [Import("function(inst, value) { inst.Arr.push(value.toString()); return inst; }", PassInstanceAsArgument = true)]
        extern public StringBuilder Append(Decimal value);

        [Import("function(inst, value) { inst.Arr.push(value.toString()); return inst; }", PassInstanceAsArgument = true)]
        extern public StringBuilder Append(double value);

        public StringBuilder Append(char[] value)
        {
            if (value != null)
            {
                for (var i = 0; i < value.Length; i++)
                    Append(value[i].ToString());
            }
            return this;
        }

        [Import("function(inst, value) { inst.Arr.push(value.toString()); return inst; }", PassInstanceAsArgument = true)]
        extern public StringBuilder Append(short value);

        [Import("function(inst, value) { inst.Arr.push(value.toString()); return inst; }", PassInstanceAsArgument = true)]
        extern public StringBuilder Append(int value);

        [Import("function(inst, value) { inst.Arr.push(value.toString()); return inst; }", PassInstanceAsArgument = true)]
        extern public StringBuilder Append(long value);

        public StringBuilder Append(object value)
        {
            if (value == null)
                return this;
            else
                return Append(value.ToString());
        }

        [Import("function(inst, value) { inst.Arr.push(value.toString()); return inst; }", PassInstanceAsArgument = true)]
        extern public StringBuilder Append(sbyte value);

        [Import("function(inst, value) { inst.Arr.push(value.toString()); return inst; }", PassInstanceAsArgument = true)]
        extern public StringBuilder Append(float value);

        [Import("function(inst, text) { if (text != null) inst.Arr.push(text); return inst; }", PassInstanceAsArgument = true)]
        public extern StringBuilder Append(string text);

        [Import("function(inst, value) { inst.Arr.push(value.toString()); return inst; }", PassInstanceAsArgument = true)]
        extern public StringBuilder Append(ushort value);

        [Import("function(inst, value) { inst.Arr.push(value.toString()); return inst; }", PassInstanceAsArgument = true)]
        extern public StringBuilder Append(uint value);

        [Import("function(inst, value) { inst.Arr.push(value.toString()); return inst; }", PassInstanceAsArgument = true)]
        extern public StringBuilder Append(ulong value);

        public StringBuilder Append(char value, int repeatCount)
        {
            if (repeatCount < 0)
                throw new ArgumentOutOfRangeException("repeatCount");
            for (var i = 0; i < repeatCount; i++)
                Append(value);
            return this;
        }

        public StringBuilder Append(string value, int startIndex, int count)
        {
            if (value == null)
            {
                if (startIndex == 0 && count == 0)
                    return this;
                else
                    throw new ArgumentNullException("value");
            }
            if (count < 0)
                throw new ArgumentOutOfRangeException("count");
            if (count == 0)
                return this;
            if (startIndex < 0 || startIndex + count > value.Length)
                throw new ArgumentOutOfRangeException("startIndex");
            return Append(value.Substring(startIndex, count));
        }

        public StringBuilder AppendFormat(string format, object arg0)
        {
            return AppendFormat(null, format, new object[] { arg0 });
        }

        public StringBuilder AppendFormat(string format, object[] args)
        {
            return AppendFormat(null, format, args);
        }

        public StringBuilder AppendFormat(string format, object arg0, object arg1)
        {
            return AppendFormat(null, format, new object[] { arg0, arg1 });
        }

        public StringBuilder AppendFormat(string format, object arg0, object arg1, object arg2)
        {
            return AppendFormat(null, format, new object[] { arg0, arg1, arg2 });
        }

        [Import("function(inst) { inst.Arr.push(\"\\n\"); return inst; }", PassInstanceAsArgument = true)]
        extern public StringBuilder AppendLine();

        [Import("function(inst, value) { if (value != null) inst.Arr.push(value); inst.Arr.push(\"\\n\"); return inst; }", PassInstanceAsArgument = true)]
        extern public StringBuilder AppendLine(string value);

        public bool Equals(StringBuilder sb)
        {
            if (sb == null)
                return false;
            return ToString().Equals(sb.ToString(), StringComparison.Ordinal);
        }

        [Import(@"function(inst) { return inst.Arr.join(""""); }", PassInstanceAsArgument = true)]
        public extern override string ToString();

        public string ToString(int startIndex, int length)
        {
            return ToString().Substring(startIndex, length);
        }

        public int Length
        {
            get { return GetLengthInternal(); }
            set { throw new NotSupportedException(); }
        }

        [Import(@"function(inst) { return inst.Arr.join("""").length; }", PassInstanceAsArgument = true)]
        private extern int GetLengthInternal();
    }
}
