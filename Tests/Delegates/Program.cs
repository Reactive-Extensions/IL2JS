using System;

namespace Microsoft.LiveLabs.JavaScript.Tests
{
    public delegate void Test();
    public delegate int TestInt();
    public delegate int TestIntInt(int i);
    public delegate string TestString();
    public delegate string TestStringString(string s);
    public delegate void TestMixed(string a, bool b, int c);

    static class TestDelegates
    {
        static Test _n;
        private static TestMixed tester;

        static void Prepare(Test n)
        {
            _n = n;
        }

        static void Call()
        {
            _n();
        }

        public static void Main()
        {
            {
                TestLogger.Log("Testing Void -> Void...");
                var a = new A() { i = 3 };
                var d = new Test(a.One);
                d();
            }

            {
                TestLogger.Log("Testing Void -> Int...");
                var a = new A() { i = 3 };
                var d = new TestInt(a.OneInt);
                TestLogger.Log(d());
            }

            {
                TestLogger.Log("Testing Int -> Int...");
                var a = new A() { i = 3 };
                var d = new TestIntInt(a.OneIntInt);
                TestLogger.Log(d(5));
            }


            {
                TestLogger.Log("Testing Void -> String...");
                var a = new A() { i = 3 };
                var d = new TestString(a.OneString);
                TestLogger.Log(d());
            }

            {
                TestLogger.Log("Testing String -> String...");
                var a = new A() { i = 3 };
                var d = new TestStringString(a.OneStringString);
                TestLogger.Log(d("5"));
            }

            {
                TestLogger.Log("Testing String -> String static...");
                var d = new TestStringString(A.StaticStringString);
                TestLogger.Log(d("2"));
            }

            {
                TestLogger.Log("Testing simple delegate hash...");
                var a1 = new A() { i = 3 };
                var a2 = new A() { i = 5 };
                var d1 = new TestStringString(a1.OneStringString);
                var d2 = new TestStringString(a2.OneStringString);
                var d3 = new TestString(a1.OneString);
                // Remember: Hash code for simple delegates is based on delegate type alone
                TestLogger.Log(d1.GetHashCode() == d1.GetHashCode());
                TestLogger.Log(d1.GetHashCode() == d2.GetHashCode());
                TestLogger.Log(d1.GetHashCode() != d3.GetHashCode());
            }

            {
                TestLogger.Log("Testing combined delegates...");
                var a1 = new A() { i = 3 };
                var a2 = new A() { i = 5 };
                var d1 = new TestStringString(a1.OneStringString);
                var d2 = new TestStringString(a2.OneStringString);
                var d3 = new TestStringString(a1.TwoStringString);
                var d4 = new TestStringString(a1.ThreeStringString);
                var d5 = (TestStringString)Delegate.Combine(d1, d2);
                var d6 = (TestStringString)Delegate.Combine(d3, d5);
                var d7 = (TestStringString)Delegate.Combine(d6, d4);
                TestLogger.Log(d7("7"));

                TestLogger.Log("Testing combined delegate hash...");
                TestLogger.Log(d7.GetHashCode() == d7.GetHashCode());
                TestLogger.Log(d7.GetHashCode() != d6.GetHashCode());

                TestLogger.Log("Testing removing delegates...");
                var d8 = (TestStringString)Delegate.Remove(d7, d5);
                TestLogger.Log(d8("8"));
                var d9 = (TestStringString)Delegate.Remove(d7, d2);
                TestLogger.Log(d9("9"));
                var d10 = (TestStringString)Delegate.Combine(d3, d2);
                var d11 = (TestStringString)Delegate.Remove(d7, d10);
                TestLogger.Log(d11("11"));
            }

            {
                TestLogger.Log("Testing virtual delegate...");
                var a = new A() { i = 3 };
                var d1 = new TestString(a.Virtual);
                TestLogger.Log(d1());
                var b = (A)new B() { i = 7 };
                var d2 = new TestString(b.Virtual);
                TestLogger.Log(d2());
            }

            {
                TestLogger.Log("Testing delegate with captured variable...");
                var outer = 7;
                FromTo(1, 3, delegate(int i) { TestLogger.Log(outer); TestLogger.Log(i); return outer * 4; });
                FromTo(1, 3, i => { TestLogger.Log(outer); TestLogger.Log(i); return outer * 4; });
            }

            {
                TestLogger.Log("Testing delegate with captured reference variable...");
                for (var i = 1; i <= 3; i++)
                {
                    Prepare(delegate { TestLogger.Log(i); });
                }
                Call();
            }

            {
                TestLogger.Log("Testing delegate with captured value variable...");
                for (var i = 1; i <= 3; i++)
                {
                    var j = i;
                    Prepare(delegate { TestLogger.Log(j); });
                }
                Call();
            }

            {
                TestLogger.Log("Testing event registering, triggering and unregestering...");
                SomethingHappened += delegate { TestLogger.Log("Something happened."); };
                if (SomethingHappened != null)
                    SomethingHappened();
                SomethingHappened += MoreHappened;
                if (SomethingHappened != null)
                    SomethingHappened();
                SomethingHappened -= MoreHappened;
                if (SomethingHappened != null)
                    SomethingHappened();
            }

            {
                TestLogger.Log("Testing delegates of higher-kinded type over polymorphic methods...");
                var polyint = new Poly<int>(3);
                var polystr = new Poly<string>("four");

                StringToString f = polyint.M<string>;
                TestLogger.Log(f("six"));
                IntToString g = polyint.M<int>;
                TestLogger.Log(g(7));
                StringToString h = polystr.M<string>;
                TestLogger.Log(h("eight"));
                IntToString i = polystr.M<int>;
                TestLogger.Log(i(9));
            }

#if false
            {
                TestLogger.Log("Testing BeginInvoke/EndInvoke...");
                Func<int, int, int> d = (x, y) =>
                {
                    TestLogger.Log("delegate called");
                    return x + y;
                };
                AsyncCallback cb = ar2 =>
                {
                    if (ar2.IsCompleted)
                        TestLogger.Log("completed asyncronously");
                    else
                        TestLogger.Log("not completed asyncronously");
                    TestLogger.Log(d.EndInvoke(ar2));
                };
                TestLogger.Log("invoking");
                System.Diagnostics.Debugger.Break();
                var ar = d.BeginInvoke(3, 7, cb, null);
                if (ar.IsCompleted)
                    TestLogger.Log("completed immediatly");
                else
                    TestLogger.Log("not completed immediatly");
                if (ar.CompletedSynchronously)
                {
                    TestLogger.Log("completed syncronously");
                    TestLogger.Log(d.EndInvoke(ar));
                }
                TestLogger.Log("done");
            }
#endif
        }

