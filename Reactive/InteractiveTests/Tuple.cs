using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.LiveLabs.CoreExTests
{

    [Serializable]
    class Tuple<TFirst, TSecond>
    {
        public TFirst First
        {
            get;
            private set;
        }

        public TSecond Second
        {
            get;
            private set;
        }

        public Tuple(TFirst first, TSecond second)
        {
            First = first;
            Second = second;
        }
    }

    static class Tuple
    {
        public static Tuple<TFirst, TSecond> Create<TFirst, TSecond>(TFirst first, TSecond second)
        {
            return new Tuple<TFirst, TSecond>(first, second);
        }
    }
}
