using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace ReactiveTests
{
    [DebuggerDisplay("({Subscribe}, {Unsubscribe})")]
    public struct Subscription : IEquatable<Subscription>
    {
        public const ushort Infinite = ushort.MaxValue;

        public ushort Subscribe;
        public ushort Unsubscribe;

        public Subscription(ushort subscribe)
        {
            Subscribe = subscribe;
            Unsubscribe = Infinite;
        }

        public Subscription(ushort subscribe, ushort unsubscribe)
        {
            Subscribe = subscribe;
            Unsubscribe = unsubscribe;
        }

        public bool Equals(Subscription other)
        {
            return Subscribe == other.Subscribe && Unsubscribe == other.Unsubscribe;
        }

        public override bool Equals(object obj)
        {
            if (obj is Subscription)
                return Equals((Subscription)obj);
            return false;
        }

        public override int GetHashCode()
        {
            return Subscribe + Unsubscribe;
        }

        public override string ToString()
        {
            return string.Format("({0}, {1})", Subscribe, Unsubscribe);
        }
    }
}
