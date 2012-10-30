using System;
using System.Text;
using System.Collections.Generic;

namespace Microsoft.LiveLabs.JavaScript.Tests
{
    static class TestArray
    {
        internal static readonly int[] primes = new int[] {
            2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73, 79, 83, 89, 97,
            101, 103, 107, 109, 113, 127, 131, 137, 139, 149, 151, 157, 163, 167, 173, 179, 181, 191, 193,
            197, 199, 211, 223, 227, 229, 233, 239, 241, 251, 257, 263, 269, 271, 277, 281, 283, 293, 307,
            311, 313, 317, 331, 337, 347, 349, 353, 359, 367, 373, 379, 383, 389, 397, 401, 409, 419, 421,
            431, 433, 439, 443, 449, 457, 461, 463, 467, 479, 487, 491, 499
        };

        static void Main()
        {
            {
                TestLogger.Log("Testing array initialization...");
                var arr = new int[4];
                arr[1] = 3;
                arr[3] = 7;
                for (var i = 0; i < arr.Length; i++)
                    TestLogger.Log(arr[i]);
            }

            {
                TestLogger.Log("Testing array initializers...");
                var arr = new int[] { 1, 2, 3, 4, 5 };
                Test(arr);

                TestLogger.Log("Testing array copy...");
                var copy = new int[9];
                Array.Copy(arr, 0, copy, 0, arr.Length);
                for (var i = 0; i < copy.Length; i++)
                    TestLogger.Log(copy[i]);

                TestLogger.Log("Testing array out of range behavior...");
                try
                {
                    TestLogger.Log(arr[29]);
                }
                catch (Exception e)
                {
                    TestLogger.LogException(e);
                }
                try
                {
                    arr[29] = 3;
                }
                catch (Exception e)
                {
                    TestLogger.LogException(e);
                }
            }

            {
                TestLogger.Log("Testing simple enumeration...");
                var sb = new StringBuilder();
                var first = true;
                foreach (var prime in primes)
                {
                    if (first)
                        first = false;
                    else
                        sb.Append(",");
                    sb.Append(prime.ToString());
                }
                TestLogger.Log(sb.ToString());
            }

            {
                TestLogger.Log("Testing refs to array elements...");
                var arr = new Value[] { new Value(1), new Value(2), new Value(3) };
                arr[1].Test();
                CallByRef(ref arr[1]);
                arr[1].Test();
            }

            {
                var arr = new B[9];
                FillAsA(arr);
                FillAsB(arr);
                FillAsArray(arr);
                EnumAsB(arr);
                EnumAsA(arr);
                EnumAsArray(arr);
            }


            {
                TestLogger.Log("Testing multi-dimensional arrays...");
                var arr = new int[3, 7];
                var k = 0;
                for (var i = 0; i < 3; i++)
                {
                    for (var j = 0; j < 7; j++)
                    {
                        arr[i, j] = primes[k++];
                        if (k >= primes.Length)
                            k = 0;
                    }
                }
                foreach (var v in arr)
                    TestLogger.Log(v);
            }
        }


        static void FillAsA(A[] arr)
        {
            TestLogger.Log("Testing covariant assignment over A[]...");
            try
            {
                arr[0] = new A();
            }
            catch (Exception e)
            {
                TestLogger.LogException(e);
            }
            arr[1] = new B();
            try
            {
                arr[2] = new C();
            }
            catch (Exception e)
            {
                TestLogger.LogException(e);
            }
        }

        static void FillAsB(B[] arr)
        {
            TestLogger.Log("Testing covariant assignment over B[]...");
            //arr[3] = new A();         // Ill-typed
            arr[4] = new B();
            //arr[5] = new C();         // Ill-typed
        }

        static void FillAsArray(System.Array arr)
        {
            TestLogger.Log("Testing covariant assignment over System.Array...");
            try
            {
                arr.SetValue(new A(), 6);
            }
            catch (Exception e)
            {
                TestLogger.LogException(e);
            }
            arr.SetValue(new B(), 7);
            try
            {
                arr.SetValue(new C(), 8);
            }
            catch (Exception e)
            {
                TestLogger.LogException(e);
            }
        }

