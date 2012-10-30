//
// Generic domains for effects analysis
//

using System;
using System.Linq;

namespace Microsoft.LiveLabs.Extras
{
    public interface IDomain<T>
    {
        bool Lte(T other);
        T Lub(T other);
        T Lub(T other, BoolRef changed);

        // Can computation with this effect be commuted with computation of other effect
        // without observable difference? We write a#b for a.CommutabelWith(b) == true.
        // Then we must have:
        //   Commutativity:  a#b => b#a
        //   Anti-monotonicity:   a#b && c <= b => a#c
        bool CommutableWith(T other);
        bool HasBottom { get; }
        bool IsBottom { get; }
        bool HasTop { get; }
        bool IsTop { get; }
        void Append(Writer w);
    }

    // false < true
    public class BooleanDomain : IDomain<BooleanDomain>
    {
        public readonly bool Value;

        public static BooleanDomain Bottom;
        public static BooleanDomain Top;

        static BooleanDomain()
        {
            Bottom = new BooleanDomain(false);
            Top = new BooleanDomain(true);
        }

        public BooleanDomain(bool mayThrow)
        {
            Value = mayThrow;
        }

        public bool Lte(BooleanDomain other)
        {
            return !Value || Value;
        }

        public BooleanDomain Lub(BooleanDomain other)
        {
            return Value || other.Value ? Top : Bottom;
        }

        public BooleanDomain Lub(BooleanDomain other, BoolRef changed)
        {
            if (!Value && other.Value)
            {
                changed.Set();
                return Top;
            }
            else
                return this;
        }

        public bool CommutableWith(BooleanDomain other)
        {
            return !(Value && other.Value);
        }

        public bool HasBottom { get { return true; } }
        public bool IsBottom { get { return !Value; } }
        public bool HasTop { get { return true; } }
        public bool IsTop { get { return Value; } }

        public void Append(Writer w)
        {
            w.Append(Value ? 't' : 'f');
        }

        public override string ToString()
        {
            return Writer.WithAppend(Append);
        }
    }

    public class VectorDomain<T> : IDomain<VectorDomain<T>> where T : class, IDomain<T>
    {
        [NotNull]
        public readonly IImSeq<T> Elements;

        public VectorDomain(IImSeq<T> elements)
        {
            Elements = elements;
        }

        public bool Lte(VectorDomain<T> other)
        {
            if (Elements.Count != other.Elements.Count)
                throw new InvalidOperationException("comparing vectors of unequal length");
            for (var i = 0; i < Elements.Count; i++)
            {
                if (!Elements[i].Lte(other.Elements[i]))
                    return false;
            }
            return true;
        }

        public VectorDomain<T> Lub(VectorDomain<T> other)
        {
            if (Elements.Count != other.Elements.Count)
                return null;
            var res = new Seq<T>(Elements.Count);
            for (var i = 0; i < Elements.Count; i++)
                res.Add(Elements[i].Lub(other.Elements[i]));
            return new VectorDomain<T>(res);
        }

        public VectorDomain<T> Lub(VectorDomain<T> other, BoolRef changed)
        {
            if (Elements.Count != other.Elements.Count)
                return null;
            var res = default(Seq<T>);
            for (var i = 0; i < Elements.Count; i++)
            {
                var thisChanged = new BoolRef();
                var elem = Elements[i].Lub(other.Elements[i], thisChanged);
                if (elem == null)
                    return null;
                if (thisChanged.Value && res == null)
                {
                    changed.Set();
                    res = new Seq<T>(Elements.Count);
                    for (var j = 0; j < i; j++)
                        res.Add(Elements[i]);
                }
                if (res != null)
                    res[i] = elem;
            }
            if (res == null)
                return this;
            else
                return new VectorDomain<T>(res);
        }

        public bool CommutableWith(VectorDomain<T> other)
        {
            if (Elements.Count != other.Elements.Count)
                throw new InvalidOperationException("incompatible domains");
            for (var i = 0; i < Elements.Count; i++)
            {
                if (!Elements[i].CommutableWith(other.Elements[i]))
                    return false;
            }
            return true;
        }

