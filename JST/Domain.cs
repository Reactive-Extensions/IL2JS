using System;
using System.Linq;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.JavaScript.JST
{
    
    public class VariableEffects : IDomain<VariableEffects>
    {
        [NotNull]
        public static readonly VariableEffects Bottom;

        [NotNull]
        private readonly IImMap<Identifier, ReadWriteDomain> vars;

        static VariableEffects()
        {
            Bottom = new VariableEffects();
        }

        public VariableEffects()
        {
            vars = new Map<Identifier, ReadWriteDomain>();
        }

        public VariableEffects(IImMap<Identifier, ReadWriteDomain> vars)
        {
            this.vars = vars;
        }

        public bool Lte(VariableEffects other)
        {
            foreach (var kv in other.vars)
            {
                var rw = default(ReadWriteDomain);
                if (vars.TryGetValue(kv.Key, out rw))
                {
                    if (!rw.Lte(kv.Value))
                        return false;
                }
            }
            return true;
        }

        public VariableEffects Lub(VariableEffects other)
        {
            var lubVars = new Map<Identifier, ReadWriteDomain>();
            foreach (var kv in vars)
            {
                var rw = default(ReadWriteDomain);
                if (other.vars.TryGetValue(kv.Key, out rw))
                    lubVars.Add(kv.Key, kv.Value.Lub(rw));
                else
                    lubVars.Add(kv.Key, kv.Value);
            }
            foreach (var kv in other.vars)
            {
                if (!vars.ContainsKey(kv.Key))
                    lubVars.Add(kv.Key, kv.Value);
            }
            return new VariableEffects(lubVars);
        }

        public VariableEffects Lub(VariableEffects other, BoolRef changed)
        {
            var lubVars = new Map<Identifier, ReadWriteDomain>();
            foreach (var kv in vars)
            {
                var rw = default(ReadWriteDomain);
                if (other.vars.TryGetValue(kv.Key, out rw))
                    lubVars.Add(kv.Key, kv.Value.Lub(rw, changed));
                else
                    lubVars.Add(kv.Key, kv.Value);
            }
            foreach (var kv in other.vars)
            {
                if (!vars.ContainsKey(kv.Key))
                {
                    lubVars.Add(kv.Key, kv.Value);
                    changed.Set();
                }
            }
            return new VariableEffects(lubVars);
        }

        public bool CommutableWith(VariableEffects other)
        {
            foreach (var kv in vars)
            {
                var rw = default(ReadWriteDomain);
                if (other.vars.TryGetValue(kv.Key, out rw))
                {
                    if (!kv.Value.CommutableWith(rw))
                        return false;
                }
            }
            return true;
        }

        public bool HasBottom { get { return true; } }
        public bool IsBottom { get { return vars.Count == 0; } }
        public bool HasTop { get { return false; } }
        public bool IsTop { get { throw new InvalidOperationException("does not support top"); } }

        public bool IsReadOnly { get { return vars.All(kv => kv.Value.IsReadOnly); } }

        public void Append(Writer w)
        {
            w.Append('{');
            var first = true;
            foreach (var kv in vars)
            {
                if (first)
                    first = false;
                else
                    w.Append(',');
                kv.Key.Append(w);
                w.Append(':');
                kv.Value.Append(w);
            }
            w.Append('}');
        }

        public override string ToString()
        {
            return Writer.WithAppend(Append);
        }

        public static VariableEffects Read(Identifier id)
        {
            return new VariableEffects
                (new Map<Identifier, ReadWriteDomain> { { id, ReadWriteDomain.Read } });
        }

        public static VariableEffects Write(Identifier id)
        {
            return new VariableEffects
                (new Map<Identifier, ReadWriteDomain> { { id, ReadWriteDomain.Top } });
        }
    }

    public class Effects : IDomain<Effects>
    {
        [NotNull]
        private static DroppedDomain<VariableEffects> BottomVars;
        [NotNull]
        private static DroppedDomain<VariableEffects> TopVars;

        [NotNull]
        public static readonly Effects Bottom;
        [NotNull]
        public static readonly Effects WriteAll;
        [NotNull]
        public static readonly Effects ReadHeap;
        [NotNull]
        public static readonly Effects WriteHeap;
        [NotNull]
        public static readonly Effects Throws;
        [NotNull]
        public static readonly Effects Top;

        [NotNull]
        public readonly DroppedDomain<VariableEffects> Vars;
        [NotNull]
        public readonly ReadWriteDomain Heap;
        [NotNull]
        public readonly BooleanDomain MayThrow;

        static Effects()
        {
            BottomVars = new DroppedDomain<VariableEffects>(VariableEffects.Bottom);
            TopVars = DroppedDomain<VariableEffects>.Top;

            Bottom = new Effects(BottomVars, ReadWriteDomain.Bottom, BooleanDomain.Bottom);
            WriteAll = new Effects(TopVars, ReadWriteDomain.Bottom, BooleanDomain.Bottom);
            ReadHeap = new Effects(BottomVars, ReadWriteDomain.Read, BooleanDomain.Bottom);
            WriteHeap = new Effects(BottomVars, ReadWriteDomain.Top, BooleanDomain.Bottom);
            Throws = new Effects(BottomVars, ReadWriteDomain.Bottom, BooleanDomain.Top);
            Top = new Effects(TopVars, ReadWriteDomain.Top, BooleanDomain.Top);
        }

        public Effects(DroppedDomain<VariableEffects> vars, ReadWriteDomain heap, BooleanDomain mayThrow)
        {
            Vars = vars;
            Heap = heap;
            MayThrow = mayThrow;
        }

        public bool Lte(Effects other)
        {
            if (Vars == null && other.Vars != null)
                return false;
            if (Vars != null && other.Vars != null && !Vars.Lte(other.Vars))
                return false;
            return Heap.Lte(other.Heap) && MayThrow.Lte(other.MayThrow);
        }

        public Effects Lub(Effects other)
        {
            var vars = Vars.Lub(other.Vars);
            var heap = Heap.Lub(other.Heap);
            var mayThrow = MayThrow.Lub(other.MayThrow);
            if (vars == null || heap == null || mayThrow == null)
                return null;
            return new Effects(vars, heap, mayThrow);
        }

        public Effects Lub(Effects other, BoolRef changed)
        {
            var thisChanged = new BoolRef();
            var vars = Vars.Lub(other.Vars, thisChanged);
            var heap = Heap.Lub(other.Heap, thisChanged);
            var mayThrow = MayThrow.Lub(other.MayThrow, thisChanged);
            if (vars == null || heap == null || mayThrow == null)
                return null;
            if (thisChanged.Value)
            {
                changed.Set();
                return new Effects(vars, heap, mayThrow);
            }
            else
                return this;
        }

        public bool HasBottom { get { return true; } }
        public bool IsBottom { get { return Vars.IsBottom && Heap.IsBottom && MayThrow.IsBottom; } }
        public bool HasTop { get { return true; } }
        public bool IsTop { get { return Vars.IsTop && Heap.IsWrite && MayThrow.IsTop; } }

        public bool IsReadOnly
        {
            get { return !Vars.IsTop && Vars.Value.IsReadOnly && Heap.IsReadOnly && MayThrow.IsBottom; }
        }

        public Effects HideVars()
        {
            return new Effects(BottomVars, Heap, MayThrow);
        }

        public Effects WithoutThrows()
        {
            return new Effects(Vars, Heap, BooleanDomain.Bottom);
        }

        public bool CommutableWith(Effects other)
        {
            return Vars.CommutableWith(other.Vars) && Heap.CommutableWith(other.Heap) && MayThrow.CommutableWith(other.MayThrow);
        }

        public void Append(Writer w)
        {
            if (IsBottom)
                w.Append("BOT");
            else if (IsTop)
                w.Append("TOP");
            else
            {
                w.Append('{');
                w.Append("{vars:");
                Vars.Append(w);
                w.Append(",heap:");
                Heap.Append(w);
                w.Append(",mayThrow:");
                MayThrow.Append(w);
                w.Append('}');
            }
        }

        public override string ToString()
        {
            return Writer.WithAppend(Append);
        }

        public static Effects Read(Identifier id)
        {
            return new Effects
                (new DroppedDomain<VariableEffects>(VariableEffects.Read(id)),
                 ReadWriteDomain.Bottom,
                 BooleanDomain.Bottom);
        }

        public static Effects Write(Identifier id)
        {
            return new Effects
                (new DroppedDomain<VariableEffects>(VariableEffects.Write(id)),
                 ReadWriteDomain.Bottom,
                 BooleanDomain.Bottom);
        }
    }
}