        static void EnumAsB(B[] arr)
        {
            TestLogger.Log("Testing foreach enumeration over B[]...");
            foreach (var a in arr)
            {
                if (a != null)
                    TestLogger.Log(a.ToString());
            }

            TestLogger.Log("Testing untyped direct enumeration over B[]...");
            var en1 = arr.GetEnumerator();
            while (en1.MoveNext())
            {
                if (en1.Current != null)
                    TestLogger.Log(en1.Current.ToString());
            }

#if TEST_INTERFACES
            TestLogger.Log("What about B[]'s interfaces?");
            foreach (var t in arr.GetType().GetInterfaces())
                TestLogger.Log(t.ToString());
#endif

            TestLogger.Log("Testing typed direct enumeration over B[] as IEnumerable<B>...");
            var en2 = (IEnumerable<B>)arr;
            var enor2 = ((IEnumerable<B>)arr).GetEnumerator();
            while (enor2.MoveNext())
            {
                if (enor2.Current != null)
                    TestLogger.Log(enor2.Current.ToString());
            }

            TestLogger.Log("Is IEnumerable<B> same as array?");
            TestLogger.Log((object)en2 == (object)arr);

            TestLogger.Log("Is interface castable back to same array?");
            TestLogger.Log((B[])en2 == arr);

#if TEST_INTERFACES
            TestLogger.Log("And what is this IEnumerable<B> anyways?");
            TestLogger.Log(en2.GetType().FullName);

            TestLogger.Log("And what members does B[] have?");
            foreach (var m in arr.GetType().GetMembers())
                TestLogger.Log(m.ToString());

            TestLogger.Log("And what about B[]'s interfaces now?");
            foreach (var t in arr.GetType().GetInterfaces())
                TestLogger.Log(t.ToString());
#endif

            TestLogger.Log("Testing typed direct enumeration over B[] as IEnumerable<A>...");
            var en3 = (IEnumerable<A>)arr;
            var enor3 = en3.GetEnumerator();
            while (enor3.MoveNext())
            {
                if (enor3.Current != null)
                    TestLogger.Log(enor3.Current.ToString());
            }

#if TEST_INTERFACES
            TestLogger.Log("Hmm, now did B[]'s interfaces grow I wonder?");
            foreach (var t in arr.GetType().GetInterfaces())
                TestLogger.Log(t.ToString());
#endif

            TestLogger.Log("Is IEnumerable<A> same as array?");
            TestLogger.Log((object)en3 == (object)arr);
        }

        static void EnumAsA(A[] arr)
        {
            TestLogger.Log("Testing foreach enumeration over A[]...");
            foreach (var a in arr)
            {
                if (a != null)
                    TestLogger.Log(a.ToString());
            }

            TestLogger.Log("Testing untyped direct enumeration over A[]...");
            var en1 = arr.GetEnumerator();
            while (en1.MoveNext())
            {
                if (en1.Current != null)
                    TestLogger.Log(en1.Current.ToString());
            }

            TestLogger.Log("Testing typed direct enumeration over A[] as IEnumerable<B>...");
            var en2 = ((IEnumerable<B>)arr).GetEnumerator();
            while (en2.MoveNext())
            {
                if (en2.Current != null)
                    TestLogger.Log(en2.Current.ToString());
            }

            TestLogger.Log("Testing typed direct enumeration over A[] as IEnumerable<C>...");
            try
            {
                var en3 = ((IEnumerable<C>)arr).GetEnumerator();
                while (en3.MoveNext())
                {
                    if (en3.Current != null)
                        TestLogger.Log(en3.Current.ToString());
                }
            }
            catch (Exception e)
            {
                TestLogger.LogException(e);
            }
        }

        static void EnumAsArray(Array arr)
        {
            TestLogger.Log("Testing foreach enumeration over Array...");
            foreach (var a in arr)
            {
                if (a != null)
                    TestLogger.Log(a.ToString());
            }

            TestLogger.Log("Testing untyped direct enumeration over Array...");
            var en1 = arr.GetEnumerator();
            while (en1.MoveNext())
            {
                if (en1.Current != null)
                    TestLogger.Log(en1.Current.ToString());
            }

            TestLogger.Log("Testing typed direct enumeration over Array...");
            var en2 = ((IEnumerable<B>)arr).GetEnumerator();
            while (en2.MoveNext())
            {
                if (en2.Current != null)
                    TestLogger.Log(en2.Current.ToString());
            }
        }

        static void Test(int[] arr)
        {
            arr[2] = 30;
            TestLogger.Log(arr[2]);
            ChangeArrayByRef(ref arr[2]);
            TestLogger.Log(arr[2]);
            for (var i = 0; i < arr.Length; i++)
                TestLogger.Log(arr[i]);
        }

        static void CallByRef(ref Value a)
        {
            a.Test();
            a = new Value(7);
        }


        static void ChangeArrayByRef(ref int a)
        {
            a *= 2;
        }
    }

    public class Value
    {
        private int v;

        public Value(int v)
        {
            this.v = v;
        }

        public void Test()
        {
            v++;
            TestLogger.Log("Value(" + v.ToString() + ")");
        }
    }

    class A
    {
        public override string ToString()
        {
            return "A";
        }
    }

    class B : A
    {
        public override string ToString()
        {
            return "B";
        }
    }

    class C : A
    {
        public override string ToString()
        {
            return "C";
        }
    }
}