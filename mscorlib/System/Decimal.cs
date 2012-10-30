//
// A re-implementation of Decimal for the JavaScript runtime.
// Underlying representation is a JavaScript number.
//

using Microsoft.LiveLabs.JavaScript.IL2JS;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace System
{
    [Runtime(true)]
    public struct Decimal : IComparable, IComparable<Decimal>, IEquatable<Decimal>
    {
        public static Decimal Zero;
        public static Decimal One;
        public static Decimal MinusOne;
        public static Decimal MaxValue;
        public static Decimal MinValue;

        static Decimal()
        {
            Zero = new Decimal(0.0);
            One = new Decimal(1.0);
            MinusOne = new Decimal(-1.0);
            MaxValue = new Decimal(Double.MaxValue);
            MinValue = new Decimal(Double.MinValue);
        }

        [Import("function(value) { return value; }")]
        extern public Decimal(double value);

        [Import("function(value) { return value; }")]
        extern public Decimal(int value);

        [Import("function(value) { return value; }")]
        extern public Decimal(long value);

        [Import("function(value) { return value; }")]
        extern public Decimal(float value);

        [Import("function(value) { return value; }")]
        extern public Decimal(ulong value);

        [Import("function(value) { return value; }")]
        extern public Decimal(uint value);

        [Import("function(d1, d2) { return d1 + d2; }")]
        extern public static Decimal Add(Decimal d1, Decimal d2);

        [Import("function(d) { return Math.ceil(d); }")]
        extern public static Decimal Ceiling(Decimal d);

        [Import("function(d1, d2) { if (d1 < d2) return -1; if (d1 > d2) return 1; return 0; }")]
        extern public static int Compare(Decimal d1, Decimal d2);

        [Import(@"function(inst, value) {
                      var d = inst.R();
                      if (d < value) return -1;
                      if (d > value) return 1;
                      return 0; }", PassInstanceAsArgument = true)]
        extern public int CompareTo(Decimal value);

        public int CompareTo(object value)
        {
            if (value == null)
                return 1;
            var d = (Decimal?)value;
            if (!d.HasValue)
                throw new ArgumentException();
            return CompareTo(d.Value);
        }

        [Import("function(d1, d2) { return d1 / d2; }")]
        extern public static Decimal Divide(Decimal d1, Decimal d2);

        [Import("function(inst, obj) { return inst.R() == obj; }", PassInstanceAsArgument = true)]
        extern public bool Equals(Decimal obj);

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Decimal))
                return false;
            return Equals((Decimal)obj);
        }

        [Import("function(d1, d2) { return d1 == d2; }")]
        extern public static bool Equals(Decimal d1, Decimal d2);

        [Import("function(d) { return Math.floor(d); }")]
        extern public static Decimal Floor(Decimal d);

        [Import("function(inst) { return inst.R() << 0; }", PassInstanceAsArgument = true)]
        extern public override int GetHashCode();

        public TypeCode GetTypeCode()
        {
            return TypeCode.Decimal;
        }

        internal static Decimal Max(Decimal d1, Decimal d2)
        {
            return Compare(d1, d2) < 0 ? d2 : d1;
        }

        internal static Decimal Min(Decimal d1, Decimal d2)
        {
            return Compare(d1, d2) < 0 ? d1 : d2;
        }

        [Import("function(d1, d2) { return d1 * d2; }")]
        extern public static Decimal Multiply(Decimal d1, Decimal d2);

        [Import("function(d) { return -d; }")]
        extern public static Decimal Negate(Decimal d);

        public static implicit operator Decimal(byte value)
        {
            return new Decimal(value);
        }

        public static implicit operator Decimal(sbyte value)
        {
            return new Decimal(value);
        }

        public static implicit operator Decimal(short value)
        {
            return new Decimal(value);
        }

        public static implicit operator Decimal(ushort value)
        {
            return new Decimal(value);
        }

        public static implicit operator Decimal(char value)
        {
            return new Decimal(value);
        }

        public static implicit operator Decimal(int value)
        {
            return new Decimal(value);
        }

        public static implicit operator Decimal(uint value)
        {
            return new Decimal(value);
        }

        public static implicit operator Decimal(long value)
        {
            return new Decimal(value);
        }

        public static implicit operator Decimal(ulong value)
        {
            return new Decimal(value);
        }

        public static explicit operator Decimal(float value)
        {
            return new Decimal(value);
        }

        public static explicit operator Decimal(double value)
        {
            return new Decimal(value);
        }

        public static explicit operator byte(Decimal value)
        {
            return ToByte(value);
        }

        public static explicit operator sbyte(Decimal value)
        {
            return ToSByte(value);
        }

        public static explicit operator char(Decimal value)
        {
            return (char)ToUInt16(value);
        }

        public static explicit operator short(Decimal value)
        {
            return ToInt16(value);
        }

        public static explicit operator ushort(Decimal value)
        {
            return ToUInt16(value);
        }

        public static explicit operator int(Decimal value)
        {
            return ToInt32(value);
        }

        public static explicit operator float(Decimal value)
        {
            return ToSingle(value);
        }

        public static explicit operator double(Decimal value)
        {
            return ToDouble(value);
        }

        public static Decimal operator +(Decimal d)
        {
            return d;
        }

        public static Decimal operator -(Decimal d)
        {
            return Negate(d);
        }

        public static Decimal operator ++(Decimal d)
        {
            return Add(d, One);
        }

        public static Decimal operator --(Decimal d)
        {
            return Subtract(d, One);
        }

        public static Decimal operator +(Decimal d1, Decimal d2)
        {
            return Add(d1, d2);
        }

        public static Decimal operator -(Decimal d1, Decimal d2)
        {
            return Subtract(d1, d2);
        }

        public static Decimal operator *(Decimal d1, Decimal d2)
        {
            return Multiply(d1, d2);
        }

        public static Decimal operator /(Decimal d1, Decimal d2)
        {
            return Divide(d1, d2);
        }

        public static Decimal operator %(Decimal d1, Decimal d2)
        {
            return Remainder(d1, d2);
        }

        public static bool operator ==(Decimal d1, Decimal d2)
        {
            return (Compare(d1, d2) == 0);
        }

        public static bool operator !=(Decimal d1, Decimal d2)
        {
            return (Compare(d1, d2) != 0);
        }

        public static bool operator <(Decimal d1, Decimal d2)
        {
            return (Compare(d1, d2) < 0);
        }

        public static bool operator <=(Decimal d1, Decimal d2)
        {
            return (Compare(d1, d2) <= 0);
        }

        public static bool operator >(Decimal d1, Decimal d2)
        {
            return (Compare(d1, d2) > 0);
        }

        public static bool operator >=(Decimal d1, Decimal d2)
        {
            return (Compare(d1, d2) >= 0);
        }

        public static Decimal Parse(string s)
        {
            if (s == null)
                throw new System.NullReferenceException();
            var r = PrimParse(s);
            if (!r.HasValue)
                throw new System.FormatException();
            return r.Value;
        }

        private static Decimal? PrimParse(string s)
        {
            var f = default(double);
            if (Double.TryParse(s, out f))
                return new Decimal(f);
            else
                return null;
        }

        [Import("function(d1, d2) { return d1 % d2; }")]
        extern public static Decimal Remainder(Decimal d1, Decimal d2);

        [Import("function(d) { return Math.round(d); }")]
        extern public static Decimal Round(Decimal d);

        [Import("function(d, decimals) { return Math.round(d, decimals); }")]
        extern public static Decimal Round(Decimal d, int decimals);

        [Import("function(d1, d2) { return d1 - d2; }")]
        extern public static Decimal Subtract(Decimal d1, Decimal d2);

        [Import("function(value) { return Math.round(value); }")]
        extern public static byte ToByte(Decimal value);

        [Import("function(d) { return d; }")]
        extern public static double ToDouble(Decimal d);

        [Import("function(value) { return Math.round(value); }")]
        extern public static short ToInt16(Decimal value);

        [Import("function(d) { return Math.round(d); }")]
        extern public static int ToInt32(Decimal d);

        [Import("function(value) { return Math.round(value); }")]
        extern public static ushort ToUInt16(Decimal value);

        [Import("function(d) { return Math.round(d); }")]
        extern public static uint ToUInt32(Decimal d);

        [Import("function(d) { return Math.round(d); }")]
        extern public static long ToInt64(Decimal d);

        [Import("function(value) { return Math.round(value); }")]
        extern public static sbyte ToSByte(Decimal value);

        [Import("function(d) { return d; }")]
        extern public static float ToSingle(Decimal d);

        [Import("function(inst) { return inst.R().toString(); }", PassInstanceAsArgument = true)]
        extern public override string ToString();

        [Import("function(d) { if (d < 0) return Math.floor(d+1); return Math.floor(v); }")]
        extern public static Decimal Truncate(decimal d);

        public static bool TryParse(string s, out Decimal result)
        {
            var r = PrimParse(s);
            if (r.HasValue)
            {
                result = r.Value;
                return true;
            }
            else
            {
                result = Zero;
                return false;
            }
        }
    }
}
