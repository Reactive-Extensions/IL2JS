//
// Storage cells (l-values) in intermediate language
//

using System;
using Microsoft.LiveLabs.Extras;
using Microsoft.LiveLabs.JavaScript.JST;
using JST = Microsoft.LiveLabs.JavaScript.JST;

namespace Microsoft.LiveLabs.CST
{
    public enum CellFlavor
    {
        Variable,
        Field,
        Element,
        Box,
        StatePCPseudo
    }

    // ----------------------------------------------------------------------
    // Cell
    // ----------------------------------------------------------------------

    public abstract class Cell : IEquatable<Cell>
    {
        [CanBeNull]
        public readonly Location Loc;

        public abstract CellFlavor Flavor { get; }

        // Identifier of variable cell, otherwise null
        public abstract JST.Identifier IsVariable { get; }

        // True if cell contents are fixed
        public abstract bool IsReadOnly(CompilationEnvironment env);

        // True if cell can be determined without any "significant" computational work.
        // WARNING: However, it may have read effects (but never write or exception effects).
        public abstract bool IsCheap { get; }

        public abstract TypeRef Type(CompilationEnvironment compEnv);

        public abstract void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage, bool asPointer);

        public abstract void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes);

        public abstract void AccumLvalueEffects(EffectsContext fxCtxt);

        public abstract Cell Simplify(SimplifierContext ctxt);

        public abstract void Append(CSTWriter w);

        public Expression AddressOf()
        {
            return new AddressOfExpression(this);
        }

        public Expression Read()
        {
            return new ReadExpression(new AddressOfExpression(this));
        }

        public Expression Write(Expression value)
        {
            return new WriteExpression(new AddressOfExpression(this), value);
        }

        public override string ToString()
        {
            return CSTWriter.WithAppendDebug(Append);
        }

        public bool Equals(Cell other)
        {
            return Flavor == other.Flavor && EqualBody(other);
        }

        public override bool Equals(object obj)
        {
            var other = obj as Cell;
            return other != null && Equals(other);
        }

        public override int GetHashCode()
        {
            throw new InvalidOperationException("derived type must override");
        }

        protected abstract bool EqualBody(Cell other);
    }

    // ----------------------------------------------------------------------
    // VariableCell
    // ----------------------------------------------------------------------

    public class VariableCell : Cell
    {
        [NotNull]
        public readonly JST.Identifier Id;

        public VariableCell(JST.Identifier id)
        {
            Id = id;
        }

        public override CellFlavor Flavor { get { return CellFlavor.Variable; } }

        public override JST.Identifier IsVariable { get { return Id; } }

        public override bool IsReadOnly(CompilationEnvironment compEnv) { return compEnv.Variable(Id).IsReadOnly; }

        // The cell itself is a value, however the contents may be changing
        public override bool IsCheap { get { return true; } }

        public override TypeRef Type(CompilationEnvironment compEnv)
        {
            return compEnv.Variable(Id).Type;
        }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage, bool asPointer)
        {
            usage.SeenVariable(Id, isAlwaysUsed);
            if (asPointer)
                usage.SeenVariablePointer(Id, isAlwaysUsed);
        }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            if (callCtxt != null && callCtxt.IsOk)
            {
                var idx = default(int);
                if (callCtxt.Parameters.TryGetValue(Id, out idx))
                {
                    if (callCtxt.SeenParameters[idx].HasValue)
                    {
                        if (callCtxt.SeenParameters[idx].Value)
                            // More than once syntactic occurence of this parameter
                            callCtxt.Fail();

                        // Remember we've seen a syntactic occurence of this parameter
                        callCtxt.SeenParameters[idx] = true;

                        if (!callCtxt.AllReadOnly)
                        {
                            for (var i = 0; i < idx; i++)
                            {
                                if (callCtxt.SeenParameters[i].HasValue && !callCtxt.SeenParameters[i].Value)
                                    // Evaluating this parameter before earlier parameters
                                    callCtxt.Fail();
                            }
                        }

                        switch (evalTimes.Value)
                        {
                        case EvalTimesEnum.Once:
                            break;
                        case EvalTimesEnum.Opt:
                            if (!callCtxt.AllReadOnly)
                                // May not always evaluate argument 
                                callCtxt.Fail();
                            break;
                        case EvalTimesEnum.AtLeastOnce:
                        case EvalTimesEnum.Any:
                            // May evaluate argument more than once
                            callCtxt.Fail();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException("evalTimes");
                        }

                        if (!fxCtxt.AccumEffects.CommutableWith(callCtxt.ArgumentEffects[idx]))
                            // Cannot defer evaluation of argument to occurance of parameter
                            callCtxt.Fail();
                    }
                    // else: argument is a value, so no constraints on occurences of parameter
                }
            }

            if (!fxCtxt.IsHidden(Id))
                fxCtxt.IncludeEffects(JST.Effects.Read(Id));
        }

        public override void AccumLvalueEffects(EffectsContext fxCtxt)
        {
            if (!fxCtxt.IsHidden(Id))
                fxCtxt.IncludeEffects(JST.Effects.Write(Id));
        }

        public override Cell Simplify(SimplifierContext ctxt)
        {
            return ctxt.ApplyCell(Id);
        }

        public override void Append(CSTWriter w)
        {
            w.AppendName(Id.Value);
        }

        protected override bool EqualBody(Cell other)
        {
            var temp = (VariableCell)other;
            return Id.Equals(temp.Id);
        }

        public override int GetHashCode()
        {
            var res = 0x08ba4799u;
            res ^= (uint)Id.GetHashCode();
            return (int)res;
        }
    }

    // ----------------------------------------------------------------------
    // FieldCell
    // ----------------------------------------------------------------------

    public class FieldCell : Cell
    {
        [CanBeNull]
        public readonly Expression Object; // null <=> static field
        [NotNull]
        public readonly FieldRef Field;

        public FieldCell(Expression obj, FieldRef field)
        {
            Object = obj;
            Field = field;
        }

        public override CellFlavor Flavor { get { return CellFlavor.Field; } }

        public override JST.Identifier IsVariable { get { return null; } }

        public override bool IsReadOnly(CompilationEnvironment env) { return false; }

        public override bool IsCheap { get { return Object == null || Object.IsCheap; } }

        public override TypeRef Type(CompilationEnvironment compEnv)
        {
            var fieldEnv = Field.Enter(compEnv);
            return fieldEnv.SubstituteType(fieldEnv.Field.FieldType);
        }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage, bool asPointer)
        {
            if (Object != null)
                Object.AccumUsage(compEnv, isAlwaysUsed, usage, false);
            usage.SeenField(Field, isAlwaysUsed);
        }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            if (Object != null)
                Object.AccumEffects(fxCtxt, callCtxt, evalTimes);
            fxCtxt.IncludeEffects(JST.Effects.ReadHeap);
        }

        public override void AccumLvalueEffects(EffectsContext fxCtxt)
        {
            fxCtxt.IncludeEffects(JST.Effects.WriteHeap);
        }

        public override Cell Simplify(SimplifierContext ctxt)
        {
            if (Object == null)
                return this;
            else
            {
                var simpObject = Object.Simplify(ctxt);
                return new FieldCell(simpObject, Field);
            }
        }

        public override void Append(CSTWriter w)
        {
            if (Object == null)
                Field.DefiningType.Append(w);
            else
                Object.Append(w, 1);
            w.Append('.');
            w.AppendName(Field.Name);
        }

        protected override bool EqualBody(Cell other)
        {
            var field = (FieldCell)other;
            if (Object == null && field.Object != null || Object != null && field.Object == null)
                return false;
            if (Object != null && !Object.Equals(field.Object))
                return false;
            return Field.Equals(field.Field);
        }

        public override int GetHashCode()
        {
            var res = 0xfb1fa3ccu;
            if (Object != null)
                res = Constants.Rot3(res) ^ (uint)Object.GetHashCode();
            res = Constants.Rot3(res) ^ (uint)Field.GetHashCode();
            return (int)res;
        }
    }

    // ----------------------------------------------------------------------
    // ElementCell
    // ----------------------------------------------------------------------

    public class ElementCell : Cell
    {
        [NotNull]
        public readonly Expression Array;
        [NotNull]
        public readonly Expression Index;
        public readonly bool isReadOnly;

        public ElementCell(Expression array, Expression index, bool isReadOnly)
        {
            Array = array;
            Index = index;
            this.isReadOnly = isReadOnly;
        }

        public override CellFlavor Flavor { get { return CellFlavor.Element; } }

        public override JST.Identifier IsVariable { get { return null; } }

        // NOTE: Even if this cell is read-only, underlying array may be mutated, thus return false.
        public override bool IsReadOnly(CompilationEnvironment compEnv) { return false; }

        // Array indexing may throw
        public override bool IsCheap { get { return false; } }

        public override TypeRef Type(CompilationEnvironment compEnv)
        {
            var arr = Array.Type(compEnv);
            return arr.Arguments[0];
        }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage, bool asPointer)
        {
            Array.AccumUsage(compEnv, isAlwaysUsed, usage, false);
            Index.AccumUsage(compEnv, isAlwaysUsed, usage, false);
        }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            Array.AccumEffects(fxCtxt, callCtxt, evalTimes);
            Index.AccumEffects(fxCtxt, callCtxt, evalTimes);
            fxCtxt.IncludeEffects(JST.Effects.ReadHeap);
        }

        public override void AccumLvalueEffects(EffectsContext fxCtxt)
        {
            fxCtxt.IncludeEffects(JST.Effects.WriteHeap);
        }

        public override Cell Simplify(SimplifierContext ctxt)
        {
            var simpArray = Array.Simplify(ctxt);
            var simpIndex = Index.Simplify(ctxt);
            return new ElementCell(simpArray, simpIndex, isReadOnly);
        }

        public override void Append(CSTWriter w)
        {
            Array.Append(w, 1);
            w.Append('[');
            Index.Append(w, 0);
            w.Append(']');
        }

        protected override bool EqualBody(Cell other)
        {
            var elem = (ElementCell)other;
            return Array.Equals(elem.Array) && Index.Equals(elem.Index) && isReadOnly == elem.isReadOnly;
        }

        public override int GetHashCode()
        {
            var res = isReadOnly ? 0x8ea5e9f8u : 0xdb3222f8u;
            res ^= (uint)Array.GetHashCode();
            res = Constants.Rot3(res) ^ (uint)Index.GetHashCode();
            return (int)res;
        }
    }

    // ----------------------------------------------------------------------
    // BoxCell
    // ----------------------------------------------------------------------

    public class BoxCell : Cell
    {
        [NotNull]
        public readonly Expression Box;
        [NotNull]
        public readonly TypeRef ValueType;

        public BoxCell(Expression box, TypeRef valueType)
        {
            Box = box;
            ValueType = valueType;
        }

        public override CellFlavor Flavor { get { return CellFlavor.Box; } }

        public override JST.Identifier IsVariable { get { return null; } }

        public override bool IsReadOnly(CompilationEnvironment compEnv) { return true; }

        // Unboxing may throw
        public override bool IsCheap { get { return false; } }

        public override TypeRef Type(CompilationEnvironment compEnv)
        {
            return compEnv.SubstituteType(ValueType);
        }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage, bool asPointer)
        {
            Box.AccumUsage(compEnv, isAlwaysUsed, usage, false);
        }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            Box.AccumEffects(fxCtxt, callCtxt, evalTimes);
            fxCtxt.IncludeEffects(JST.Effects.ReadHeap);
        }

        public override void AccumLvalueEffects(EffectsContext fxCtxt)
        {
            fxCtxt.IncludeEffects(JST.Effects.WriteHeap);
        }

        public override Cell Simplify(SimplifierContext ctxt)
        {
            var simpBox = Box.Simplify(ctxt);
            return new BoxCell(simpBox, ValueType);
        }

        public override void Append(CSTWriter w)
        {
            w.Append("box(");
            Box.Append(w, 0);
            w.Append(':');
            ValueType.Append(w);
            w.Append(')');
        }

        protected override bool EqualBody(Cell other)
        {
            var box = (BoxCell)other;
            return Box.Equals(box.Box) && ValueType.Equals(box.ValueType);
        }

        public override int GetHashCode()
        {
            var res = 0x3c7516dfu;
            res ^= (uint)Box.GetHashCode();
            res = Constants.Rot3(res) ^ (uint)ValueType.GetHashCode();
            return (int)res;
        }
    }

    // ----------------------------------------------------------------------
    // StatePCPseudoCell
    // ----------------------------------------------------------------------

    // Represents cell of current basic block id within state structure
    public class StatePCPseudoCell : Cell
    {
        [NotNull]
        public readonly JST.Identifier StateId; // temporary holding machine state

        public StatePCPseudoCell(JST.Identifier stateId)
        {
            StateId = stateId;
        }

        public override CellFlavor Flavor { get { return CellFlavor.StatePCPseudo; } }

        public override bool IsReadOnly(CompilationEnvironment compEnv) { return false; }

        public override bool IsCheap { get { return true; } }

        public override JST.Identifier IsVariable { get { return null; } }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage, bool asPointer)
        {
            usage.SeenVariable(StateId, isAlwaysUsed);
        }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            fxCtxt.IncludeEffects(JST.Effects.Read(StateId));
            fxCtxt.IncludeEffects(JST.Effects.ReadHeap);
        }

        public override void AccumLvalueEffects(EffectsContext fxCtxt)
        {
            fxCtxt.IncludeEffects(JST.Effects.WriteHeap);
        }

        public override TypeRef Type(CompilationEnvironment compEnv)
        {
            return compEnv.Global.Int32Ref;
        }

        public override Cell Simplify(SimplifierContext ctxt)
        {
            return this;
        }

        public override void Append(CSTWriter w)
        {
            w.Append(StateId.Value);
            w.Append(".PC");
        }

        protected override bool EqualBody(Cell other)
        {
            var state = (StatePCPseudoCell)other;
            return StateId.Equals(state.StateId);
        }

        public override int GetHashCode()
        {
            var res = 0x2f2f2218u;
            res ^= (uint)StateId.GetHashCode();
            return (int)res;
        }
    }

}