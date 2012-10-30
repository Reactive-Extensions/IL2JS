//
// Expressions in intermediate language
//

using System;
using System.Linq;
using Microsoft.LiveLabs.Extras;
using Microsoft.LiveLabs.JavaScript.JST;
using JST = Microsoft.LiveLabs.JavaScript.JST;

namespace Microsoft.LiveLabs.CST
{
    public enum ExpressionFlavor
    {
        Null,
        TypeHandle,
        FieldHandle,
        MethodHandle,
        CodePointer,
        Int32,
        Int64,
        Single,
        Double,
        String,
        Unary,
        Binary,
        Convert,
        Read,
        Write,
        AddressOf,
        ConditionalDeref,
        Call,
        NewObject,
        NewArray,
        NewBox,
        Cast,
        Clone,
        IsInst,
        IfThenElse,
        ImportExport,
        CallImportedPseudo,
        CallInlinedPseudo,
        StatementsPseudo,
        InitialStatePseudo
    }

    // ----------------------------------------------------------------------
    // Expression
    // ----------------------------------------------------------------------

    public abstract class Expression : IEquatable<Expression>
    {
        [CanBeNull] // currently always null
        public readonly Location Loc;

        public abstract ExpressionFlavor Flavor { get; }

        // True if expression is a value, ie can be duplicated and is uneffected by other expressions
        public abstract bool IsValue(CompilationEnvironment compEnv);

        // True if expression does not require any "significant" computational work.
        // WARNING: However, it may have read effects (but never write or exception effects).
        public abstract bool IsCheap { get; }

        // Stack type of expression
        public abstract TypeRef Type(CompilationEnvironment compEnv);

        public abstract void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage, bool inReadWrite);

