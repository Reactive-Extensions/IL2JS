//
// A reimplementation of the math helpersfor the JavaScript runtime.
//

using Microsoft.LiveLabs.JavaScript.Interop;

namespace System
{
    public static class Math
    {
        public const double E = 2.718281828;
        public const double PI = 3.1415926535898;

        [Import("Math.abs")]
        extern public static decimal Abs(decimal value);

        [Import("Math.abs")]
        extern public static double Abs(double value);

        [Import("Math.abs")]
        extern public static float Abs(float value);

        [Import("Math.abs")]
        extern public static int Abs(int value);

        [Import("Math.abs")]
        extern public static long Abs(long value);

        [Import("Math.abs")]
        extern public static sbyte Abs(sbyte value);

        [Import("Math.abs")]
        extern public static short Abs(short value);

        [Import("Math.acos")]
        extern public static double Acos(double d);

        [Import("Math.asin")]
        extern public static double Asin(double d);

        [Import("Math.atan")]
        extern public static double Atan(double d);

        [Import("Math.atan2")]
        extern public static double Atan2(double y, double x);

        [Import("function(a, b) { return a * b; }")]
        extern public static long BigMul(int a, int b);

        [Import("Math.ceil")]
        extern public static decimal Ceiling(decimal d);

        [Import("Math.ceil")]
        extern public static double Ceiling(double a);

        [Import("Math.cos")]
        extern public static double Cos(double d);

        public static double Cosh(double value)
        {
            return 0.5 * (Exp(value) + Exp(-value));
        }

        public static int DivRem(int a, int b, out int result)
        {
            result = a % b;
            return a / b;
        }

        public static long DivRem(long a, long b, out long result)
        {
            result = a % b;
            return a / b;
        }

        [Import("Math.exp")]
        extern public static double Exp(double d);

        [Import("Math.floor")]
        extern public static decimal Floor(decimal d);

        [Import("Math.floor")]
        extern public static double Floor(double d);

        [Import("Math.log")]
        extern public static double Log(double d);

        public static double Log(double a, double newBase)
        {
            return Log(a) / Log(newBase);
        }

        public static double Log10(double d)
        {
            return Log(d, 10.0);
        }

        [Import("Math.max")]
        extern public static byte Max(byte val1, byte val2);

        [Import("Math.max")]
        extern public static decimal Max(decimal val1, decimal val2);

        [Import("Math.max")]
        extern public static double Max(double val1, double val2);

        [Import("Math.max")]
        extern public static float Max(float val1, float val2);

        [Import("Math.max")]
        extern public static int Max(int val1, int val2);

        [Import("Math.max")]
        extern public static long Max(long val1, long val2);

        [Import("Math.max")]
        extern public static sbyte Max(sbyte val1, sbyte val2);

        [Import("Math.max")]
        extern public static short Max(short val1, short val2);

        [Import("Math.max")]
        extern public static uint Max(uint val1, uint val2);

        [Import("Math.max")]
        extern public static ulong Max(ulong val1, ulong val2);

        [Import("Math.max")]
        extern public static ushort Max(ushort val1, ushort val2);

        [Import("Math.min")]
        extern public static byte Min(byte val1, byte val2);

        [Import("Math.min")]
        extern public static decimal Min(decimal val1, decimal val2);

        [Import("Math.min")]
        extern public static double Min(double val1, double val2);

        [Import("Math.min")]
        extern public static float Min(float val1, float val2);

        [Import("Math.min")]
        extern public static int Min(int val1, int val2);

        [Import("Math.min")]
        extern public static long Min(long val1, long val2);

        [Import("Math.min")]
        extern public static sbyte Min(sbyte val1, sbyte val2);

        [Import("Math.min")]
        extern public static short Min(short val1, short val2);

        [Import("Math.min")]
        extern public static uint Min(uint val1, uint val2);

        [Import("Math.min")]
        extern public static ulong Min(ulong val1, ulong val2);

        [Import("Math.min")]
        extern public static ushort Min(ushort val1, ushort val2);

        [Import("Math.pow")]
        extern public static double Pow(double x, double y);

        [Import("Math.round")]
        extern public static decimal Round(decimal d);

        [Import("Math.round")]
        extern public static double Round(double a);

        [Import("function(d, decimals) { return d.toFixed(decimals); }")]
        public extern static decimal Round(decimal d, int decimals);

        [Import("function(value, digits){ return value.toFixed(digits); }")]
        extern public static double Round(double value, int digits);

        [Import("function(value) { if (value < 0) return -1; if (value > 0) return 1; return 0; }")]
        extern public static int Sign(decimal value);

        [Import("function(value) { if (value < 0) return -1; if (value > 0) return 1; return 0; }")]
        extern public static int Sign(double value);

        [Import("function(value) { if (value < 0) return -1; if (value > 0) return 1; return 0; }")]
        extern public static int Sign(float value);

        [Import("function(value) { if (value < 0) return -1; if (value > 0) return 1; return 0; }")]
        extern public static int Sign(int value);

        [Import("function(value) { if (value < 0) return -1; if (value > 0) return 1; return 0; }")]
        extern public static int Sign(long value);

        [Import("function(value) { if (value < 0) return -1; if (value > 0) return 1; return 0; }")]
        extern public static int Sign(sbyte value);

        [Import("function(value) { if (value < 0) return -1; if (value > 0) return 1; return 0; }")]
        extern public static int Sign(short value);

        [Import("Math.sin")]
        extern public static double Sin(double a);

        public static double Sinh(double value)
        {
            return 0.5 * (Exp(value) - Exp(-value));
        }

        [Import("Math.sqrt")]
        extern public static double Sqrt(double d);

        [Import("Math.tan")]
        extern public static double Tan(double a);

        public static double Tanh(double value)
        {
            return (Exp(value) - Exp(-value)) / (Exp(value) + Exp(-value));
        }

        [Import("function(d) { if (d < 0) return Math.floor(d+1); return Math.floor(d); }")]
        extern public static decimal Truncate(decimal d);

        [Import("function(d) { if (d < 0) return Math.floor(d+1); return Math.floor(d); }")]
        extern public static double Truncate(double d);
    }
}