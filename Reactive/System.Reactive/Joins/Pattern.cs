using System;

namespace
#if WM7
Microsoft.Windows.Phone
#endif
Reactive.Joins
{
    /// <summary>
    /// Represents a join pattern.
    /// </summary>
    public abstract class Pattern
    {
        internal Pattern()
        {
        }
    }

    /// <summary>
    /// Represents a join pattern.
    /// </summary>
    internal class Pattern<T1> : Pattern
    {
        internal IObservable<T1> First { get; private set; }

        internal Pattern(IObservable<T1> first)
        {
            First = first;
        }

        /// <summary>
        /// Matches when all observable sequences have an available value and projects the values.
        /// </summary>
        public Plan<TResult> Then<TResult>(Func<T1, TResult> selector)
        {
            return new Plan<T1, TResult>(this, selector);
        }
    }

    /// <summary>
    /// Represents a join pattern.
    /// </summary>
    public class Pattern<T1, T2> : Pattern
    {
        internal IObservable<T1> First { get; private set; }

        internal IObservable<T2> Second { get; private set; }

        internal Pattern(IObservable<T1> first, IObservable<T2> second)
        {
            First = first;
            Second = second;
        }

        /// <summary>
        /// Matches when all observable sequences have an available value.
        /// </summary>
        public Pattern<T1, T2, T3> And<T3>(IObservable<T3> other)
        {
            return new Pattern<T1, T2, T3>(First, Second, other);
        }

        /// <summary>
        /// Matches when all observable sequences have an available value and projects the values.
        /// </summary>
        public Plan<TResult> Then<TResult>(Func<T1, T2, TResult> selector)
        {
            return new Plan<T1, T2, TResult>(this, selector);
        }
    }

    /// <summary>
    /// Represents a join pattern.
    /// </summary>
    public class Pattern<T1, T2, T3> : Pattern
    {
        internal IObservable<T1> First { get; private set; }

        internal IObservable<T2> Second { get; private set; }

        internal IObservable<T3> Third { get; private set; }

        internal Pattern(IObservable<T1> first, IObservable<T2> second, IObservable<T3> third)
        {
            First = first;
            Second = second;
            Third = third;
        }

        /// <summary>
        /// Matches when all observable sequences have an available value.
        /// </summary>
        public Pattern<T1, T2, T3, T4> And<T4>(IObservable<T4> other)
        {
            return new Pattern<T1, T2, T3, T4>(First, Second, Third, other);
        }

        /// <summary>
        /// Matches when all observable sequences have an available value and projects the values.
        /// </summary>
        public Plan<TResult> Then<TResult>(Func<T1, T2, T3, TResult> selector)
        {
            return new Plan<T1, T2, T3, TResult>(this, selector);
        }
    }

    /// <summary>
    /// Represents a join pattern.
    /// </summary>
    public class Pattern<T1, T2, T3, T4> : Pattern
    {
        internal IObservable<T1> First { get; private set; }

        internal IObservable<T2> Second { get; private set; }

        internal IObservable<T3> Third { get; private set; }

        internal IObservable<T4> Fourth { get; private set; }

        internal Pattern(IObservable<T1> first, IObservable<T2> second, IObservable<T3> third,
                         IObservable<T4> fourth)
        {
            First = first;
            Second = second;
            Third = third;
            Fourth = fourth;
        }

#if DESKTOPCLR20 || DESKTOPCLR40
        /// <summary>
        /// Matches when all observable sequences have an available value.
        /// </summary>
        public Pattern<T1, T2, T3, T4, T5> And<T5>(IObservable<T5> other)
        {
            return new Pattern<T1, T2, T3, T4, T5>(First, Second, Third, Fourth, other);
        }
#endif

        /// <summary>
        /// Matches when all observable sequences have an available value and projects the values.
        /// </summary>
        public Plan<TResult> Then<TResult>(Func<T1, T2, T3, T4, TResult> selector)
        {
            return new Plan<T1, T2, T3, T4, TResult>(this, selector);
        }
    }
#if DESKTOPCLR20 || DESKTOPCLR40
    /// <summary>
    /// Represents a join pattern.
    /// </summary>    
    public class Pattern<T1, T2, T3, T4, T5> : Pattern
    {
        internal IObservable<T1> First { get; private set; }

        internal IObservable<T2> Second { get; private set; }

        internal IObservable<T3> Third { get; private set; }

        internal IObservable<T4> Fourth { get; private set; }
        
        internal IObservable<T5> Fifth { get; private set; }

        internal Pattern(IObservable<T1> first, IObservable<T2> second, IObservable<T3> third,
                         IObservable<T4> fourth, IObservable<T5> fifth)
        {
            First = first;
            Second = second;
            Third = third;
            Fourth = fourth;
            Fifth = fifth;
        }

        /// <summary>
        /// Matches when all observable sequences have an available value.
        /// </summary>
        public Pattern<T1, T2, T3, T4, T5, T6> And<T6>(IObservable<T6> other)
        {
            return new Pattern<T1, T2, T3, T4, T5, T6>(First, Second, Third, Fourth, Fifth, other);
        }

        /// <summary>
        /// Matches when all observable sequences have an available value and projects the values.
        /// </summary>
        public Plan<TResult> Then<TResult>(Func<T1, T2, T3, T4, T5, TResult> selector)
        {
            return new Plan<T1, T2, T3, T4, T5, TResult>(this, selector);
        }
    }

    /// <summary>
    /// Represents a join pattern.
    /// </summary>    
    public class Pattern<T1, T2, T3, T4, T5, T6> : Pattern
    {
        internal IObservable<T1> First { get; private set; }

        internal IObservable<T2> Second { get; private set; }

        internal IObservable<T3> Third { get; private set; }

        internal IObservable<T4> Fourth { get; private set; }

        internal IObservable<T5> Fifth { get; private set; }

        internal IObservable<T6> Sixth { get; private set; }

        internal Pattern(IObservable<T1> first, IObservable<T2> second, IObservable<T3> third,
                         IObservable<T4> fourth, IObservable<T5> fifth, IObservable<T6> sixth)
        {
            First = first;
            Second = second;
            Third = third;
            Fourth = fourth;
            Fifth = fifth;
            Sixth = sixth;
        }

        /// <summary>
        /// Matches when all observable sequences have an available value.
        /// </summary>
        public Pattern<T1, T2, T3, T4, T5, T6, T7> And<T7>(IObservable<T7> other)
        {
            return new Pattern<T1, T2, T3, T4, T5, T6, T7>(First, Second, Third, Fourth, Fifth, Sixth, other);
        }

        /// <summary>
        /// Matches when all observable sequences have an available value and projects the values.
        /// </summary>
        public Plan<TResult> Then<TResult>(Func<T1, T2, T3, T4, T5, T6, TResult> selector)
        {
            return new Plan<T1, T2, T3, T4, T5, T6, TResult>(this, selector);
        }
    }

    /// <summary>
    /// Represents a join pattern.
    /// </summary>    
    public class Pattern<T1, T2, T3, T4, T5, T6, T7> : Pattern
    {
        internal IObservable<T1> First { get; private set; }

        internal IObservable<T2> Second { get; private set; }

        internal IObservable<T3> Third { get; private set; }

        internal IObservable<T4> Fourth { get; private set; }

        internal IObservable<T5> Fifth { get; private set; }

        internal IObservable<T6> Sixth { get; private set; }

        internal IObservable<T7> Seventh { get; private set; }

        internal Pattern(IObservable<T1> first, IObservable<T2> second, IObservable<T3> third,
                         IObservable<T4> fourth, IObservable<T5> fifth, IObservable<T6> sixth,
                         IObservable<T7> seventh)
        {
            First = first;
            Second = second;
            Third = third;
            Fourth = fourth;
            Fifth = fifth;
            Sixth = sixth;
            Seventh = seventh;
        }

        /// <summary>
        /// Matches when all observable sequences have an available value.
        /// </summary>
        public Pattern<T1, T2, T3, T4, T5, T6, T7, T8> And<T8>(IObservable<T8> other)
        {
            return new Pattern<T1, T2, T3, T4, T5, T6, T7, T8>(First, Second, Third, Fourth, Fifth, Sixth, Seventh, other);
        }

        /// <summary>
        /// Matches when all observable sequences have an available value and projects the values.
        /// </summary>
        public Plan<TResult> Then<TResult>(Func<T1, T2, T3, T4, T5, T6, T7, TResult> selector)
        {
            return new Plan<T1, T2, T3, T4, T5, T6, T7, TResult>(this, selector);
        }
    }

    /// <summary>
    /// Represents a join pattern.
    /// </summary>    
    public class Pattern<T1, T2, T3, T4, T5, T6, T7, T8> : Pattern
    {
        internal IObservable<T1> First { get; private set; }

        internal IObservable<T2> Second { get; private set; }

        internal IObservable<T3> Third { get; private set; }

        internal IObservable<T4> Fourth { get; private set; }

        internal IObservable<T5> Fifth { get; private set; }

        internal IObservable<T6> Sixth { get; private set; }

        internal IObservable<T7> Seventh { get; private set; }

        internal IObservable<T8> Eighth { get; private set; }

        internal Pattern(IObservable<T1> first, IObservable<T2> second, IObservable<T3> third,
                         IObservable<T4> fourth, IObservable<T5> fifth, IObservable<T6> sixth,
                         IObservable<T7> seventh, IObservable<T8> eighth)
        {
            First = first;
            Second = second;
            Third = third;
            Fourth = fourth;
            Fifth = fifth;
            Sixth = sixth;
            Seventh = seventh;
            Eighth = eighth;
        }

        /// <summary>
        /// Matches when all observable sequences have an available value.
        /// </summary>
        public Pattern<T1, T2, T3, T4, T5, T6, T7, T8, T9> And<T9>(IObservable<T9> other)
        {
            return new Pattern<T1, T2, T3, T4, T5, T6, T7, T8, T9>(First, Second, Third, Fourth, Fifth, Sixth, Seventh, Eighth, other);
        }

        /// <summary>
        /// Matches when all observable sequences have an available value and projects the values.
        /// </summary>
        public Plan<TResult> Then<TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> selector)
        {
            return new Plan<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(this, selector);
        }
    }

    /// <summary>
    /// Represents a join pattern.
    /// </summary>    
    public class Pattern<T1, T2, T3, T4, T5, T6, T7, T8, T9> : Pattern
    {
        internal IObservable<T1> First { get; private set; }

        internal IObservable<T2> Second { get; private set; }

        internal IObservable<T3> Third { get; private set; }

        internal IObservable<T4> Fourth { get; private set; }

        internal IObservable<T5> Fifth { get; private set; }

        internal IObservable<T6> Sixth { get; private set; }

        internal IObservable<T7> Seventh { get; private set; }

        internal IObservable<T8> Eighth { get; private set; }

        internal IObservable<T9> Ninth { get; private set; }

        internal Pattern(IObservable<T1> first, IObservable<T2> second, IObservable<T3> third,
                         IObservable<T4> fourth, IObservable<T5> fifth, IObservable<T6> sixth,
                         IObservable<T7> seventh, IObservable<T8> eighth, IObservable<T9> ninth)
        {
            First = first;
            Second = second;
            Third = third;
            Fourth = fourth;
            Fifth = fifth;
            Sixth = sixth;
            Seventh = seventh;
            Eighth = eighth;
            Ninth = ninth;
        }

        /// <summary>
        /// Matches when all observable sequences have an available value.
        /// </summary>
        public Pattern<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> And<T10>(IObservable<T10> other)
        {
            return new Pattern<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(First, Second, Third, Fourth, Fifth, Sixth, Seventh, Eighth, Ninth, other);
        }

        /// <summary>
        /// Matches when all observable sequences have an available value and projects the values.
        /// </summary>
        public Plan<TResult> Then<TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> selector)
        {
            return new Plan<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(this, selector);
        }
    }

    /// <summary>
    /// Represents a join pattern.
    /// </summary>    
    public class Pattern<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : Pattern
    {
        internal IObservable<T1> First { get; private set; }

        internal IObservable<T2> Second { get; private set; }

        internal IObservable<T3> Third { get; private set; }

        internal IObservable<T4> Fourth { get; private set; }

        internal IObservable<T5> Fifth { get; private set; }

        internal IObservable<T6> Sixth { get; private set; }

        internal IObservable<T7> Seventh { get; private set; }

        internal IObservable<T8> Eighth { get; private set; }

        internal IObservable<T9> Ninth { get; private set; }

        internal IObservable<T10> Tenth { get; private set; }

        internal Pattern(IObservable<T1> first, IObservable<T2> second, IObservable<T3> third,
                         IObservable<T4> fourth, IObservable<T5> fifth, IObservable<T6> sixth,
                         IObservable<T7> seventh, IObservable<T8> eighth, IObservable<T9> ninth,
                         IObservable<T10> tenth)
        {
            First = first;
            Second = second;
            Third = third;
            Fourth = fourth;
            Fifth = fifth;
            Sixth = sixth;
            Seventh = seventh;
            Eighth = eighth;
            Ninth = ninth;
            Tenth = tenth;
        }

        /// <summary>
        /// Matches when all observable sequences have an available value.
        /// </summary>
        public Pattern<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> And<T11>(IObservable<T11> other)
        {
            return new Pattern<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
                First, Second, Third, Fourth, Fifth, Sixth, Seventh, Eighth, Ninth, Tenth, other);
        }

        /// <summary>
        /// Matches when all observable sequences have an available value and projects the values.
        /// </summary>
        public Plan<TResult> Then<TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> selector)
        {
            return new Plan<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(this, selector);
        }
    }

    /// <summary>
    /// Represents a join pattern.
    /// </summary>    
    public class Pattern<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> : Pattern
    {
        internal IObservable<T1> First { get; private set; }

        internal IObservable<T2> Second { get; private set; }

        internal IObservable<T3> Third { get; private set; }

        internal IObservable<T4> Fourth { get; private set; }

        internal IObservable<T5> Fifth { get; private set; }

        internal IObservable<T6> Sixth { get; private set; }

        internal IObservable<T7> Seventh { get; private set; }

        internal IObservable<T8> Eighth { get; private set; }

        internal IObservable<T9> Ninth { get; private set; }

        internal IObservable<T10> Tenth { get; private set; }

        internal IObservable<T11> Eleventh { get; private set; }

        internal Pattern(IObservable<T1> first, IObservable<T2> second, IObservable<T3> third,
                         IObservable<T4> fourth, IObservable<T5> fifth, IObservable<T6> sixth,
                         IObservable<T7> seventh, IObservable<T8> eighth, IObservable<T9> ninth,
                         IObservable<T10> tenth, IObservable<T11> eleventh)
        {
            First = first;
            Second = second;
            Third = third;
            Fourth = fourth;
            Fifth = fifth;
            Sixth = sixth;
            Seventh = seventh;
            Eighth = eighth;
            Ninth = ninth;
            Tenth = tenth;
            Eleventh = eleventh;
        }

        /// <summary>
        /// Matches when all observable sequences have an available value.
        /// </summary>
        public Pattern<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> And<T12>(IObservable<T12> other)
        {
            return new Pattern<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
                First, Second, Third, Fourth, Fifth, Sixth, Seventh, Eighth, Ninth, Tenth, Eleventh, other);
        }

        /// <summary>
        /// Matches when all observable sequences have an available value and projects the values.
        /// </summary>
        public Plan<TResult> Then<TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult> selector)
        {
            return new Plan<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>(this, selector);
        }
    }


    /// <summary>
    /// Represents a join pattern.
    /// </summary>    
    public class Pattern<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> : Pattern
    {
        internal IObservable<T1> First { get; private set; }

        internal IObservable<T2> Second { get; private set; }

        internal IObservable<T3> Third { get; private set; }

        internal IObservable<T4> Fourth { get; private set; }

        internal IObservable<T5> Fifth { get; private set; }

        internal IObservable<T6> Sixth { get; private set; }

        internal IObservable<T7> Seventh { get; private set; }

        internal IObservable<T8> Eighth { get; private set; }

        internal IObservable<T9> Ninth { get; private set; }

        internal IObservable<T10> Tenth { get; private set; }

        internal IObservable<T11> Eleventh { get; private set; }

        internal IObservable<T12> Twelfth { get; private set; }

        internal Pattern(IObservable<T1> first, IObservable<T2> second, IObservable<T3> third,
                         IObservable<T4> fourth, IObservable<T5> fifth, IObservable<T6> sixth,
                         IObservable<T7> seventh, IObservable<T8> eighth, IObservable<T9> ninth,
                         IObservable<T10> tenth, IObservable<T11> eleventh, IObservable<T12> twelfth)
        {
            First = first;
            Second = second;
            Third = third;
            Fourth = fourth;
            Fifth = fifth;
            Sixth = sixth;
            Seventh = seventh;
            Eighth = eighth;
            Ninth = ninth;
            Tenth = tenth;
            Eleventh = eleventh;
            Twelfth = twelfth;
        }

        /// <summary>
        /// Matches when all observable sequences have an available value.
        /// </summary>
        public Pattern<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> And<T13>(IObservable<T13> other)
        {
            return new Pattern<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
                First, Second, Third, Fourth, Fifth, Sixth, Seventh, Eighth, Ninth, Tenth, Eleventh, Twelfth, other);
        }

        /// <summary>
        /// Matches when all observable sequences have an available value and projects the values.
        /// </summary>
        public Plan<TResult> Then<TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult> selector)
        {
            return new Plan<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>(this, selector);
        }
    }


    /// <summary>
    /// Represents a join pattern.
    /// </summary>    
    public class Pattern<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> : Pattern
    {
        internal IObservable<T1> First { get; private set; }

        internal IObservable<T2> Second { get; private set; }

        internal IObservable<T3> Third { get; private set; }

        internal IObservable<T4> Fourth { get; private set; }

        internal IObservable<T5> Fifth { get; private set; }

        internal IObservable<T6> Sixth { get; private set; }

        internal IObservable<T7> Seventh { get; private set; }

        internal IObservable<T8> Eighth { get; private set; }

        internal IObservable<T9> Ninth { get; private set; }

        internal IObservable<T10> Tenth { get; private set; }

        internal IObservable<T11> Eleventh { get; private set; }

        internal IObservable<T12> Twelfth { get; private set; }

        internal IObservable<T13> Thirteenth { get; private set; }

        internal Pattern(IObservable<T1> first, IObservable<T2> second, IObservable<T3> third,
                         IObservable<T4> fourth, IObservable<T5> fifth, IObservable<T6> sixth,
                         IObservable<T7> seventh, IObservable<T8> eighth, IObservable<T9> ninth,
                         IObservable<T10> tenth, IObservable<T11> eleventh, IObservable<T12> twelfth,
                         IObservable<T13> thirteenth)
        {
            First = first;
            Second = second;
            Third = third;
            Fourth = fourth;
            Fifth = fifth;
            Sixth = sixth;
            Seventh = seventh;
            Eighth = eighth;
            Ninth = ninth;
            Tenth = tenth;
            Eleventh = eleventh;
            Twelfth = twelfth;
            Thirteenth = thirteenth;
        }

        /// <summary>
        /// Matches when all observable sequences have an available value.
        /// </summary>
        public Pattern<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> And<T14>(IObservable<T14> other)
        {
            return new Pattern<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
                First, Second, Third, Fourth, Fifth, Sixth, Seventh, Eighth, Ninth, Tenth, Eleventh, Twelfth, Thirteenth, other);
        }

        /// <summary>
        /// Matches when all observable sequences have an available value and projects the values.
        /// </summary>
        public Plan<TResult> Then<TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult> selector)
        {
            return new Plan<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>(this, selector);
        }
    }

    /// <summary>
    /// Represents a join pattern.
    /// </summary>    
    public class Pattern<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> : Pattern
    {
        internal IObservable<T1> First { get; private set; }

        internal IObservable<T2> Second { get; private set; }

        internal IObservable<T3> Third { get; private set; }

        internal IObservable<T4> Fourth { get; private set; }

        internal IObservable<T5> Fifth { get; private set; }

        internal IObservable<T6> Sixth { get; private set; }

        internal IObservable<T7> Seventh { get; private set; }

        internal IObservable<T8> Eighth { get; private set; }

        internal IObservable<T9> Ninth { get; private set; }

        internal IObservable<T10> Tenth { get; private set; }

        internal IObservable<T11> Eleventh { get; private set; }

        internal IObservable<T12> Twelfth { get; private set; }

        internal IObservable<T13> Thirteenth { get; private set; }

        internal IObservable<T14> Fourteenth { get; private set; }


        internal Pattern(IObservable<T1> first, IObservable<T2> second, IObservable<T3> third,
                         IObservable<T4> fourth, IObservable<T5> fifth, IObservable<T6> sixth,
                         IObservable<T7> seventh, IObservable<T8> eighth, IObservable<T9> ninth,
                         IObservable<T10> tenth, IObservable<T11> eleventh, IObservable<T12> twelfth,
                         IObservable<T13> thirteenth, IObservable<T14> fourteenth)
        {
            First = first;
            Second = second;
            Third = third;
            Fourth = fourth;
            Fifth = fifth;
            Sixth = sixth;
            Seventh = seventh;
            Eighth = eighth;
            Ninth = ninth;
            Tenth = tenth;
            Eleventh = eleventh;
            Twelfth = twelfth;
            Thirteenth = thirteenth;
            Fourteenth = fourteenth;
        }

        /// <summary>
        /// Matches when all observable sequences have an available value.
        /// </summary>
        public Pattern<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> And<T15>(IObservable<T15> other)
        {
            return new Pattern<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(
                First, Second, Third, Fourth, Fifth, Sixth, Seventh, Eighth, Ninth, Tenth, Eleventh, Twelfth, Thirteenth, Fourteenth, other);
        }

        /// <summary>
        /// Matches when all observable sequences have an available value and projects the values.
        /// </summary>
        public Plan<TResult> Then<TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult> selector)
        {
            return new Plan<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>(this, selector);
        }
    }

    /// <summary>
    /// Represents a join pattern.
    /// </summary>    
    public class Pattern<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> : Pattern
    {
        internal IObservable<T1> First { get; private set; }

        internal IObservable<T2> Second { get; private set; }

        internal IObservable<T3> Third { get; private set; }

        internal IObservable<T4> Fourth { get; private set; }

        internal IObservable<T5> Fifth { get; private set; }

        internal IObservable<T6> Sixth { get; private set; }

        internal IObservable<T7> Seventh { get; private set; }

        internal IObservable<T8> Eighth { get; private set; }

        internal IObservable<T9> Ninth { get; private set; }

        internal IObservable<T10> Tenth { get; private set; }

        internal IObservable<T11> Eleventh { get; private set; }

        internal IObservable<T12> Twelfth { get; private set; }

        internal IObservable<T13> Thirteenth { get; private set; }

        internal IObservable<T14> Fourteenth { get; private set; }

        internal IObservable<T15> Fifteenth { get; private set; }


        internal Pattern(IObservable<T1> first, IObservable<T2> second, IObservable<T3> third,
                         IObservable<T4> fourth, IObservable<T5> fifth, IObservable<T6> sixth,
                         IObservable<T7> seventh, IObservable<T8> eighth, IObservable<T9> ninth,
                         IObservable<T10> tenth, IObservable<T11> eleventh, IObservable<T12> twelfth,
                         IObservable<T13> thirteenth, IObservable<T14> fourteenth, IObservable<T15> fifteenth)
        {
            First = first;
            Second = second;
            Third = third;
            Fourth = fourth;
            Fifth = fifth;
            Sixth = sixth;
            Seventh = seventh;
            Eighth = eighth;
            Ninth = ninth;
            Tenth = tenth;
            Eleventh = eleventh;
            Twelfth = twelfth;
            Thirteenth = thirteenth;
            Fourteenth = fourteenth;
            Fifteenth = fifteenth;
        }


        /// <summary>
        /// Matches when all observable sequences have an available value.
        /// </summary>
        public Pattern<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> And<T16>(IObservable<T16> other)
        {
            return new Pattern<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(
                First, Second, Third, Fourth, Fifth, Sixth, Seventh, Eighth, Ninth, Tenth, Eleventh, Twelfth, Thirteenth, Fourteenth, Fifteenth, other);
        }

        /// <summary>
        /// Matches when all observable sequences have an available value and projects the values.
        /// </summary>
        public Plan<TResult> Then<TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult> selector)
        {
            return new Plan<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>(this, selector);
        }
    }

    /// <summary>
    /// Represents a join pattern.
    /// </summary>    
    public class Pattern<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> : Pattern
    {
        internal IObservable<T1> First { get; private set; }

        internal IObservable<T2> Second { get; private set; }

        internal IObservable<T3> Third { get; private set; }

        internal IObservable<T4> Fourth { get; private set; }

        internal IObservable<T5> Fifth { get; private set; }

        internal IObservable<T6> Sixth { get; private set; }

        internal IObservable<T7> Seventh { get; private set; }

        internal IObservable<T8> Eighth { get; private set; }

        internal IObservable<T9> Ninth { get; private set; }

        internal IObservable<T10> Tenth { get; private set; }

        internal IObservable<T11> Eleventh { get; private set; }

        internal IObservable<T12> Twelfth { get; private set; }

        internal IObservable<T13> Thirteenth { get; private set; }

        internal IObservable<T14> Fourteenth { get; private set; }

        internal IObservable<T15> Fifteenth { get; private set; }

        internal IObservable<T16> Sixteenth { get; private set; }


        internal Pattern(IObservable<T1> first, IObservable<T2> second, IObservable<T3> third,
                         IObservable<T4> fourth, IObservable<T5> fifth, IObservable<T6> sixth,
                         IObservable<T7> seventh, IObservable<T8> eighth, IObservable<T9> ninth,
                         IObservable<T10> tenth, IObservable<T11> eleventh, IObservable<T12> twelfth,
                         IObservable<T13> thirteenth, IObservable<T14> fourteenth, IObservable<T15> fifteenth,
                         IObservable<T16> sixteenth
            )
        {
            First = first;
            Second = second;
            Third = third;
            Fourth = fourth;
            Fifth = fifth;
            Sixth = sixth;
            Seventh = seventh;
            Eighth = eighth;
            Ninth = ninth;
            Tenth = tenth;
            Eleventh = eleventh;
            Twelfth = twelfth;
            Thirteenth = thirteenth;
            Fourteenth = fourteenth;
            Fifteenth = fifteenth;
            Sixteenth = sixteenth;
        }

        /// <summary>
        /// Matches when all observable sequences have an available value and projects the values.
        /// </summary>
        public Plan<TResult> Then<TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult> selector)
        {
            return new Plan<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult>(this, selector);
        }
    }
#endif
}