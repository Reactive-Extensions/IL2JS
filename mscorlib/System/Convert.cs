////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////

namespace System
{
	public class Convert
	{
        public static int ToInt32(bool value)
        {
            if (!value)
            {
                return 0;
            }
            return 1;
        }
        public static int ToInt32(byte value) { return value; }
        public static int ToInt32(char value) { return value; }
        public static int ToInt32(DateTime value) { throw new NotSupportedException(); }
        public static int ToInt32(decimal value) { throw new NotSupportedException(); }
        public static int ToInt32(double value)
        {
            if (value >= 0.0)
            {
                if (value < 2147483647.5)
                {
                    int num = (int)value;
                    double num2 = value - num;
                    if ((num2 > 0.5) || ((num2 == 0.5) && ((num & 1) != 0)))
                    {
                        num++;
                    }
                    return num;
                }
            }
            else if (value >= -2147483648.5)
            {
                int num3 = (int)value;
                double num4 = value - num3;
                if ((num4 < -0.5) || ((num4 == -0.5) && ((num3 & 1) != 0)))
                {
                    num3--;
                }
                return num3;
            }
            throw new OverflowException();
        }
        public static int ToInt32(short value) { return value; }
        public static int ToInt32(int value) { return value; }
        public static int ToInt32(long value)
        {
            if ((value < -2147483648L) || (value > 0x7fffffffL))
            {
                throw new OverflowException();
            }
            return (int)value;
        }
        public static int ToInt32(object value) { throw new NotSupportedException(); }
        public static int ToInt32(sbyte value) { return value; }
        public static int ToInt32(float value) { return ToInt32((double)value); }
        public static int ToInt32(string value)
        {
            if (value == null)
            {
                return 0;
            }
            return int.Parse(value);
        }
        public static int ToInt32(ushort value) { return value; }
        public static int ToInt32(uint value)
        {
            if (value > 0x7fffffff)
            {
                throw new OverflowException();
            }
            return (int)value;
        }
	}
}
