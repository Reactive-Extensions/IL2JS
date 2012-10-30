
using System;
using System.Text;
using Microsoft.LiveLabs.JavaScript.IL2JS;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.JavaScript.Tests
{
    public static class TestInterop
    {
        public static void Main()
        {
#if WSH
            ManagedInterop.WSH.WSHBridge.Initialize(true, true);
#endif
            Run();
        }

        private static void Run()
        {
            {
                TestLogger.Log("Testing basic interop...");
                var fso = new FileSystemObject();
                var drive1 = fso.GetDrive("C:");
                var drive2 = fso.GetDrive("C:");
                TestLogger.Log(drive1.FreeSpace == drive2.FreeSpace);
            }

            {
                TestLogger.Log("Tesing getters...");
                var prop = new PropertyTest();
                prop.Setup();
                TestLogger.Log(prop.X);
            }

            {
                TestLogger.Log("Testing virtuals...");
                var b = new VirtualsBase();
                TestLogger.Log(b.V());
                TestLogger.Log(b.CallV());
                TestLogger.Log(b.U());
                var d = (VirtualsBase)new VirtualsDerived();
                TestLogger.Log(d.V());
                TestLogger.Log(d.CallV());
                TestLogger.Log(d.U());
            }

            {
                TestLogger.Log("Testing JSObject...");
                var obj = new JSObject
                              {
                                  { "int", 1 },
                                  { "string", "two" },
                                  { "object", new JSObject { { "inner", 3 }, { "null", null } } }
                              };
                TestLogger.Log(obj.GetField<int>("int"));
                TestLogger.Log(obj.GetField<string>("string"));
                TestLogger.Log(obj.GetField<JSObject>("object").GetField<int>("inner"));
            }

            {
                TestLogger.Log("Testing exceptions...");
                var obj = new JSObject { { "int", 1 }, { "string", "two" } };
                try
                {
                    // Invalid cast
                    var dummy = obj.GetField<int[]>("int");
                    TestLogger.Log(dummy.ToString());
                }
                catch (Exception e)
                {
                    TestLogger.LogException(e);
                }
                try
                {
                    // User exception
                    TestException.ThrowFromUnmanaged();
                }
                catch (Exception e)
                {
                    TestLogger.LogException(e);
                }
                // Managed -> unmanaged exception
                TestException.CatchFromManagedInUnmanaged();
                try
                {
                    // Managed -> unmanaged -> managed exception
                    TestException.ThrowViaUnmanaged();
                }
                catch (Exception e)
                {
                    TestLogger.LogException(e);
                }
                // Unmanaged -> managed -> unmanaged -> catch
                TestException.CatchViaManagedInUnmanaged();
                try
                {
                    // Unmanaged exception
                    TestException.InvalidJavaScript();
                }
                catch (Exception e)
                {
                    TestLogger.LogException(e);
                }
            }

            {
                TestLogger.Log("Testing JSArray...");
                var arr = JSArray.FromArray(0, 1, "two", new JSObject { { "value", 3 } }, new JSObject { { "value", "four" } });
                TestLogger.Log(arr.Length);
                TestLogger.Log(arr[0].ToString());
                TestLogger.Log(arr[1].ToString());
                TestLogger.Log(arr[2].ToString());
                TestLogger.Log(arr[3].GetField<int>("value"));
                TestLogger.Log(arr[4].GetField<string>("value"));
            }

            {
                TestLogger.Log("Testing polymorphic method of higher-kinded type...");
                var t1 = new TestGeneric<int>(1);
                TestLogger.Log(t1.M<string>("two"));
                var obj = new JSObject();
                obj.SetField("f", "field");
                var t2 = new TestGeneric<JSObject>(obj);
                TestLogger.Log(t2.M<int>(3));
                TestLogger.Log(t2.M<JSObject>(obj));
            }

            {
                TestLogger.Log("Testing exported instance methods in base types...");
                var d = new TestGenericDerived<int>();
                d.Value = new int[] { 1, 2, 3 };
                d.OtherValue = 4;
                TestLogger.Log(d.N());
            }

            {
                TestLogger.Log("Testing delegates...");
                TestLogger.Log(TestDelegate.TestWithDelegate());
                TestLogger.Log
                    (TestDelegate.WithManagedDelegate
                         ((i, obj) =>
                          new JSObject { { "test", i.ToString() + " " + obj.GetField<int>("test").ToString() } },
                          new JSObject { { "test", 7 } }));
            }

            {
                TestLogger.Log("Testing param arrays...");
                TestLogger.Log(TestParams.JoinA(3, 7, "a", "b", "c"));
                TestLogger.Log(TestParams.TestB());
                ParamsDelegate f = TestParams.JoinA;
                TestLogger.Log(TestParams.Call(f));
                var g = TestParams.GetTestB();
                TestLogger.Log(g(3, 7, "a", "b", "c"));
            }

            {
                TestLogger.Log("Testing proxied object...");
                var p = new ProxiedTest();
                TestLogger.Log(ProxiedBase.TestGlobal);
                TestLogger.Log(p.One);
                TestLogger.Log(p.Two);
                TestLogger.Log(p.Three.GetField<int>("value"));
                TestLogger.Log(p.GetValue());
                p.One = 11;
                p.Two = "twenty-two";
                p.Three = new JSObject { { "value", 55 } };
#if true
                // BROKEN: Polymorphic delegates
                p.Delegate = i => TestLogger.Log(i);
#endif
                TestLogger.Log(p.One);
                TestLogger.Log(p.Two);
                TestLogger.Log(p.Three.GetField<int>("value"));
#if true
                // BROKEN: Polymorphic delegates
                p.Delegate(7);
#endif
                TestLogger.Log(p.GetValue());
                p.Morph();
                // TODO: Managed Interop will not notice '2' is an int rather than a string
#if false
                try
                {
                    TestLogger.Log(p.Two);
                }
                catch (InvalidCastException e)
                {
                    TestLogger.LogException(e);
                }
#endif
                var obj = JSObject.From(p);
                TestLogger.Log(obj.GetField<int>("two"));
                obj.SetField<int>("one", 111);
                TestLogger.Log(p.One);
                p.One = 1111;
                TestLogger.Log(obj.GetField<int>("one"));
            }

            {
                TestLogger.Log("Testing keyed object...");
                var k = new KeyedTest(1, "two", 3, "four");
                TestLogger.Log(k.I);
                TestLogger.Log(k.S);
                TestLogger.Log(k.X);
                TestLogger.Log(k.Y);
                k.I = 11;
                k.X = 33;
                TestLogger.Log(k.I);
                TestLogger.Log(k.X);
                var k2 = k.Clone();
                TestLogger.Log(k2.I);
                TestLogger.Log(k2.X);
                k.I = 111;
                k.X = 333;
                TestLogger.Log(k2.I);
                TestLogger.Log(k2.X);
                var k3 = k.PassThrough();
                TestLogger.Log(k3.I);
                TestLogger.Log(k3.X);
                k3.I = 1111;
                k3.X = 3333;
                TestLogger.Log(k.I);
                TestLogger.Log(k.X);
                k.Morph();
                try
                {
                    TestLogger.Log(k.X);
                }
                catch (Exception e)
                {
                    TestLogger.LogException(e);
                }
                var obj = JSObject.From(k);
                TestLogger.Log(obj.GetField<string>("x"));
                obj.SetField<int>("x", 1);
                TestLogger.Log(k.X);
            }

            {
                TestLogger.Log("Testing nullable...");
                var ni = new int?(3);
                TestLogger.Log(ni.Value);
                ni = NullableTest.Incr(ni);
                TestLogger.Log(ni.Value);
                var ni2 = default(int?);
                TestLogger.Log(ni2.HasValue);
                ni2 = NullableTest.Incr(ni2);
                TestLogger.Log(ni2.HasValue);
            }

            {
                TestLogger.Log("Testing primitive arrays...");
                var arr = new int[] { 0, 1, 2, 3, 4, 5 };
                var arr2 = TestArray.WithArray(arr);
                TestLogger.Log(arr[3]);
                TestLogger.Log(arr2.Length);
                TestLogger.Log(arr2[3]);
            }

            {
                TestLogger.Log("Testing normal object...");
                {
                    var l = "left";
                    var r = new Normal { I = 7, S = "nine" };
                    var o = TestNormal.Right(l, r);
                    var r2 = (Normal)o;
                    TestLogger.Log(r2.I);
                    TestLogger.Log(r2.S);
                }
                {
                    var l = new Normal { I = 7, S = "nine" };
                    var r = "right";
                    var o = TestNormal.Right(l, r);
                    var r2 = (string)o;
                    TestLogger.Log(r2);
                }
                {
                    var l = "left";
                    var r = 2;
                    var o = TestNormal.Right(l, r);
                    var r2 = (int)o;
                    TestLogger.Log(r2);
                }
                {
                    var l = new KeyedTest(1, "two", 3, "four");
                    var r = new ProxiedTest();
                    try
                    {
                        var o = TestNormal.Right(l, r);
                        var r2 = (ProxiedTest)o;
                        TestLogger.Log(r2.One);
                    }
                    catch (Exception e)
                    {
                        TestLogger.LogException(e);
                    }
                }
            }

            {
                TestLogger.Log("Testing default instances bug...");
                var t = new TestDefaultInstance();
                TestLogger.Log(t.X.ToString());
                t.X = 3;
                TestLogger.Log(t.X.ToString());
            }

            TestLogger.Log("Done.");
        }
    }

    [Import(MemberNameCasing = Casing.Pascal)] // No constructor
    public class Drive
    {
        private Drive()
        {
        }

        extern public long FreeSpace { get; set; }
    }

    [Import(MemberNameCasing = Casing.Pascal)]
    public class FileSystemObject
    {
        [Import("function() { return new ActiveXObject('Scripting.FileSystemObject'); }")]
        public extern FileSystemObject();

        public extern Drive GetDrive(string letter);
    }

    public class PropertyTest {
        [Import("function() { this.X = 3; this.get_X = function() { return this.X }; }")]
        extern public void Setup();

        extern public int X
        {
            [Import(MemberNameCasing=Casing.Pascal)]
            get;
        }
    }

    [Interop(State = InstanceState.ManagedAndJavaScript, DefaultKey = "id")]
    public class VirtualsBase
    {
        [Import(@"function() { return { v : function() { return 3; }, u: function() { return 4; } }; }")]
        public extern VirtualsBase();

        [Import(@"function() { return this.v(); }")]
        extern public int CallV();

        [Import]
        extern public virtual int V();

        [Import]
        extern public Func<int> U
        {
            get;
            set;
        }
    }

    public class VirtualsDerived : VirtualsBase
    {
        public VirtualsDerived()
        {
            U = () => 8;
        }

        public override int V() { return 7; }
    }

    public static class TestException
    {
        [Import("function() { TestException.ThrowFromManaged(); }")]
        extern public static void ThrowViaUnmanaged();

        [Export(Qualification = Qualification.Type, MemberNameCasing = Casing.Pascal)]
        public static void ThrowFromManaged()
        {
            throw new InvalidOperationException("thrown from managed");
        }

        [Inline(false)] // suppress inline to wrap the exception 
        [Import("function() { NoSuchObject.NoSuchField = 3; }")]
        extern public static void InvalidJavaScript();

        [Import(@"function() { try { TestException.ThrowFromManaged(); return ""no exception""; } catch (e) { return ""unmanaged caught exception: "" + e.message; } }")]
        extern public static string CatchFromManagedInUnmanaged();

        [Import(@"function() { try { TestException.ThrowViaManaged(); return ""no exception""; } catch (e) { return ""unmanaged caught exception: "" + e.message; } }")]
        extern public static void CatchViaManagedInUnmanaged();

        [Export(Qualification = Qualification.Type, MemberNameCasing = Casing.Pascal)]
        public static void ThrowViaManaged()
        {
            ThrowFromUnmanaged();
        }

        [Inline(false)] // suppress inline to wrap the exception 
        [Import(@"function() { throw Error(""thrown from unmanaged""); }")]
        extern public static void ThrowFromUnmanaged();
    }

    [Interop(DefaultKey = "Id")]
    public class TestGeneric<T>
    {
        [Import("function(t) { return {}; }")]
        extern public TestGeneric(T t);

        public TestGeneric(JSContext ctxt, T t)
        {
            Value = t;
        }

        public TestGeneric(JSContext ctxt)
        {
        }

        [Export(MemberNameCasing = Casing.Pascal)]
        public T Value { get; set; }

        [Import(@"function(arg) { return ""Value = "" + this.get_Value().toString() + "", arg = "" + arg.toString(); }")]
        extern public string M<U>(U arg);
    }

    public class TestGenericDerived<T> : TestGeneric<T[]>
    {
        [Import(Creation = Creation.Object)]
        extern public TestGenericDerived();

        [Export(MemberNameCasing = Casing.Pascal)]
        public T OtherValue { get; set; }

        [Import(@"function() { return ""Value = "" + this.get_Value().length.toString() + "", OtherValue = "" + this.get_OtherValue().toString(); }")]
        extern public string N();
    }

    public delegate JSObject F(int x, JSObject y);

    public static class TestDelegate
    {
        [Import(@"function(f, obj) { return f(3, obj).test; }")]
        extern public static string WithManagedDelegate(F f, JSObject obj);

        [Import(@"function() { return WithUnmanagedDelegate((function(i, obj) { return { test: i.toString() + "" "" + obj.test.toString() }; }), { test: 7 }); }")]
        extern public static string TestWithDelegate();

        [Export(Qualification = Qualification.None, MemberNameCasing = Casing.Pascal)]
        public static string WithUnmanagedDelegate(F f, JSObject obj)
        {
            return f(3, obj).GetField<string>("test");
        }
    }

    [Interop(State = InstanceState.JavaScriptOnly)]
    public class ProxiedBase
    {
        [Import("function() { TestGlobal = 42; }")]
        extern static ProxiedBase();

        [Import(MemberNameCasing = Casing.Exact)]
        extern public static int TestGlobal { get; }

#if false
        [Import("function() { return 43; }")]
        public static int NotAllowed()
        {
            return 42;
        }
#endif

        public ProxiedBase()
        {
            TestLogger.Log("ProxiedBase::.ctor()");
        }

        public ProxiedBase(JSContext ctxt)
        {
            TestLogger.Log("ProxiedBase::.ctor(JSContext)");
            TestLogger.Log(ctxt.GetField<int>("one"));
            TestLogger.Log(ctxt.GetField<string>("two"));
            TestLogger.Log(ctxt.GetField<JSObject>("three").GetField<int>("value"));
        }
    }

    public class ProxiedTest : ProxiedBase
    {
        [Import(@"function() { return { one: 1, two: ""two"", three: { value: 5 } }; }")]
        extern public ProxiedTest();

        [Import]
        extern public int One { get; set; }

        [Import]
        extern public string Two { get; set; }

        [Import]
        extern public JSObject Three { get; set; }

        // BROKEN: Polymorphic delegates
#if true
        [Import]
        extern public Action<int> Delegate { get; set; }
#endif

        [Import("function() { return this.three.value; }")]
        extern public int GetValue();

        [Import("function() { this.two = 2; }")]
        extern public void Morph();
    }

    [Interop(State = InstanceState.ManagedAndJavaScript, DefaultKey = "TheKey")]
    public class KeyedBase
    {
        public int I;
        public string S;

        public KeyedBase(JSContext ctxt, int i, string s)
        {
            TestLogger.Log("KeyedBase::.ctor()");
            // TestLogger.Log(ctxt.GetField<int>("TheKey"));
            TestLogger.Log(i);
            TestLogger.Log(s);
            I = i;
            S = s;
        }

        public KeyedBase(JSContext ctxt)
        {
            I = 0;
            S = "";
        }
    }

    public class KeyedTest : KeyedBase
    {
        [Import("function(i, s, x, y) { return { x: x, y: y }; }")]
        extern public KeyedTest(int i, string s, int x, string y);

        public KeyedTest(JSContext ctxt, int i, string s, int x, string y)
            : base(ctxt, i, s)
        {
            TestLogger.Log("KeyedTest::.ctor()");
            TestLogger.Log(x);
            TestLogger.Log(y);
        }

        public KeyedTest(JSContext ctxt) : base(ctxt)
        {
        }

        [Import]
        extern public int X { get; set; }

        [Import]
        extern public string Y { get; set; }

        [Import("function() { return { x: this.x, y: this.y }; }")]
        extern public KeyedTest Clone();

        [Import("function() { return this; }")]
        extern public KeyedTest PassThrough();

        [Import(@"function() { this.x = ""one""; }")]
        extern public void Morph();
    }

    public static class NullableTest
    {
        [Import("function(ni) { if (ni == null) return null; else return ni + 1; }")]
        extern public static int? Incr(int? ni);
    }

    public static class TestArray
    {
        [Import("function(arr) { arr[3] = 33; return [arr[0], arr[1], arr[2], arr[3]]; }")]
        extern public static int[] WithArray(int[] arr);
    }

    public class Normal
    {
        public int I;
        public string S;
    }

    public static class TestNormal
    {
        [Import("function(l, r) { return r; }")]
        extern public static object Right(object l, object r);
    }

    [Interop(DefaultKey = "id")]
    public class TestDefaultInstance
    {
        [Import("x")]
        public extern int X
        {
            get;
            set;
        }
    }

    [Export(InlineParamsArray = true)]
    public delegate string ParamsDelegate(int first, int second, params string[] rest);

    public static class TestParams
    {
        [Import(@"function(first, second) { var args = [first, second]; for (var i = 2; i < arguments.length; i++) args.push(arguments[i]); return ""["" + args.join("","") + ""]""; }", InlineParamsArray = true)]
        extern static public string JoinA(int first, int second, params string[] rest);

        [Export(InlineParamsArray = true)]
        static public string JoinB(int first, int second, params string[] rest)
        {
            var sb = new StringBuilder();
            sb.Append('[');
            sb.Append(first);
            sb.Append(',');
            sb.Append(second);
            for (var i = 0; i < rest.Length; i++)
            {
                sb.Append(',');
                sb.Append(rest[i]);
            }
            sb.Append(']');
            return sb.ToString();
        }

        [Import(@"function() { return joinB(3, 7, ""a"", ""b"", ""c""); }")]
        extern public static string TestB();

        [Import(@"function(f) { return f(3, 7, ""a"", ""b"", ""c""); }")]
        extern public static string Call(ParamsDelegate f);

        [Import(@"function() { return joinB; }")]
        extern public static ParamsDelegate GetTestB();
    }
}
