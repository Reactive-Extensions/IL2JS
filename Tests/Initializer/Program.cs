using System.Collections.Generic;

namespace Microsoft.LiveLabs.JavaScript.Tests
{
    static class TestInitializer
    {
        public static int si;
        public static string sstr;
        public static Enum se;
        public static Class sc;
        public static Struct ss;

        static void Main()
        {
            {
                TestLogger.Log("Testing static initialization...");
                TestLogger.Log(si.ToString());
                TestLogger.Log(sstr == null ? "null" : sstr);
                TestLogger.Log(((int)se).ToString());
                TestLogger.Log(sc == null ? "null" : sc.ToString());
                TestLogger.Log(ss.ToString());
            }

            {
                TestLogger.Log("Testing struct initialization...");
                var s = new Struct();
                TestLogger.Log(s.ToString());
            }

            {
                TestLogger.Log("Testing struct constructor...");
                var s = new Struct(7.3, 12.11);
                TestLogger.Log(s.ToString());
            }

            {
                TestLogger.Log("Testing class initialization...");
                var c = new Class();
                TestLogger.Log(c.ToString());
            }

            {
                TestLogger.Log("Testing class constructor...");
                var c = new Class(7, "test");
                TestLogger.Log(c.ToString());
            }

            {
                TestLogger.Log("Testing derived initialization...");
                var c = new DerivedClass();
                TestLogger.Log(c.ToString());
            }

            {
                TestLogger.Log("Testing derived constructor...");
                var c = new DerivedClass(7, "test", 12);
                TestLogger.Log(c.ToString());
            }

            {
                TestLogger.Log("Testing generic initialization over int...");
                var c = new GenericClass<int>();
                TestLogger.Log(c.ToString());
            }

            {
                TestLogger.Log("Testing generic constructor over int...");
                var c = new GenericClass<int>(7);
                TestLogger.Log(c.ToString());
            }

            {
                TestLogger.Log("Testing generic initialization over string...");
                var c = new GenericClass<string>();
                TestLogger.Log(c.ToString());
            }

            {
                TestLogger.Log("Testing generic constructor over string...");
                var c = new GenericClass<string>("test");
                TestLogger.Log(c.ToString());
            }

            {
                TestLogger.Log("Testing construction of generic parameter with int...");
                var c = new GenericConstructorTest<int>();
                TestLogger.Log(c.ToString());
                TestLogger.Log(c.GenericBox().ToString());
            }

            {
                TestLogger.Log("Testing construction of generic parameter with class...");
                var c = new GenericConstructorTest<Class>();
                TestLogger.Log(c.ToString());
                TestLogger.Log(c.GenericBox().ToString());
            }
        }
    }

    public enum Enum
    {
        One,
        Two,
        Three
    }

    public struct Struct
    {
        public double x;
        public double y;

        public Struct(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        public override string ToString()
        {
            return "Struct() { x = " + x.ToString() + ", y = " + y.ToString() + " }";
        }
    }

    public class Class
    {
        public Struct s;
        public int i;
        public string str;
        public object o;
        public char c;
        public Enum e;

        public Class()
        {
        }

        public Class(int i, string str)
        {
            this.i = i;
            this.str = str;
        }

        public override string ToString()
        {
            return "Class() { s = " + s.ToString() + ", i = " + i.ToString() + ", str = " + (str == null ? "null" : str) + ", o = " + (o == null ? "null" : o.ToString()) + ", c = " + ((int)c).ToString() + ", e = " + ((int)e).ToString() + " }";
        }
    }

    public struct GenericStruct<T>
    {
        public T t;

        public GenericStruct(T t)
        {
            this.t = t;
        }

        public override string ToString()
        {
            return "GenericStruct() { t = " + (EqualityComparer<T>.Default.Equals(t, default(T)) ? "default" : t.ToString()) + " }";
        }
    }

    public class GenericClass<T>
    {
        public GenericStruct<T> s;

        public GenericClass()
        {
        }

        public GenericClass(T t)
        {
            s = new GenericStruct<T>(t); 
        }

        public override string ToString()
        {
            return "GenericClass() { s = " + s.ToString() + " }";
        }
    }

    public class DerivedClass : Class
    {
        public long l;

        public DerivedClass()
        {
        }

        public DerivedClass(int i, string str, long l)
            : base(i, str)
        {
            this.l = l;
        }

        public override string ToString()
        {
            return "DerivedClass() { l = " + l.ToString() + ", base = " + base.ToString() + " }";
        }
    }

    public class GenericConstructorTest<T> where T : new()
    {
        public T t;

        public GenericConstructorTest()
        {
            t = new T();
        }

        public object GenericBox()
        {
            return t;
        }

        public override string ToString()
        {
            return "GenericConstructorTest() { t = " + (System.Collections.Generic.EqualityComparer<T>.Default.Equals(t, default(T)) ? "default" : t.ToString()) + " }";
        }
    }

}