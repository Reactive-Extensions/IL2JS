////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////

using Microsoft.LiveLabs.JavaScript.Interop;

namespace System
{
    /// <summary>
    /// Represents a globally unique identifier (GUID).
    /// </summary>
    public struct Guid
    {
        private string value;

        /// <summary>
        /// A read-only instance of the System.Guid class whose value is guaranteed to
        //  be all zeros.
        /// </summary>
        public static readonly Guid Empty = new Guid("00000000-0000-0000-0000-000000000000");

        public Guid(byte[] bytes) { throw new NotSupportedException(); }

        public Guid(string g)
        {
            this.value = g;
        }

        public static Guid Parse(string input)
        {
            if (input == null)
            {
                throw new Exception("input");
            }
            if(input.Length != Guid.Empty.value.Length)
            {
                throw new Exception("Unrecognized Guid format");
            }            
            return new Guid(input);
        }

        [Import(@"function() { return (((1+Math.random()) * 0x10000) |0).toString(16).substring(1); }")]
        extern private static string S4();

        /// <summary>
        /// NEED TO SUPPORT SOME SORT OF NEWGUID MAYBE? We could theoretically fix up the Guids on the Server later
        /// </summary>
        /// <returns>A New Guid (Randomly generated for now)</returns>
        public static Guid NewGuid()
        {
            var value = string.Format("ABCDABCD-{0}-{1}-{2}-{3}{4}{5}", S4(), S4(), S4(), S4(), S4(), S4());
            return new Guid(value);
        }

        public override string ToString()
        {
            return this.value;
        }

        public static bool operator !=(Guid a, Guid b)
        {
            return !(a == b);
        }

        public static bool operator ==(Guid a, Guid b)
        {
            return a.value == b.value;            
        }

        public bool Equals(Guid g)
        {
            return this.value == g.value;
        }

        public override bool Equals(object o)
        {
            if ((o == null) || !(o is Guid))
            {
                return false;
            }
            return this.Equals((Guid)o);
        }

        public override int GetHashCode()
        {
            return this.value.GetHashCode();
        }

        public byte[] ToByteArray()
        {
            throw new NotSupportedException();
        }
    }
}