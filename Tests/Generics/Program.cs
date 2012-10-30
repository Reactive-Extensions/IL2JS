using System;
using System.Collections.Generic;

namespace Microsoft.LiveLabs.JavaScript.Tests
{
    static class TestGenerics
    {
        static void Main()
        {
            {
                TestLogger.Log("Testing first- vs higher-kinded and mono- vs polymorphic-method combinations...");
                var l = new List<string>();
                l.Add("abc");
                TestLogger.Log(new FirstOrderClassPolymorphicMethod().Test(l));
                TestLogger.Log(new HigherKindedClassMonomorphicMethod<string>().Test(l));
                var dict = new Dictionary<string, int>();
                dict.Add("def", 3);
                TestLogger.Log(new HigherKindedClassPolymorphicMethod<string>().Test(dict));
            }

            {
                TestLogger.Log("Testing implicit implementations of interface methods on different instantiations of the same interface ...");
                TestSameInterfaceTwice<string>("test");
                TestSameInterfaceTwice<int>(1);
            }

            {
                TestLogger.Log("Testing implicit and explicit implementations of interface methods on the same interface...");
                var x = new ImplicitAndExplicit<string>();
                x.Test1(new Wrapper<string>("hij"));
                ITwoTests<Wrapper<string>> y = x;
                y.Test2(new Wrapper<string>("klm"));
            }

            {
                TestLogger.Log("Testing construction and field mutation...");
                var p = new Person("pqr");
                TestLogger.Log(p.ToString());

                var b1 = new MyBox<Person, string>(new Person("opq"));
                b1.u = "b1";
                var b2 = new MyBox<int, string>(1);
                b2.u = "b2";

                TestLogger.Log(b1.ToString());
                TestLogger.Log(b2.ToString());

                b1.t = new Person("rst");
                b1.u = "uvw";
                b2.t = 3;

                TestLogger.Log(b1.ToString());
                TestLogger.Log(b2.ToString());
            }

            {
                TestLogger.Log("Testing mini generic lists of object...");
                var list = new MyList<object>();
                list.Add(1);
                list.Add("2");
                list.Add(new Person("pqr"));
                foreach (var obj in list)
                    TestLogger.Log(obj.ToString());
            }

            {
                TestLogger.Log("Testing mini generic dictionary of string to int...");
                var dict = new MyDictionary<string, int>();
                dict.Add("one", 1);
                dict.Add("two", 2);
                dict.Add("three", 3);
                foreach (var kvPair in dict)
                    TestLogger.Log(kvPair.Key + " -> " + kvPair.Value);
            }

            {
                TestLogger.Log("Testing static fields on distinct type instances...");
                MyList<int>.testVal = 1;
                MyList<string>.testVal = 2;

                TestLogger.Log(MyList<int>.testVal.ToString());
                TestLogger.Log(MyList<string>.testVal.ToString());
            }

            {
                TestLogger.Log("Testing generic methods with interesting permutation of type arguments.."); ;
                var b = new MyBox<Person, string>(new Person("opq"));
                b.u = "second";
                TestGenericMethod<Person, string, int>(b, 5, null);
                TestGenericMethod<Person, string, string>(b, "xyz", null);
                TestGenericMethod<Person, string, Person>(b, new Person("efg"), null);
            }

            {
                TestLogger.Log("Testing recursive types...");
                var a = new A<B>();
                var b = new B();
                TestLogger.Log(a.M(b));
                TestLogger.Log(b.M(a));
            }
        }

        static void TestSameInterfaceTwice<T>(T x)
        {
            var adder = (IHigherKindedAndPolymorphic<T>)new SameInterfaceTwice();
            adder.Test<object>(x, null);
        }

        static void TestGenericMethod<B, C, A>(MyBox<B, C> b, A val, B dummyVal)
        {
            TestLogger.Log(b.ConcatFirstAndPrint<A>(val, dummyVal));
        }
    }

    interface IHigherKindedAndPolymorphic<T>
    {
        void Test<U>(T x, U y);
    }

