using System;
using System.Collections.Generic;
using Microsoft.LiveLabs.JavaScript.IL2JS;

namespace Microsoft.LiveLabs.JavaScript.Tests
{
    static class TestLists
    {

        public static void Main()
        {
            {
                TestLogger.Log("Testing List<int>...");
                var l = new List<int>();
                TestLogger.Log("...addding...");
                l.Add(1);
                l.Add(3);
                l.Add(5);
                l.Add(7);
                l.Add(9);
                PrintList(l);
                TestLogger.Log("...counting...");
                TestLogger.Log(l.Count);
                TestLogger.Log("...removing...");
                l.Remove(3);
                PrintList(l);
                TestLogger.Log("...removing at index...");
                l.RemoveAt(2);
                PrintList(l);
                TestLogger.Log("...inserting at index...");
                l.Insert(2, 11);
                PrintList(l);
                TestLogger.Log("...contains...");
                TestLogger.Log(l.Contains(11));
                TestLogger.Log(l.Contains(199));
                TestLogger.Log("...indexing...");
                TestLogger.Log(l[2]);
                TestLogger.Log("...copy to array...");
                var a = new int[l.Count];
                l.CopyTo(a, 0);
                foreach (var i in a)
                    TestLogger.Log(i);
                TestLogger.Log("...clear...");
                l.Clear();
                PrintList(l);
            }

            {
                TestLogger.Log("Testing List<Class>...");
                var l = new List<Class>();
                TestLogger.Log("...adding...");
                Class c1 = new Class("1");
                Class c3 = new Class("3");
                Class c5 = new Class("5");
                Class c7 = new Class("7");
                Class c9 = new Class("9");

                l.Add(c1);
                l.Add(c3);
                TestLogger.Log("...index of...");
                TestLogger.Log(l.IndexOf(c3));
                l.Add(c5);
                l.Add(c7);
                l.Add(c9);
                PrintList(l);
                TestLogger.Log("...counting...");
                TestLogger.Log(l.Count);
                TestLogger.Log("...removing...");
                l.Remove(c3);
                PrintList(l);
                TestLogger.Log("...removing at...");
                l.RemoveAt(2);
                PrintList(l);
                TestLogger.Log("...inserting...");
                var c11 = new Class("11");
                l.Insert(2, c11);
                PrintList(l);
                TestLogger.Log("...contains...");
                TestLogger.Log(l.Contains(c11));
                var c199 = new Class("199");
                TestLogger.Log(l.Contains(c199));
                var other11 = new Class("11");
                TestLogger.Log(l.Contains(other11));
                TestLogger.Log("...indexing...");
                TestLogger.Log(l[2].ToString());
                TestLogger.Log("...copy to array...");
                Class[] a = new Class[l.Count];
                l.CopyTo(a, 0);
                foreach (var c in a)
                    TestLogger.Log(c.ToString());
                TestLogger.Log("...clearing...");
                l.Clear();
                PrintList(l);
                TestLogger.Log("...adding...");
                var item = new Class("3");
                l.Add(item);
                TestLogger.Log("...setting value via item...");
                item.Value = "5";
                TestLogger.Log(item.Value);
                TestLogger.Log(l[0].Value);
                TestLogger.Log("...setting value via list...");
                l[0].Value = "9";
                TestLogger.Log(l[0].Value);
                TestLogger.Log(item.Value);
            }

            {
                TestLogger.Log("Testing List<Struct>...");
                var l = new List<Struct>();
                TestLogger.Log("...adding...");
                var item = new Struct(3);
                l.Add(item);
                TestLogger.Log("...setting value via item...");
                item.Value = 5;
                TestLogger.Log(item.Value);
                TestLogger.Log(l[0].Value);
                TestLogger.Log("...setting value via list...");
                var back = l[0];
                back.Value = 9;
                TestLogger.Log(back.Value);
                TestLogger.Log(l[0].Value);
                TestLogger.Log(item.Value);
            }

            {
                TestLogger.Log("Testing Dictionary<int, string>...");
                var d = new Dictionary<int, string>();
                TestLogger.Log("...adding...");
                d.Add(1, "one");
                d.Add(3, "three");
                d.Add(5, "five");
                PrintDictionary(d, (x, y) => x.CompareTo(y));
                TestLogger.Log("...values...");
                var vs = new List<string>();
                foreach (var v in d.Values)
                    vs.Add(v);
                vs.Sort((s, t) => s.CompareTo(t));
                foreach (var v in vs)
                    TestLogger.Log(v);
                TestLogger.Log("...keys...");
                var ks = new List<int>();
                foreach (var k in d.Keys)
                    ks.Add(k);
                ks.Sort((s, t) => s.CompareTo(t));
                foreach (var k in ks)
                    TestLogger.Log(k);
                TestLogger.Log("...count...");
                TestLogger.Log(d.Count);
                TestLogger.Log("...contains key...");
                TestLogger.Log(d.ContainsKey(3));
                TestLogger.Log(d.ContainsKey(9));

                var value = default(string);
                TestLogger.Log("...try get...");
                TestLogger.Log(d.TryGetValue(3, out value));
                TestLogger.Log(value);
                TestLogger.Log(d.TryGetValue(9, out value));
                TestLogger.Log("...remove...");
                TestLogger.Log(d.Remove(3));
                PrintDictionary(d, (x, y) => x.CompareTo(y));
                TestLogger.Log(d.Remove(58));
                PrintDictionary(d, (x, y) => x.CompareTo(y));
                TestLogger.Log("...clear...");
                d.Clear();
                PrintDictionary(d, (x, y) => x.CompareTo(y));
            }

            {
                TestLogger.Log("Testing Dictionary<string, int>...");
                var d = new Dictionary<string, int>();
                TestLogger.Log("...adding...");
                d.Add("one", 1);
                d.Add("three", 3);
                d.Add("five", 5);
                PrintDictionary(d, (x, y) => x.CompareTo(y));
                TestLogger.Log("...values...");
                var vs = new List<int>();
                foreach (var v in d.Values)
                    vs.Add(v);
                vs.Sort((s, t) => s.CompareTo(t));
                foreach (var v in vs)
                    TestLogger.Log(v);
                TestLogger.Log("...keys...");
                var ks = new List<string>();
                foreach (var k in d.Keys)
                    ks.Add(k);
                ks.Sort((s, t) => s.CompareTo(t));
                foreach (var k in ks)
                    TestLogger.Log(k);
                TestLogger.Log("...count...");
                TestLogger.Log(d.Count);
                TestLogger.Log("...contains key...");
                TestLogger.Log(d.ContainsKey("three"));
                TestLogger.Log(d.ContainsKey("nine"));

                var value = default(int);
                TestLogger.Log("...try get...");
                TestLogger.Log(d.TryGetValue("three", out value));
                TestLogger.Log(value);
                TestLogger.Log(d.TryGetValue("nine", out value));
                TestLogger.Log("...remove...");
                TestLogger.Log(d.Remove("three"));
                PrintDictionary(d, (x, y) => x.CompareTo(y));
                TestLogger.Log(d.Remove("nine"));
                PrintDictionary(d, (x, y) => x.CompareTo(y));
                TestLogger.Log("...clear...");
                d.Clear();
                PrintDictionary(d, (x, y) => x.CompareTo(y));
            }

            {
                TestLogger.Log("Testing Dictionary<Type, string>...");
                var d = new Dictionary<Type, string>();
                TestLogger.Log("...adding...");
                d.Add(typeof(A), "A");
                d.Add(typeof(B), "B");
                d.Add(typeof(C), "C");
                PrintDictionary(d, (x, y) => x.FullName.CompareTo(y.FullName));
                TestLogger.Log("...values...");
                var vs = new List<string>();
                foreach (var v in d.Values)
                    vs.Add(v);
                vs.Sort((s, t) => s.CompareTo(t));
                foreach (var v in vs)
                    TestLogger.Log(v);
                TestLogger.Log("...keys...");
                var ks = new List<string>();
                foreach (var k in d.Keys)
                    ks.Add(k.FullName);
                ks.Sort((s, t) => s.CompareTo(t));
                foreach (var k in ks)
                    TestLogger.Log(k);
                TestLogger.Log("...count...");
                TestLogger.Log(d.Count);
                TestLogger.Log("...contains key...");
                TestLogger.Log(d.ContainsKey(typeof(A)));
                TestLogger.Log(d.ContainsKey(typeof(D)));

                var value = default(string);
                TestLogger.Log("...try get...");
                TestLogger.Log(d.TryGetValue(typeof(A), out value));
                TestLogger.Log(value);
                TestLogger.Log(d.TryGetValue(typeof(D), out value));
                TestLogger.Log("...remove...");
                TestLogger.Log(d.Remove(typeof(A)));
                PrintDictionary(d, (x, y) => x.FullName.CompareTo(y.FullName));
                TestLogger.Log(d.Remove(typeof(D)));
                PrintDictionary(d, (x, y) => x.FullName.CompareTo(y.FullName));
                TestLogger.Log("...clear...");
                d.Clear();
                PrintDictionary(d, (x, y) => x.FullName.CompareTo(y.FullName));
            }

            // TODO: Move to System.dll tests
#if false
            {
                TestLogger.Log("Testing Queue<string>...");
                var q = new Queue<string>();
                TestLogger.Log("...initial count...");
                TestLogger.Log(q.Count);

                TestLogger.Log("...[a]...");
                q.Enqueue("a");
                TestLogger.Log(q.Count);
                TestLogger.Log(q.Peek());

                TestLogger.Log("...[a, b]...");
                q.Enqueue("b");
                TestLogger.Log(q.Count);
                TestLogger.Log(q.Peek());

                TestLogger.Log("...[b]...");
                TestLogger.Log(q.Dequeue());
                TestLogger.Log(q.Count);
                TestLogger.Log(q.Peek());

                TestLogger.Log("...[b, c]...");
                q.Enqueue("c");
                TestLogger.Log(q.Count);
                TestLogger.Log(q.Peek());

                TestLogger.Log("...[b, c, d]...");
                q.Enqueue("d");
                TestLogger.Log(q.Count);
                TestLogger.Log(q.Peek());

                TestLogger.Log("...[b, c, d, e]...");
                q.Enqueue("e");
                TestLogger.Log(q.Count);
                TestLogger.Log(q.Peek());

                TestLogger.Log("...[c, d, e]...");
                TestLogger.Log(q.Dequeue());
                TestLogger.Log(q.Count);
                TestLogger.Log(q.Peek());

                TestLogger.Log("...[d, e]...");
                TestLogger.Log(q.Dequeue());
                TestLogger.Log(q.Count);
                TestLogger.Log(q.Peek());

                TestLogger.Log("...[d, e, f]...");
                q.Enqueue("f");
                TestLogger.Log(q.Count);
                TestLogger.Log(q.Peek());

                TestLogger.Log("...[e, f]...");
                TestLogger.Log(q.Dequeue());
                TestLogger.Log(q.Count);
                TestLogger.Log(q.Peek());

                TestLogger.Log("...[f]...");
                TestLogger.Log(q.Dequeue());
                TestLogger.Log(q.Count);
                TestLogger.Log(q.Peek());

                TestLogger.Log("...[]...");
                TestLogger.Log(q.Dequeue());
                TestLogger.Log(q.Count);
            }
#endif
        }

        private static void PrintDictionary<TKey, TValue>(Dictionary<TKey, TValue> dict, Comparison<TKey> comp)
        {
            var ks = new List<TKey>();
            foreach (var kv in dict)
                ks.Add(kv.Key);
            ks.Sort(comp);
            foreach (var k in ks)
                TestLogger.Log("key=" + k.ToString() + ", value=" + dict[k].ToString());
        }

        private static void PrintList<T>(List<T> list)
        {
            foreach (var item in list)
                TestLogger.Log(item.ToString());
        }
    }

    [Reflection(ReflectionLevel.Full)]
    public class A { }
    [Reflection(ReflectionLevel.Full)]
    public class B { }
    [Reflection(ReflectionLevel.Full)]
    public class C { }
    [Reflection(ReflectionLevel.Full)]
    public class D { }

    public class Class
    {
        public string Value;

        public Class(string value)
        {
            Value = value;
        }

        public override string ToString()
        {
            return "Class() { Value = " + Value + " }";
        }
    }

    public struct Struct
    {
        public int Value;

        public Struct(int value)
        {
            Value = value;
        }

        public override string ToString()
        {
            return "Struct() { Value = " + Value.ToString() + " }";
        }
    }

}