        public bool HasBottom { get { return Elements.Count == 0 || Elements[0].HasBottom; } }
        public bool IsBottom { get { return Elements.All(e => e.IsBottom); } }
        public bool HasTop { get { return Elements.Count == 0 || Elements[0].HasTop; } }
        public bool IsTop { get { return Elements.All(e => e.IsTop); } }

        public void Append(Writer w)
        {
            w.Append('[');
            for (var i = 0; i < Elements.Count; i++)
            {
                if (i > 0)
                    w.Append(',');
                Elements[i].Append(w);
            }
            w.Append(']');
        }

        public override string ToString()
        {
            return Writer.WithAppend(Append);
        }

        public static VectorDomain<T> FromSameElement(T initElement, int n)
        {
            var elements = new Seq<T>(n);
            for (var i = 0; i < n; i++)
                elements.Add(initElement);
            return new VectorDomain<T>(elements);
        }

        public static VectorDomain<T> FromSameElementExcept(T initElement, int n, T specificElement, int index)
        {
            var elements = new Seq<T>(n);
            for (var i = 0; i < n; i++)
                elements.Add(i == index ? specificElement : initElement);
            return new VectorDomain<T>(elements);
        }
    }

    public enum ReadWriteEnum
    {
        None,
        Read,
        ReadWrite
    }

    // None < Read < ReadWrite
    public class ReadWriteDomain : IDomain<ReadWriteDomain>
    {
        public readonly ReadWriteEnum Value;

        public static ReadWriteDomain Bottom;
        public static ReadWriteDomain Read;
        public static ReadWriteDomain Top;

        static ReadWriteDomain()
        {
            Bottom = new ReadWriteDomain(ReadWriteEnum.None);
            Read = new ReadWriteDomain(ReadWriteEnum.Read);
            Top = new ReadWriteDomain(ReadWriteEnum.ReadWrite);
        }

        public ReadWriteDomain(ReadWriteEnum value)
        {
            Value = value;
        }

        public ReadWriteDomain(bool isRead, bool isWrite)
        {
            Value = isWrite ? ReadWriteEnum.ReadWrite : (isRead ? ReadWriteEnum.Read : ReadWriteEnum.None);
        }

