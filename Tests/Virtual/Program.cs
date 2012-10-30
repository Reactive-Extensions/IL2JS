using Microsoft.LiveLabs.JavaScript.IL2JS;

namespace Microsoft.LiveLabs.JavaScript.Tests
{
    static class TestVirtual
    {
        static void Main()
        {
            {
                TestLogger.Log("Testing basic interface methods...");
                ISimple s1 = new CSimple();
                s1.S();
                ISimple s2 = new SSimple();
                s2.S();
                ISimple s3 = new CDerived1();
                s3.S();
                ISimple s4 = new CDerived2();
                s4.S();
                ISimple s5 = new CDerived3();
                s5.S();
            }

            {
                TestLogger.Log("Testing virtuals...");
                object c = new CChild();
                var ia = c as IA;

                var d = c as Dummy;
                if (d != null)
                    TestLogger.Log("Invalid casting");

                TestLogger.Log(5.ToString());
                TestLogger.Log(ia.A());
                TestLogger.Log(((CA)c).Virtual());
                TestLogger.Log(((CChild)c).VirtualDontOverride());
                TestLogger.Log(((IB)c).A());
                TestLogger.Log(((IAB)c).B());
                TestLogger.Log((new CGrandChild()).Virtual());
                // TestLogger.Log(((IAB)c).A()); // ambiguous at compile-time
            }

            {
                TestLogger.Log("Testing constrained call on 'naked' type parameter...");
                var cic = new ConstrainedInterface<CSimple>();
                cic.M(new CSimple());
                var cis = new ConstrainedInterface<SSimple>();
                cis.M(new SSimple());
                var cc = new ConstrainedClass<CSimple>();
                cc.M(new CSimple());
                var cs = new ConstrainedStruct<SSimple>();
                cs.M(new SSimple());
                var kc = new KnownClass();
                kc.M(new CSimple());
                var ks = new KnownStruct();
                ks.M(new SSimple());
            }

            {
                TestLogger.Log("Testing implicit interface implementations...");
                var b = new BaseImplicit();
                var d = new DerivedImplicit();
                ((IImplicit)b).M(1);
                ((IImplicit)b).M(false);
                ((IImplicit)b).M("three");
                ((IImplicit)b).M(4.0f);
                ((IImplicit)d).M(2);
                ((IImplicit)d).M(true);
                ((IImplicit)d).M("five");
                ((IImplicit)d).M(6.0f);
            }
        }
    }

    interface ISimple
    {
        void S();
    }

    [Reflection(ReflectionLevel.Names)]
    class CSimple : ISimple
    {
        public void S() { TestLogger.Log("CSimple::S"); }
    }

    [Reflection(ReflectionLevel.Names)]
    struct SSimple : ISimple
    {
        public void S() { TestLogger.Log("SSimple::S"); }
    }

    class CDerived1 : CSimple { }

    class CDerived2 : CSimple
    {
        public new void S()
        {
            TestLogger.Log("CDerived2::S");
        }
    }

    class CDerived3 : CSimple, ISimple
    {
        public new void S()
        {
            TestLogger.Log("CDerived3::S");
        }
    }

    interface IBase
    {
        string Base();
    }

    interface IA : IBase
    {
        string A();
    }

    interface IB : IBase
    {
        string A();
        string B();
    }

    interface IAB : IA, IB
    {
        string AB(int a, string b);
    }

    class CA : IA
    {
        public string Base() { return "CA::Base"; }

        public string A() { return "CA::A"; }

        public virtual string Virtual() { return "CA::Virtual"; }

        public virtual string VirtualDontOverride() { return "CA::VirtualDontOverride"; }

    }

    class Dummy { }

    class CChild : CA, IAB
    {
        override public string Virtual() { return "CChild.Virtual"; }

        public string B() { return "CChild::B"; }

        string IB.A() { return "CChild::IB.A"; }

        public string AB(int a, string b) { return "CChild::AB"; }
    }

    class CGrandChild : CChild
    {
        override public string Virtual() { return "CGrandChild::Virtual"; }
    }

    class ConstrainedInterface<T> where T : ISimple
    {
        public void M(T t)
        {
            t.S();
            TestLogger.Log(t.ToString());
            TestLogger.Log(t.GetType().Name);
        }
    }

    class ConstrainedClass<T> where T : class, ISimple
    {
        public void M(T t)
        {
            t.S();
            TestLogger.Log(t.ToString());
            TestLogger.Log(t.GetType().Name);
        }
    }

    class ConstrainedStruct<T> where T : struct, ISimple
    {
        public void M(T t)
        {
            t.S();
            TestLogger.Log(t.ToString());
            TestLogger.Log(t.GetType().Name);
        }
    }

    class KnownClass
    {
        public void M(CSimple t)
        {
            t.S();
            TestLogger.Log(t.ToString());
            TestLogger.Log(t.GetType().Name);
        }
    }

    class KnownStruct
    {
        public void M(SSimple t)
        {
            t.S();
            TestLogger.Log(t.ToString());
            TestLogger.Log(t.GetType().Name);
        }
    }

    interface IImplicit {
        void M(int i);
        void M(bool b);
        void M(string s);
        void M(float f);
    }

    class BaseImplicit : IImplicit
    {
        void IImplicit.M(int i)
        {
            TestLogger.Log("BaseImplicit::Implicit.M(int)");
        }

        void IImplicit.M(bool b)
        {
            TestLogger.Log("BaseImplicit::Implicit.M(bool)");
        }

        public void M(string s)
        {
            TestLogger.Log("BaseImplicit::M(string)");
        }

        public void M(float f)
        {
            TestLogger.Log("BaseImplicit::M(float)");
        }
    }

    class DerivedImplicit : BaseImplicit, IImplicit
    {
        void IImplicit.M(bool b)
        {
            TestLogger.Log("DerivedImplicit::Implicit.M(bool)");
        }

        public new void M(string s)
        {
            TestLogger.Log("DerivedImplicit::M(string)");
        }
    }
}