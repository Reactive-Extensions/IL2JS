using System;

namespace Microsoft.LiveLabs.JavaScript.Tests
{
    static class TestValueTypes
    {
        static Struct f;

        public static void Main()
        {
            {
                var i = 3;
                var o = (object)i;
                var o2 = (object)7;

                TestLogger.Log("Testing instance methods on unboxed int...");
                TestLogger.Log(i + 9);
                TestLogger.Log(Math.Max(i, 9));
                TestLogger.Log((int)i.GetTypeCode());

                TestLogger.Log("Testing virtual method on unboxed int...");
                TestLogger.Log(i.ToString());

                TestLogger.Log("Testing virtual method on boxed int...");
                TestLogger.Log(o.ToString());

                TestLogger.Log("Testing interface call on unboxed int...");
                TestLogger.Log(i.CompareTo(o));
                TestLogger.Log(i.CompareTo(o2));
            }


            {
                TestLogger.Log("Testing boxing of value field...");
                var s = new Struct(5);
                var o = (object)s.v;
                TestLogger.Log(o.ToString());

                TestLogger.Log("Testing boxing of value element...");
                var a = new int[3] { 1, 2, 3 };
                var o2 = (object)a[1];
                TestLogger.Log(o2.ToString());
            }

            {
                var s = new Struct(3);
                var o = (object)s;
                var o2 = (object)new Struct(7);

                TestLogger.Log("Testing instance methods on unboxed struct...");
                TestLogger.Log(s.Instance());

                TestLogger.Log("Testing virtual method on unboxed struct...");
                TestLogger.Log(s.ToString());

                TestLogger.Log("Testing virtual method on boxed struct...");
                TestLogger.Log(o.ToString());

                TestLogger.Log("Testing interface call on unboxed struct...");
                TestLogger.Log(s.CompareTo(o));
                TestLogger.Log(s.CompareTo(o2));
            }

            {
                var c = new Class(3);
                var o = (object)c;
                var o2 = (object)new Class(7);

                TestLogger.Log("Testing instance methods on class...");
                TestLogger.Log(c.Instance());

                TestLogger.Log("Testing virtual method on class...");
                TestLogger.Log(c.ToString());

                TestLogger.Log("Testing virtual method on cast class...");
                TestLogger.Log(o.ToString());

                TestLogger.Log("Testing interface call on class...");
                TestLogger.Log(c.CompareTo(o));
                TestLogger.Log(c.CompareTo(o2));
            }

            {
                var i = 3;

                TestLogger.Log("Testing passing int by value...");
                TestLogger.Log(i);
                ModifyIntCopy(i);
                TestLogger.Log(i);

                TestLogger.Log("Testing passing int by ref...");
                ModifyIntInPlace(ref i);
                TestLogger.Log(i);
            }

            {
                var s = new Struct(3);

                TestLogger.Log("Testing modifying struct in place...");
                TestLogger.Log(s.ToString());
                s.Modify();
                TestLogger.Log(s.ToString());

                TestLogger.Log("Testing passing struct by value...");
                ModifyStructCopy(s);
                TestLogger.Log(s.ToString());

                TestLogger.Log("Testing passing struct by boxed value...");
                ModifyStructBoxed(s);
                TestLogger.Log(s.ToString());

                TestLogger.Log("Testing passing struct by ref value...");
                ModifyStructInPlace(ref s);
                TestLogger.Log(s.ToString());

                TestLogger.Log("Testing replacing struct in place...");
                ReplaceStructInPlace(ref s);
                TestLogger.Log(s.ToString());
            }

            {
                var c = new Class(3);

                TestLogger.Log("Testing modifying class in place...");
                TestLogger.Log(c.ToString());
                c.Modify();
                TestLogger.Log(c.ToString());

                TestLogger.Log("Testing passing class...");
                ModifyClass(c);
                TestLogger.Log(c.ToString());

                TestLogger.Log("Testing passing class as object...");
                ModifyClassAsObject(c);
                TestLogger.Log(c.ToString());

                TestLogger.Log("Testing passing class by ref value...");
                ModifyClassInPlace(ref c);
                TestLogger.Log(c.ToString());

                TestLogger.Log("Testing replacing class in place...");
                ReplaceClassInPlace(ref c);
                TestLogger.Log(c.ToString());
            }


            {
                var s = new Struct(3);

                TestLogger.Log("Testing static field...");
                f = s;
                f.Modify();

                TestLogger.Log(s.ToString());
                TestLogger.Log(f.ToString());
            }

            {
                var s = new Struct(3);
                TestLogger.Log("Testing returning self...");
                var self = s.ReturnSelf();
                self.Modify();
                TestLogger.Log(self.ToString());
                TestLogger.Log(s.ToString());
            }

            {
                TestLogger.Log("Testing wrapping...");
                var w = new WrappedStruct(new Struct(5));
                TestLogger.Log(w.ToString());
            }

            {
                TestLogger.Log("Testing generic struct of ref type...");
                var g = new GenericStruct<string>();
                g.v = "a";
                TestLogger.Log(g.v);
                Call(g, "b");
                TestLogger.Log(g.v);
            }

            {
                TestLogger.Log("Testing generic struct of struct...");
                var g = new GenericStruct<Struct>();
                g.v = new Struct(1);
                TestLogger.Log(g.v.ToString());
                g.v.Modify();
                TestLogger.Log(g.v.ToString());
                Call(g, new Struct(9));
                TestLogger.Log(g.v.ToString());
            }

            {
                TestLogger.Log("Testing generic method on struct...");
                var arr = new Struct[2] { new Struct(1), new Struct(3) };
                GenericMethod.TestLoop(arr);
            }

            {
                TestLogger.Log("Testing generic method on ref type...");
                var arr = new Class[2] { new Class(1), new Class(3) };
                GenericMethod.TestLoop(arr);
            }

            {
                TestLogger.Log("Testing generic boxing...");

                var i = 3;
                GenericBoxer(i);

                var str = "test";
                GenericBoxer(str);

                var s = new GenericStruct<int>() { v = 7 };
                GenericBoxer(s);

                var c = new GenericClass<int>() { v = 9 };
                GenericBoxer(c);
            }

            {
                TestLogger.Log("Testing generic return...");

                var i = 3;
                var i2 = GenericCopier(i, a => a + 1);
                TestLogger.Log(i2.ToString());

                var str = "test";
                var str2 = GenericCopier(str, a => a + "done");
                TestLogger.Log(str2.ToString());

                var s = new GenericStruct<int>() { v = 7 };
                var s2 = GenericCopier(s, a => new GenericStruct<int>() { v = a.v + 1 });
                TestLogger.Log(s2.ToString());

                var c = new GenericClass<int>() { v = 9 };
                var c2 = GenericCopier(c, a => new GenericClass<int>() { v = a.v + 1 });
                TestLogger.Log(c2.ToString());
            }

            {
                TestLogger.Log("Testing generic modification...");

                var i = 3;
                GenericModifier(i, a => a++);
                TestLogger.Log(i.ToString());

                var str = "test";
                GenericModifier(str, a => a = "done");
                TestLogger.Log(str.ToString());

                var s = new GenericStruct<int>() { v = 7 };
                GenericModifier(s, a => a.v++);
                TestLogger.Log(s.ToString());
                GenericCallToGenericModifier(s, a => a.v++);
                TestLogger.Log(s.ToString());

                var c = new GenericClass<int>() { v = 9 };
                GenericModifier(c, a => a.v++);
                TestLogger.Log(c.ToString());
                GenericCallToGenericModifier(c, a => a.v++);
                TestLogger.Log(c.ToString());
            }

            {
                TestLogger.Log("Testing stack non-interferance...");
                var a = 3;
                var b = 7;
                var c = 10;
                var x = true;
                var y = true;
                TestLogger.Log(String.Format("a = {0}, b = {1}, c = {2}", a, b, c));
                TestLogger.Log(AddArgs(a, x ? a++ : b++, y ? a++ : c++).ToString());
                TestLogger.Log(String.Format("a = {0}, b = {1}, c = {2}", a, b, c));
            }

            {
                TestLogger.Log("Testing integer equalities...");
                var one = 1;
                var a = 1;
                var two = 2;
                TestLogger.Log(one.Equals(one));
                TestLogger.Log(one.Equals(a));
                TestLogger.Log(one.Equals(two));

                TestLogger.Log(a.Equals(one));
                TestLogger.Log(two.Equals(one));

                var boxedOne = (object)one;
                var boxedA = (object)a;
                var boxedTwo = (object)two;

                TestLogger.Log("Testing boxed integer equalities...");
                TestLogger.Log(boxedOne.Equals(boxedOne));
                TestLogger.Log(boxedOne.Equals(boxedA));
                TestLogger.Log(boxedOne.Equals(boxedTwo));

                TestLogger.Log(boxedA.Equals(boxedOne));
                TestLogger.Log(boxedTwo.Equals(boxedOne));

                TestLogger.Log("Testing reference equalities...");

                var five = new Class(5);
                Class six = new Class(6);
                Class fiveAsWell = new Class(5);
                Class sixAsWell = six;

                TestLogger.Log(five.Equals(five));
                TestLogger.Log(five.Equals(six));
                TestLogger.Log(five.Equals(fiveAsWell));
                TestLogger.Log(six.Equals(sixAsWell));

                TestLogger.Log(six.Equals(five));
                TestLogger.Log(fiveAsWell.Equals(five));
                TestLogger.Log(sixAsWell.Equals(six));
            }
        }