    class SameInterfaceTwice : IHigherKindedAndPolymorphic<int>, IHigherKindedAndPolymorphic<string>
    {
        public void Test<U>(string x, U y)
        {
            TestLogger.Log("IHigherKindedAndPolymorphic<string>::Add");
        }

        public void Test<U>(int x, U y)
        {
            TestLogger.Log("IHigherKindedAndPolymorphic<int>::Add");
        }

        public override string ToString()
        {
            return "";
        }
    }

    class MyBox<T, U>
    {
        public T t;
        public U u;

        public MyBox(T val)
        {
            t = val;
        }

        override public string ToString()
        {
            return "MyBox(t=" + t + ", u=" + u + ")";
        }

        public string ConcatFirstAndPrint<A>(A a, T dummy)
        {
            var b1 = new MyBox<A, int>(a);
            var b2 = new MyBox<int, MyBox<int, int>>(3);
            var b3 = new MyBox<int, MyBox<A, A>>(4);
            return "t=" + t + ", a=" + a.ToString() + ", Second=" + Second;
        }

        public string ConcatFirstAndPrint<A, B>(A a)
        {
            return "...never called...";
        }

        virtual public U Second { get { return u; } } 
    }

    class Person : MyBox<int, int>
    {
        private string name;

        public Person(string name)
            : base(5)
        {
            this.name = name;
        }

        override public string ToString()
        {
            return "Person(t=" + t + ", u=" + u + ", name=" + name + ")";
        }

        override public int Second { get { return 10; } }
    }

    public interface ITwoTests<T>
    {
        void Test1(T test);
        void Test2(T test);
    }

    public class Wrapper<T>
    {
        public T Value;

        public Wrapper(T value)
        {
            Value = value;
        }

        public override string ToString()
        {
            return "Wrapper(Value=" + Value + ")";
        }
    }

    public class ImplicitAndExplicit<T> : ITwoTests<Wrapper<T>>
    {
        public void Test1(Wrapper<T> test)
        {
            TestLogger.Log("ImplicitAndExplicit::Test(" + test + ")");
        }

        void ITwoTests<Wrapper<T>>.Test2(Wrapper<T> test)
        {
            TestLogger.Log("ImplicitAndExplicit::Test2(" + test + ")");
        }
    }

    class MyList<T, V> { }

    class MyList<T> : IEnumerable<T>
    {
        static public int testVal;

        private int count = 0;
        private int capacity = 10;
        private T[] array;

        public MyList()
        {
            array = new T[capacity];
        }

        public int Count { get { return count; } }

        public void Add(T val)
        {
            if (count < capacity)
            {
                array[count] = val;
                count++;
            }
            else
            {
                var temp = array;
                capacity = capacity * 2;
                array = new T[capacity];
                for (var i = 0; i < temp.Length; i++)
                    array[i] = temp[i];
                Add(val);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < count; i++)
                yield return array[i];
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public T this[int index]
        {
            get
            {
                if (index < count)
                    return array[index];
                else
                    throw new ArgumentOutOfRangeException("index");
            }
        }
    }

    class MyKeyValuePair<K, V>
    {
        public K key;
        public V value;

        public MyKeyValuePair(K key, V value)
        {
            this.key = key;
            this.value = value;
        }

        public K Key { get { return key; } }
        public V Value { get { return value; } }
    }

    class MyDictionary<K, V> : IEnumerable<MyKeyValuePair<K, V>>
    {
        private MyList<MyKeyValuePair<K, V>> list;

        public MyDictionary()
        {
            list = new MyList<MyKeyValuePair<K, V>>();
        }

        public IEnumerator<MyKeyValuePair<K, V>> GetEnumerator()
        {
            foreach (var kv in list)
                 yield return kv;
         }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(K key, V value)
        {
            list.Add(new MyKeyValuePair<K, V>(key, value));
        }
    }

    public interface IX<T>
    {
        int M(T t);
    }

    public class A<T> : List<A<T>>, IX<B>
    {
        private static B theB;
        static A() { theB = new B(); }
        public int M(B b) { return 1; }
    }

    public class B : IX<A<B>>
    {
        private static A<B> theA;
        static B() { theA = new A<B>(); }
        public int M(A<B> a) { return 2; }
    }
}