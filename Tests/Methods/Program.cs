namespace Microsoft.LiveLabs.JavaScript.Tests
{
    static class Program
    {
        static void Main()
        {
            var b = new Base("A");
            var d = new Derived("B", "C");

            TestLogger.Log("Static method calls...");
            TestLogger.Log(Base.S("D")); // "DBS"
            TestLogger.Log(Derived.S("E")); // "EDS"
            TestLogger.Log("Instance method calls...");
            TestLogger.Log(b.I("F")); // "AFBI";
            TestLogger.Log(d.I("G")); // "BCGDI";
            TestLogger.Log("Virtual method calls...");
            TestLogger.Log(b.V("H")); // "AHBV";
            TestLogger.Log(((Base)d).V("I")); // "BCIDV";
            TestLogger.Log("Interface method calls...");
            TestLogger.Log(((IMN)b).M("J")); // "AJBM";
            TestLogger.Log(((IMN)d).M("K")); // "BCKDM";
            TestLogger.Log(((IOP)b).O("L")); // "ALBO";
            TestLogger.Log(((IOP)d).O("M")); // "BMBO";
            TestLogger.Log("Virtual interface method calls...");
            TestLogger.Log(((IMN)b).N("N")); // "ANBN";
            TestLogger.Log(((IMN)d).N("O")); // "BCODN";
            TestLogger.Log(((IOP)b).P("P")); // "APBP";
            TestLogger.Log(((IOP)d).P("Q")); // "BCQDP";
        }
    }

    public interface IMN
    {
        string M(string x);
        string N(string x);
    }

    public interface IOP
    {
        string O(string x);
        string P(string x);
    }

    public class Base : IMN, IOP
    {
        public string f;

        public Base(string f)
        {
            this.f = f;
        }

        public static string S(string x)
        {
            return x + "BS";
        }

        public string I(string x)
        {
            return f + x + "BI";
        }

        public virtual string V(string x)
        {
            return f + x + "BV";
        }

        public string M(string x)
        {
            return f + x + "BM";
        }

        public virtual string N(string x)
        {
            return f + x + "BN";
        }

        public string O(string x)
        {
            return f + x + "BO";
        }

        public virtual string P(string x)
        {
            return f + x + "BP";
        }
    }

    public class Derived : Base, IMN
    {
        public string g;

        public Derived(string f, string g)
            : base(f)
        {
            this.g = g;
        }

        public static new string S(string x)
        {
            return x + "DS";
        }

        public new string I(string x)
        {
            return f + g + x + "DI";
        }

        public override string V(string x)
        {
            return f + g + x + "DV";
        }

        string IMN.M(string x)
        {
            return f + g + x + "DM";
        }

        public override string N(string x)
        {
            return f + g + x + "DN";
        }

        public override string P(string x)
        {
            return f + g + x + "DP";
        }
    }
}