using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using Microsoft.LiveLabs.JavaScript.IL2JS;

namespace Microsoft.LiveLabs.JavaScript.Tests
{
    static class TestReflection
    {
        // Normalize assembly public keys without using regexp
        static string NA(string name)
        {
            var i = name.IndexOf(',');
            if (i >= 0)
                name = name.Substring(0, i);

            var replaceA = "mscorlib_il2js";
            var zapA = "il2js";
            var zapB = "wsh";
            var sb = new StringBuilder();
            i = 0;
            while (i < name.Length)
            {
                if (i + zapA.Length <= name.Length && name.Substring(i, zapA.Length).Equals(zapA, StringComparison.OrdinalIgnoreCase))
                {
                    sb.Append("<target>");
                    i += zapA.Length;
                }
                else if (i + zapB.Length <= name.Length && name.Substring(i, zapB.Length).Equals(zapB, StringComparison.OrdinalIgnoreCase))
                {
                    sb.Append("<target>");
                    i += zapB.Length;
                }
                else if (i + replaceA.Length <= name.Length && name.Substring(i, replaceA.Length).Equals(replaceA, StringComparison.OrdinalIgnoreCase))
                {
                    sb.Append("mscorlib");
                    i += replaceA.Length;
                }
                else
                    sb.Append(name[i++]);
            }
            return sb.ToString();
        }