        public static void ModifyIntCopy(int i)
        {
            i++;
            TestLogger.Log(i);
        }

        public static void ModifyIntInPlace(ref int i)
        {
            i++;
            TestLogger.Log(i);
        }

        public static void ModifyStructCopy(Struct s)
        {
            s.Modify();
            TestLogger.Log(s.ToString());
        }

        public static void ModifyStructBoxed(object o)
        {
            var s = (Struct)o;
            s.Modify();
            TestLogger.Log(s.ToString());
        }

        public static void ModifyStructInPlace(ref Struct s)
        {
            s.Modify();
            TestLogger.Log(s.ToString());
        }

        public static void ReplaceStructInPlace(ref Struct s)
        {
            s = new Struct(42);
            TestLogger.Log(s.ToString());
        }

        public static void ModifyClass(Class c)
        {
            c.Modify();
            TestLogger.Log(c.ToString());
        }

        public static void ModifyClassAsObject(object o)
        {
            var c = (Class)o;
            c.Modify();
            TestLogger.Log(c.ToString());
        }

        public static void ModifyClassInPlace(ref Class c)
        {
            c.Modify();
            TestLogger.Log(c.ToString());
        }

        public static void ReplaceClassInPlace(ref Class c)
        {
            c = new Class(42);
            TestLogger.Log(c.ToString());
        }

