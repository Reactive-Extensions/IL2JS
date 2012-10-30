namespace Microsoft.LiveLabs.JavaScript.Tests
{
    static class TestInstructions
    {

        public static void Main()
        {
            {
                TestLogger.Log("Testing cpobj...");
                var s = new Struct();
                TestLogger.Log(s.ToString());
                s.a = 3;
                s.b = true;
                s.c = 'x';
                s.d = 4;
                s.e = 2.5f;
                s.f = 9.4;
                s.g.x = 10;
                s.g.y = 12;
                s.h = "test";
                TestLogger.Log(s.ToString());
                var t = new Struct();
                TestLogger.Log(t.ToString());
                CopyStruct(ref s, ref t);
                TestLogger.Log(t.ToString());
            }

            {
                TestLogger.Log("Testing ldflda...");
                var s = new Struct();
                s.a = 3;
                s.g.x = 10;
                LogInt(ref s.a);
                LogInt(ref s.g.x);
            }

            {
                TestLogger.Log("Testing indexer...");
                Arr arr = new Arr();
                for (var i = 0; i < arr.Count; i++)
                    arr[i] = i * 37 % 11;
                for (var i = 0; i < arr.Count; i++)
                    TestLogger.Log(arr[i]);
            }

            {
                TestLogger.Log("Testing parameters...");
                var p = new Params(1, 2, 3, 4, 5);
                TestLogger.Log(p.ToString());
            }

        }

        public static void CopyStruct(ref Struct si, ref Struct so)
        {
            so = si;  // rewritted by il fixup to use cpobj instead of ldobj;stobj
        }

        public static void LogInt(ref int i)
        {
            TestLogger.Log("i=" + i);
        }

    }

    public struct SubStruct
    {
        public int x;
        public int y;

        public override string ToString()
        {
            return "SubStruct(x=" + x + ", y=" + y + ")";
        }
    }

    public struct Struct
    {
        public int a;
        public bool b;
        public char c;
        public long d;
        public float e;
        public double f;
        public SubStruct g;
        public string h;

        public override string ToString()
        {
            // JScript takes the zero character to represent end-of-string, so avoid printing it
            return "Struct(a=" + a + ", b=" + b + ", c=" + (c == '\x00' ? "ZERO" : c.ToString()) + ", d=" + d + ", e=" + e + ", f=" + f + ", g=" + g + ", h=" + (h ?? "NULL");
        }
    }

    public class Arr
    {
        private int[] arr = new int[10];

        public int this[int i]
        {
            get { return arr[i]; }
            set { arr[i] = value; }
        }

        public int Count { get { return arr.Length; } }
    }

    class Params
    {
        private int a, b, c, d, e;

        public Params(int a, int b, int c, int d, int e)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
            this.e = e;
        }

        public override string ToString()
        {
            return "Params(a=" + a + ", b=" + b + ", c=" + c + ", d=" + d + ", e=" + e + ")";
        }
    }

}