        public abstract void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes);

        public abstract void AccumLvalueEffects(EffectsContext fxCtxt);

        public virtual Cell IsAddressOfCell { get { return null; } }

        public virtual bool IsCondition(CompilationEnvironment compEnv)
        {
            return false;
        }

        public abstract Expression Simplify(SimplifierContext ctxt);

        // Can negation of this expression be expressed without requiring a wrapping negation operator?
        public virtual bool CanAbsorbLogNot(CompilationEnvironment compEnv) { return false; }

        public virtual Expression SimplifyLogNot(SimplifierContext ctxt)
        {
            return new UnaryExpression(Simplify(ctxt), UnaryOp.LogNot, false, false);
        }

        // If expression colud be a structure, wrap it in a 'clone' operator to make value semantics explicit
        public Expression CloneIfStruct(CompilationEnvironment compEnv)
        {
            var typeRef = Type(compEnv);
            var s = typeRef.Style(compEnv);

            if (s is NullableTypeStyle)
                s = typeRef.Arguments[0].Style(compEnv);

            if (s is StructTypeStyle || s is ParameterTypeStyle)
                return new CloneExpression(this, typeRef);
            else
                return this;
        }

        public abstract void Append(CSTWriter w, int prec);

        public override string ToString()
        {
            return CSTWriter.WithAppendDebug(w => Append(w, 0));
        }

        protected void Wrap(CSTWriter w, int prec, int min, Action<CSTWriter, int> f)
        {
            if (prec > min)
                w.Append('(');
            f(w, 0);
            if (prec > min)
                w.Append(')');
        }

        public bool Equals(Expression other)
        {
            return Flavor == other.Flavor && EqualBody(other);
        }

        public override bool Equals(object obj)
        {
            var other = obj as Expression;
            return other != null && Equals(other);
        }

        public override int GetHashCode()
        {
            throw new InvalidOperationException("derived type must override");
        }

        protected abstract bool EqualBody(Expression other);
    }

    // ----------------------------------------------------------------------
    // NullConstantExpression
    // ----------------------------------------------------------------------

    public class NullConstantExpression : Expression
    {
        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.Null; } }

        public override bool IsValue(CompilationEnvironment compEnv) { return true; }

        public override bool IsCheap { get { return true; } }

        public override TypeRef Type(CompilationEnvironment compEnv)
        {
            return compEnv.Global.NullRef;
        }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage, bool inReadWrite)
        {
        }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
        }

        public override void AccumLvalueEffects(EffectsContext fxCtxt)
        {
            fxCtxt.IncludeEffects(JST.Effects.Throws);
        }

        public override Expression Simplify(SimplifierContext ctxt)
        {
            return this;
        }

        public override void Append(CSTWriter w, int prec)
        {
            w.Append("null");
        }

        protected override bool EqualBody(Expression other)
        {
            return true;
        }

        public override int GetHashCode()
        {
            var res = 0xd07e9efeu;
            return (int)res;
        }
    }

    // ----------------------------------------------------------------------
    // TypeHandleConstantExpression
    // ----------------------------------------------------------------------

    public class TypeHandleConstantExpression : Expression
    {
        [NotNull]
        public readonly TypeRef RuntimeType;

        public TypeHandleConstantExpression(TypeRef runtimeType)
        {
            RuntimeType = runtimeType;
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.TypeHandle; } }

        public override bool IsValue(CompilationEnvironment compEnv) { return true; }

        public override bool IsCheap { get { return true; } }

        public override TypeRef Type(CompilationEnvironment compEnv)
        {
            return compEnv.Global.RuntimeTypeHandleRef;
        }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage, bool inReadWrite)
        {
            RuntimeType.AccumUsage(usage, isAlwaysUsed);
        }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
        }

        public override void AccumLvalueEffects(EffectsContext fxCtxt)
        {
            fxCtxt.IncludeEffects(JST.Effects.Throws);
        }

        public override Expression Simplify(SimplifierContext ctxt)
        {
            return this;
        }

        public override void Append(CSTWriter w, int prec)
        {
            w.Append("typeof(");
            RuntimeType.Append(w);
            w.Append(')');
        }

        protected override bool EqualBody(Expression other)
        {
            var handle = (TypeHandleConstantExpression)other;
            return RuntimeType.Equals(handle.RuntimeType);
        }

        public override int GetHashCode()
        {
            var res = 0x2dd1d35bu;
            res ^= (uint)RuntimeType.GetHashCode();
            return (int)res;
        }
    }

    // ----------------------------------------------------------------------
    // FieldHandleConstantExpression
    // ----------------------------------------------------------------------

    public class FieldHandleConstantExpression : Expression
    {
        [NotNull]
        public readonly FieldRef RuntimeField;

        public FieldHandleConstantExpression(FieldRef runtimeField)
        {
            RuntimeField = runtimeField;
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.FieldHandle; } }

        public override bool IsValue(CompilationEnvironment compEnv) { return true; }

        public override bool IsCheap { get { return true; } }

        public override TypeRef Type(CompilationEnvironment compEnv)
        {
            return compEnv.Global.RuntimeFieldHandleRef;
        }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage, bool inReadWrite)
        {
            usage.SeenField(RuntimeField, isAlwaysUsed);
        }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
        }

        public override void AccumLvalueEffects(EffectsContext fxCtxt)
        {
            fxCtxt.IncludeEffects(JST.Effects.Throws);
        }

        public override Expression Simplify(SimplifierContext ctxt)
        {
            return this;
        }

        public override void Append(CSTWriter w, int prec)
        {
            w.Append("fieldof(");
            RuntimeField.Append(w);
            w.Append(')');
        }

        protected override bool EqualBody(Expression other)
        {
            var handle = (FieldHandleConstantExpression)other;
            return RuntimeField.Equals(handle.RuntimeField);
        }

        public override int GetHashCode()
        {
            var res = 0xb4a84fe0u;
            res ^= (uint)RuntimeField.GetHashCode();
            return (int)res;
        }
    }

    // ----------------------------------------------------------------------
    // MethodHandleConstantExpression
    // ----------------------------------------------------------------------

    public class MethodHandleConstantExpression : Expression
    {
        [NotNull]
        public readonly MethodRef RuntimeMethod;

        public MethodHandleConstantExpression(MethodRef runtimeMethod)
        {
            RuntimeMethod = runtimeMethod;
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.MethodHandle; } }

        public override bool IsValue(CompilationEnvironment compEnv) { return true; }

        public override bool IsCheap { get { return true; } }

        public override TypeRef Type(CompilationEnvironment compEnv)
        {
            return compEnv.Global.RuntimeMethodHandleRef;
        }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage, bool inReadWrite)
        {
            usage.SeenMethod(RuntimeMethod, isAlwaysUsed);
        }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
        }

        public override void AccumLvalueEffects(EffectsContext fxCtxt)
        {
            fxCtxt.IncludeEffects(JST.Effects.Throws);
        }

        public override Expression Simplify(SimplifierContext ctxt)
        {
            return this;
        }

        public override void Append(CSTWriter w, int prec)
        {
            w.Append("methodof(");
            RuntimeMethod.Append(w);
            w.Append(')');
        }

        protected override bool EqualBody(Expression other)
        {
            var handle = (MethodHandleConstantExpression)other;
            return RuntimeMethod.Equals(handle.RuntimeMethod);
        }

        public override int GetHashCode()
        {
            var res = 0x7cc43b81u;
            res ^= (uint)RuntimeMethod.GetHashCode();
            return (int)res;
        }
    }

    // ----------------------------------------------------------------------
    // CodePointerExpression
    // ----------------------------------------------------------------------

    public class CodePointerExpression : Expression
    {
        [CanBeNull] // non-null => virtual function
        public readonly Expression Object;
        [NotNull]
        public readonly MethodRef Method;

        public CodePointerExpression(Expression obj, MethodRef method)
        {
            Object = obj;
            Method = method;
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.CodePointer; } }

        public override bool IsValue(CompilationEnvironment compEnv) { return Object == null || Object.IsValue(compEnv); }

        public override bool IsCheap { get { return Object == null || Object.IsCheap; } }

        public override TypeRef Type(CompilationEnvironment compEnv)
        {
            return Method.ToCodePointer(compEnv);
        }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage, bool inReadWrite)
        {
            if (Object != null)
                Object.AccumUsage(compEnv, isAlwaysUsed, usage, false);
            usage.SeenMethod(Method, isAlwaysUsed);
        }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            if (Object != null)
            {
                Object.AccumEffects(fxCtxt, callCtxt, evalTimes);
                fxCtxt.IncludeEffects(JST.Effects.Throws);
            }
        }

        public override void AccumLvalueEffects(EffectsContext fxCtxt)
        {
            fxCtxt.IncludeEffects(JST.Effects.Throws);
        }

        public override Expression Simplify(SimplifierContext ctxt)
        {
            if (Object == null)
                return this;
            else
            {
                var simpObject = Object.Simplify(ctxt);
                return new CodePointerExpression(simpObject, Method);
            }
        }

        public override void Append(CSTWriter w, int prec)
        {
            Wrap
                (w,
                 prec,
                 0,
                 (w2, prec2) =>
                 {
                     if (Object != null)
                     {
                         Object.Append(w2, 1);
                         w.Append('.');
                     }
                     w.Append("method(");
                     Method.Append(w2);
                     w.Append(')');
                 });
        }

        protected override bool EqualBody(Expression other)
        {
            var func = (CodePointerExpression)other;
            if (Object == null && func.Object != null || Object != null && func.Object == null)
                return false;
            if (Object != null && !Object.Equals(func.Object))
                return false;
            return Method.Equals(func.Method);
        }

        public override int GetHashCode()
        {
            var res = 0x62fb1341u;
            if (Object != null)
                res = Constants.Rot3(res) ^ (uint)Object.GetHashCode();
            res ^= (uint)Method.GetHashCode();
            return (int)res;
        }
    }

    // ----------------------------------------------------------------------
    // Int32ConstantExpression
    // ----------------------------------------------------------------------

    public class Int32ConstantExpression : Expression
    {
        public readonly int Value;

        public Int32ConstantExpression(int value)
        {
            Value = value;
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.Int32; } }

        public override bool IsValue(CompilationEnvironment compEnv) { return true; }

        public override bool IsCheap { get { return true; } }

        public override TypeRef Type(CompilationEnvironment compEnv)
        {
            return compEnv.Global.Int32Ref;
        }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage, bool inReadWrite)
        {
        }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
        }

        public override void AccumLvalueEffects(EffectsContext fxCtxt)
        {
            fxCtxt.IncludeEffects(JST.Effects.Throws);
        }

        public override Expression Simplify(SimplifierContext ctxt)
        {
            return this;
        }

        public override bool CanAbsorbLogNot(CompilationEnvironment compEnv) { return true; }

        public override Expression SimplifyLogNot(SimplifierContext ctxt)
        {
            return new Int32ConstantExpression(Value == 0 ? 1 : 0);
        }

        public override void Append(CSTWriter w, int prec)
        {
            w.Append(Value);
        }

        protected override bool EqualBody(Expression other)
        {
            var con = (Int32ConstantExpression)other;
            return Value == con.Value;
        }

        public override int GetHashCode()
        {
            var res = 0x93cc7314u;
            res ^= (uint)Value;
            return (int)res;
        }
    }

    // ----------------------------------------------------------------------
    // Int64ConstantExpression
    // ----------------------------------------------------------------------

    public class Int64ConstantExpression : Expression
    {
        public readonly long Value;

        public Int64ConstantExpression(long value)
        {
            Value = value;
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.Int64; } }

        public override bool IsValue(CompilationEnvironment compEnv) { return true; }

        public override bool IsCheap { get { return true; } }

        public override TypeRef Type(CompilationEnvironment compEnv)
        {
            return compEnv.Global.Int64Ref;
        }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage, bool inReadWrite)
        {
        }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
        }

        public override void AccumLvalueEffects(EffectsContext fxCtxt)
        {
            fxCtxt.IncludeEffects(JST.Effects.Throws);
        }

        public override Expression Simplify(SimplifierContext ctxt)
        {
            return this;
        }

        public override void Append(CSTWriter w, int prec)
        {
            w.Append(Value);
            w.Append('l');
        }

        protected override bool EqualBody(Expression other)
        {
            var con = (Int64ConstantExpression)other;
            return Value == con.Value;
        }

        public override int GetHashCode()
        {
            var res = 0x93cc7314u;
            res ^= (uint)((ulong)Value >> 32);
            res ^= (uint)((ulong)Value & ((1LU << 32) - 1));
            return (int)res;
        }
    }
    
    // ----------------------------------------------------------------------
    // SingleConstantExpression
    // ----------------------------------------------------------------------

    public class SingleConstantExpression : Expression
    {
        public readonly float Value;

        public SingleConstantExpression(float value)
        {
            Value = value;
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.Single; } }

        public override bool IsValue(CompilationEnvironment compEnv) { return true; }

        public override bool IsCheap { get { return true; } }

        public override TypeRef Type(CompilationEnvironment compEnv)
        {
            return compEnv.Global.DoubleRef; // since stack type
        }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage, bool inReadWrite)
        {
        }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
        }

        public override void AccumLvalueEffects(EffectsContext fxCtxt)
        {
            fxCtxt.IncludeEffects(JST.Effects.Throws);
        }

        public override Expression Simplify(SimplifierContext ctxt)
        {
            return this;
        }

        public override void Append(CSTWriter w, int prec)
        {
            w.Append(Value);
            w.Append('f');
        }

        protected override bool EqualBody(Expression other)
        {
            var con = (SingleConstantExpression)other;
            return Value == con.Value;
        }

        public override int GetHashCode()
        {
            var res = 0x8ff6e2fbu;
            var v = (ulong)BitConverter.DoubleToInt64Bits(Value);
            res ^= (uint)(v >> 32);
            res ^= (uint)(v & ((1UL << 32) - 1));
            return (int)res;
        }
    }

    // ----------------------------------------------------------------------
    // DoubleConstantExpression
    // ----------------------------------------------------------------------

    public class DoubleConstantExpression : Expression
    {
        public double Value;

        public DoubleConstantExpression(double value)
        {
            Value = value;
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.Double; } }

        public override bool IsValue(CompilationEnvironment compEnv) { return true; }

        public override bool IsCheap { get { return true; } }

        public override TypeRef Type(CompilationEnvironment compEnv)
        {
            return compEnv.Global.DoubleRef;
        }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage, bool inReadWrite)
        {
        }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
        }

        public override void AccumLvalueEffects(EffectsContext fxCtxt)
        {
            fxCtxt.IncludeEffects(JST.Effects.Throws);
        }

        public override Expression Simplify(SimplifierContext ctxt)
        {
            return this;
        }

        public override void Append(CSTWriter w, int prec)
        {
            w.Append(Value);
        }

        protected override bool EqualBody(Expression other)
        {
            var con = (DoubleConstantExpression)other;
            return Value == con.Value;
        }

        public override int GetHashCode()
        {
            var res = 0xcee4c6e8u;
            var v = (ulong)BitConverter.DoubleToInt64Bits(Value);
            res ^= (uint)(v >> 32);
            res ^= (uint)(v & ((1LU << 32) - 1));
            return (int)res;
        }
    }

    // ----------------------------------------------------------------------
    // StringConstantExpression
    // ----------------------------------------------------------------------

    public class StringConstantExpression : Expression
    {
        [NotNull]
        public string Value;

        public StringConstantExpression(string value)
        {
            Value = value;
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.String; } }

        public override bool IsValue(CompilationEnvironment compEnv) { return true; }

        public override bool IsCheap { get { return true; } }

        public override TypeRef Type(CompilationEnvironment compEnv)
        {
            return compEnv.Global.StringRef;
        }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage, bool inReadWrite)
        {
        }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
        }

        public override void AccumLvalueEffects(EffectsContext fxCtxt)
        {
            fxCtxt.IncludeEffects(JST.Effects.Throws);
        }

        public override Expression Simplify(SimplifierContext ctxt)
        {
            return this;
        }

        public override void Append(CSTWriter w, int prec)
        {
            w.AppendQuotedString(Value);
        }

        protected override bool EqualBody(Expression other)
        {
            var con = (StringConstantExpression)other;
            return Value.Equals(con.Value, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            var res = 0x8b021fa1u;
            res ^= (uint)Value.GetHashCode();
            return (int)res;
        }
    }

    // ----------------------------------------------------------------------
    // UnaryExpression
    // ----------------------------------------------------------------------

    public enum UnaryOp
    {
        CheckFinite,       // double => double
        Length,            // array => int32
        Neg,               // x => -x                  where x is number
        BitNot,            // x => ~x                  where x is integer
        LogNot,            // x => !x                  where x is integer
        IsZero,            // x => x == 0 ? 1 : 0      where x is integer
        IsNonZero,         // x => x != 0 ? 1 : 0      where x is integer
        IsNull,            // x => x == null ? 1 : 0   where x is object
        IsNonNull,         // x => x != null ? 1 : 0   where x is object
    }

    public class UnaryExpression : Expression
    {
        [NotNull]
        public readonly Expression Value;
        public readonly UnaryOp Op;
        public readonly bool WithOverflow;
        public readonly bool IsUnsigned;

        public UnaryExpression(Expression value, UnaryOp op, bool withOverflow, bool isUnsigned)
        {
            Value = value;
            Op = op;
            WithOverflow = withOverflow;
            IsUnsigned = isUnsigned;
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.Unary; } }

        public override bool IsValue(CompilationEnvironment compEnv) { return false; }

        public override bool IsCheap { get { return false; } }

        public override TypeRef Type(CompilationEnvironment compEnv)
        {
            switch (Op)
            {
                case UnaryOp.CheckFinite:
                    return compEnv.Global.DoubleRef;
                case UnaryOp.Length:
                case UnaryOp.LogNot:
                case UnaryOp.IsZero:
                case UnaryOp.IsNonZero:
                case UnaryOp.IsNull:
                case UnaryOp.IsNonNull:
                    return compEnv.Global.Int32Ref;
                case UnaryOp.Neg:
                case UnaryOp.BitNot:
                    return Value.Type(compEnv);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage, bool inReadWrite)
        {
            Value.AccumUsage(compEnv, isAlwaysUsed, usage, false);
        }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            Value.AccumEffects(fxCtxt, callCtxt, evalTimes);
            if (Op == UnaryOp.CheckFinite || WithOverflow)
                fxCtxt.IncludeEffects(JST.Effects.Throws);
        }

        public override void AccumLvalueEffects(EffectsContext fxCtxt)
        {
            fxCtxt.IncludeEffects(JST.Effects.Throws);
        }

        public override bool IsCondition(CompilationEnvironment compEnv)
        {
            switch (Op)
            {
                case UnaryOp.CheckFinite:
                case UnaryOp.Length:
                case UnaryOp.Neg:
                    return false;
                case UnaryOp.BitNot:
                    return Value.IsCondition(compEnv);
                case UnaryOp.LogNot:
                case UnaryOp.IsZero:
                case UnaryOp.IsNonZero:
                case UnaryOp.IsNull:
                case UnaryOp.IsNonNull:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override Expression Simplify(SimplifierContext ctxt)
        {
            switch (Op)
            {
            case UnaryOp.BitNot:
                {
                    if (Value.IsCondition(ctxt.CompEnv))
                    {
                        if (Value.CanAbsorbLogNot(ctxt.CompEnv))
                            return Value.SimplifyLogNot(ctxt);
                        else
                        {
                            var simpValue = Value.Simplify(ctxt);
                            return new UnaryExpression(simpValue, UnaryOp.LogNot, false, false);
                        }
                    }
                    // else: fall-through
                    break;
                }
            case UnaryOp.LogNot:
                {
                    if (Value.CanAbsorbLogNot(ctxt.CompEnv))
                        return Value.SimplifyLogNot(ctxt);
                    // else: fall-through
                    break;
                }
            case UnaryOp.IsZero:
                {
                    if (Value.IsCondition(ctxt.CompEnv))
                    {
                        if (Value.CanAbsorbLogNot(ctxt.CompEnv))
                            return Value.SimplifyLogNot(ctxt);
                        else
                        {
                            var simpValue = Value.Simplify(ctxt);
                            new UnaryExpression(simpValue, UnaryOp.LogNot, false, false);
                        }
                    }
                    // else: fall-through
                    break;
                }
            case UnaryOp.IsNonZero:
                {
                    if (Value.IsCondition(ctxt.CompEnv))
                        return Value.Simplify(ctxt);
                    // else: fall-through
                    break;
                }
            default:
                // fall-through
                break;
            }
            {
                var simpValue = Value.Simplify(ctxt);
                return new UnaryExpression(simpValue, Op, WithOverflow, IsUnsigned);
            }
        }

        public override bool CanAbsorbLogNot(CompilationEnvironment compEnv)
        {
            switch (Op)
            {
            case UnaryOp.BitNot:
                return Value.IsCondition(compEnv);
            case UnaryOp.LogNot:
            case UnaryOp.IsZero:
            case UnaryOp.IsNonZero:
            case UnaryOp.IsNull:
            case UnaryOp.IsNonNull:
                return true;
            default:
                return false;
            }
        }

        public override Expression SimplifyLogNot(SimplifierContext ctxt)
        {
            switch (Op)
            {
            case UnaryOp.BitNot:
            case UnaryOp.LogNot:
                        return Value.Simplify(ctxt);
            case UnaryOp.IsZero:
                {
                    if (Value.IsCondition(ctxt.CompEnv))
                        return Value.Simplify(ctxt);
                    else
                    {
                        var simpValue = Value.Simplify(ctxt);
                        return new UnaryExpression(simpValue, UnaryOp.IsNonZero, WithOverflow, IsUnsigned);
                    }
                }
            case UnaryOp.IsNonZero:
                {
                    if (Value.IsCondition(ctxt.CompEnv))
                        return Value.SimplifyLogNot(ctxt);
                    else
                    {
                        var simpValue = Value.Simplify(ctxt);
                        return new UnaryExpression(simpValue, UnaryOp.IsZero, WithOverflow, IsUnsigned);
                    }
                }
            case UnaryOp.IsNull:
                {
                    var simpValue = Value.Simplify(ctxt);
                    return new UnaryExpression(simpValue, UnaryOp.IsNonNull, WithOverflow, IsUnsigned);
                }
            case UnaryOp.IsNonNull:
                {
                    var simpValue = Value.Simplify(ctxt);
                    return new UnaryExpression(simpValue, UnaryOp.IsNull, WithOverflow, IsUnsigned);
                }
            default:
                throw new InvalidOperationException("cannot simplify");
            }
        }

        public override void Append(CSTWriter w, int prec)
        {
            if (WithOverflow)
            {
                w.Append("overflow(");
                prec = 0;
            }
            if (IsUnsigned)
            {
                w.Append("unsigned(");
                prec = 0;
            }
            Wrap
                (w,
                 prec,
                 0,
                 (w2, prec2) =>
                 {
                     switch (Op)
                     {
                         case UnaryOp.CheckFinite:
                             w2.Append("checkFinite ");
                             break;
                         case UnaryOp.Length:
                             w2.Append("length ");
                             break;
                         case UnaryOp.Neg:
                             w2.Append("-");
                             break;
                         case UnaryOp.BitNot:
                             w2.Append("~");
                             break;
                         case UnaryOp.LogNot:
                             w2.Append("!");
                             break;
                         case UnaryOp.IsZero:
                             w2.Append("iszero ");
                             break;
                         case UnaryOp.IsNonZero:
                             w2.Append("isnonzero ");
                             break;
                         case UnaryOp.IsNull:
                             w2.Append("isnull ");
                             break;
                         case UnaryOp.IsNonNull:
                             w2.Append("isnonnull ");
                             break;
                         default:
                             throw new ArgumentOutOfRangeException();
                     }
                      Value.Append(w2, 0);
                  });
            if (IsUnsigned)
                w.Append(')');
            if (WithOverflow)
                w.Append(')');
        }

        protected override bool EqualBody(Expression other)
        {
            var un = (UnaryExpression)other;
            return Op == un.Op && Value.Equals(un.Value) && WithOverflow == un.WithOverflow && IsUnsigned == un.IsUnsigned;
        }

        public override int GetHashCode()
        {
            var res = default(uint);
            switch (Op)
            {
                case UnaryOp.CheckFinite:
                    res = 0xef20cadau;
                    break;
                case UnaryOp.Length:
                    res = 0x36774c01u;
                    break;
                case UnaryOp.Neg:
                    res = 0x2bf11fb4u;
                    break;
                case UnaryOp.BitNot:
                    res = 0x95dbda4du;
                    break;
                case UnaryOp.LogNot:
                    res = 0xae909198u;
                    break;
                case UnaryOp.IsZero:
                    res = 0x02e5b9c5u;
                    break;
                case UnaryOp.IsNonZero:
                    res = 0x83260376u;
                    break;
                case UnaryOp.IsNull:
                    res = 0x6295cfa9u;
                    break;
                case UnaryOp.IsNonNull:
                    res = 0x11c81968u;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            if (WithOverflow)
                res ^= 0xae1e7e49u;
            if (IsUnsigned)
                res ^= 0xd95a537fu;
            res ^= (uint)Value.GetHashCode();
            return (int)res;
        }
    }

    // ----------------------------------------------------------------------
    // BinaryExpression
    // ----------------------------------------------------------------------

    public enum BinaryOp
    {
        Eq,                // (x, x) => int32          where x is number, address or object
        Ne,                // (x, x) => int32          where x is number, address or object
        Lt,                // (x, x) => int32          where x is number
        Le,                // (x, x) => int32          where x is number
        Gt,                // (x, x) => int32          where x is number
        Ge,                // (x, x) => int32          where x is number
        Add,               // (x, x) => x              where x is number
        Sub,               // (x, x) => x              where x is number
        Mul,               // (x, x) => x              where x is number
        Div,               // (x, x) => x              where x is number
        Rem,               // (x, x) => x              where x is number
        LogAnd,            // (int32, int32) => int32
        LogOr,             // (int32, int32) => int32
        BitAnd,            // (x, x) => x              where x is integer
        BitOr,             // (x, x) => x              where x is integer
        BitXor,            // (x, x) => x              where x is integer
        Shl,               // (x, int32) => x          where x is integer
        Shr                // (x, int32) => x          where x is integer
    }

    public class BinaryExpression : Expression
    {
        [NotNull]
        public readonly Expression LeftValue;
        public readonly BinaryOp Op;
        [NotNull]
        public readonly Expression RightValue;
        public readonly bool WithOverflow;
        public readonly bool IsUnsigned;

        public BinaryExpression(Expression leftValue, BinaryOp op, Expression rightValue, bool withOverflow, bool isUnsigned)
        {
            LeftValue = leftValue;
            Op = op;
            RightValue = rightValue;
            WithOverflow = withOverflow;
            IsUnsigned = isUnsigned;
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.Binary; } }

        public override bool IsValue(CompilationEnvironment compEnv) { return false; }

        public override bool IsCheap { get { return false; } }

        public override TypeRef Type(CompilationEnvironment compEnv)
        {
            switch (Op)
            {
                case BinaryOp.Eq:
                case BinaryOp.Ne:
                case BinaryOp.Lt:
                case BinaryOp.Le:
                case BinaryOp.Gt:
                case BinaryOp.Ge:
                case BinaryOp.LogAnd:
                case BinaryOp.LogOr:
                    return compEnv.Global.Int32Ref;
                case BinaryOp.Add:
                case BinaryOp.Sub:
                case BinaryOp.Mul:
                case BinaryOp.Div:
                case BinaryOp.Rem:
                case BinaryOp.BitAnd:
                case BinaryOp.BitOr:
                case BinaryOp.BitXor:
                case BinaryOp.Shl:
                case BinaryOp.Shr:
                    return LeftValue.Type(compEnv);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage, bool inReadWrite)
        {
            LeftValue.AccumUsage(compEnv, isAlwaysUsed, usage, false);
            if (Op == BinaryOp.LogAnd || Op == BinaryOp.LogOr)
                isAlwaysUsed = false;
            RightValue.AccumUsage(compEnv, isAlwaysUsed, usage, false);
        }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            LeftValue.AccumEffects(fxCtxt, callCtxt, evalTimes);
            if (callCtxt != null && (Op == BinaryOp.LogAnd || Op == BinaryOp.LogOr))
                evalTimes = evalTimes.Lub(EvalTimes.Opt);
            RightValue.AccumEffects(fxCtxt, callCtxt, evalTimes);
            if (WithOverflow || Op == BinaryOp.Div || Op == BinaryOp.Rem)
                fxCtxt.IncludeEffects(JST.Effects.Throws);
        }

        public override void AccumLvalueEffects(EffectsContext fxCtxt)
        {
            fxCtxt.IncludeEffects(JST.Effects.Throws);
        }

        public override bool IsCondition(CompilationEnvironment compEnv)
        {
            switch (Op)
            {
                case BinaryOp.Eq:
                case BinaryOp.Ne:
                case BinaryOp.Lt:
                case BinaryOp.Le:
                case BinaryOp.Gt:
                case BinaryOp.Ge:
                case BinaryOp.LogAnd:
                case BinaryOp.LogOr:
                    return true;
                case BinaryOp.Add:
                case BinaryOp.Sub:
                case BinaryOp.Mul:
                case BinaryOp.Div:
                case BinaryOp.Rem:
                case BinaryOp.BitAnd:
                case BinaryOp.BitOr:
                case BinaryOp.BitXor:
                case BinaryOp.Shl:
                case BinaryOp.Shr:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override Expression Simplify(SimplifierContext ctxt)
        {
            var leftSimp = LeftValue.Simplify(ctxt);
            var rightSimp = RightValue.Simplify(ctxt);
            if (Op == BinaryOp.Div || Op == BinaryOp.Rem)
                ctxt.IncludeEffects(JST.Effects.Throws);
            return new BinaryExpression(leftSimp, Op, rightSimp, WithOverflow, IsUnsigned);
        }

        public override bool CanAbsorbLogNot(CompilationEnvironment compEnv)
        {
            switch (Op)
            {
            case BinaryOp.Eq:
            case BinaryOp.Ne:
            case BinaryOp.Lt:
            case BinaryOp.Le:
            case BinaryOp.Gt:
            case BinaryOp.Ge:
                return true;
            case BinaryOp.LogAnd:
            case BinaryOp.LogOr:
                return LeftValue.CanAbsorbLogNot(compEnv) && RightValue.CanAbsorbLogNot(compEnv);
            default:
                return false;
            }
        }

        public override Expression SimplifyLogNot(SimplifierContext ctxt)
        {
            switch (Op)
            {
            case BinaryOp.Eq:
                return new BinaryExpression
                    (LeftValue.Simplify(ctxt), BinaryOp.Ne, RightValue.Simplify(ctxt), WithOverflow, IsUnsigned);
            case BinaryOp.Ne:
                return new BinaryExpression
                    (LeftValue.Simplify(ctxt), BinaryOp.Eq, RightValue.Simplify(ctxt), WithOverflow, IsUnsigned);
            case BinaryOp.Lt:
                return new BinaryExpression
                    (LeftValue.Simplify(ctxt), BinaryOp.Ge, RightValue.Simplify(ctxt), WithOverflow, IsUnsigned);
            case BinaryOp.Le:
                return new BinaryExpression
                    (LeftValue.Simplify(ctxt), BinaryOp.Gt, RightValue.Simplify(ctxt), WithOverflow, IsUnsigned);
            case BinaryOp.Gt:
                return new BinaryExpression
                    (LeftValue.Simplify(ctxt), BinaryOp.Le, RightValue.Simplify(ctxt), WithOverflow, IsUnsigned);
            case BinaryOp.Ge:
                return new BinaryExpression
                    (LeftValue.Simplify(ctxt), BinaryOp.Lt, RightValue.Simplify(ctxt), WithOverflow, IsUnsigned);
            case BinaryOp.LogAnd:
                return new BinaryExpression
                    (LeftValue.SimplifyLogNot(ctxt),
                     BinaryOp.LogOr,
                     RightValue.SimplifyLogNot(ctxt),
                     WithOverflow,
                     IsUnsigned);
            case BinaryOp.LogOr:
                return new BinaryExpression
                    (LeftValue.SimplifyLogNot(ctxt),
                     BinaryOp.LogAnd,
                     RightValue.SimplifyLogNot(ctxt),
                     WithOverflow,
                     IsUnsigned);
            default:
                throw new InvalidOperationException("cannot simplify");
            }
        }

        public override void Append(CSTWriter w, int prec)
        {
            if (WithOverflow)
            {
                w.Append("overflow(");
                prec = 0;
            }
            if (IsUnsigned)
            {
                w.Append("unsigned(");
                prec = 0;
            }
            Wrap
                (w,
                 prec,
                 0,
                 (w2, prec2) =>
                 {
                     LeftValue.Append(w2, 1);
                     w2.Append(' ');
                     switch (Op)
                     {
                         case BinaryOp.Eq:
                             w2.Append("==");
                             break;
                         case BinaryOp.Ne:
                             w2.Append("!=");
                             break;
                         case BinaryOp.Lt:
                             w2.Append("<");
                             break;
                         case BinaryOp.Le:
                             w2.Append("<=");
                             break;
                         case BinaryOp.Gt:
                             w2.Append(">");
                             break;
                         case BinaryOp.Ge:
                             w2.Append(">=");
                             break;
                         case BinaryOp.Add:
                             w2.Append("+");
                             break;
                         case BinaryOp.Sub:
                             w2.Append("-");
                             break;
                         case BinaryOp.Mul:
                             w2.Append("*");
                             break;
                         case BinaryOp.Div:
                             w2.Append("/");
                             break;
                         case BinaryOp.Rem:
                             w2.Append("%");
                             break;
                         case BinaryOp.LogAnd:
                             w2.Append("&&");
                             break;
                         case BinaryOp.LogOr:
                             w2.Append("||");
                             break;
                         case BinaryOp.BitAnd:
                             w2.Append("&");
                             break;
                         case BinaryOp.BitOr:
                             w2.Append("|");
                             break;
                         case BinaryOp.BitXor:
                             w2.Append("^");
                             break;
                         case BinaryOp.Shl:
                             w2.Append("<<");
                             break;
                         case BinaryOp.Shr:
                             w2.Append(">>");
                             break;
                         default:
                             throw new ArgumentOutOfRangeException();
                     }
                     w.Append(' ');
                     RightValue.Append(w, 1);
                 });
            if (IsUnsigned)
                w.Append(')');
            if (WithOverflow)
                w.Append(')');
        }

        protected override bool EqualBody(Expression other)
        {
            var bin = (BinaryExpression)other;
            return Op == bin.Op && LeftValue.Equals(bin.LeftValue) && RightValue.Equals(bin.RightValue) &&
                   WithOverflow == bin.WithOverflow && IsUnsigned == bin.IsUnsigned;
        }

        public override int GetHashCode()
        {
            var res = default(uint);
            switch (Op)
            {
                case BinaryOp.Eq:
                    res = 0xd542a8f6u;
                    break;
                case BinaryOp.Ne:
                    res = 0x287effc3u;
                    break;
                case BinaryOp.Lt:
                    res = 0xac6732c6u;
                    break;
                case BinaryOp.Le:
                    res = 0x8c4f5573u;
                    break;
                case BinaryOp.Gt:
                    res = 0x695b27b0u;
                    break;
                case BinaryOp.Ge:
                    res = 0xbbca58c8u;
                    break;
                case BinaryOp.Add:
                    res = 0xe1ffa35du;
                    break;
                case BinaryOp.Sub:
                    res = 0xb8f011a0u;
                    break;
                case BinaryOp.Mul:
                    res = 0x10fa3d98u;
                    break;
                case BinaryOp.Div:
                    res = 0x8e7594b7u;
                    break;
                case BinaryOp.Rem:
                    res = 0xf2122b64u;
                    break;
                case BinaryOp.LogAnd:
                    res = 0x8888b812u;
                    break;
                case BinaryOp.LogOr:
                    res = 0x900df01cu;
                    break;
                case BinaryOp.BitAnd:
                    res = 0x4fad5ea0u;
                    break;
                case BinaryOp.BitOr:
                    res = 0x688fc31cu;
                    break;
                case BinaryOp.BitXor:
                    res = 0x6b93d5a0u;
                    break;
                case BinaryOp.Shl:
                    res = 0xd08ed1d0u;
                    break;
                case BinaryOp.Shr:
                    res = 0xafc725e0u;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            if (WithOverflow)
                res ^= 0xd6411bd3u;
            if (IsUnsigned)
                res ^= 0xff34052eu;
            res ^= (uint)LeftValue.GetHashCode();
            res = Constants.Rot3(res) ^ (uint)RightValue.GetHashCode();
            return (int)res;
        }
    }

    // ----------------------------------------------------------------------
    // ConvertExpression
    // ----------------------------------------------------------------------

    public class ConvertExpression : Expression
    {
        [NotNull]
        public readonly Expression Value;
        [NotNull]
        public readonly TypeRef ResultType; // as stack type
        public readonly bool WithOverflow;
        public readonly bool IsUnsigned;

        public ConvertExpression(Expression value, TypeRef resultType, bool withOverflow, bool isUnsigned)
        {
            Value = value;
            ResultType = resultType;
            WithOverflow = withOverflow;
            IsUnsigned = isUnsigned;
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.Convert; } }

        public override bool IsValue(CompilationEnvironment compEnv) { return false; }

        public override bool IsCheap { get { return false; } }

        public override TypeRef Type(CompilationEnvironment compEnv)
        {
            return compEnv.SubstituteType(ResultType);
        }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage, bool inReadWrite)
        {
            Value.AccumUsage(compEnv, isAlwaysUsed, usage, false);
            // For the moment, conversions don't rely on result type
            // compEnv.SubstituteType(ResultType).AccumUsage(usage, evalTimes);
        }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            Value.AccumEffects(fxCtxt, callCtxt, evalTimes);
            if (WithOverflow)
                fxCtxt.IncludeEffects(JST.Effects.Throws);
        }

        public override void AccumLvalueEffects(EffectsContext fxCtxt)
        {
            fxCtxt.IncludeEffects(JST.Effects.Throws);
        }

        public override bool IsCondition(CompilationEnvironment compEnv)
        {
            return ResultType.Style(compEnv) is Int32TypeStyle && Value.IsCondition(compEnv);
        }

        public override Expression Simplify(SimplifierContext ctxt)
        {
            var simpValue = Value.Simplify(ctxt);
            if (simpValue.Type(ctxt.CompEnv).IsEquivalentTo(ctxt.CompEnv, ResultType))
                return simpValue;
            else
            {
                if (WithOverflow)
                    ctxt.IncludeEffects(JST.Effects.Throws);
                return new ConvertExpression(simpValue, ResultType, WithOverflow, IsUnsigned);
            }
        }

        public override void Append(CSTWriter w, int prec)
        {
            if (WithOverflow)
                w.Append("overflow(");
            if (IsUnsigned)
                w.Append("unsigned(");
            w.Append("conv(");
            Value.Append(w, 0);
            w.Append(',');
            ResultType.Append(w);
            w.Append(')');
            if (IsUnsigned)
                w.Append(')');
            if (WithOverflow)
                w.Append(')');
        }

        protected override bool EqualBody(Expression other)
        {
            var conv = (ConvertExpression)other;
            return Value.Equals(conv.Value) && ResultType.Equals(conv.ResultType) && WithOverflow == conv.WithOverflow &&
                   IsUnsigned == conv.IsUnsigned;
        }

        public override int GetHashCode()
        {
            var res = 0x2071b35eu;
            if (WithOverflow)
                res ^= 0x4e734a41u;
            if (IsUnsigned)
                res ^= 0xe7b9f9b6u;
            res ^= (uint)Value.GetHashCode();
            res = Constants.Rot3(res) ^ (uint)ResultType.GetHashCode();
            return (int)res;
        }
    }

    // ----------------------------------------------------------------------
    // ReadExpression
    // ----------------------------------------------------------------------

    public class ReadExpression : Expression
    {
        [NotNull]
        public readonly Expression Address;

        public ReadExpression(Expression address)
        {
            Address = address;
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.Read; } }

        public override bool IsValue(CompilationEnvironment compEnv)
        {
            var cell = Address.IsAddressOfCell;
            if (cell == null)
                return false;
            else
                return cell.IsReadOnly(compEnv);
        }

        public override bool IsCheap
        {
            get
            {
                var cell = Address.IsAddressOfCell;
                if (cell == null)
                    return false;
                else
                    return cell.IsCheap;
            }
        }

        public override TypeRef Type(CompilationEnvironment compEnv)
        {
            var ptr = Address.Type(compEnv);
            return ptr.Arguments[0];
        }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage, bool inReadWrite)
        {
            Address.AccumUsage(compEnv, isAlwaysUsed, usage, true);
        }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            Address.AccumEffects(fxCtxt, callCtxt, evalTimes);
        }

        public override void AccumLvalueEffects(EffectsContext fxCtxt)
        {
            fxCtxt.IncludeEffects(JST.Effects.WriteAll);
            fxCtxt.IncludeEffects(JST.Effects.WriteHeap);
        }

        public override Expression Simplify(SimplifierContext ctxt)
        {
            var cell = Address.IsAddressOfCell;
            if (cell != null)
            {
                var id = cell.IsVariable;
                if (id != null)
                    return ctxt.ApplyReadFrom(id);
                // else: fall-through
            }
            // else: fall-through

            var simpAddr = Address.Simplify(ctxt);
            return new ReadExpression(simpAddr);
        }

        public override void Append(CSTWriter w, int prec)
        {
            var cell = Address.IsAddressOfCell;
            if (cell == null)
            {
                Wrap
                    (w,
                     prec,
                     0,
                     (w2, prec2) =>
                         {
                             w.Append('*');
                             Address.Append(w, 1);
                         });
            }
            else
                cell.Append(w);
        }

        protected override bool EqualBody(Expression other)
        {
            var read = (ReadExpression)other;
            return Address.Equals(read.Address);
        }

        public override int GetHashCode()
        {
            var res = 0x7b3e89a0u;
            res ^= (uint)Address.GetHashCode();
            return (int)res;
        }
    }

    // ----------------------------------------------------------------------
    // WriteExpression
    // ----------------------------------------------------------------------

    public class WriteExpression : Expression
    {
        [NotNull]
        public readonly Expression Address;
        [NotNull]
        public readonly Expression Value;

        public WriteExpression(Expression address, Expression value)
        {
            Address = address;
            Value = value;
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.Write; } }

        public override bool IsValue(CompilationEnvironment compEnv) { return false; }

        public override bool IsCheap { get { return false; } }

        public override TypeRef Type(CompilationEnvironment compEnv)
        {
            return Value.Type(compEnv);
        }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage, bool inReadWrite)
        {
            Address.AccumUsage(compEnv, isAlwaysUsed, usage, true);
            Value.AccumUsage(compEnv, isAlwaysUsed, usage, false);
        }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            Address.AccumEffects(fxCtxt, callCtxt, evalTimes);
            Value.AccumEffects(fxCtxt, callCtxt, evalTimes);
            Address.AccumLvalueEffects(fxCtxt);
        }

        public override void AccumLvalueEffects(EffectsContext fxCtxt)
        {
            fxCtxt.IncludeEffects(JST.Effects.Throws);
        }

        public override Expression Simplify(SimplifierContext ctxt)
        {
            var simpAddr = Address.Simplify(ctxt);
            var simpValue = Value.Simplify(ctxt);
            return new WriteExpression(simpAddr, simpValue);
        }

        public override void Append(CSTWriter w, int prec)
        {
            Wrap
                (w,
                 prec,
                 0,
                 (w2, prec2) =>
                     {
                         var cell = Address.IsAddressOfCell;
                         if (cell == null)
                         {
                             w2.Append('*');
                             Address.Append(w2, 1);
                         }
                         else
                             cell.Append(w);
                         w.Append(" = ");
                         Value.Append(w2, 0);
                     });
        }

        protected override bool EqualBody(Expression other)
        {
            var write = (WriteExpression)other;
            return Address.Equals(write.Address) && Value.Equals(write.Value);
        }

        public override int GetHashCode()
        {
            var res = 0x77b5fa86u;
            res ^= (uint)Address.GetHashCode();
            res = Constants.Rot3(res) ^ (uint)Value.GetHashCode();
            return (int)res;
        }
    }

    // ----------------------------------------------------------------------
    // AddressOfExpression
    // ----------------------------------------------------------------------

    public class AddressOfExpression : Expression
    {
        [NotNull]
        public readonly Cell Cell;

        public AddressOfExpression(Cell cell)
        {
            Cell = cell;
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.AddressOf; } }

        public override bool IsValue(CompilationEnvironment compEnv) { return false; }

        public override bool IsCheap { get { return Cell.IsCheap; } }

        public override Cell IsAddressOfCell { get { return Cell; } }

        public override TypeRef Type(CompilationEnvironment compEnv)
        {
            return compEnv.Global.ManagedPointerTypeConstructorRef.ApplyTo(Cell.Type(compEnv));
        }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage, bool inReadWrite)
        {
            Cell.AccumUsage(compEnv, isAlwaysUsed, usage, !inReadWrite);
        }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            Cell.AccumEffects(fxCtxt, callCtxt, evalTimes);
        }

        public override void AccumLvalueEffects(EffectsContext fxCtxt)
        {
            Cell.AccumLvalueEffects(fxCtxt);
        }

        public override Expression Simplify(SimplifierContext ctxt)
        {
            var simpCell = Cell.Simplify(ctxt);
            return new AddressOfExpression(simpCell);
        }

        public override void Append(CSTWriter w, int prec)
        {
            Wrap
                (w,
                 prec,
                 0,
                 (w2, prec2) =>
                 {
                     w.Append('&');
                     Cell.Append(w2);
                 });
        }

        protected override bool EqualBody(Expression other)
        {
            var addr = (AddressOfExpression)other;
            return Cell.Equals(addr.Cell);
        }

        public override int GetHashCode()
        {
            var res = 0xb6636521u;
            res ^= (uint)Cell.GetHashCode();
            return (int)res;
        }
    }

    // ----------------------------------------------------------------------
    // ConditionalDerefExpression
    // ----------------------------------------------------------------------

    public class ConditionalDerefExpression : Expression
    {
        [NotNull]
        public readonly Expression Address;
        [NotNull]
        public readonly TypeRef ConstrainedType; // as stack type

        public ConditionalDerefExpression(Expression address, TypeRef constrainedType)
        {
            Address = address;
            ConstrainedType = constrainedType;
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.ConditionalDeref; } }

        public override bool IsValue(CompilationEnvironment compEnv) { return false; }

        public override bool IsCheap { get { return false; } }

        public override TypeRef Type(CompilationEnvironment compEnv)
        {
            var type = compEnv.SubstituteType(ConstrainedType);
            var s = type.Style(compEnv);
            if (s is ValueTypeStyle)
                return compEnv.Global.ManagedPointerTypeConstructorRef.ApplyTo(type);
            else if (s is ReferenceTypeStyle)
                return type;
            else if (s is ParameterTypeStyle)
                return compEnv.Global.BoxTypeConstructorRef.ApplyTo(type);
            else
                throw new InvalidOperationException("ill-typed intermediate expression");
        }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage, bool inReadWrite)
        {
            Address.AccumUsage(compEnv, isAlwaysUsed, usage, false);
            compEnv.SubstituteType(ConstrainedType).AccumUsage(usage, isAlwaysUsed);
        }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            Address.AccumEffects(fxCtxt, callCtxt, evalTimes);
            fxCtxt.IncludeEffects(JST.Effects.Throws);
        }

        public override void AccumLvalueEffects(EffectsContext fxCtxt)
        {
            fxCtxt.IncludeEffects(JST.Effects.Throws);
        }

        public override Expression Simplify(SimplifierContext ctxt)
        {
            var simpAddr = Address.Simplify(ctxt);
            return new ConditionalDerefExpression(simpAddr, ConstrainedType);
        }

        public override void Append(CSTWriter w, int prec)
        {
            w.Append("condderef(");
            Address.Append(w, 0);
            w.Append(',');
            ConstrainedType.Append(w);
            w.Append(')');
        }

        protected override bool EqualBody(Expression other)
        {
            var deref = (ConditionalDerefExpression)other;
            return Address.Equals(deref.Address) && ConstrainedType.Equals(deref.ConstrainedType);
        }

        public override int GetHashCode()
        {
            var res = 0x7b14a94au;
            res ^= (uint)Address.GetHashCode();
            res = Constants.Rot3(res) ^ (uint)ConstrainedType.GetHashCode();
            return (int)res;
        }
    }

    // ----------------------------------------------------------------------
    // CallExpression
    // ----------------------------------------------------------------------

    public enum CallFlavor
    {
        Normal,
        Virtual,
        // Implicit first argument is not to be passed, and call returns value of method's defining type
        // (Used when replacing "new .ctor(x, y)" with ".ctor(x, y)")
        Factory
    }

    public class CallExpression : Expression
    {
        public readonly CallFlavor CallFlavor;
        [NotNull]
        public readonly MethodRef Method;
        [NotNull]
        public readonly IImSeq<Expression> Arguments; // includes target object if instance method

        public CallExpression(CallFlavor callFlavor, MethodRef method, IImSeq<Expression> arguments)
        {
            CallFlavor = callFlavor;
            Method = method;
            Arguments = arguments ?? Constants.EmptyExpressions;
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.Call; } }

        public override bool IsValue(CompilationEnvironment compEnv) { return false; }

        public override bool IsCheap { get { return false; } }

        public override TypeRef Type(CompilationEnvironment compEnv)
        {
            switch (CallFlavor)
            {
                case CallFlavor.Normal:
                case CallFlavor.Virtual:
                    {
                        var targetMethEnv = Method.EnterMethod(compEnv);
                        if (targetMethEnv.Method.Result == null)
                            return null;
                        else
                            return targetMethEnv.SubstituteType(targetMethEnv.Method.Result.Type).ToRunTimeType
                                (compEnv, true);
                    }
                case CallFlavor.Factory:
                    return Method.DefiningType.ToRunTimeType(compEnv, true); ;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage, bool inReadWrite)
        {
            if (Method.IsStatic || CallFlavor == CallFlavor.Factory)
                compEnv.SubstituteType(Method.DefiningType).AccumUsage(usage, isAlwaysUsed);
            // else: all instance calls go via the instance itself and don't require the type structure
            usage.SeenMethod(compEnv.SubstituteMember(Method), isAlwaysUsed);
            foreach (var e in Arguments)
                e.AccumUsage(compEnv, isAlwaysUsed, usage, false);
        }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            foreach (var e in Arguments)
                e.AccumEffects(fxCtxt, callCtxt, evalTimes);
            fxCtxt.IncludeEffects(JST.Effects.Top);
        }

        public override void AccumLvalueEffects(EffectsContext fxCtxt)
        {
            fxCtxt.IncludeEffects(JST.Effects.Throws);
        }

        public override Expression Simplify(SimplifierContext ctxt)
        {
            var isFactory = CallFlavor == CallFlavor.Factory;
            var delta = isFactory ? 1 : 0;
            var arity = Method.ValueParameters.Count - delta;
            if (Arguments.Count != arity)
                throw new InvalidOperationException("mismatched method arity");

            // NOTE: Inlining will supress check for non-null instance of virtual calls to non-virtuals
            if (ctxt.Database.IsInlinableImported(Method))
            {
                var inlinedMethEnv = Method.EnterMethod(ctxt.CompEnv);
                var exportedArgs = new Seq<Expression>(Arguments.Count);
                for (var i = 0; i < Arguments.Count; i++)
                {
                    if (ctxt.Database.IsNoInteropParameter(Method, i + delta))
                        exportedArgs.Add(Arguments[i]);
                    else
                        exportedArgs.Add
                            (new ImportExportExpression
                                 (false,
                                  Arguments[i],
                                  inlinedMethEnv.SubstituteType(inlinedMethEnv.Method.ValueParameters[i + delta].Type)));
                }
                var call = new CallImportedExpression(isFactory, Method, exportedArgs);
                var import = default(Expression);
                if (isFactory)
                {
                    if (ctxt.Database.IsNoInteropParameter(Method, 0))
                        import = call;
                    else
                        import = new ImportExportExpression(true, call, Method.DefiningType);
                }
                else if (inlinedMethEnv.Method.Result == null || ctxt.Database.IsNoInteropResult(Method))
                    import = call;
                else
                {
                    import = new ImportExportExpression
                        (true, call, inlinedMethEnv.SubstituteType(inlinedMethEnv.Method.Result.Type));
                }
                ctxt.IncludeEffects(JST.Effects.Top);
                return import.Simplify(ctxt);
            }
            else if (!isFactory && ctxt.Database.IsInlinable(Method))
            {
                // Since a method definition with IsInlinable true may not be emitted, we MUST inline this method

                // Translate method body, using fresh name supply for all args/locals/temporaries, and
                // in environment which will bind type parameters with actual type arguments. Result
                // will have all type parameters fully substituted.
                var inlinedMethEnv = Method.EnterMethod(ctxt.CompEnv);
                var cstmethod = CSTMethod.Translate(inlinedMethEnv, new JST.NameSupply(), ctxt.Trace);

                // Extract body as statements and optional return  result
                var body = new Seq<Statement>();
                var retres = cstmethod.Body.ToReturnResult(body);
                if (retres.Status != ReturnStatus.One)
                    throw new InvalidOperationException("unexpected return structure");

                var simpArguments = Arguments.Select(e => e.Simplify(ctxt)).ToSeq();

                // Can we safely substitute arguments for parameters in the function body and result?
                var callCtxt = new CallContext(ctxt.CompEnv, cstmethod.CompEnv, simpArguments);
                var fxCtxt = new EffectsContext(null);
                foreach (var kv in cstmethod.CompEnv.Variables)
                    fxCtxt.Bind(kv.Key);
                foreach (var s in body)
                    s.AccumEffects(fxCtxt, callCtxt, EvalTimes.Bottom);
                if (retres.Value != null)
                    retres.Value.AccumEffects(fxCtxt, callCtxt, EvalTimes.Bottom);
                callCtxt.Final();

                var subCtxt = ctxt.InSubMethod();
                if (callCtxt.IsOk)
                {
                    for (var i = 0; i < simpArguments.Count; i++)
                        subCtxt.BindArgument(cstmethod.CompEnv.ValueParameterIds[i], simpArguments[i]);
                }
                else
                {
                    for (var i = 0; i < simpArguments.Count; i++)
                    {
                        if (simpArguments[i].IsValue(ctxt.CompEnv))
                            subCtxt.BindArgument(cstmethod.CompEnv.ValueParameterIds[i], simpArguments[i]);
                        else
                        {
                            var newid = subCtxt.FreshenArgument
                                (cstmethod.CompEnv.ValueParameterIds[i],
                                 inlinedMethEnv.SubstituteType(cstmethod.CompEnv.Method.ValueParameters[i].Type));
                            var cell = new VariableCell(newid);
                            subCtxt.Add(new ExpressionStatement(cell.Write(simpArguments[i])));
                        }
                    }
                }
                subCtxt.FreshenLocals(cstmethod.CompEnv);
                foreach (var s in body)
                    s.Simplify(subCtxt);
                var finalValue = retres.Value == null ? null : retres.Value.Simplify(subCtxt);

                if (ctxt.Statements != null)
                {
                    var subFxCtxt = new EffectsContext(null);
                    foreach (var s in subCtxt.Statements)
                        s.AccumEffects(subFxCtxt, null, null);
                    if (subFxCtxt.AccumEffects.CommutableWith(ctxt.ContextEffects))
                    {
                        foreach (var s in subCtxt.Statements)
                            ctxt.Add(s);
                        return finalValue;
                    }
                }
                return new StatementsPseudoExpression(new Statements(subCtxt.Statements), finalValue);
            }
            else
            {
                var simpArguments = Arguments.Select(e => e.Simplify(ctxt)).ToSeq();
                ctxt.IncludeEffects(JST.Effects.Top);
                return new CallExpression(CallFlavor, Method, simpArguments);
            }
        }

        public override void Append(CSTWriter w, int prec)
        {
            Method.Append(w);
            w.Append('(');
            switch (CallFlavor)
            {
                case CallFlavor.Normal:
                    w.Append("normal");
                    break;
                case CallFlavor.Virtual:
                    w.Append("virtual");
                    break;
                case CallFlavor.Factory:
                    w.Append("factory");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            for (var i = 0; i < Arguments.Count; i++)
            {
                w.Append(',');
                Arguments[i].Append(w, 0);
            }
            w.Append(')');
        }

        protected override bool EqualBody(Expression other)
        {
            var call = (CallExpression)other;
            if (CallFlavor != call.CallFlavor || !Method.Equals(call.Method) || Arguments.Count != call.Arguments.Count)
                return false;
            for (var i = 0; i < Arguments.Count; i++)
            {
                if (!Arguments[i].Equals(call.Arguments[i]))
                    return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            var res = default(uint);
            switch (CallFlavor)
            {
                case CallFlavor.Normal:
                    res = 0x9cee60b8u;
                    break;
                case CallFlavor.Virtual:
                    res = 0xc75442f5u;
                    break;
                case CallFlavor.Factory:
                    res = 0x53b02d5du;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            res ^= (uint)Method.GetHashCode();
            for (var i = 0; i < Arguments.Count; i++)
                res = Constants.Rot3(res) ^ (uint)Arguments[i].GetHashCode();
            return (int)res;
        }
    }

    // ----------------------------------------------------------------------
    // NewObjectExpression
    // ----------------------------------------------------------------------

    public class NewObjectExpression : Expression
    {
        [NotNull]
        public readonly MethodRef Method;
        [NotNull]
        public readonly IImSeq<Expression> Arguments; // does not include argument for first parameter

        public NewObjectExpression(MethodRef method, IImSeq<Expression> arguments)
        {
            Method = method;
            Arguments = arguments ?? Constants.EmptyExpressions;
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.NewObject; } }

        public override bool IsValue(CompilationEnvironment compEnv) { return false; }

        public override bool IsCheap { get { return false; } }

        public override TypeRef Type(CompilationEnvironment compEnv)
        {
            return compEnv.SubstituteType(Method.DefiningType).ToRunTimeType(compEnv, true);
        }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage, bool inReadWrite)
        {
            usage.SeenMethod(compEnv.SubstituteMember(Method), isAlwaysUsed);
            compEnv.SubstituteType(Method.DefiningType).AccumUsage(usage, isAlwaysUsed);
            foreach (var e in Arguments)
                e.AccumUsage(compEnv, isAlwaysUsed, usage, false);
        }

        public override void  AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            foreach (var e in Arguments)
                e.AccumEffects(fxCtxt, callCtxt, evalTimes);
            fxCtxt.IncludeEffects(JST.Effects.Top);
        }

        public override void AccumLvalueEffects(EffectsContext fxCtxt)
        {
            fxCtxt.IncludeEffects(JST.Effects.Throws);
        }

        public override Expression Simplify(SimplifierContext ctxt)
        {
            if (ctxt.Database.IsFactory(Method))
            {
                var res = new CallExpression(CallFlavor.Factory, Method, Arguments);
                return res.Simplify(ctxt);
            }
            else
            {
                var simpArguments = Arguments.Select(e => e.Simplify(ctxt)).ToSeq();
                return new NewObjectExpression(Method, simpArguments);
            }
        }

        public override void Append(CSTWriter w, int prec)
        {
            w.Append("new ");
            Method.Append(w);
            w.Append('(');
            for (var i = 0; i < Arguments.Count; i++)
            {
                if (i > 0)
                    w.Append(',');
                Arguments[i].Append(w, 0);
            }
            w.Append(')');
        }

        protected override bool EqualBody(Expression other)
        {
            var newobj = (NewObjectExpression)other;
            if (!Method.Equals(newobj.Method) || Arguments.Count != newobj.Arguments.Count)
                return false;
            for (var i = 0; i < Arguments.Count; i++)
            {
                if (!Arguments[i].Equals(newobj.Arguments[i]))
                    return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            var res = 0x59dfa6aau;
            res ^= (uint)Method.GetHashCode();
            for (var i = 0; i < Arguments.Count; i++)
                res = Constants.Rot3(res) ^ (uint)Arguments[i].GetHashCode();
            return (int)res;
        }
    }

    // ----------------------------------------------------------------------
    // NewArrayExpression
    // ----------------------------------------------------------------------

    public class NewArrayExpression : Expression
    {
        [NotNull]
        public readonly Expression Length;
        [NotNull]
        public readonly TypeRef ElementType; // as stack type

        public NewArrayExpression(Expression length, TypeRef elementType)
        {
            Length = length;
            ElementType = elementType;
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.NewArray; } }

        public override bool IsValue(CompilationEnvironment compEnv) { return false; }

        public override bool IsCheap { get { return false; } }

        public override TypeRef Type(CompilationEnvironment compEnv)
        {
            return compEnv.Global.ArrayTypeConstructorRef.ApplyTo(compEnv.SubstituteType(ElementType));
        }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage, bool inReadWrite)
        {
            Length.AccumUsage(compEnv, isAlwaysUsed, usage, false);
            compEnv.SubstituteType(ElementType).AccumUsage(usage, isAlwaysUsed);
        }

        public override void  AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            Length.AccumEffects(fxCtxt, callCtxt, evalTimes);
        }

        public override void AccumLvalueEffects(EffectsContext fxCtxt)
        {
            fxCtxt.IncludeEffects(JST.Effects.Throws);
        }

        public override Expression Simplify(SimplifierContext ctxt)
        {
            var simpLength = Length.Simplify(ctxt);
            return new NewArrayExpression(simpLength, ElementType);
        }

        public override void Append(CSTWriter w, int prec)
        {
            w.Append("new ");
            ElementType.Append(w);
            w.Append('[');
            Length.Append(w, 0);
            w.Append(']');
        }

        protected override bool EqualBody(Expression other)
        {
            var newarr = (NewArrayExpression)other;
            return Length.Equals(newarr.Length) && ElementType.Equals(newarr.ElementType);
        }

        public override int GetHashCode()
        {
            var res = 0xc5855664u;
            res ^= (uint)Length.GetHashCode();
            res = Constants.Rot3(res) ^ (uint)ElementType.GetHashCode();
            return (int)res;
        }
    }

    // ----------------------------------------------------------------------
    // NewBoxExpression
    // ----------------------------------------------------------------------

    public class NewBoxExpression : Expression
    {
        [NotNull]
        public readonly Expression Value;
        [NotNull]
        public readonly TypeRef ValueType;

        public NewBoxExpression(Expression value, TypeRef valueType)
        {
            Value = value;
            ValueType = valueType;
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.NewBox; } }

        public override bool IsValue(CompilationEnvironment compEnv) { return false; }

        public override bool IsCheap { get { return false; } }

        public override TypeRef Type(CompilationEnvironment compEnv)
        {
            return compEnv.Global.BoxTypeConstructorRef.ApplyTo(compEnv.SubstituteType(ValueType));
        }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage, bool inReadWrite)
        {
            Value.AccumUsage(compEnv, isAlwaysUsed, usage, false);
            compEnv.SubstituteType(ValueType).AccumUsage(usage, isAlwaysUsed);
        }

        public override void  AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            Value.AccumEffects(fxCtxt, callCtxt, evalTimes);
        }

        public override void AccumLvalueEffects(EffectsContext fxCtxt)
        {
            fxCtxt.IncludeEffects(JST.Effects.Throws);
        }

        public override Expression Simplify(SimplifierContext ctxt)
        {
            var simpValue = Value.Simplify(ctxt);
            return new NewBoxExpression(simpValue, ValueType);
        }

        public override void Append(CSTWriter w, int prec)
        {
            w.Append("newbox(");
            Value.Append(w, 0);
            w.Append(':');
            ValueType.Append(w);
            w.Append(')');
        }

        protected override bool EqualBody(Expression other)
        {
            var newbox = (NewBoxExpression)other;
            return Value.Equals(newbox.Value) && ValueType.Equals(newbox.ValueType);
        }

        public override int GetHashCode()
        {
            var res = 0x1b510052u;
            res ^= (uint)Value.GetHashCode();
            res = Constants.Rot3(res) ^ (uint)ValueType.GetHashCode();
            return (int)res;
        }
    }

    // ----------------------------------------------------------------------
    // CastExpression
    // ----------------------------------------------------------------------

    public class CastExpression : Expression
    {
        [NotNull]
        public readonly Expression Value;
        [NotNull]
        public readonly TypeRef ResultType; // as stack type

        public CastExpression(Expression value, TypeRef resultType)
        {
            Value = value;
            ResultType = resultType;
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.Cast; } }

        public override bool IsValue(CompilationEnvironment compEnv) { return false; }

        public override bool IsCheap { get { return false; } }

        public override TypeRef Type(CompilationEnvironment compEnv)
        {
            return compEnv.SubstituteType(ResultType);
        }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage, bool inReadWrite)
        {
            Value.AccumUsage(compEnv, isAlwaysUsed, usage, false);
            compEnv.SubstituteType(ResultType).AccumUsage(usage, isAlwaysUsed);
        }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            Value.AccumEffects(fxCtxt, callCtxt, evalTimes);
            fxCtxt.IncludeEffects(JST.Effects.Throws);
        }

        public override void AccumLvalueEffects(EffectsContext fxCtxt)
        {
            fxCtxt.IncludeEffects(JST.Effects.Throws);
        }

        public override Expression Simplify(SimplifierContext ctxt)
        {
            var simpValue = Value.Simplify(ctxt);
            return new CastExpression(simpValue, ResultType);
        }

        public override void Append(CSTWriter w, int prec)
        {
            Wrap
                (w,
                 prec,
                 0,
                 (w2, prec2) =>
                 {
                     w2.Append('(');
                     ResultType.Append(w2);
                     w2.Append(')');
                     Value.Append(w2, 1);
                 });
        }

        protected override bool EqualBody(Expression other)
        {
            var cast = (CastExpression)other;
            return Value.Equals(cast.Value) && ResultType.Equals(cast.ResultType);
        }

        public override int GetHashCode()
        {
            var res = 0xd60f573fu;
            res ^= (uint)Value.GetHashCode();
            res = Constants.Rot3(res) ^ (uint)ResultType.GetHashCode();
            return (int)res;
        }
    }

    // ----------------------------------------------------------------------
    // CloneExpression
    // ----------------------------------------------------------------------

    public class CloneExpression : Expression
    {
        [NotNull]
        public readonly Expression Value;
        [NotNull]
        public readonly TypeRef ResultType; // as stack type

        public CloneExpression(Expression value, TypeRef resultType)
        {
            Value = value;
            ResultType = resultType;
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.Clone; } }

        public override bool IsValue(CompilationEnvironment compEnv) { return false; }

        public override bool IsCheap { get { return false; } }

        public override TypeRef Type(CompilationEnvironment compEnv)
        {
            return compEnv.SubstituteType(ResultType);
        }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage, bool inReadWrite)
        {
            Value.AccumUsage(compEnv, isAlwaysUsed, usage, false);
            compEnv.SubstituteType(ResultType).AccumUsage(usage, isAlwaysUsed);
        }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            Value.AccumEffects(fxCtxt, callCtxt, evalTimes);
        }

        public override void AccumLvalueEffects(EffectsContext fxCtxt)
        {
            fxCtxt.IncludeEffects(JST.Effects.Throws);
        }

        public override Expression Simplify(SimplifierContext ctxt)
        {
            var simpValue = Value.Simplify(ctxt);
            return new CloneExpression(simpValue, ResultType);
        }

        public override void Append(CSTWriter w, int prec)
        {
            w.Append("clone(");
            Value.Append(w, 1);
            w.Append(':');
            ResultType.Append(w);
            w.Append(')');
        }

        protected override bool EqualBody(Expression other)
        {
            var clone = (CloneExpression)other;
            return Value.Equals(clone.Value) && ResultType.Equals(clone.ResultType);
        }

        public override int GetHashCode()
        {
            var res = 0xeaad8e71u;
            res ^= (uint)Value.GetHashCode();
            res = Constants.Rot3(res) ^ (uint)ResultType.GetHashCode();
            return (int)res;
        }
    }

    // ----------------------------------------------------------------------
    // IsInstExpression
    // ----------------------------------------------------------------------

    public class IsInstExpression : Expression
    {
        [NotNull]
        public readonly Expression Value;
        [NotNull]
        public readonly TypeRef TestType; // as stack type

        public IsInstExpression(Expression value, TypeRef testType)
        {
            Value = value;
            TestType = testType;
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.IsInst; } }

        public override bool IsValue(CompilationEnvironment compEnv) { return false; }

        public override bool IsCheap { get { return false; } }

        public override TypeRef Type(CompilationEnvironment compEnv)
        {
            return compEnv.SubstituteType(TestType);
        }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage, bool inReadWrite)
        {
            Value.AccumUsage(compEnv, isAlwaysUsed, usage, false);
            compEnv.SubstituteType(TestType).AccumUsage(usage, isAlwaysUsed);
        }

        public override void  AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            Value.AccumEffects(fxCtxt, callCtxt, evalTimes);
        }

        public override void AccumLvalueEffects(EffectsContext fxCtxt)
        {
            fxCtxt.IncludeEffects(JST.Effects.Throws);
        }

        public override Expression Simplify(SimplifierContext ctxt)
        {
            var simpValue = Value.Simplify(ctxt);
            return new IsInstExpression(simpValue, TestType);
        }

        public override void Append(CSTWriter w, int prec)
        {
            Wrap
                (w,
                 prec,
                 0,
                 (w2, prec2) =>
                 {
                     Value.Append(w2, 1);
                     w2.Append(" is ");
                     TestType.Append(w2);
                 });
        }

        protected override bool EqualBody(Expression other)
        {
            var isinst = (IsInstExpression)other;
            return Value.Equals(isinst.Value) && TestType.Equals(isinst.TestType);
        }

        public override int GetHashCode()
        {
            var res = 0xe5a0cc0fu;
            res ^= (uint)Value.GetHashCode();
            res = Constants.Rot3(res) ^ (uint)TestType.GetHashCode();
            return (int)res;
        }
    }

    // ----------------------------------------------------------------------
    // IfThenElseExpression
    // ----------------------------------------------------------------------

    public class IfThenElseExpression : Expression
    {
        [NotNull]
        public readonly Expression Condition;
        [NotNull]
        public readonly Expression Then;
        [NotNull]
        public readonly Expression Else;

        public IfThenElseExpression(Expression condition, Expression then, Expression els)
        {
            Condition = condition;
            Then = then;
            Else = els;
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.IfThenElse; } }

        public override bool IsValue(CompilationEnvironment compEnv) { return false; }

        public override bool IsCheap { get { return false; } }

        public override TypeRef Type(CompilationEnvironment compEnv)
        {
            var res = Then.Type(compEnv);
            if (Else != null)
                res = res.Lub(compEnv, Else.Type(compEnv));
            return res;
        }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage, bool inReadWrite)
        {
            Condition.AccumUsage(compEnv, isAlwaysUsed, usage, false);
            var thenUsage = new Usage();
            Then.AccumUsage(compEnv, false, thenUsage, false);
            var elseUsage = new Usage();
            Else.AccumUsage(compEnv, false, elseUsage, false);
            usage.Merge(new Seq<Usage> { thenUsage, elseUsage });
        }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            Condition.AccumEffects(fxCtxt, callCtxt, evalTimes);
            if (callCtxt != null)
                evalTimes = evalTimes.Lub(EvalTimes.Opt);
            var thenCtxt = fxCtxt.Fork();
            Then.AccumEffects(thenCtxt, callCtxt, evalTimes);
            var elseCtxt = fxCtxt.Fork();
            Else.AccumEffects(elseCtxt, callCtxt, evalTimes);
            fxCtxt.IncludeEffects(thenCtxt.AccumEffects);
            fxCtxt.IncludeEffects(elseCtxt.AccumEffects);
        }

        public override void AccumLvalueEffects(EffectsContext fxCtxt)
        {
            Then.AccumLvalueEffects(fxCtxt);
            Else.AccumLvalueEffects(fxCtxt);
        }

        public override Expression Simplify(SimplifierContext ctxt)
        {
            var simpCondition = Condition.Simplify(ctxt);
            var simpThen = Then.Simplify(ctxt);
            var simpElse = Else.Simplify(ctxt);
            return new IfThenElseExpression(simpCondition, simpThen, simpElse);
        }

        public override bool IsCondition(CompilationEnvironment compEnv)
        {
            return Then.IsCondition(compEnv) && Else.IsCondition(compEnv);
        }

        public override void Append(CSTWriter w, int prec)
        {
            Wrap
                (w,
                 prec,
                 0,
                 (w2, prec2) =>
                 {
                     Condition.Append(w2, 1);
                     w.Append(" ? ");
                     Then.Append(w, 1);
                     w.Append(" : ");
                     Else.Append(w, 1);
                 });
        }

        protected override bool EqualBody(Expression other)
        {
            var ife = (IfThenElseExpression)other;
            return Condition.Equals(ife.Condition) && Then.Equals(ife.Then) && Else.Equals(ife.Else);
        }

        public override int GetHashCode()
        {
            var res = 0xbc9bc6e4u;
            res ^= (uint)Condition.GetHashCode();
            res = Constants.Rot3(res) ^ (uint)Then.GetHashCode();
            res = Constants.Rot3(res) ^ (uint)Else.GetHashCode();
            return (int)res;
        }
    }

    // ----------------------------------------------------------------------
    // ImportExportExpression
    // ----------------------------------------------------------------------

    //
    // Given imported properties C::X of type D and D::Y of type E, an expression such as:
    //   c.X.Y = e
    // is initially represented as:
    //   D::set_Y(C::get_X(c), e)
    //
    // Naively inlining get_X and set_Y leads to:
    //   D.Export(D.Import(C.Export(v).X)).Y = E.Export(e)
    //
    // Instead, we first unfold the expression to:
    //   CALLIMPORTED(D::set_Y, D::EXPORT(D::IMPORT(CALLIMPORTED(C::get_X, C::EXPORT(c)))), E::EXPORT(e))
    // which we can simplify to:
    //   CALLIMPORTED(D::set_Y, CALLIMPORTED(C::get_X, C::EXPORT(c)), E::EXPORT(e))
    // which renders as:
    //   C.Export(c).X.Y = E.Export(e);
    //

    // Import or export value of given type
    public class ImportExportExpression : Expression
    {
        public readonly bool IsImport;
        public readonly Expression Value;
        public readonly TypeRef ManagedType;

        public ImportExportExpression(bool isImport, Expression value, TypeRef managedType)
        {
            IsImport = isImport;
            Value = value;
            ManagedType = managedType;
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.ImportExport; } }

        public override bool IsValue(CompilationEnvironment compEnv) { return false; }

        public override bool IsCheap { get { return false; } }

        public override TypeRef Type(CompilationEnvironment compEnv)
        {
            if (IsImport)
                return ManagedType;
            else
                // Result is arbitrary object with no representation in our type system
                return null;
        }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage, bool inReadWrite)
        {
            Value.AccumUsage(compEnv, isAlwaysUsed, usage, false);
            ManagedType.AccumUsage(usage, isAlwaysUsed);
        }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            Value.AccumEffects(fxCtxt, callCtxt, evalTimes);
            fxCtxt.IncludeEffects(JST.Effects.Throws);
        }

        public override void AccumLvalueEffects(EffectsContext fxCtxt)
        {
            fxCtxt.IncludeEffects(JST.Effects.Throws);
        }

        public override Expression Simplify(SimplifierContext ctxt)
        {
            var simpValue = Value.Simplify(ctxt);
            if (simpValue.Flavor == ExpressionFlavor.ImportExport)
            {
                var iee = (ImportExportExpression)simpValue;
                if (IsImport == !iee.IsImport && ManagedType.IsEquivalentTo(ctxt.CompEnv, iee.ManagedType))
                    // Import(Export(x)) => x, Export(Import(x)) => x
                    return iee.Value;
                // else: fall-through
            }
            // else: fall-through

            return new ImportExportExpression(IsImport, simpValue, ManagedType);
        }

        public override void Append(CSTWriter w, int prec)
        {
            w.Append(IsImport ? "import" : "export");
            w.Append('(');
            ManagedType.Append(w);
            w.Append(',');
            Value.Append(w, 0);
            w.Append(')');
        }

        protected override bool EqualBody(Expression other)
        {
            var iee = (ImportExportExpression)other;
            return IsImport == iee.IsImport && Value.Equals(iee.Value) && ManagedType.Equals(iee.ManagedType);
        }

        public override int GetHashCode()
        {
            var res = IsImport ? 0xb3472dcau : 0x9a532915u;
            res ^= (uint)Value.GetHashCode();
            res = Constants.Rot7(res) ^ (uint)ManagedType.GetHashCode();
            return (int)res;
        }
    }

    // ----------------------------------------------------------------------
    // CallImportedExpression
    // ----------------------------------------------------------------------
    
    // Apply script corresponding to imported method to arguments, assuming they have been exported
    // as required, and without importing any result.
    public class CallImportedExpression : Expression
    {
        public readonly bool IsFactory;
        public readonly MethodRef Method;
        public readonly IImSeq<Expression> Arguments;

        public CallImportedExpression(bool isFactory, MethodRef method, IImSeq<Expression> arguments)
        {
            IsFactory = isFactory;
            Method = method;
            Arguments = arguments ?? Constants.EmptyExpressions;
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.CallImportedPseudo; } }

        public override bool IsValue(CompilationEnvironment compEnv) { return false; }

        public override bool IsCheap { get { return false; } }

        public override TypeRef Type(CompilationEnvironment compEnv)
        {
            var targetMethEnv = Method.EnterMethod(compEnv);
            if (IsFactory)
                return Method.DefiningType.ToRunTimeType(compEnv, true);
            else if (targetMethEnv.Method.Result == null)
                return null;
            else
                return targetMethEnv.SubstituteType(targetMethEnv.Method.Result.Type).ToRunTimeType(compEnv, true);
        }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage, bool inReadWrite)
        {
            foreach (var e in Arguments)
                e.AccumUsage(compEnv, isAlwaysUsed, usage, false);
        }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            foreach (var e in Arguments)
                e.AccumEffects(fxCtxt, callCtxt, evalTimes);
            fxCtxt.IncludeEffects(JST.Effects.Top);
        }

        public override void AccumLvalueEffects(EffectsContext fxCtxt)
        {
            fxCtxt.IncludeEffects(JST.Effects.Throws);
        }

        public override Expression Simplify(SimplifierContext ctxt)
        {
            var simpArguments = Arguments.Select(e => e.Simplify(ctxt)).ToSeq();
            return new CallImportedExpression(IsFactory, Method, simpArguments);
        }

        public override void Append(CSTWriter w, int prec)
        {
            Method.Append(w);
            w.Append("(imported,");
            if (IsFactory)
                w.Append("factory");
            else
                w.Append("normal");
            for (var i = 0; i < Arguments.Count; i++)
            {
                w.Append(',');
                Arguments[i].Append(w, 0);
            }
            w.Append(')');
        }

        protected override bool EqualBody(Expression other)
        {
            var ci = (CallImportedExpression)other;
            if (IsFactory != ci.IsFactory || !Method.Equals(ci.Method) || Arguments.Count != ci.Arguments.Count)
                return false;
            for (var i = 0; i < Arguments.Count; i++)
            {
                if (!Arguments[i].Equals(ci.Arguments[i]))
                    return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            var res = IsFactory ? 0x2b60a476u : 0xad6ea6b0;
            res ^= (uint)Method.GetHashCode();
            for (var i = 0; i < Arguments.Count; i++)
                res = Constants.Rot3(res) ^ (uint)Arguments[i].GetHashCode();
            return (int)res;
        }
    }

    // ----------------------------------------------------------------------
    // StatementsPseudoExpression
    // ----------------------------------------------------------------------

    // Represents application of inlined method to arguments.
    public class StatementsPseudoExpression : Expression
    {
        [NotNull]
        public readonly Statements Body;
        [CanBeNull]
        public readonly Expression Value;

        public StatementsPseudoExpression(Statements body, Expression value)
        {
            Body = body ?? Statements.Empty;
            Value = value;
        }

        public StatementsPseudoExpression(Statement body, Expression value)
        {
            Body = new Statements(body);
            Value = value;
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.StatementsPseudo; } }

        public override bool IsValue(CompilationEnvironment compEnv) { return false; }

        public override bool IsCheap { get { return false; } }

        public override TypeRef Type(CompilationEnvironment compEnv)
        {
            return Value == null ? null : Value.Type(compEnv);
        }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage, bool inReadWrite)
        {
            usage.Merge(Body.Usage(compEnv), isAlwaysUsed);
            if (Value != null)
                Value.AccumUsage(compEnv, isAlwaysUsed, usage, inReadWrite);
        }

        public override void  AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            Body.AccumEffects(fxCtxt, callCtxt, evalTimes);
            if (Value != null)
                Value.AccumEffects(fxCtxt, callCtxt, evalTimes);
        }

        public override void AccumLvalueEffects(EffectsContext fxCtxt)
        {
            if (Value != null)
                Value.AccumLvalueEffects(fxCtxt);
        }

        public override Expression Simplify(SimplifierContext ctxt)
        {
            var simpBody = Body.Simplify(ctxt);
            var simpValue = Value == null ? null : Value.Simplify(ctxt);
            return new StatementsPseudoExpression(simpBody, simpValue);
        }

        public override void Append(CSTWriter w, int prec)
        {
            w.Append('{');
            w.Indented(w2 =>
                           {
                               Body.Append(w2);
                               if (Value != null)
                               {
                                   w2.Append("return ");
                                   Value.Append(w2, 0);
                                   w.Append(';');
                               }
                           });
            w.Append('}');
        }

        protected override bool EqualBody(Expression other)
        {
            var se = (StatementsPseudoExpression)other;
            if ((Value == null) != (se.Value == null))
                return false;
            if (!Body.Equals(se.Body))
                return false;
            return Value == null || Value.Equals(se.Value);
        }

        public override int GetHashCode()
        {
            var res = 0xb5b32944u;
            res ^= (uint)Body.GetHashCode();
            if (Value != null)
                res = Constants.Rot3(res) ^ (uint)Value.GetHashCode();
            return (int)res;
        }
    }

    // ----------------------------------------------------------------------
    // InitialStatePseudoExpression
    // ----------------------------------------------------------------------

    // Represents the machine state at entry to method
    public class InitialStatePseudoExpression : Expression
    {
        public readonly int Entry; // id of first basic block 

        public InitialStatePseudoExpression(int entry)
        {
            Entry = entry;
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.InitialStatePseudo; } }

        public override bool IsValue(CompilationEnvironment compEnv) { return false; }

        public override bool IsCheap { get { return false; } }

        public override TypeRef Type(CompilationEnvironment compEnv)
        {
            throw new InvalidOperationException("no type to represent machine state");
        }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage, bool inReadWrite)
        {
        }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
        }

        public override void AccumLvalueEffects(EffectsContext fxCtxt)
        {
            fxCtxt.IncludeEffects(JST.Effects.Throws);
        }

        public override Expression Simplify(SimplifierContext ctxt)
        {
            return this;
        }

        public override void Append(CSTWriter w, int prec)
        {
            w.Append("$Initial(");
            w.Append(Entry);
            w.Append(")");
        }

        protected override bool EqualBody(Expression other)
        {
            var init = (InitialStatePseudoExpression)other;
            return Entry == init.Entry;
        }

        public override int GetHashCode()
        {
            var res = 0x49a7df7du;
            res ^= (uint)Entry;
            return (int)res;
        }
    }
}