        static void FromTo(int from, int to, TestIntInt d)
        {
            for (var i = from; i <= to; i++)
                TestLogger.Log(d(i));
        }

        static event Test SomethingHappened;

        static void MoreHappened()
        {
            TestLogger.Log("More happened");
        }
    }

    delegate string StringToString(string s);
    delegate string IntToString(int i);

    public class A
    {
        public int i;

        protected void Log(string s)
        {
            TestLogger.Log(s + " invoked");
            TestLogger.Log("i: " + i.ToString());
        }

        public static string StaticStringString(string s)
        {
            TestLogger.Log("StaticStringString invoked");
            return "1" + s;
        }

        public void One()
        {
            Log("One");
        }

        public int OneInt()
        {
            Log("OneInt");
            return 1 + i;
        }

        public int OneIntInt(int j)
        {
            Log("OneIntInt");
            return j + i;
        }


        public string OneString()
        {
            Log("OneString");
            return "1" + i.ToString();
        }

        public string OneStringString(string s)
        {
            Log("OneStringString");
            return s + i.ToString();
        }

        public string TwoStringString(string s)
        {
            Log("TwoStringString");
            return s + i.ToString();
        }

        public string ThreeStringString(string s)
        {
            Log("ThreeStringString");
            return s + i.ToString();
        }

        public virtual string Virtual()
        {
            Log("A.Virtual");
            return "A" + i.ToString();
        }
    }

    public class B : A
    {
        public override string Virtual()
        {
            Log("B.Virtual");
            return "B" + i.ToString();
        }
    }

    public class Poly<A>
    {
        A a;

        public Poly(A a)
        {
            this.a = a;
        }

        public string M<B>(B b)
        {
            return "a = " + a + ", b = " + b;
        }
    }

}