        static void Main()
        {
            {
                TestLogger.Log("Testing names...");
                TestLogger.Log(typeof(DerivedClass).Name);
                TestLogger.Log(typeof(DerivedClass).Namespace);
                TestLogger.Log(typeof(DerivedClass).FullName);
            }

            {
                TestLogger.Log("Testing Invoke...");
                typeof(BaseClass).GetMethod("Test").Invoke(new BaseClass(), null);
                typeof(BaseClass).GetMethod("Test").Invoke(new DerivedClass(), null);
                try
                {
                    typeof(DerivedClass).GetMethod("Test").Invoke(new BaseClass(), null);
                }
                catch (Exception e)
                {
                    TestLogger.LogException(e);
                }
                typeof(DerivedClass).GetMethod("Test").Invoke(new DerivedClass(), null);
            }

            {
                TestLogger.Log("Testing SetValue on value type...");
                var b = new DerivedClass();
                typeof(DerivedClass).GetProperty("A").SetValue(b, 5, null);
                TestLogger.Log(b.A);
                TestLogger.Log(typeof(DerivedClass).GetProperty("A").GetValue(b, null).ToString());
            }

            {
                TestLogger.Log("Testing SetValue on reference type...");
                var b = new DerivedClass();
                typeof(DerivedClass).GetProperty("B").SetValue(b, "test", null);
                TestLogger.Log(b.B);
                TestLogger.Log(typeof(DerivedClass).GetProperty("B").GetValue(b, null).ToString());
            }

            {
                TestLogger.Log("Testing generic construction...");
                var i = GenericNew<TestClass>();
                i.Method();
            }

            {
                TestLogger.Log("Testing assembly loading...");
#if IL2JS
                // Assuming assembly name resolution is 'Name'
                var assembly = Assembly.Load("IL2JS_Tests_ReflectionBase_IL2JS");
#else
                var assembly = Assembly.Load("IL2JS_Tests_ReflectionBase_WSH, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
#endif
                TestLogger.Log(NA(assembly.FullName));
                var type = assembly.GetType("Microsoft.LiveLabs.JavaScript.Tests.BaseClass");
                var instance = (BaseClass)Activator.CreateInstance(type);
                instance.Test();
            }

            // DISABLED: Only types with [Reflection(ReflectionLevel.Name)] or greater support names
#if false
            {
                TestLogger.Log("Testing names...");
                Type tp;

                tp = typeof(System.Object);
                TestLogger.Log("System.Object: tp.Name: " + tp.Name);
                TestLogger.Log("System.Object: tp.Namespace: " + tp.Namespace);
                TestLogger.Log("System.Object: tp.FullName: " + NA(tp.FullName));

                tp = typeof(System.String);
                TestLogger.Log("System.String: tp.Name: " + tp.Name);
                TestLogger.Log("System.String: tp.Namespace: " + tp.Namespace);
                TestLogger.Log("System.String: tp.FullName: " + NA(tp.FullName));

                tp = typeof(TestClass);
                TestLogger.Log("TestClass: tp.Name: " + tp.Name);
                TestLogger.Log("TestClass: tp.Namespace: " + tp.Namespace);
                TestLogger.Log("TestClass: tp.FullName: " + NA(tp.FullName));

                tp = typeof(TestClass[]);
                TestLogger.Log("TestClass[]: tp.Name: " + tp.Name);
                TestLogger.Log("TestClass[]: tp.Namespace: " + tp.Namespace);
                TestLogger.Log("TestClass[]: tp.FullName: " + NA(tp.FullName));

                tp = typeof(int?);
                TestLogger.Log("int?: tp.Name: " + tp.Name);
                TestLogger.Log("int?: tp.Namespace: " + tp.Namespace);
                TestLogger.Log("int?: tp.FullName: " + NA(tp.FullName));

                tp = typeof(int?[]);
                TestLogger.Log("int?[]: tp.Name: " + tp.Name);
                TestLogger.Log("int?[]: tp.Namespace: " + tp.Namespace);
                TestLogger.Log("int?[]: tp.FullName: " + NA(tp.FullName));

                tp = typeof(int[][]);
                TestLogger.Log("int[][]: tp.Name: " + tp.Name);
                TestLogger.Log("int[][]: tp.Namespace: " + tp.Namespace);
                TestLogger.Log("int[][]: tp.FullName: " + NA(tp.FullName));

                tp = typeof(TestStruct?);
                TestLogger.Log("TestStruct?: tp.Name: " + tp.Name);
                TestLogger.Log("TestStruct?: tp.Namespace: " + tp.Namespace);
                TestLogger.Log("TestStruct?: tp.FullName: " + NA(tp.FullName));

                tp = typeof(List<TestClass>[]);
                TestLogger.Log("List<TestClass>[]: tp.Name: " + tp.Name);
                TestLogger.Log("List<TestClass>[]: tp.Namespace: " + tp.Namespace);
                TestLogger.Log("List<TestClass>[]: tp.FullName: " + NA(tp.FullName));

                tp = typeof(OuterClassNonGeneric.NestedClassNonGeneric);
                TestLogger.Log("OuterClassNonGeneric.NestedClassNonGeneric: tp.Name: " + tp.Name);
                TestLogger.Log("OuterClassNonGeneric.NestedClassNonGeneric: tp.Namespace: " + tp.Namespace);
                TestLogger.Log("OuterClassNonGeneric.NestedClassNonGeneric: tp.FullName: " + NA(tp.FullName));

                tp = typeof(OuterClassNonGeneric.NestedClassGeneric<int>);
                TestLogger.Log("OuterClassNonGeneric.NestedClassGeneric<int>: tp.Name: " + tp.Name);
                TestLogger.Log("OuterClassNonGeneric.NestedClassGeneric<int>: tp.Namespace: " + tp.Namespace);
                TestLogger.Log("OuterClassNonGeneric.NestedClassGeneric<int>: tp.FullName: " + NA(tp.FullName));

                tp = typeof(OuterClassGeneric<string>.NestedClassNonGeneric);
                TestLogger.Log("OuterClassGeneric<string>.NestedClassNonGeneric: tp.Name: " + tp.Name);
                TestLogger.Log("OuterClassGeneric<string>.NestedClassNonGeneric: tp.Namespace: " + tp.Namespace);
                TestLogger.Log("OuterClassGeneric<string>.NestedClassNonGeneric: tp.FullName: " + NA(tp.FullName));

                tp = typeof(OuterClassGeneric<string>.NestedClassGeneric<List<string>>);
                TestLogger.Log("OuterClassGeneric<string>.NestedClassGeneric<List<string>>: tp.Name: " + tp.Name);
                TestLogger.Log("OuterClassGeneric<string>.NestedClassGeneric<List<string>>: tp.Namespace: " + tp.Namespace);
                TestLogger.Log("OuterClassGeneric<string>.NestedClassGeneric<List<string>>: tp.FullName: " + NA(tp.FullName));

                tp = typeof(OuterClassGeneric<List<int>[]>.NestedClassGeneric<List<string>[]>[]);
                TestLogger.Log("OuterClassGeneric<List<int>[]>.NestedClassGeneric<List<string>[]>[]: tp.Name: " + tp.Name);
                TestLogger.Log("OuterClassGeneric<List<int>[]>.NestedClassGeneric<List<string>[]>[]: tp.Namespace: " + tp.Namespace);
                TestLogger.Log("OuterClassGeneric<List<int>[]>.NestedClassGeneric<List<string>[]>[]: tp.FullName: " + NA(tp.FullName));
            }
#endif

            {
                TestLogger.Log("Testing custom attributes...");
                foreach (var o in typeof(TestClass).GetCustomAttributes(false))
                {
                    TestLogger.Log(o.GetType().Name);
                    var ta = o as TestAttribute;
                    if (ta != null)
                    {
                        TestLogger.Log(ta.One);
                        TestLogger.Log(ta.Two);
                    }
                }
            }

            {
                TestLogger.Log("Testing GetFields...");
                foreach (var f in typeof(DerivedClass).GetFields())
                {
                    TestLogger.Log(f.Name);
                }
                TestLogger.Log("Testing GetMethods...");
                var nms = new List<string>();
                foreach (var m in typeof(DerivedClass).GetMethods())
                {
                    // Object is not marked as [Reflection(ReflectionLevel.Full)]
                    if (!m.DeclaringType.IsAssignableFrom(typeof(Object)))
                    {
                        nms.Add(m.Name);
                    }
                }
                nms.Sort((l, r) => string.Compare(l, r, StringComparison.Ordinal));
                foreach (var nm in nms)
                    TestLogger.Log(nm);
                TestLogger.Log("Testing GetProperties...");
                foreach (var p in typeof(DerivedClass).GetProperties())
                    TestLogger.Log(p.Name);
                TestLogger.Log("Testing GetEvents...");
                foreach (var e in typeof(DerivedClass).GetEvents())
                    TestLogger.Log(e.Name);
            }
        }

        public static T GenericNew<T>() where T : new()
        {
            return new T();
        }
    }

    public class OuterClassNonGeneric
    {
        public class NestedClassNonGeneric
        {
        }

        public class NestedClassGeneric<T>
        {
        }
    }

    public class OuterClassGeneric<S>
    {
        public class NestedClassNonGeneric
        {
        }

        public class NestedClassGeneric<T>
        {
        }
    }

    public struct TestStruct
    {
        public TestStruct(string value)
        {
            Value = value;
        }

        public string Value;
    }

    public class TestAttribute : Attribute
    {
        public int One;

        public string Two { get; set; }

        public TestAttribute(int one)
        {
            One = one;
        }
    }

    [Test(1, Two = "two")]
    [Reflection(ReflectionLevel.Full)]
    public class TestClass
    {
        string a = "failed";

        public void Method()
        {
            TestLogger.Log(a);
        }

        public TestClass()
        {
            TestLogger.Log("In TestClass constructor");
            a = "success";
        }
    }

    public class DerivedClass : BaseClass
    {
        public override void Test()
        {
            TestLogger.Log("DerivedClass::Test");
        }

        public int A { get; set; }

        public string B { get; set; }
    }
}