        private static void Call<T>(GenericStruct<T> g, T t)
        {
            g.v = t;
            var u = g.v;
            TestLogger.Log(u.ToString());
        }

        private static void GenericBoxer<T>(T t)
        {
            TestLogger.Log(t.ToString());
            var o = (object)t;
            TestLogger.Log(o.ToString());
            var u = (T)o;
            TestLogger.Log(u.ToString());
        }

        private static void GenericModifier<T>(T t, Modifier<T> f)
        {
            TestLogger.Log(t.ToString());
            f(t);
            TestLogger.Log(t.ToString());
        }

        private static T GenericCopier<T>(T t, Copier<T> f)
        {
            TestLogger.Log(t.ToString());
            var u = f(t);
            TestLogger.Log(u.ToString());
            return u;
        }

        private static void GenericCallToGenericModifier<T>(T t, Modifier<T> f)
        {
            TestLogger.Log(t.ToString());
            GenericModifier<T>(t, f);
            TestLogger.Log(t.ToString());
        }

        private static int AddArgs(int a, int b, int c)
        {
            return a + b + c;
        }
    }

    public struct Struct : IComparable, IComparable<Struct>
    {
        public int v;
        public int old;

        public Struct(int v)
        {
            this.v = v;
            this.old = v;
        }

        public void Modify()
        {
            v++;
        }

        internal Struct ReturnSelf()
        {
            return this;
        }

        public string Instance()
        {
            TestLogger.Log("Instance invoked");
            return v.ToString();
        }

        public override string ToString()
        {
            return "Struct { v = " + v.ToString() + ", old = " + old.ToString() + " }";
        }

        public int CompareTo(Struct other)
        {
            return v.CompareTo(other.v);
        }

        public int CompareTo(object other)
        {
            var s = (Struct)other;
            return v.CompareTo(s.v);
        }
    }

    public class Class : IComparable, IComparable<Struct>
    {
        public int v;
        public int old;

        public Class(int v)
        {
            this.v = v;
            this.old = v;
        }

        public void Modify()
        {
            v++;
        }

        internal Class ReturnSelf()
        {
            return this;
        }

        public string Instance()
        {
            TestLogger.Log("Instance invoked");
            return v.ToString();
        }

        public override string ToString()
        {
            return "Class { v = " + v.ToString() + ", old = " + old.ToString() + " }";
        }

        public int CompareTo(Struct other)
        {
            return v.CompareTo(other.v);
        }

        public int CompareTo(object other)
        {
            var s = (Class)other;
            return v.CompareTo(s.v);
        }
    }

    public struct WrappedStruct
    {
        private Struct s;

        public WrappedStruct(Struct s)
        {
            this.s = s;
        }

        public override string ToString()
        {
            return "WrappedStruct(" + s.ToString() + ")";
        }
    }

    public delegate void Modifier<T>(T t);
    public delegate T Copier<T>(T t);

    public class GenericClass<T>
    {
        public T v;

        public override string ToString()
        {
            return "GenericClass(" + v.ToString() + ")";
        }
    }

    public struct GenericStruct<T>
    {
        public T v;

        public override string ToString()
        {
            return "GenericStruct(" + v.ToString() + ")";
        }
    }

    public class GenericMethod
    {
        public static void TestLoop<T>(T[] list)
        {
            for (var i = 0; i < list.Length; i++)
                TestLogger.Log(list[i].ToString());
        }
    }
}