        public bool Lte(ReadWriteDomain other)
        {
            switch (Value)
            {
                case ReadWriteEnum.None:
                    return true;
                case ReadWriteEnum.Read:
                    return other.Value != ReadWriteEnum.None;
                case ReadWriteEnum.ReadWrite:
                    return other.Value == ReadWriteEnum.ReadWrite;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public ReadWriteDomain Lub(ReadWriteDomain other)
        {
            switch (Value)
            {
                case ReadWriteEnum.None:
                    if (other.Value != ReadWriteEnum.None)
                        return other;
                    else
                        return this;
                case ReadWriteEnum.Read:
                    if (other.Value == ReadWriteEnum.ReadWrite)
                        return other;
                    else
                        return this;
                case ReadWriteEnum.ReadWrite:
                    return this;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public ReadWriteDomain Lub(ReadWriteDomain other, BoolRef changed)
        {
            switch (Value)
            {
                case ReadWriteEnum.None:
                    if (other.Value != ReadWriteEnum.None)
                    {
                        changed.Set();
                        return other;
                    }
                    else
                        return this;
                case ReadWriteEnum.Read:
                    if (other.Value == ReadWriteEnum.ReadWrite)
                    {
                        changed.Set();
                        return other;
                    }
                    else
                        return this;
                case ReadWriteEnum.ReadWrite:
                    return this;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        //   this  other  commutable?
        //   ----  -----  -----------
        //   n     n      yes
        //   n     r      yes
        //   n     w      yes
        //   r     n      yes
        //   r     r      yes
        //   r     w      no
        //   w     n      yes
        //   w     r      no
        //   w     w      no
        public bool CommutableWith(ReadWriteDomain other)
        {
            switch (Value)
            {
                case ReadWriteEnum.None:
                    return true;
                case ReadWriteEnum.Read:
                    return other.Value != ReadWriteEnum.ReadWrite;
                case ReadWriteEnum.ReadWrite:
                    return other.Value == ReadWriteEnum.None;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool HasBottom { get { return true; } }
        public bool IsBottom { get { return Value == ReadWriteEnum.None; } }
        public bool HasTop { get { return true; } }
        public bool IsTop { get { return Value == ReadWriteEnum.ReadWrite; } }

        public void Append(Writer w)
        {
            switch (Value)
            {
                case ReadWriteEnum.None:
                    w.Append('n');
                    break;
                case ReadWriteEnum.Read:
                    w.Append('r');
                    break;
                case ReadWriteEnum.ReadWrite:
                    w.Append("rw");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return Writer.WithAppend(Append);
        }

        public bool IsReadOnly { get { return Value != ReadWriteEnum.ReadWrite; } }
        public bool IsRead { get { return Value != ReadWriteEnum.None; } }
        public bool IsWrite { get { return Value == ReadWriteEnum.ReadWrite; } }
    }

    public enum DiscreteEnum
    {
        None,
        Specific,
        Any
    }

    // None < Specific(x) < Any
    public class DiscreteDomain<T> : IDomain<DiscreteDomain<T>>
    {
        public readonly DiscreteEnum Flag;
        public readonly T Value;

        public static DiscreteDomain<T> Bottom;
        public static DiscreteDomain<T> Top;

        static DiscreteDomain()
        {
            Bottom = new DiscreteDomain<T>(DiscreteEnum.None, default(T));
            Top = new DiscreteDomain<T>(DiscreteEnum.Any, default(T));
        }

        public DiscreteDomain(DiscreteEnum flag, T value)
        {
            Flag = flag;
            Value = value;
        }

        public bool Lte(DiscreteDomain<T> other)
        {
            if (Flag == DiscreteEnum.None)
                return true;
            if (Flag == DiscreteEnum.Specific)
            {
                if (other.Flag == DiscreteEnum.Specific)
                    return Value.Equals(other.Value);
                return other.Flag == DiscreteEnum.Any;
            }
            return other.Flag == DiscreteEnum.Any;
        }

        public DiscreteDomain<T> Lub(DiscreteDomain<T> other)
        {
            switch (Flag)
            {
                case DiscreteEnum.None:
                    return other.Flag == DiscreteEnum.None ? this : other;
                case DiscreteEnum.Specific:
                    if (other.Flag == DiscreteEnum.None)
                        return this;
                    else if (other.Flag == DiscreteEnum.Specific)
                        return Value.Equals(other.Value) ? this : null;
                    else
                        return other;
                case DiscreteEnum.Any:
                    return this;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public DiscreteDomain<T> Lub(DiscreteDomain<T> other, BoolRef changed)
        {
            switch (Flag)
            {
                case DiscreteEnum.None:
                    if (other.Flag == DiscreteEnum.None)
                        return this;
                    else
                    {
                        changed.Set();
                        return other;
                    }
                case DiscreteEnum.Specific:
                    if (other.Flag == DiscreteEnum.None)
                        return this;
                    else if (other.Flag == DiscreteEnum.Specific)
                    {
                        if (Value.Equals(other.Value))
                            return this;
                        else
                            return null;
                    }
                    else
                    {
                        changed.Set();
                        return other;
                    }
                case DiscreteEnum.Any:
                    return this;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool CommutableWith(DiscreteDomain<T> other)
        {
            if (Flag == DiscreteEnum.None || other.Flag == DiscreteEnum.None)
                return true;
            if (Flag == DiscreteEnum.Any || other.Flag == DiscreteEnum.Any)
                return false;
            return Value.Equals(other.Value);
        }

        public bool HasBottom { get { return true; } }
        public bool IsBottom { get { return Flag == DiscreteEnum.None; } }
        public bool HasTop { get { return true; } }
        public bool IsTop { get { return Flag == DiscreteEnum.Any; } }

        public void Append(Writer w)
        {
            switch (Flag)
            {
                case DiscreteEnum.None:
                    w.Append('-');
                    break;
                case DiscreteEnum.Specific:
                    w.Append(Value.ToString());
                    break;
                case DiscreteEnum.Any:
                    w.Append('*');
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return Writer.WithAppend(Append);
        }
    }

    // An optimized version of VectorDomain<ReadWriteDomain>
    public class ReadWriteVectorDomain : IDomain<ReadWriteVectorDomain>
    {
        [NotNull]
        public readonly IntSet IsRead; // TODO: should be immutable
        [NotNull]
        public readonly IntSet IsWrite; // TODO: should be immutable

        public ReadWriteVectorDomain(IntSet isRead, IntSet isWrite)
        {
            if (isRead.Capacity != isWrite.Capacity)
                throw new InvalidOperationException("read and write sets must have same capacity");
            IsRead = isRead;
            IsWrite = isWrite;
        }

        public ReadWriteVectorDomain(int n)
            : this(new IntSet(n), new IntSet(n))
        {
        }

        public int Capacity { get { return IsRead.Capacity; } }

        public ReadWriteDomain this[int i]
        {
            get
            {
                return new ReadWriteDomain(IsRead[i], IsWrite[i]);
            }
        }

        public bool Lte(ReadWriteVectorDomain other)
        {
            return IsRead.IsSubset(other.IsRead) && IsWrite.IsSubset(other.IsWrite);
        }

        public ReadWriteVectorDomain Lub(ReadWriteVectorDomain other)
        {
            return new ReadWriteVectorDomain(IsRead.Union(other.IsRead), IsWrite.Union(other.IsWrite));
        }

        public ReadWriteVectorDomain Lub(ReadWriteVectorDomain other, BoolRef changed)
        {
            var thisChanged = new BoolRef();
            var isRead = IsRead.Union(other.IsRead, thisChanged);
            var isWrite = IsWrite.Union(other.IsWrite, thisChanged);
            if (thisChanged.Value)
            {
                changed.Set();
                return new ReadWriteVectorDomain(isRead, isWrite);
            }
            else
                return this;
        }

        //   this read  this write  other read  other write  commutable?
        //   ---------  ----------  ----------  -----------  -----------
        //   f          f           f           f            yes
        //   f          f           t           f            yes
        //   f          f           t           t            yes
        //   t          f           f           f            yes
        //   t          f           t           f            yes
        //   t          f           t           t            no
        //   t          t           f           f            yes
        //   t          t           t           f            no
        //   t          t           t           t            no
        public bool CommutableWith(ReadWriteVectorDomain other)
        {
            return
                (IsRead.Intersect(IsWrite).Intersect(other.IsRead)).Union
                    (other.IsRead.Intersect(other.IsWrite).Intersect(IsRead)).IsEmpty;
        }

        public bool HasBottom { get { return true; } }
        public bool IsBottom { get { return IsRead.IsEmpty && IsWrite.IsEmpty; } }
        public bool HasTop { get { return true; } }
        public bool IsTop { get { return IsRead.IsFull && IsWrite.IsFull; } }

        public bool IsReadOnly { get { return IsWrite.IsEmpty; } }

        public void Append(Writer w)
        {
            w.Append('[');
            for (var i = 0; i < IsRead.Capacity; i++)
            {
                if (i > 0)
                    w.Append(',');
                if (IsWrite[i])
                    w.Append('w');
                else if (IsRead[i])
                    w.Append('r');
                else
                    w.Append('n');
            }
            w.Append(']');
        }

        public override string ToString()
        {
            return Writer.WithAppend(Append);
        }

        public static ReadWriteVectorDomain FromSameElement(ReadWriteDomain element, int n)
        {
            var isRead = new IntSet(n);
            if (element.IsRead)
                isRead.SetAll(true);
            var isWrite = new IntSet(n);
            if (element.IsWrite)
                isWrite.SetAll(true);
            return new ReadWriteVectorDomain(isRead, isWrite);
        }

        public static ReadWriteVectorDomain FromSameElementExcept(ReadWriteDomain element, int n, ReadWriteDomain specific, int index)
        {
            var isRead = new IntSet(n);
            if (element.IsRead)
                isRead.SetAll(true);
            var isWrite = new IntSet(n);
            if (element.IsWrite)
                isWrite.SetAll(true);
            isRead[index] = specific.IsRead;
            isWrite[index] = specific.IsWrite;
            return new ReadWriteVectorDomain(isRead, isWrite);
        }
    }

    public class IntPowersetDomain : IDomain<IntPowersetDomain>
    {
        [NotNull]
        public readonly IntSet Members; // TODO: Should be immutable

        public IntPowersetDomain(IntSet members)
        {
            Members = members;
        }

        public bool Lte(IntPowersetDomain other)
        {
            return Members.IsSubset(other.Members);
        }

        public IntPowersetDomain Lub(IntPowersetDomain other)
        {
            return new IntPowersetDomain(Members.Union(other.Members));
        }

        public IntPowersetDomain Lub(IntPowersetDomain other, BoolRef changed)
        {
            var thisChanged = new BoolRef();
            var members = Members.Union(other.Members, thisChanged);
            if (thisChanged.Value)
            {
                changed.Set();
                return new IntPowersetDomain(members);
            }
            else
                return this;
        }

        public bool CommutableWith(IntPowersetDomain other)
        {
            return Members.IsDisjoint(other.Members);
        }

        public bool HasBottom { get { return true; } }
        public bool IsBottom { get { return Members.IsEmpty; } }
        public bool HasTop { get { return true; } }
        public bool IsTop { get { return Members.IsFull; } }

        public static IntPowersetDomain Bottom(int n)
        {
            return new IntPowersetDomain(new IntSet(n));
        }

        public static IntPowersetDomain Top(int n)
        {
            var set = new IntSet(n);
            set.SetAll(true);
            return new IntPowersetDomain(set);
        }

        public static IntPowersetDomain Singleton(int n, int i)
        {
            var set = new IntSet(n);
            set[i] = true;
            return new IntPowersetDomain(set);
        }

        public void Append(Writer w)
        {
            w.Append('{');
            var first = true;
            foreach (var i in Members)
            {
                if (first)
                    first = false;
                else
                    w.Append(',');
                w.Append(i);
            }
            w.Append('}');
        }

        public override string ToString()
        {
            return Writer.WithAppend(Append);
        }
    }

    // Inserts a 'top' element into T's domain
    public class DroppedDomain<T> : IDomain<DroppedDomain<T>> where T : class, IDomain<T>, new()
    {
        [CanBeNull] // null => top
        public readonly T Value;

        public static DroppedDomain<T> Top;

        static DroppedDomain()
        {
            Top = new DroppedDomain<T>(null);
        }

        public DroppedDomain(T value)
        {
            Value = value;
        }

        public bool Lte(DroppedDomain<T> other)
        {
            if (other.IsTop)
                return true;
            if (IsTop)
                return false;
            return Value.Lte(other.Value);
        }

        public DroppedDomain<T> Lub(DroppedDomain<T> other)
        {
            if (IsTop || other.IsTop)
                return Top;
            var value = Value.Lub(other.Value);
            return value == null ? null : new DroppedDomain<T>(value);
        }

        public DroppedDomain<T> Lub(DroppedDomain<T> other, BoolRef changed)
        {
            if (IsTop)
                return Top;
            else if (other.IsTop)
            {
                changed.Set();
                return Top;
            }
            else
            {
                var value = Value.Lub(other.Value, changed);
                return value == null ? null : new DroppedDomain<T>(value);
            }
        }

        public bool CommutableWith(DroppedDomain<T> other)
        {
            if (IsTop && other.IsTop)
                return false;
            else if (IsTop)
                return other.IsBottom;
            else if (other.IsTop)
                return IsBottom;
            else
                return Value.CommutableWith(other.Value);
        }

        public bool HasBottom { get { return Value == null ? new T().HasBottom : Value.HasBottom; } }
        public bool IsBottom { get { return Value != null && Value.IsBottom; } }
        public bool HasTop { get { return true; } }
        public bool IsTop { get { return Value == null; } }

        public void Append(Writer w)
        {
            if (Value == null)
                w.Append("TOP");
            else
                Value.Append(w);
        }
    }


    public enum ControlFlowEnum
    {
        // Control always returns to invoking context
        AlwaysReturn,
        // Control never return to invoking context
        NeverReturn,
        // No information
        Any
    }

    // AlwaysReturn < Any, NeverReturn < Any
    public class ControlFlow : IDomain<ControlFlow>
    {
        public readonly ControlFlowEnum Value;

        public static ControlFlow AlwaysReturn;
        public static ControlFlow NeverReturn;
        public static ControlFlow Top;

        public ControlFlow(ControlFlowEnum value)
        {
            Value = value;
        }

        static ControlFlow()
        {
            AlwaysReturn = new ControlFlow(ControlFlowEnum.AlwaysReturn);
            NeverReturn = new ControlFlow(ControlFlowEnum.NeverReturn);
            Top = new ControlFlow(ControlFlowEnum.Any);
        }

        public bool Lte(ControlFlow other)
        {
            if (other.Value == ControlFlowEnum.Any)
                return true;
            return Value == other.Value;
        }

        public ControlFlow Lub(ControlFlow other)
        {
            return Value == other.Value ? this : Top;
        }

        public ControlFlow Lub(ControlFlow other, BoolRef changed)
        {
            if (Value == other.Value || Value == ControlFlowEnum.Any)
                return this;
            else
            {
                changed.Set();
                return Top;
            }
        }

        public bool CommutableWith(ControlFlow other)
        {
            return true;
        }

        public bool HasBottom { get { return false; } }
        public bool IsBottom { get { throw new InvalidOperationException("domain does not have a bottom"); } }
        public bool HasTop { get { return true; } }
        public bool IsTop { get { return Value == ControlFlowEnum.Any; } }

        public void Append(Writer w)
        {
            switch (Value)
            {
                case ControlFlowEnum.AlwaysReturn:
                    w.Append("RET");
                    break;
                case ControlFlowEnum.NeverReturn:
                    w.Append("NEV");
                    break;
                case ControlFlowEnum.Any:
                    w.Append("TOP");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }


    // How many times could an expression be evaluated
    public enum EvalTimesEnum
    {
        Once,        // 1
        AtLeastOnce, // [1..]
        Opt,         // [0..1]
        Any          // [0..]
    }

    // Once < AtLeastOnce < Any, Once < Opt < Any
    public class EvalTimes : IDomain<EvalTimes>
    {
        public readonly EvalTimesEnum Value;

        public static EvalTimes Bottom;
        public static EvalTimes AtLeastOnce;
        public static EvalTimes Opt;
        public static EvalTimes Top;

        public EvalTimes(EvalTimesEnum value)
        {
            Value = value;
        }

        static EvalTimes()
        {
            Bottom = new EvalTimes(EvalTimesEnum.Once);
            AtLeastOnce = new EvalTimes(EvalTimesEnum.AtLeastOnce);
            Opt = new EvalTimes(EvalTimesEnum.Opt);
            Top = new EvalTimes(EvalTimesEnum.Any);
        }

        public bool Lte(EvalTimes other)
        {
            return Value == other.Value || Value == EvalTimesEnum.Once || other.Value == EvalTimesEnum.Any;
        }

        public EvalTimes Lub(EvalTimes other)
        {
            if (Value == other.Value || Value == EvalTimesEnum.Any || other.Value == EvalTimesEnum.Once)
                return this;
            else if (other.Value == EvalTimesEnum.Any || Value == EvalTimesEnum.Once)
                return other;
            else
                return Top;
        }

        public EvalTimes Lub(EvalTimes other, BoolRef changed)
        {
            if (Value == other.Value || Value == EvalTimesEnum.Any || other.Value == EvalTimesEnum.Once)
                return this;
            else if (other.Value == EvalTimesEnum.Any || Value == EvalTimesEnum.Once)
            {
                changed.Set();
                return other;
            }
            else
            {
                changed.Set();
                return Top;
            }
        }

        public bool CommutableWith(EvalTimes other)
        {
            return true;
        }

        public bool HasBottom { get { return true; } }
        public bool IsBottom { get { return Value == EvalTimesEnum.Once; } }
        public bool HasTop { get { return true; } }
        public bool IsTop { get { return Value == EvalTimesEnum.Any; } }

        public void Append(Writer w)
        {
            switch (Value)
            {
                case EvalTimesEnum.Once:
                    w.Append("ONCE");
                    break;
                case EvalTimesEnum.AtLeastOnce:
                    w.Append("ALO");
                    break;
                case EvalTimesEnum.Opt:
                    w.Append("OPT");
                    break;
                case EvalTimesEnum.Any:
                    w.Append("ANY");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
