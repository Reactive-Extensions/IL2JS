namespace System.Threading
{
    public static class Interlocked
    {
        public static int Add(ref int location1, int value)
        {
            location1 += value;
            return location1;
        }

        public static long Add(ref long location1, long value)
        {
            location1 += value;
            return location1;
        }

        public static int CompareExchange(ref int location1, int value, int comparand)
        {
            var res = location1;
            if (res == comparand)
                location1 = value;
            return res;
        }

        public static T CompareExchange<T>(ref T location1, T value, T comparand) where T : class
        {
            var res = location1;
            if (res == comparand)
                location1 = value;
            return res;
        }

        public static long CompareExchange(ref long location1, long value, long comparand)
        {
            var res = location1;
            if (res == comparand)
                location1 = value;
            return res;
        }

        public static int Decrement(ref int location)
        {
            return --location;
        }

        public static long Decrement(ref long location)
        {
            return --location;
        }

        public static T Exchange<T>(ref T location1, T value) where T : class
        {
            var res = location1;
            location1 = value;
            return res;
        }

        public static int Exchange(ref int location1, int value)
        {
            var res = location1;
            location1 = value;
            return res;
        }

        public static long Exchange(ref long location1, long value)
        {
            var res = location1;
            location1 = value;
            return res;
        }

        public static int Increment(ref int location)
        {
            return ++location;
        }

        public static long Increment(ref long location)
        {
            return ++location;
        }
    }
}