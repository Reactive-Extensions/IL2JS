//
// Domains for effects analysis
//

using System;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.CST
{
    //
    // Domain of values
    // ~~~~~~~~~~~~~~~~
    //
    // We wish to track which values are pointers and what those pointers could resolve to. We use the sub-domains:
    //  - Boolean = { false, true }, where false < true.
    //  - IntPowerset: usual powerset over bounded naturals
    //  - Args: IntPowerset of arguments. Argument index is in set if pointer could resolve to it.
    //  - Locals: IntPowerset of locals. Local index is in set if pointer could resolve to it.
    //  - Heap = Boolean. True if pointer could resolve to (anywhere within) heap.
    // 
    // The overall value domain is Args x Locals x Heap, ordered pointwise.
    //
    // Note that values known not to be pointers use the bottom element, since clearly they cannot point into any
    // of the above locations.
    //
    public class PointsTo : IDomain<PointsTo>
    {
        [NotNull]
        public readonly IntPowersetDomain Args;
        [NotNull]
        public readonly IntPowersetDomain Locals;
        [NotNull]
        public readonly BooleanDomain Heap;

        public PointsTo(IntPowersetDomain args, IntPowersetDomain locals, BooleanDomain heap)
        {
            Args = args;
            Locals = locals;
            Heap = heap;
        }

        public bool Lte(PointsTo other)
        {
            return Args.Lte(other.Args) && Locals.Lte(other.Locals) && Heap.Lte(other.Heap);
        }

        public PointsTo Lub(PointsTo other)
        {
            var args = Args.Lub(other.Args);
            var locals = Locals.Lub(other.Locals);
            var heap = Heap.Lub(other.Heap);
            if (args == null || locals == null || heap == null)
                return null;
            else
                return new PointsTo(args, locals, heap);
        }

        public PointsTo Lub(PointsTo other, BoolRef changed)
        {
            var thisChanged = new BoolRef();
            var args = Args.Lub(other.Args, thisChanged);
            var locals = Locals.Lub(other.Locals, thisChanged);
            var heap = Heap.Lub(other.Heap, thisChanged);
            if (args == null || locals == null || heap == null)
                return null;
            else if (thisChanged.Value)
            {
                changed.Set();
                return new PointsTo(args, locals, heap);
            }
            else
                return this;
        }

        public bool HasBottom { get { return true; } }
        public bool IsBottom { get { return Args.IsBottom && Locals.IsBottom && Heap.IsBottom; } }
        public bool HasTop { get { return true; } }
        public bool IsTop { get { return Args.IsTop && Locals.IsTop && Heap.IsTop; } }

        public bool CommutableWith(PointsTo other)
        {
            return Args.CommutableWith(other.Args) && Locals.CommutableWith(other.Locals) && Heap.CommutableWith(other.Heap);
        }

        public bool PointsOutsideOfHeap { get { return !Args.IsBottom || !Locals.IsBottom; } }

        public void Append(Writer w)
        {
            w.Append('{');
            var first = true;
            if (!Args.IsBottom)
            {
                foreach (var i in Args.Members)
                {
                    if (first)
                        first = false;
                    else
                        w.Append(',');
                    w.Append("*arg");
                    w.Append(i);
                }
            }
            if (!Locals.IsBottom)
            {
                foreach (var i in Locals.Members)
                {
                    if (first)
                        first = false;
                    else
                        w.Append(',');
                    w.Append("*loc");
                    w.Append(i);
                }
            }
            if (!Heap.IsBottom)
            {
                if (!first)
                    w.Append(',');
                w.Append("*heap");
            }
            w.Append('}');
        }

        public override string ToString()
        {
            return Writer.WithAppend(Append);
        }

        public static PointsTo MakeBottom(int nArgs, int nLocals)
        {
            return new PointsTo
                (IntPowersetDomain.Bottom(nArgs), IntPowersetDomain.Bottom(nLocals), BooleanDomain.Bottom);
        }

        public static PointsTo MakeTop(int nArgs, int nLocals)
        {
            return new PointsTo
                (IntPowersetDomain.Top(nArgs), IntPowersetDomain.Top(nLocals), BooleanDomain.Top);
        }

        public static PointsTo MakeArgLocal(int nArgs, int nLocals, ArgLocal argLocal, int index)
        {
            switch (argLocal)
            {
            case ArgLocal.Arg:
                return new PointsTo
                    (IntPowersetDomain.Singleton(nArgs, index),
                     IntPowersetDomain.Bottom(nLocals),
                     BooleanDomain.Bottom);
            case ArgLocal.Local:
                return new PointsTo
                    (IntPowersetDomain.Bottom(nArgs),
                     IntPowersetDomain.Singleton(nLocals, index),
                     BooleanDomain.Bottom);
            default:
                throw new ArgumentOutOfRangeException("argLocal");
            }
        }

        public static PointsTo MakeHeap(int nArgs, int nLocals)
        {
            return new PointsTo(IntPowersetDomain.Bottom(nArgs), IntPowersetDomain.Bottom(nLocals), BooleanDomain.Top);
        }

        public int NumArgs { get { return Args.Members.Capacity; } }
        public int NumLocals { get { return Locals.Members.Capacity; } }

        public Effects ReadEffect()
        {
            if (IsBottom)
                return Effects.MakeBottom(NumArgs, NumLocals);
            else
                return new Effects(Args.Members.Clone(), new IntSet(NumArgs), Locals.Members.Clone(), new IntSet(NumLocals), Heap.Value, false, false);
        }

        public Effects WriteEffect()
        {
            if (IsBottom)
                return Effects.MakeBottom(NumArgs, NumLocals);
            else
                return new Effects(Args.Members.Clone(), Args.Members.Clone(), Locals.Members.Clone(), Locals.Members.Clone(), Heap.Value, Heap.Value, false);
        }
    }

    //
    // Domain of effects
    // ~~~~~~~~~~~~~~~~~
    //
    // We wish to track the possible side-effects of evaluating instructions so as to check for interference. 
    // We use the sub-domains:
    //  - Boolean = { false, true }, where false < true.
    //  - NRW = {N, R, W}, where N < R < W. Represent whether a storage location is definitely neither read nor written
    //    (least effectfull, most precise), may be read but definitely not written, or may be read and written (most
    //    effectfull, least precise)
    //  - Args = cross product of NRW for each argument, ordered pointwise. Captures the effects on arguments.
    //  - Locals = cross product of NRW for each local, ordered pointwise. Captures the effects on locals.
    //  - Heap = NRW. Captures the effect on the heap, considered as a single monolithic storage location.
    //  - Exception = Boolean. False if definitely no exception is thrown (least effectfull, most precise), or
    //    true if exception may be thrown (most effectfull, least precise).
    //
    // The overall effects domain is Args x Locals x Heap x Throws, ordered pointwise.
    //
    public class Effects : IDomain<Effects>
    {
        [NotNull]
        public readonly ReadWriteVectorDomain Args;
        [NotNull]
        public readonly ReadWriteVectorDomain Locals;
        [NotNull]
        public readonly ReadWriteDomain Heap;
        [NotNull]
        public readonly BooleanDomain MayThrow;

        public Effects
            (ReadWriteVectorDomain args, ReadWriteVectorDomain locals, ReadWriteDomain heap, BooleanDomain mayThrow)
        {
            Args = args;
            Locals = locals;
            Heap = heap;
            MayThrow = mayThrow;
        }

        public Effects(IntSet argsIsRead, IntSet argsIsWrite, IntSet localsIsRead, IntSet localsIsWrite,
            bool heapIsRead, bool heapIsWrite, bool exceptions)
        {
            Args = new ReadWriteVectorDomain(argsIsRead, argsIsWrite);
            Locals = new ReadWriteVectorDomain(localsIsRead, localsIsWrite);
            Heap = new ReadWriteDomain(heapIsRead, heapIsWrite);
            MayThrow = new BooleanDomain(exceptions);
        }

        public bool Lte(Effects other)
        {
            return Args.Lte(other.Args) && Locals.Lte(other.Locals) && Heap.Lte(other.Heap) && MayThrow.Lte(other.MayThrow);
        }

        public Effects Lub(Effects other)
        {
            var args = Args.Lub(other.Args);
            var locals = Locals.Lub(other.Locals);
            var heap = Heap.Lub(other.Heap);
            var mayThrow = MayThrow.Lub(other.MayThrow);
            if (args == null || locals == null || heap == null || mayThrow == null)
                return null;
            else
                return new Effects(args, locals, heap, mayThrow);
        }

        public Effects Lub(Effects other, BoolRef changed)
        {
            var thisChanged = new BoolRef();
            var args = Args.Lub(other.Args, thisChanged);
            var locals = Locals.Lub(other.Locals, thisChanged);
            var heap = Heap.Lub(other.Heap, thisChanged);
            var mayThrow = MayThrow.Lub(other.MayThrow, thisChanged);
            if (args == null || locals == null || heap == null || mayThrow == null)
                return null;
            else if (thisChanged.Value)
            {
                changed.Set();
                return new Effects(args, locals, heap, mayThrow);
            }
            else
                return this;
        }

        public bool HasBottom { get { return true; } }
        public bool IsBottom { get { return Args.IsBottom && Locals.IsBottom && Heap.IsBottom && MayThrow.IsBottom; } }
        public bool HasTop { get { return true; } }
        public bool IsTop { get { return Args.IsTop && Locals.IsTop && Heap.IsTop && MayThrow.IsTop; } }

        public bool CommutableWith(Effects other)
        {
            return Args.CommutableWith(other.Args) && Locals.CommutableWith(other.Locals) &&
                   Heap.CommutableWith(other.Heap) && MayThrow.CommutableWith(other.MayThrow);
        }

        public void Append(Writer w)
        {
            w.Append('{');
            var first = true;
            for (var i = 0; i < Args.Capacity; i++)
            {
                var rw = Args[i];
                if (!rw.IsBottom)
                {
                    if (first) first = false;
                    else w.Append(' ');
                    w.Append("arg");
                    w.Append(i);
                    w.Append(':');
                    w.Append('r');
                    if (rw.IsWrite)
                        w.Append('w');
                }
            }
            for (var i = 0; i < Locals.Capacity; i++)
            {
                var rw = Locals[i];
                if (!rw.IsBottom)
                {
                    if (first) first = false;
                    else w.Append(' ');
                    w.Append("loc");
                    w.Append(i);
                    w.Append(':');
                    w.Append('r');
                    if (rw.IsWrite)
                        w.Append('w');
                }
            }
            if (!Heap.IsBottom)
            {
                if (first) first = false;
                else w.Append(' ');
                w.Append("heap:r");
                if (Heap.IsWrite)
                    w.Append('w');
            }
            if (!MayThrow.IsBottom)
            {
                if (first) first = false;
                else w.Append(' ');
                w.Append("mayThrow");
            }
            w.Append('}');
        }

        public override string ToString()
        {
            return Writer.WithAppend(Append);
        }

        public static bool AllCommutable(IImSeq<Effects> effects)
        {
            // Ideally, we'd just check all pairwise combinations, but that would be to expensive.

            // If any exceptions, no luck. Otherwise, if all args, locals and the head are read-only, we are good.
            var noneWrite = true;
            foreach (var e in effects)
            {
                if (e.MayThrow.Value)
                    return false;
                if (!e.IsReadOnly)
                    noneWrite = false;
            }
            if (noneWrite)
                return true;

            // At this point we could check if reads and writes are all to distinct locations, but for now
            // lets just give up
            return false;
        }

        public bool IsReadOnly
        {
            get
            {
                return Args.IsReadOnly && Locals.IsReadOnly && Heap.IsReadOnly;
            }
        }

        public static Effects MakeBottom(int nArgs, int nLocals)
        {
            return new Effects
                (ReadWriteVectorDomain.FromSameElement(ReadWriteDomain.Bottom, nArgs),
                 ReadWriteVectorDomain.FromSameElement(ReadWriteDomain.Bottom, nLocals),
                 ReadWriteDomain.Bottom,
                 BooleanDomain.Bottom);
        }

        public static Effects MakeTop(int nArgs, int nLocals)
        {
            return new Effects
                (ReadWriteVectorDomain.FromSameElement(ReadWriteDomain.Top, nArgs),
                 ReadWriteVectorDomain.FromSameElement(ReadWriteDomain.Top, nLocals),
                 ReadWriteDomain.Top,
                 BooleanDomain.Top);
        }

        public static Effects MakeArgLocal(int nArgs, int nLocals, ArgLocal argLocal, int index, bool isWrite, bool couldThrow)
        {
            switch (argLocal)
            {
            case ArgLocal.Arg:
            return new Effects
                (ReadWriteVectorDomain.FromSameElementExcept
                     (ReadWriteDomain.Bottom, nArgs, new ReadWriteDomain(true, isWrite), index),
                 ReadWriteVectorDomain.FromSameElement(ReadWriteDomain.Bottom, nLocals),
                 ReadWriteDomain.Bottom,
                 new BooleanDomain(couldThrow));
            case ArgLocal.Local:
            return new Effects
                (ReadWriteVectorDomain.FromSameElement(ReadWriteDomain.Bottom, nArgs),
                 ReadWriteVectorDomain.FromSameElementExcept
                     (ReadWriteDomain.Bottom, nLocals, new ReadWriteDomain(true, isWrite), index),
                 ReadWriteDomain.Bottom,
                 new BooleanDomain(couldThrow));
            default:
                throw new ArgumentOutOfRangeException("argLocal");
            }
        }

        public static Effects MakeHeap(int nArgs, int nLocals, bool isWrite, bool couldThrow)
        {
            return new Effects
                (ReadWriteVectorDomain.FromSameElement(ReadWriteDomain.Bottom, nArgs),
                 ReadWriteVectorDomain.FromSameElement(ReadWriteDomain.Bottom, nLocals),
                 new ReadWriteDomain(true, isWrite),
                 new BooleanDomain(couldThrow));
        }

        public static Effects MakeArgLocalHeap(int nArgs, int nLocals, bool isWrite, bool couldThrow)
        {
            return new Effects
                (ReadWriteVectorDomain.FromSameElement(new ReadWriteDomain(true, isWrite), nArgs),
                 ReadWriteVectorDomain.FromSameElement(new ReadWriteDomain(true, isWrite), nLocals),
                 new ReadWriteDomain(true, isWrite),
                 new BooleanDomain(couldThrow));
        }

        public static Effects MakeThrows(int nArgs, int nLocals)
        {
            return new Effects
                (ReadWriteVectorDomain.FromSameElement(ReadWriteDomain.Bottom, nArgs),
                 ReadWriteVectorDomain.FromSameElement(ReadWriteDomain.Bottom, nLocals),
                 ReadWriteDomain.Bottom,
                 BooleanDomain.Top);
        }

        public Effects Lfp(Func<Effects, Effects> f)
        {
            var current = this;
            while (true)
            {
                var next = f(current);
                if (next.Lte(current))
                    return current;
                current = next;
            }
        }
    }
}