using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.JavaScript.JST
{
    public enum ExpressionFlavor
    {
        Array,
        Binary,
        Boolean,
        Call,
        Comment,
        Conditional,
        Debugger,
        Function,
        Identifier,
        Index,
        New,
        Null,
        Numeric,
        Object,
        RegularExpression,
        String,
        This,
        Unary,
        StatementsPseudo
    }

    public enum Precedence
    {
        Expression = 0,  // weakest
        Assignment = 1,
        LogicalOR = 2,
        LogicalAND = 3,
        BitwiseOR = 4,
        BitwiseXOR = 5,
        BitwiseAND = 6,
        Equality = 7,
        Relational = 8,
        Shift = 9,
        Additive = 10,
        Multiplicative = 11,
        Unary = 12,
        Postfix = 13,
        LHS = 14,
        Primary = 15     // strongest
    }

    public enum Associativity
    {
        Left,
        Right,
        None
    }

    // ----------------------------------------------------------------------
    // Expression
    // ----------------------------------------------------------------------

    public abstract class Expression : IEquatable<Expression>, IComparable<Expression>
    {
        [CanBeNull] // null => no location known
        public readonly Location Loc;

        protected Expression(Location loc)
        {
            Loc = loc;
        }

        protected Expression()
        {
        }

        public abstract ExpressionFlavor Flavor { get; }

        public override bool Equals(object obj)
        {
            var other = obj as Expression;
            return other != null && Equals(other);
        }

        public override int GetHashCode()
        {
            throw new InvalidOperationException("must be overridden");
        }

        public bool Equals(Expression other)
        {
            return Flavor == other.Flavor && EqualBody(other);
        }

        protected abstract bool EqualBody(Expression other);

        public int CompareTo(Expression other)
        {
            var i = Flavor.CompareTo(other.Flavor);
            if (i != 0)
                return i;
            return CompareBody(other);
        }

        protected abstract int CompareBody(Expression other);

        // True if expression must be delimited by spaces in initializer, binary, or '.' expression
        public abstract bool NeedsDelimitingSpace { get; }

        // True if expression may be duplicated without duplicating work or side-effects
        // (however the expression's result may be effected by side-effects of other expressions)
        public abstract bool IsDuplicatable { get; }

        // True if expression does not perform side-effects, and is not effected by side-effects of other expressions
        public abstract bool IsValue { get; }

        public virtual Identifier IsIdentifier { get { return null; } }

        public virtual bool IsUndefinedIdentifier { get { return false; } }

        public virtual bool? IsBoolean { get { return null; } }

        public abstract int Size { get; }

        public abstract void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes);

        // Accumulate effects of expression when used as an l-value, assuming effects of evaluation have
        // already been accumulated using above.
        public virtual void AccumLvalueEffects(EffectsContext fxCtxt, CallContext callCtxt)
        {
            fxCtxt.IncludeEffects(Effects.Throws);
        }

        public void AccumLvalueEffects(SimplifierContext ctxt)
        {
            var fxCtxt = new EffectsContext(ctxt.isValue);
            AccumLvalueEffects(fxCtxt, null);
            ctxt.IncludeEffects(fxCtxt.AccumEffects);
        }

        public abstract IImSeq<Expression> SubExpressions { get; }
        public abstract Expression CloneWithSubExpressions(IImSeq<Expression> subExpressions);

        public abstract IImSeq<Statements> SubStatementss { get; }
        public abstract Expression CloneWithSubStatementss(IImSeq<Statements> subStatementss);

        public abstract void CollectBoundVars(IMSet<Identifier> boundVars);
        public abstract void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars);

        // Simplify this expression and return the result.
        //  - If statements is non-null, attempt to hoist any embedded statements in expression into it,
        //    and return the residual expression, or null if no residual expression.
        //  - Otherwise, always return the simplified expression.
        public abstract Expression Simplify(SimplifierContext ctxt, EvalTimes evalTimes);

        // As above, but always return a simplified expression, even if it is 'undefined'
        public Expression SimplifyValue(SimplifierContext ctxt, EvalTimes evalTimes)
        {
            var value = Simplify(ctxt, evalTimes);
            return value ?? new IdentifierExpression(Identifier.Undefined);
        }

        public virtual FunctionExpression TryToFunction()
        {
            return null;
        }

        public virtual Identifier TryToIdentifier()
        {
            return null;
        }

        protected void Wrap(Writer writer, Precedence actual, Precedence required, Action<Writer, Precedence> f)
        {
            if (actual < required)
            {
                writer.Append('(');
                f(writer, Precedence.Expression);
                writer.Append(')');
            }
            else
                f(writer, required);
        }

        public abstract void Append(Writer writer, Precedence required);

        public override string ToString()
        {
            return ToString(true);
        }

        public string ToString(bool prettyPrint)
        {
            var sw = new StringWriter();
            var w = new Writer(sw, prettyPrint);
            Append(w);
            return sw.ToString();
        }

        public virtual void AppendMemberExpression(Writer writer)
        {
            Append(writer, Precedence.Primary);
        }

        public virtual void AppendMemberExpressionAndArguments(Writer writer)
        {
            throw new InvalidOperationException("pretty-printer logic failure");
        }

        public virtual void AppendCallExpression(Writer writer)
        {
            AppendMemberExpression(writer);
        }

        public virtual void AppendNewExpression(Writer writer)
        {
            AppendMemberExpression(writer);
        }

        public void Append(Writer writer)
        {
            Append(writer, Precedence.Expression);
        }

        public static Expression Dot(Expression left, IImSeq<Identifier> fields)
        {
            return fields.Aggregate(left, (current, id) => new IndexExpression(current, id.ToStringLiteral()));
        }

        public static Expression Dot(Expression left, params Identifier[] fields)
        {
            return fields.Aggregate(left, (current, id) => new IndexExpression(current, id.ToStringLiteral()));
        }

        public static Expression Dot(Expression left, IImSeq<PropertyName> fields)
        {
            return fields.Aggregate(left, (current, n) => new IndexExpression(current, n.ToLiteral()));
        }

        public static Expression Dot(Expression left, params PropertyName[] fields)
        {
            return fields.Aggregate(left, (current, n) => new IndexExpression(current, n.ToLiteral()));
        }

        public static Expression Path(IImSeq<PropertyName> names)
        {
            if (names.Count == 0)
                throw new InvalidOperationException("empty path");
            var id = names[0].ToIdentifier();
            if (id == null)
                throw new InvalidOperationException("path must begin with an identifier");
            var res = id.ToE();
            for (var i = 1; i < names.Count; i++)
                res = new IndexExpression(res, names[i].ToLiteral());
            return res;
        }

        public static ISeq<PropertyName> ExplodePath(Expression expr)
        {
            var res = new Seq<PropertyName>();
            var curr = expr;
            while (true)
            {
                var currIndex = curr as IndexExpression;
                if (currIndex != null)
                {
                    var currStr = currIndex.Right as StringLiteral;
                    if (currStr != null)
                    {
                        res.Insert(0, new PropertyName(currStr));
                        curr = currIndex.Left;
                    }
                    else
                    {
                        var currNum = currIndex.Right as NumericLiteral;
                        if (currNum != null)
                        {
                            res.Insert(0, new PropertyName(currNum));
                            curr = currIndex.Left;
                        }
                        else
                            return null;
                    }
                }
                else
                {
                    var currIE = curr as IdentifierExpression;
                    if (currIE != null)
                    {
                        res.Insert(0, new PropertyName(currIE.Identifier));
                        return res;
                    }
                    else
                    {
                        var currStr = curr as StringLiteral;
                        if (currStr != null)
                        {
                            res.Insert(0, new PropertyName(currStr));
                            return res;
                        }
                        else
                        {
                            var currNum = curr as NumericLiteral;
                            if (currNum != null)
                            {
                                res.Insert(0, new PropertyName(currNum));
                                return res;
                            }
                            else
                                return null;
                        }
                    }
                }
            }
        }

        public static Expression DotCall(Expression left, Identifier field, IImSeq<Expression> arguments)
        {
            return new CallExpression(new IndexExpression(left, field.ToStringLiteral()), arguments);
        }

        public static Expression DotCall(Expression left, Identifier field, params Expression[] arguments)
        {
            return new CallExpression(new IndexExpression(left, field.ToStringLiteral()), arguments);
        }

        public static Expression IsNull(Expression exp)
        {
            return new BinaryExpression(exp, new BinaryOperator(BinaryOp.Equals), new NullExpression());
        }

        public static Expression IsNotNull(Expression exp)
        {
            return new BinaryExpression(exp, new BinaryOperator(BinaryOp.NotEquals), new NullExpression());
        }

        public static Expression IsUndefined(Expression exp)
        {
            return new BinaryExpression(exp, new BinaryOperator(BinaryOp.StrictEquals), Identifier.Undefined.ToE());
        }

        public static Expression IsNotUndefined(Expression exp)
        {
            return new BinaryExpression(exp, new BinaryOperator(BinaryOp.StrictNotEquals), Identifier.Undefined.ToE());
        }

        public static Expression Nary(BinaryOp op, params Expression[] expressions)
        {
            return Nary(new BinaryOperator(op), expressions);
        }

        public static Expression Nary(BinaryOperator binaryOperator, params Expression[] expressions)
        {
            if (expressions.Length == 0)
                throw new InvalidOperationException("binary expressions have no zero");
            else if (expressions.Length == 1)
                return expressions[0];
            else
            {
                var res = expressions[expressions.Length - 1];
                for (var i = expressions.Length - 2; i >= 0; i--)
                    res = new BinaryExpression(expressions[i], binaryOperator, res);
                return res;
            }
        }

        public static Expression And(params Expression[] expressions)
        {
            if (expressions.Length == 0)
                return new BooleanLiteral(true);
            else
                return Nary(new BinaryOperator(BinaryOp.LogicalAND), expressions);
        }

        public static Expression Or(params Expression[] expressions)
        {
            if (expressions.Length == 0)
                return new BooleanLiteral(false);
            else
                return Nary(new BinaryOperator(BinaryOp.LogicalOR), expressions);
        }

        public static Expression Not(Expression exp)
        {
            return new UnaryExpression(new UnaryOperator(UnaryOp.LogicalNot), exp);
        }

        public static Expression FromString(string str, string label, bool strict)
        {
            using (var reader = new StringReader(str))
            {
                var lexer = new Lexer(reader, label, strict);
                var parser = new Parser(lexer);
                return parser.TopLevelExpression();
            }
        }
    }

    // ----------------------------------------------------------------------
    // ArrayLiteral
    // ----------------------------------------------------------------------

    public class ArrayLiteral : Expression
    {
        [NotNull]
        public readonly IImSeq<Expression> Elements;

        public ArrayLiteral(Location loc, IImSeq<Expression> elements) : base(loc)
        {
            Elements = elements ?? Constants.EmptyExpressions;
        }

        public ArrayLiteral(Location loc, params Expression[] elements) : base(loc)
        {
            Elements = new Seq<Expression>(elements);
        }

        public ArrayLiteral(IImSeq<Expression> elements)
        {
            Elements = elements ?? Constants.EmptyExpressions;
        }

        public ArrayLiteral(params Expression[] elements)
        {
            Elements = new Seq<Expression>(elements);
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.Array; } }

        public override bool NeedsDelimitingSpace { get { return false; } }

        public override bool IsDuplicatable { get { return false; } }

        public override bool IsValue { get { return Elements.All(e => e.IsValue); } }

        public override int Size { get { return 1 + Elements.Sum(e => e.Size); } }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            foreach (var e in Elements)
                e.AccumEffects(fxCtxt, callCtxt, evalTimes);
        }

        public override IImSeq<Expression> SubExpressions { get { return Elements; } }

        public override Expression CloneWithSubExpressions(IImSeq<Expression> subExpressions)
        {
            return new ArrayLiteral(Loc, subExpressions);
        }

        public override IImSeq<Statements> SubStatementss { get { return Constants.EmptyStatementss; } }

        public override Expression CloneWithSubStatementss(IImSeq<Statements> subStatementss)
        {
            if (subStatementss.Count != 0)
                throw new InvalidOperationException("mismatched sub-statements arity");
            return this;
        }

        public override void CollectBoundVars(IMSet<Identifier> boundVars)
        {
            foreach (var e in Elements)
                e.CollectBoundVars(boundVars);
        }

        public override void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
            foreach (var e in Elements)
                e.CollectFreeVars(boundVars, freeVars);
        }

        public override Expression Simplify(SimplifierContext ctxt, EvalTimes evalTimes)
        {
            if (Elements.Count == 0)
                return this;
            else
            {
                var simpElements = Elements.Select(e => e.Simplify(ctxt, evalTimes)).ToSeq();
                return new ArrayLiteral(Loc, simpElements);
            }
        }

        private static void PrintOrSkip(Writer writer, Expression e)
        {
            var ie = e as IdentifierExpression;
            if (ie == null || !ie.Identifier.IsUndefined)
                e.Append(writer, Precedence.Assignment);
        }

        public override void Append(Writer writer, Precedence required)
        {
            if (Elements.Count > 4)
            {
                writer.EndLine();
                writer.Append('[');
                writer.Indented
                    (w =>
                         {
                             var first = true;
                             foreach (var e in Elements)
                             {
                                 if (first)
                                     first = false;
                                 else
                                     writer.Append(',');
                                 writer.EndLine();
                                 PrintOrSkip(w, e);
                             }
                         });
                writer.EndLine();
                writer.Append(']');
                writer.EndLine();
            }
            else
            {
                writer.Append('[');
                var first = true;
                foreach (var e in Elements)
                {
                    if (first)
                        first = false;
                    else
                    {
                        writer.Append(',');
                        writer.Space();
                    }
                    PrintOrSkip(writer, e);
                }
                writer.Append(']');
            }
        }

        public override int GetHashCode()
        {
            return (int)Elements.Aggregate(0x243f6a88u, (current, e) => Constants.Rot7(current) ^ (uint)e.GetHashCode());
        }

        protected override bool EqualBody(Expression other)
        {
            var al = (ArrayLiteral)other;
            if (Elements.Count != al.Elements.Count)
                return false;
            return !Elements.Where((t, i) => !t.Equals(al.Elements[i])).Any();
        }

        protected override int CompareBody(Expression other)
        {
            var al = (ArrayLiteral)other;
            var i = Elements.Count.CompareTo(al.Elements.Count);
            if (i != 0)
                return i;
            for (var j = 0; j < Elements.Count; j++)
            {
                i = Elements[j].CompareTo(al.Elements[j]);
                if (i != 0)
                    return i;
            }
            return 0;
        }

        public int Count { get { return Elements.Count; } }
    }

    // ----------------------------------------------------------------------
    // BinaryExpression
    // ----------------------------------------------------------------------

    public class BinaryExpression : Expression
    {
        [NotNull]
        public readonly Expression Left;
        [NotNull]
        public readonly BinaryOperator Operator;
        [NotNull]
        public readonly Expression Right;

        public BinaryExpression(Location loc, Expression left, BinaryOperator binaryOperator, Expression right)
            : base(loc)
        {
            Left = left;
            Operator = binaryOperator;
            Right = right;
        }

        public BinaryExpression(Location loc, Expression left, BinaryOp op, Expression right)
            : base(loc)
        {
            Left = left;
            Operator = new BinaryOperator(op);
            Right = right;
        }

        public BinaryExpression(Expression left, BinaryOperator binaryOperator, Expression right)
        {
            Left = left;
            Operator = binaryOperator;
            Right = right;
        }

        public BinaryExpression(Expression left, BinaryOp op, Expression right)
        {
            Left = left;
            Operator = new BinaryOperator(op);
            Right = right;
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.Binary; } }

        public override bool NeedsDelimitingSpace { get { return false; } }
        public override bool IsDuplicatable { get { return false; } }
        public override bool IsValue { get { return false; } }

        public override int Size { get { return 1 + Left.Size + Right.Size; } }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            Left.AccumEffects(fxCtxt, callCtxt, evalTimes);
            if (callCtxt != null && Operator.IsLogical)
                evalTimes = evalTimes.Lub(EvalTimes.Opt);
            Right.AccumEffects(fxCtxt, callCtxt, evalTimes);
            if (Operator.IsAssignment)
                Left.AccumLvalueEffects(fxCtxt, callCtxt);
            if (Operator.IsDivide)
                fxCtxt.IncludeEffects(Effects.Throws);
        }

        public override IImSeq<Expression> SubExpressions
        {
            get { return new Seq<Expression> { Left, Right }; }
        }

        public override Expression CloneWithSubExpressions(IImSeq<Expression> subExpressions)
        {
            if (subExpressions.Count != 2)
                throw new InvalidOperationException("mismatched sub-expressions arity");
            return new BinaryExpression(Loc, subExpressions[0], Operator, subExpressions[1]);
        }

        public override IImSeq<Statements> SubStatementss { get { return Constants.EmptyStatementss; } }

        public override Expression CloneWithSubStatementss(IImSeq<Statements> subStatementss)
        {
            if (subStatementss.Count != 0)
                throw new InvalidOperationException("mismatched sub-statements arity");
            return this;
        }

        public override void CollectBoundVars(IMSet<Identifier> boundVars)
        {
            Left.CollectBoundVars(boundVars);
            Right.CollectBoundVars(boundVars);
        }

        public override void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
            Left.CollectFreeVars(boundVars, freeVars);
            Right.CollectFreeVars(boundVars, freeVars);
        }

        public override Expression Simplify(SimplifierContext ctxt, EvalTimes evalTimes)
        {
            var simpLeft = Left.SimplifyValue(ctxt, evalTimes);
            if (Operator.IsLogical)
                evalTimes = evalTimes.Lub(EvalTimes.Opt);
            var simpRight = Right.SimplifyValue(ctxt, evalTimes);

            var lb = simpLeft.IsBoolean;
            var rb = simpRight.IsBoolean;
            if (lb.HasValue &&
                ((!lb.Value && Operator.Op == BinaryOp.LogicalAND) || lb.Value && Operator.Op == BinaryOp.LogicalOR))
                return simpLeft;
            else if (rb.HasValue &&
                     ((rb.Value && Operator.Op == BinaryOp.LogicalAND) ||
                      (!rb.Value && Operator.Op == BinaryOp.LogicalOR)))
                return simpLeft;
            else if (lb.HasValue &&
                     ((lb.Value && Operator.Op == BinaryOp.LogicalAND) ||
                      !lb.Value && Operator.Op == BinaryOp.LogicalOR))
                return simpRight;

            if (simpLeft.IsValue && simpRight.IsValue)
            {
                if (Operator.Op == BinaryOp.Equals)
                    return new BooleanLiteral(simpLeft.Equals(simpRight));
                else if (Operator.Op == BinaryOp.NotEquals)
                    return new BooleanLiteral(!simpLeft.Equals(simpRight));
            }

            if (Operator.IsAssignment)
                simpLeft.AccumLvalueEffects(ctxt);
            if (Operator.IsDivide)
                ctxt.IncludeEffects(Effects.Throws);
            return new BinaryExpression(Loc, simpLeft, Operator, simpRight);
        }

        private static Precedence Stronger(Precedence precedence)
        {
            if (precedence == Precedence.Primary)
                return precedence;
            else
                return (Precedence)((int)precedence + 1);
        }

        public override void Append(Writer writer, Precedence required)
        {
            Wrap(writer,
                 Operator.Precedence,
                 required,
                 (w, _) =>
                 {
                     var leftSpace = Left.NeedsDelimitingSpace;
                     var opSpace = Operator.NeedsDelimitingSpace;
                     var rightSpace = Right.NeedsDelimitingSpace;
                     Left.Append(w,
                                Operator.Associativity == Associativity.Left
                                    ? Operator.Precedence
                                    : Stronger(Operator.Precedence));
                     if (leftSpace || opSpace)
                         w.HardSpace();
                     else
                         w.Space();
                     Operator.Append(w);
                     if (opSpace || rightSpace)
                         w.HardSpace();
                     else
                         w.Space();
                     Right.Append(w,
                                 Operator.Associativity == Associativity.Right
                                     ? Operator.Precedence
                                     : Stronger(Operator.Precedence));
                 });
        }

        public override int GetHashCode()
        {
            var res = 0x13198a2eu;
            res ^= (uint)Left.GetHashCode();
            res = Constants.Rot7(res) ^ (uint)Operator.GetHashCode();
            res = Constants.Rot7(res) ^ (uint)Right.GetHashCode();
            return (int)res;
        }

        protected override bool EqualBody(Expression other)
        {
            var be = (BinaryExpression)other;
            return Left.Equals(be.Left) && Operator.Equals(be.Operator) && Right.Equals(be.Right);
        }

        protected override int CompareBody(Expression other)
        {
            var be = (BinaryExpression)other;
            var i = Left.CompareTo(be.Left);
            if (i != 0)
                return i;
            i = Operator.CompareTo(be.Operator);
            if (i != 0)
                return i;
            return Right.CompareTo(be.Right);
        }
    }

    // ----------------------------------------------------------------------
    // BooleanLiteral
    // ----------------------------------------------------------------------

    public class BooleanLiteral : Expression
    {
        public readonly bool Value;

        public BooleanLiteral(Location loc, bool value) : base(loc)
        {
            Value = value;
        }
        
        public BooleanLiteral(bool value)
        {
            Value = value;
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.Boolean; } }

        public override bool NeedsDelimitingSpace { get { return false; } }
        public override bool IsDuplicatable { get { return true; } }
        public override bool IsValue { get { return true; } }

        public override bool? IsBoolean { get { return Value; } }

        public override int Size { get { return 1; } }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
        }

        public override IImSeq<Expression> SubExpressions
        {
            get { return Constants.EmptyExpressions; }
        }

        public override Expression CloneWithSubExpressions(IImSeq<Expression> subExpressions)
        {
            if (subExpressions.Count != 0)
                throw new InvalidOperationException("mismatched sub-expressions arity");
            return this;
        }

        public override IImSeq<Statements> SubStatementss { get { return Constants.EmptyStatementss; } }

        public override Expression CloneWithSubStatementss(IImSeq<Statements> subStatementss)
        {
            if (subStatementss.Count != 0)
                throw new InvalidOperationException("mismatched sub-statements arity");
            return this;
        }

        public override void CollectBoundVars(IMSet<Identifier> boundVars)
        {
        }

        public override void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
        }

        public override Expression Simplify(SimplifierContext ctxt, EvalTimes evalTimes)
        {
            return this;
        }

        public override void Append(Writer writer, Precedence required)
        {
            writer.Append(Lexemes.BooleanToJavaScript(Value));
        }

        public override int GetHashCode()
        {
            return (int)(Value ? 0xe98575b1u : 0xdc262302u);
        }

        protected override bool EqualBody(Expression other)
        {
            var bl = (BooleanLiteral)other;
            return Value == bl.Value;
        }

        protected override int CompareBody(Expression other)
        {
            var bl = (BooleanLiteral)other;
            return Value.CompareTo(bl.Value);
        }

        public static BooleanLiteral FromJavaScript(Location loc, string str)
        {
            return new BooleanLiteral(loc, Lexemes.JavaScriptToBoolean(str));
        }
    }

    // ----------------------------------------------------------------------
    // CallExpression
    // ----------------------------------------------------------------------

    public class CallExpression : Expression
    {
        [NotNull]
        public readonly Expression Applicand;
        [NotNull]
        public readonly IImSeq<Expression> Arguments;

        public CallExpression(Location loc, Expression applicand, IImSeq<Expression> arguments)
            : base(loc)
        {
            Applicand = applicand;
            Arguments = arguments ?? Constants.EmptyExpressions;
        }

        public CallExpression(Location loc, Expression applicand, params Expression[] arguments)
            : base(loc)
        {
            Applicand = applicand;
            Arguments = new Seq<Expression>(arguments);
        }

        public CallExpression(Expression applicand, IImSeq<Expression> arguments)
        {
            Applicand = applicand;
            Arguments = arguments ?? Constants.EmptyExpressions;
        }

        public CallExpression(Expression applicand, params Expression[] arguments)
        {
            Applicand = applicand;
            Arguments = new Seq<Expression>(arguments);
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.Call; } }

        public override bool NeedsDelimitingSpace { get { return false; } }
        public override bool IsDuplicatable { get { return false; } }
        public override bool IsValue { get { return false; } }

        public override int Size { get { return 1 + Applicand.Size + Arguments.Sum(e => e.Size); } }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            var func = Applicand.TryToFunction();
            if (func == null)
            {
                if (!fxCtxt.IsValue(Applicand))
                    Applicand.AccumEffects(fxCtxt, callCtxt, evalTimes);
            }
            // else: evaluation of function has no side-effects
            foreach (var e in Arguments)
                e.AccumEffects(fxCtxt, callCtxt, evalTimes);
            if (func == null || func.Parameters.Count != Arguments.Count)
            {
                if (!fxCtxt.IsValue(Applicand))
                    fxCtxt.IncludeEffects(Effects.Top);
            }
            else
            {
                // Function body will be evaluated exactly once, so include its effects, but ignore effects
                // on any locally bound variables
                // NOTE: Of course a locally bound variable could escape within a closure, but in that case
                //       the application of the (necessarily non-lambda) closure will yield TOP
                var subFxCtxt = fxCtxt.Fork();
                foreach (var id in func.FunctionBoundVars())
                    subFxCtxt.Bind(id);
                func.Body.AccumEffects(subFxCtxt, callCtxt, evalTimes);
                fxCtxt.IncludeEffects(subFxCtxt.AccumEffects);
            }
        }

        public override IImSeq<Expression> SubExpressions
        {
            get
            {
                var res = new Seq<Expression>();
                res.Add(Applicand);
                foreach (var e in Arguments)
                    res.Add(e);
                return res;
            }
        }

        public override Expression CloneWithSubExpressions(IImSeq<Expression> subExpressions)
        {
            if (subExpressions.Count < 1)
                throw new InvalidOperationException("mismatched sub-expressions arity");
            var args = new Seq<Expression>();
            for (var i = 1; i < subExpressions.Count; i++)
                args.Add(subExpressions[i]);
            return new CallExpression(Loc, subExpressions[0], args);
        }

        public override IImSeq<Statements> SubStatementss { get { return Constants.EmptyStatementss; } }

        public override Expression CloneWithSubStatementss(IImSeq<Statements> subStatementss)
        {
            if (subStatementss.Count != 0)
                throw new InvalidOperationException("mismatched sub-statements arity");
            return this;
        }

        public override void CollectBoundVars(IMSet<Identifier> boundVars)
        {
            Applicand.CollectBoundVars(boundVars);
            foreach (var e in Arguments)
                e.CollectBoundVars(boundVars);
        }

        public override void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
            Applicand.CollectFreeVars(boundVars, freeVars);
            foreach (var e in Arguments)
                e.CollectFreeVars(boundVars, freeVars);
        }

        public override Expression Simplify(SimplifierContext ctxt, EvalTimes evalTimes)
        {
            // Is call a non-recursive function literal applied to the correct arguments?
            var func = Applicand.TryToFunction();
            if (func != null && func.Parameters.Count == Arguments.Count && !func.IsRecursive)
            {
                // Is function applied at most once, or does not capture any outer vars?
                if (evalTimes.Lte(EvalTimes.Opt) || func.FunctionFreeVars().Count == 0)
                {
                    // Is function body of the form '<statments>; return <value>;' ?
                    var body = new Seq<Statement>();
                    var retres = func.Body.ToReturnResult(body);
                    var bodyStmnts = new Statements(body);
                    if (retres.Status != ReturnStatus.Fail)
                    {
                        var origBoundVars = new Set<Identifier>();
                        bodyStmnts.CollectBoundVars(origBoundVars);
                        if (retres.Value != null)
                            retres.Value.CollectBoundVars(origBoundVars);

                        // Simplify arguments, but keep their effects isolated since their evaluation position
                        // will be determined below
                        var argCtxt = ctxt.InLocalEffects();
                        var simpArguments = Arguments.Select(e => e.SimplifyValue(argCtxt, evalTimes)).ToSeq();

                        // PASS 1: Simplify function body, inlining known value arguments and freshening everything else.
                        //         (Fresh names are taken from a forked name supply, since they are only needed
                        //          between now and the end of pass 2)
                        var pass1Ctxt = ctxt.InPass1SubContext();
                        var pass1BoundVars = new Set<Identifier>();
                        var pass1Parameters = new Seq<Identifier>();
                        var nonValueSimpArguments = new Seq<Expression>();
                        for (var i = 0; i < simpArguments.Count; i++)
                        {
                            if (ctxt.IsValue(simpArguments[i]))
                                pass1Ctxt.Bind(func.Parameters[i], simpArguments[i]);
                            else
                            {
                                var newid = pass1Ctxt.Freshen(func.Parameters[i]);
                                pass1Parameters.Add(newid);
                                nonValueSimpArguments.Add(simpArguments[i]);
                            }
                        }
                        foreach (var id in origBoundVars)
                            pass1BoundVars.Add(pass1Ctxt.Freshen(id));

                        bodyStmnts.Simplify(pass1Ctxt, evalTimes, false);
                        var pass1Value = retres.Value == null ? null : retres.Value.Simplify(pass1Ctxt, evalTimes);
                        var pass1Stmnts = new Statements(pass1Ctxt.Statements);

                        // Can we safely substitute arguments for parameters in the function body and result
                        // without changing observable side effects?
                        var callCtxt = new CallContext(pass1Parameters, nonValueSimpArguments, ctxt.isValue);
                        var fxCtxt = new EffectsContext(ctxt.isValue);
                        foreach (var id in pass1Parameters)
                            fxCtxt.Bind(id);
                        foreach (var id in pass1BoundVars)
                            fxCtxt.Bind(id);
                        pass1Stmnts.AccumBlockEffects(fxCtxt, callCtxt, EvalTimes.Bottom);
                        if (pass1Value != null)
                            pass1Value.AccumEffects(fxCtxt, callCtxt, EvalTimes.Bottom);
                        callCtxt.Final();

                        // PASS 2: Simplify function body again, with arguments in their final binding positions
                        //         (This time fresh names are taken from the context's name supply, since the
                        //          body is about to leave it's nested scope)
                        // NOTE: To avoid exponential time complexity on depth of nested functions we must perform
                        //       the second pass over the result of the first pass. We must thus start from a fresh
                        //       substitution.
                        var pass2Ctxt = ctxt.InPass2SubContext();
                        if (callCtxt.IsOk)
                        {
                            for (var i = 0; i < nonValueSimpArguments.Count; i++)
                                pass2Ctxt.Bind(pass1Parameters[i], nonValueSimpArguments[i]);
                        }
                        else
                        {
                            // Must share the evaluation of non-value arguments
                            for (var i = 0; i < nonValueSimpArguments.Count; i++)
                            {
                                var newid = pass2Ctxt.Freshen(pass1Parameters[i]);
                                pass2Ctxt.Add(Statement.Var(newid, nonValueSimpArguments[i]));
                            }
                        }
                        foreach (var id in pass1BoundVars)
                            pass2Ctxt.Freshen(id);
                        pass1Stmnts.Simplify(pass2Ctxt, evalTimes, false);
                        var pass2Value = pass1Value == null ? null : pass1Value.Simplify(pass2Ctxt, evalTimes);
                        var pass2Stmnts = new Statements(pass2Ctxt.Statements);

                        // Can statements of function be hoisted into current statement context?
                        if (ctxt.Statements != null)
                        {
                            var subFxCtxt = new EffectsContext(ctxt.isValue);
                            pass2Stmnts.AccumEffects(subFxCtxt, null, null);
                            if (subFxCtxt.AccumEffects.CommutableWith(ctxt.ContextEffects))
                            {
                                foreach (var s in pass2Stmnts.Body)
                                    ctxt.Add(s);
                                return pass2Value;
                            }
                            // else: fall-through
                        }
                        // else: fall-through
                        return new StatementsPseudoExpression(pass2Stmnts, pass2Value);
                    }
                    // else: fall-through
                }
                // else: fall-through
            }
            // else: fall-through

            {
                var simpApplicand = Applicand.SimplifyValue(ctxt, evalTimes);
                var simpArguments = Arguments.Select(e => e.SimplifyValue(ctxt, evalTimes)).ToSeq();
                if (!ctxt.IsValue(simpApplicand))
                    ctxt.IncludeEffects(Effects.Top);
                return new CallExpression(Loc, simpApplicand, simpArguments);
            }
        }

        private void AppendArguments(Writer writer)
        {
            if (Arguments.Count > 5)
            {
                writer.Indented
                    (w => w.List
                              (Arguments,
                               w2 =>
                               {
                                   w2.Append('(');
                                   w2.EndLine();
                               },
                               (w2, e) => e.Append(w2, Precedence.Assignment),
                               w2 =>
                               {
                                   w2.Append(',');
                                   w2.EndLine();
                               },
                               w2 => w2.Append(')')));
            }
            else
            {
                writer.List
                    (Arguments,
                     w => w.Append('('),
                     (w, e) => e.Append(w, Precedence.Assignment),
                     w =>
                     {
                         w.Append(',');
                         w.Space();
                     },
                     w => w.Append(')'));
            }
        }

        public override void AppendMemberExpressionAndArguments(Writer writer)
        {
            Applicand.AppendMemberExpression(writer);
            AppendArguments(writer);
        }

        public override void AppendNewExpression(Writer writer)
        {
            AppendMemberExpressionAndArguments(writer);
        }

        public override void AppendCallExpression(Writer writer)
        {
            Applicand.AppendCallExpression(writer);
            AppendArguments(writer);
        }

        public override void Append(Writer writer, Precedence required)
        {
            if (Precedence.LHS < required)
            {
                writer.Append('(');
                Append(writer, Precedence.LHS);
                writer.Append(')');
            }
            else if (required < Precedence.LHS)
                Append(writer, Precedence.LHS);
            else
                AppendCallExpression(writer);
        }

        public override int GetHashCode()
        {
            var res = 0xbe5466cfu;
            res ^= (uint)Applicand.GetHashCode();
            return (int)Arguments.Aggregate(res, (h, e) => Constants.Rot7(h) ^ (uint)e.GetHashCode());
        }

        protected override bool EqualBody(Expression other)
        {
            var ce = (CallExpression)other;
            if (Arguments.Count != ce.Arguments.Count || !Applicand.Equals(ce.Applicand))
                return false;
            return !Arguments.Where((t, i) => !t.Equals(ce.Arguments[i])).Any();
        }

        protected override int CompareBody(Expression other)
        {
            var ce = (CallExpression)other;
            var i = Arguments.Count.CompareTo(ce.Arguments.Count);
            if (i != 0)
                return i;
            i = Applicand.CompareTo(ce.Applicand);
            if (i != 0)
                return i;
            for (var j = 0; j < Arguments.Count; j++)
            {
                i = Arguments[j].CompareTo(ce.Arguments[j]);
                if (i != 0)
                    return i;
            }
            return 0;
        }
    }

    // ----------------------------------------------------------------------
    // CommentExpression
    // ----------------------------------------------------------------------

    public class CommentExpression : Expression
    {
        [NotNull]
        public readonly Expression Expression;
        [CanBeNull] // null => no comment
        public readonly string Comment;

        public CommentExpression(Location loc, Expression expression, string comment)
            : base(loc)
        {
            Expression = expression;
            Comment = comment;
        }

        public CommentExpression(Expression expression, string comment)
        {
            Expression = expression;
            Comment = comment;
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.Comment; } }

        public override bool NeedsDelimitingSpace { get { return Expression.NeedsDelimitingSpace; } }
        public override bool IsDuplicatable { get { return Expression.IsDuplicatable; } }
        public override bool IsValue { get { return Expression.IsValue; } }
        public override Identifier IsIdentifier { get { return Expression.IsIdentifier; } }
        public override bool IsUndefinedIdentifier { get { return Expression.IsUndefinedIdentifier; } }
        public override bool? IsBoolean { get { return Expression.IsBoolean; } }
        public override int Size { get { return Expression.Size; } }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            Expression.AccumEffects(fxCtxt, callCtxt, evalTimes);
        }

        public override void AccumLvalueEffects(EffectsContext fxCtxt, CallContext callCtxt)
        {
            Expression.AccumLvalueEffects(fxCtxt, callCtxt);
        }

        public override IImSeq<Expression> SubExpressions
        {
            get { return new Seq<Expression> { Expression }; }
        }

        public override Expression CloneWithSubExpressions(IImSeq<Expression> subExpressions)
        {
            if (subExpressions.Count != 1)
                throw new InvalidOperationException("mismatched sub-expressions arity");
            return new CommentExpression(Loc, subExpressions[0], Comment);
        }

        public override IImSeq<Statements> SubStatementss { get { return Constants.EmptyStatementss; } }

        public override Expression CloneWithSubStatementss(IImSeq<Statements> subStatementss)
        {
            if (subStatementss.Count != 0)
                throw new InvalidOperationException("mismatched sub-statements arity");
            return this;
        }

        public override void CollectBoundVars(IMSet<Identifier> boundVars)
        {
            Expression.CollectBoundVars(boundVars);
        }

        public override void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
            Expression.CollectFreeVars(boundVars, freeVars);
        }

        public override Expression Simplify(SimplifierContext ctxt, EvalTimes evalTimes)
        {
            var simpExpression = Expression.Simplify(ctxt, evalTimes);
            if (simpExpression == null)
                return null;
            else if (Comment != null)
                return new CommentExpression(Loc, simpExpression, Comment);
            else
                return simpExpression;
        }

        public override FunctionExpression TryToFunction()
        {
            return Expression.TryToFunction();
        }

        public override Identifier TryToIdentifier()
        {
            return Expression.TryToIdentifier();
        }

        public void AppendComment(Writer writer)
        {
            if (writer.PrettyPrint)
            {
                if (Comment != null)
                {
                    writer.HardSpace();
                    writer.Append("/* ");
                    var s = 0;
                    var i = 0;
                    while (i < Comment.Length)
                    {
                        if (Comment[i] == '\r' && i < Comment.Length - 1 && Comment[i + 1] == '\n')
                        {
                            writer.Append(Comment.Substring(s, i - s));
                            writer.EndLine();
                            i += 2;
                            s = i;
                        }
                        else if (Comment[i] == '\n' || Comment[i] == '\r')
                        {
                            writer.Append(Comment.Substring(s, i - s));
                            writer.EndLine();
                            i++;
                            s = i;
                        }
                        else
                            i++;
                    }
                    writer.Append(Comment.Substring(s, i - s));
                    writer.Append(" */");
                    writer.HardSpace();
                }
            }
        }

        public override void AppendMemberExpression(Writer writer)
        {
            Expression.AppendMemberExpression(writer);
            AppendComment(writer);
        }

        public override void AppendMemberExpressionAndArguments(Writer writer)
        {
            Expression.AppendMemberExpressionAndArguments(writer);
            AppendComment(writer);
        }

        public override void AppendCallExpression(Writer writer)
        {
            Expression.AppendCallExpression(writer);
            AppendComment(writer);
        }

        public override void AppendNewExpression(Writer writer)
        {
            Expression.AppendNewExpression(writer);
            AppendComment(writer);
        }

        public override void Append(Writer writer, Precedence required)
        {
            Expression.Append(writer, required);
            AppendComment(writer);
        }

        public override int GetHashCode()
        {
            return (int)(0xecaa8c71u ^ (uint)Expression.GetHashCode());
        }

        protected override bool EqualBody(JST.Expression other)
        {
            var ce = (CommentExpression)other;
            return Expression.Equals(ce.Expression);
        }

        protected override int CompareBody(Expression other)
        {
            var ce = (CommentExpression)other;
            return Expression.CompareTo(ce.Expression);
        }
    }

    // ----------------------------------------------------------------------
    // ConditionalExpression
    // ----------------------------------------------------------------------

    public class ConditionalExpression : Expression
    {
        [NotNull]
        public readonly Expression Condition;
        [NotNull]
        public readonly Expression Then;
        [NotNull]
        public readonly Expression Else;

        public ConditionalExpression(Location loc, Expression condition, Expression thenExpression, Expression elseExpression)
            : base(loc)
        {
            Condition = condition;
            Then = thenExpression;
            Else = elseExpression;
        }

        public ConditionalExpression(Expression condition, Expression thenExpression, Expression elseExpression)
        {
            Condition = condition;
            Then = thenExpression;
            Else = elseExpression;
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.Conditional; } }

        public override bool NeedsDelimitingSpace { get { return false; } }
        public override bool IsDuplicatable { get { return false; } }
        public override bool IsValue { get { return false; } }

        public override int Size { get { return 1 + Condition.Size + Then.Size + Else.Size; } }

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

        public override IImSeq<Expression> SubExpressions
        {
            get { return new Seq<Expression> { Condition, Then, Else }; }
        }

        public override Expression CloneWithSubExpressions(IImSeq<Expression> subExpressions)
        {
            if (subExpressions.Count != 3)
                throw new InvalidOperationException("mismatched sub-expressions arity");
            return new ConditionalExpression(Loc, subExpressions[0], subExpressions[1], subExpressions[2]);
        }

        public override IImSeq<Statements> SubStatementss { get { return Constants.EmptyStatementss; } }

        public override Expression CloneWithSubStatementss(IImSeq<Statements> subStatementss)
        {
            if (subStatementss.Count != 0)
                throw new InvalidOperationException("mismatched sub-statements arity");
            return this;
        }

        public override void CollectBoundVars(IMSet<Identifier> boundVars)
        {
            Condition.CollectBoundVars(boundVars);
            Then.CollectBoundVars(boundVars);
            Else.CollectBoundVars(boundVars);
        }

        public override void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
            Condition.CollectFreeVars(boundVars, freeVars);
            Then.CollectFreeVars(boundVars, freeVars);
            Else.CollectFreeVars(boundVars, freeVars);
        }

        public override Expression Simplify(SimplifierContext ctxt, EvalTimes evalTimes)
        {
            var simpCondition = Condition.SimplifyValue(ctxt, evalTimes);
            var b = simpCondition.IsBoolean;
            if (b.HasValue)
                return b.Value ? Then.Simplify(ctxt, evalTimes) : Else.Simplify(ctxt, evalTimes);
            else
            {
                // TODO: We chain the effects of the 'then' clause into the 'else' clause, which
                //       may prevent some hoisting in the 'else' clause.
                evalTimes = evalTimes.Lub(EvalTimes.Opt);
                var simpThen = Then.Simplify(ctxt, evalTimes);
                var simpElse = Else.Simplify(ctxt, evalTimes);
                return new ConditionalExpression(Loc, simpCondition, simpThen, simpElse);
            }
        }

        public override void Append(Writer writer, Precedence required)
        {
            Wrap(writer,
                 Precedence.Assignment,
                 required,
                 (w, _) =>
                 {
                     Condition.Append(writer, Precedence.LogicalOR);
                     w.Space();
                     w.Append('?');
                     w.Space();
                     Then.Append(writer, Precedence.Assignment);
                     w.Space();
                     w.Append(':');
                     w.Space();
                     Else.Append(writer, Precedence.Assignment);
                 });
        }

        public override int GetHashCode()
        {
            var res = 0xb5470917u;
            res ^= (uint)Condition.GetHashCode();
            res = Constants.Rot7(res) ^ (uint)Then.GetHashCode();
            res = Constants.Rot7(res) ^ (uint)Else.GetHashCode();
            return (int)res;
        }

        protected override bool EqualBody(Expression other)
        {
            var ce = (ConditionalExpression)other;
            return Condition.Equals(ce.Condition) && Then.Equals(ce.Then) && Else.Equals(ce.Else);
        }

        protected override int CompareBody(Expression other)
        {
            var ce = (ConditionalExpression)other;
            var i = Condition.CompareTo(ce.Condition);
            if (i != 0)
                return i;
            i = Then.CompareTo(ce.Then);
            if (i != 0)
                return i;
            return Else.CompareTo(ce.Else);
        }
    }

    // ----------------------------------------------------------------------
    // DebuggerExpression
    // ----------------------------------------------------------------------

    public class DebuggerExpression : Expression
    {
        public DebuggerExpression(Location loc)
            : base(loc)
        {
        }

        public DebuggerExpression()
        {
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.Debugger; } }

        public override bool NeedsDelimitingSpace { get { return false; } }
        public override bool IsDuplicatable { get { return true; } }
        public override bool IsValue { get { return false; } }

        public override IImSeq<Expression> SubExpressions
        {
            get { return Constants.EmptyExpressions; }
        }

        public override Expression CloneWithSubExpressions(IImSeq<Expression> subExpressions)
        {
            if (subExpressions.Count != 0)
                throw new InvalidOperationException("mismatched sub-expressions arity");
            return this;
        }

        public override int Size { get { return 1; } }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
        }

        public override IImSeq<Statements> SubStatementss { get { return Constants.EmptyStatementss; } }

        public override Expression CloneWithSubStatementss(IImSeq<Statements> subStatementss)
        {
            if (subStatementss.Count != 0)
                throw new InvalidOperationException("mismatched sub-statements arity");
            return this;
        }

        public override void CollectBoundVars(IMSet<Identifier> boundVars)
        {
        }

        public override void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
        }

        public override Expression Simplify(SimplifierContext ctxt, EvalTimes evalTimes)
        {
            return this;
        }

        public override void Append(Writer writer, Precedence required) { writer.Append("debugger"); }


        public override int GetHashCode()
        {
            var res = 0x8979fb1bu;
            return (int)res;
        }

        protected override bool EqualBody(Expression other)
        {
            return true;
        }

        protected override int CompareBody(Expression node)
        {
            return 0;
        }
    }

    // ----------------------------------------------------------------------
    // FunctionExpression
    // ----------------------------------------------------------------------

    public class FunctionExpression : Expression
    {
        [CanBeNull] // null => function cannot be recursive
        public readonly Identifier Name;
        [NotNull]
        public readonly IImSeq<Identifier> Parameters;
        [NotNull]
        public readonly Statements Body;

        public FunctionExpression(Location loc, IImSeq<Identifier> parameters, Statements body)
            : base(loc)
        {
            Name = null;
            Parameters = parameters ?? Constants.EmptyIdentifiers;
            Body = body;
        }

        public FunctionExpression(Location loc, Identifier name, IImSeq<Identifier> parameters, Statements body)
            : base(loc)
        {
            Name = name;
            Parameters = parameters ?? Constants.EmptyIdentifiers;
            Body = body;
        }

        public FunctionExpression(IImSeq<Identifier> parameters, Statements body)
        {
            Name = null;
            Parameters = parameters ?? Constants.EmptyIdentifiers;
            Body = body;
        }

        public FunctionExpression(Identifier name, IImSeq<Identifier> parameters, Statements body)
        {
            Name = name;
            Parameters = parameters ?? Constants.EmptyIdentifiers;
            Body = body;
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.Function; } }

        public override bool NeedsDelimitingSpace { get { return false; } }
        public override bool IsDuplicatable { get { return false; } }
        public override bool IsValue { get { return false; } }

        public override int Size { get { return 1 + Parameters.Count + Body.Size; } }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            if (callCtxt != null && callCtxt.IsOk)
            {
                var funcFreeVars = new Set<Identifier>();
                CollectFreeVars(new Set<Identifier>(), funcFreeVars);
                foreach (var kv in callCtxt.Parameters)
                {
                    if (funcFreeVars.Contains(kv.Key))
                        // Don't know how many times function body, and thus argument, will be evaluated
                        callCtxt.Fail();
                }
            }
        }

        public override IImSeq<Expression> SubExpressions
        {
            get { return Constants.EmptyExpressions; }
        }

        public override Expression CloneWithSubExpressions(IImSeq<Expression> subExpressions)
        {
            if (subExpressions.Count != 0)
                throw new InvalidOperationException("mismatched sub-expressions arity");
            return this;
        }

        public override IImSeq<Statements> SubStatementss { get { return new Seq<Statements> {Body }; } }

        public override Expression CloneWithSubStatementss(IImSeq<Statements> subStatementss)
        {
            if (subStatementss.Count == 1)
                throw new InvalidOperationException("mismatched sub-statements arity");
            return new FunctionExpression(Loc, Name, Parameters, subStatementss[0]);
        }

        public override void CollectBoundVars(IMSet<Identifier> boundVars)
        {
#if !JSCRIPT_IS_CORRECT
            // BUG: IE treats name as bound in scope containing function expression rather than function body
            if (Name != null)
                boundVars.Add(Name);
#endif
        }

        public override void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
            var subBoundVars = new Set<Identifier>();
            foreach (var id in boundVars)
                subBoundVars.Add(id);
#if JSCRIPT_IS_CORRECT
            if (Name != null)
                subBoundVars.Add(Name);
#endif
            foreach (var id in Parameters)
                subBoundVars.Add(id);
            Body.CollectBoundVars(subBoundVars);
            Body.CollectFreeVars(subBoundVars, freeVars);
        }

        public IImSet<Identifier> FunctionBoundVars()
        {
            var boundVars = new Set<Identifier>();
#if JSCRIPT_IS_CORRECT
            if (Name != null)
                boundVars.Add(Name);
#endif
            foreach (var id in Parameters)
                boundVars.Add(id);
            Body.CollectBoundVars(boundVars);
            return boundVars;
        }

        public IImSet<Identifier> FunctionFreeVars()
        {
            var boundVars = FunctionBoundVars();
            var freeVars = new Set<Identifier>();
            Body.CollectFreeVars(boundVars, freeVars);
            return freeVars;
        }

        public bool IsRecursive
        {
            get
            {
                if (Name == null)
                    return false;
                var boundVars = new Set<Identifier>();
                Body.CollectBoundVars(boundVars);
                var freeVars = new Set<Identifier>();
                Body.CollectFreeVars(boundVars, freeVars);
                return freeVars.Contains(Name);
            }
        }

        public override Expression Simplify(SimplifierContext ctxt, EvalTimes evalTimes)
        {
            var subCtxt = ctxt.InFreshScope();
            var simpName = default(Identifier);
            if (Name != null)
            {
                if (ctxt.InGlobalScope || ctxt.KeepFunctionNames)
                {
                    // WARNING: Assuming existing name won't clash with fresh names
                    simpName = Name;
                }
                else
                {
                    if (IsRecursive)
#if JSCRIPT_IS_CORRECT
                        simpName = subCtxt.Freshen(Name);
#else
                        simpName = ctxt.Rename(Name);
#endif
                    // else: drop name
                }
            }
            var simpParameters = Parameters.Select(subCtxt.Freshen).ToSeq();
            var boundVars = new Set<Identifier>();
            Body.CollectBoundVars(boundVars);
            foreach (var id in boundVars)
                subCtxt.Freshen(id);
            Body.Simplify(subCtxt, EvalTimes.Top, true);
            return new FunctionExpression(Loc, simpName, simpParameters, new Statements(subCtxt.Statements));
        }

        public override FunctionExpression TryToFunction()
        {
            return this;
        }

        public override void Append(Writer writer, Precedence required)
        {
            // Though the grammer suggests functions are primitive, many parsers don't handle them
            // correctly. So pretend they are next few levels weaker in precedence.
            Wrap
                (writer,
                 Precedence.Unary,
                 required,
                 (w, _) =>
                 {
                     writer.Append("function");
                     if (Name != null)
                     {
                         writer.HardSpace();
                         Name.Append(writer);
                     }
                     writer.List
                         (Parameters,
                          w2 => w2.Append('('),
                          (w2, p) => p.Append(w2),
                          w2 =>
                          {
                              w2.Append(',');
                              w2.Space();
                          },
                          w2 => w2.Append(')'));
                     writer.Space();
                     Body.AppendBlockNoEndLine(writer);
                 });
        }

        public override int GetHashCode()
        {
            var res = 0xa458fea3u;
            if (Name != null)
                res = Constants.Rot7(res) ^ (uint)Name.GetHashCode();
            res = Parameters.Aggregate(res, (h, p) => Constants.Rot7(h) ^ (uint)p.GetHashCode());
            res = Constants.Rot7(res) ^ (uint)Body.GetHashCode();
            return (int)res;
        }

        protected override bool EqualBody(Expression other)
        {
            var fe = (FunctionExpression)other;
            if ((Name == null) != (fe.Name == null))
                return false;
            if (Name != null && !Name.Equals(fe.Name))
                return false;
            if (Parameters.Count != fe.Parameters.Count)
                return false;
            return !Parameters.Select((p, i) => !p.Equals(fe.Parameters[i])).Any() && Body.Equals(fe.Body);
        }

        protected override int CompareBody(Expression other)
        {
            var fe = (FunctionExpression)other;
            if (Name == null && fe.Name != null)
                return -1;
            if (Name != null && fe.Name == null)
                return 1;
            var i = Name == null ? 0 : Name.CompareTo(fe.Name);
            if (i != 0)
                return i;
            i = Parameters.Count.CompareTo(fe.Parameters.Count);
            if (i != 0)
                return i;
            for (var j = 0; j < Parameters.Count; j++)
            {
                i = Parameters[j].CompareTo(fe.Parameters[j]);
                if (i != 0)
                    return i;
            }
            return Body.CompareTo(fe.Body);
        }
    }

    // ----------------------------------------------------------------------
    // IdentifierExpression
    // ----------------------------------------------------------------------

    public class IdentifierExpression : Expression
    {
        [NotNull]
        public readonly Identifier Identifier;

        public IdentifierExpression(Location loc, Identifier identifier) : base(loc)
        {
            Identifier = identifier;
        }

        public IdentifierExpression(Identifier identifier)
        {
            Identifier = identifier;
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.Identifier; } }

        public override bool NeedsDelimitingSpace { get { return false; } }
        public override bool IsDuplicatable { get { return true; } }
        public override bool IsValue { get { return false; } }
        public override Identifier IsIdentifier { get { return Identifier; } }
        public override bool IsUndefinedIdentifier { get { return Identifier.Equals(Identifier.Undefined); } }
        public override int Size { get { return 1; } }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            if (callCtxt != null)
            {
                var idx = default(int);
                if (callCtxt.Parameters.TryGetValue(Identifier, out idx))
                {
                    if (callCtxt.SeenParameters[idx])
                        // More than once syntactic occurence of this parameter
                        callCtxt.Fail();

                    // Remember we've seen a syntactic occurence of this parameter
                    callCtxt.SeenParameters[idx] = true;

                    if (!callCtxt.AllReadOnly)
                    {
                        for (var i = 0; i < idx; i++)
                        {
                            if (!callCtxt.SeenParameters[i])
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
            }

            if (!fxCtxt.IsHidden(Identifier))
                fxCtxt.IncludeEffects(Effects.Read(Identifier));
        }

        public override void AccumLvalueEffects(EffectsContext fxCtxt, CallContext callCtxt)
        {
            if (callCtxt != null && callCtxt.IsOk)
            {
                if (callCtxt.Parameters.ContainsKey(Identifier))
                    // A parameter is used as an l-value
                    callCtxt.Fail();
            }

            if (!fxCtxt.IsHidden(Identifier))
                fxCtxt.IncludeEffects(Effects.Write(Identifier));
        }

        public override IImSeq<Expression> SubExpressions
        {
            get { return Constants.EmptyExpressions; }
        }

        public override Expression CloneWithSubExpressions(IImSeq<Expression> subExpressions)
        {
            if (subExpressions.Count != 0)
                throw new InvalidOperationException("mismatched sub-expressions arity");
            return this;
        }

        public override IImSeq<Statements> SubStatementss { get { return Constants.EmptyStatementss; } }

        public override Expression CloneWithSubStatementss(IImSeq<Statements> subStatementss)
        {
            if (subStatementss.Count != 0)
                throw new InvalidOperationException("mismatched sub-statements arity");
            return this;
        }

        public override void CollectBoundVars(IMSet<Identifier> boundVars)
        {
        }

        public override void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
            if (!boundVars.Contains(Identifier))
                freeVars.Add(Identifier);
        }

        public override Expression Simplify(SimplifierContext ctxt, EvalTimes evalTimes)
        {
            return ctxt.Apply(Identifier);
        }

        public override Identifier TryToIdentifier()
        {
            return Identifier;
        }

        public override void Append(Writer writer, Precedence required)
        {
            Identifier.Append(writer);
        }


        public override int GetHashCode()
        {
            return (int)(0x718bcd58u ^ (uint)Identifier.GetHashCode());
        }

        protected override bool EqualBody(Expression other)
        {
            var ie = (IdentifierExpression)other;
            return Identifier.Equals(ie.Identifier);
        }

        protected override int CompareBody(Expression other)
        {
            var ie = (IdentifierExpression)other;
            return Identifier.CompareTo(ie.Identifier);
        }
    }

    // ----------------------------------------------------------------------
    // IndexExpression
    // ----------------------------------------------------------------------

    public class IndexExpression : Expression
    {
        [NotNull]
        public readonly Expression Left;
        [NotNull]
        public readonly Expression Right;

        public IndexExpression(Location loc, Expression left, Expression right) : base(loc)
        {
            Left = left;
            Right = right;
        }

        public IndexExpression(Location loc, Expression left, string right) : base(loc)
        {
            Left = left;
            Right = new StringLiteral(loc, right);
        }

        public IndexExpression(Location loc, Expression left, double right) : base(loc)
        {
            Left = left;
            Right = new NumericLiteral(loc, right);
        }

        public IndexExpression(Expression left, Expression right)
        {
            Left = left;
            Right = right;
        }

        public IndexExpression(Expression left, string right)
        {
            Left = left;
            Right = new StringLiteral(right);
        }

        public IndexExpression(Expression left, double right)
        {
            Left = left;
            Right = new NumericLiteral(right);
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.Index; } }

        public override bool NeedsDelimitingSpace { get { return false; } }
        public override bool IsDuplicatable { get { return false; } }
        public override bool IsValue { get { return Left.IsValue  && Right.IsValue; } }

        public override int Size { get { return 1 + Left.Size + Right.Size; } }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            Left.AccumEffects(fxCtxt, callCtxt, evalTimes);
            Right.AccumEffects(fxCtxt, callCtxt, evalTimes);
            fxCtxt.IncludeEffects(Effects.ReadHeap);
            fxCtxt.IncludeEffects(Effects.Throws);
        }

        public override void AccumLvalueEffects(EffectsContext fxCtxt, CallContext callCtxt)
        {
            fxCtxt.IncludeEffects(Effects.WriteHeap);
        }

        public override IImSeq<Expression> SubExpressions
        {
            get { return new Seq<Expression> { Left, Right }; }
        }

        public override Expression CloneWithSubExpressions(IImSeq<Expression> subExpressions)
        {
            if (subExpressions.Count != 2)
                throw new InvalidOperationException("mismatched sub-expressions arity");
            return new IndexExpression(Loc, subExpressions[0], subExpressions[1]);
        }

        public override IImSeq<Statements> SubStatementss { get { return Constants.EmptyStatementss; } }

        public override Expression CloneWithSubStatementss(IImSeq<Statements> subStatementss)
        {
            if (subStatementss.Count != 0)
                throw new InvalidOperationException("mismatched sub-statements arity");
            return this;
        }

        public override void CollectBoundVars(IMSet<Identifier> boundVars)
        {
            Left.CollectBoundVars(boundVars);
            Right.CollectBoundVars(boundVars);
        }

        public override void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
            Left.CollectFreeVars(boundVars, freeVars);
            Right.CollectFreeVars(boundVars, freeVars);
        }

        public override Expression Simplify(SimplifierContext ctxt, EvalTimes evalTimes)
        {
            var simpLeft = Left.SimplifyValue(ctxt, evalTimes);
            var simpRight = Right.SimplifyValue(ctxt, evalTimes);
            ctxt.IncludeEffects(Effects.ReadHeap);
            ctxt.IncludeEffects(Effects.Throws);
            return new IndexExpression(Loc, simpLeft, simpRight);
        }

        private void AppendRight(Writer writer)
        {
            var strlit = Right as StringLiteral;
            if (strlit != null && Lexemes.IsIdentifier(strlit.Value))
            {
                if (Left.NeedsDelimitingSpace)
                    // Eg: can't have 1.toString()
                    writer.Append(' ');
                writer.Append('.');
                writer.Append(strlit.Value);
            }
            else
            {
                writer.Append('[');
                Right.Append(writer, Precedence.Expression);
                writer.Append(']');
            }
        }

        public override void AppendMemberExpression(Writer writer)
        {
            Left.AppendMemberExpression(writer);
            AppendRight(writer);
        }

        public override void AppendCallExpression(Writer writer)
        {
            Left.AppendCallExpression(writer);
            AppendRight(writer);
        }

        public override void Append(Writer writer, Precedence required)
        {
            if (Precedence.LHS < required)
            {
                writer.Append('(');
                Append(writer, Precedence.LHS);
                writer.Append(')');
            }
            else if (required < Precedence.LHS)
                Append(writer, Precedence.LHS);
            else
                AppendCallExpression(writer);
        }

        public override int GetHashCode()
        {
            var res = 0xb8e1afedu;
            res ^= (uint)Left.GetHashCode();
            res = Constants.Rot7(res) ^ (uint)Right.GetHashCode();
            return (int)res;
        }

        protected override bool EqualBody(Expression other)
        {
            var ie = (IndexExpression)other;
            return Left.Equals(ie.Left) && Right.Equals(ie.Right);
        }

        protected override int CompareBody(Expression other)
        {
            var ie = (IndexExpression)other;
            var i = Left.CompareTo(ie.Left);
            if (i != 0)
                return i;
            return Right.CompareTo(ie.Right);
        }
    }

    // ----------------------------------------------------------------------
    // NewExpression
    // ----------------------------------------------------------------------

    public class NewExpression : Expression
    {
        [NotNull]
        public readonly Expression Constructor;

        public NewExpression(Location loc, Expression constructor)
            : base(loc)
        {
            Constructor = constructor;
        }

        public NewExpression(Expression constructor)
        {
            Constructor = constructor;
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.New; } }

        public override bool NeedsDelimitingSpace { get { return false; } }
        public override bool IsDuplicatable { get { return false; } }
        public override bool IsValue { get { return false; } }

        public override int Size { get { return 1 + Constructor.Size; } }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            Constructor.AccumEffects(fxCtxt, callCtxt, evalTimes);
        }

        public override IImSeq<Expression> SubExpressions
        {
            get { return new Seq<Expression> { Constructor }; }
        }

        public override Expression CloneWithSubExpressions(IImSeq<Expression> subExpressions)
        {
            if (subExpressions.Count != 1)
                throw new InvalidOperationException("mismatched sub-expressions arity");
            return new NewExpression(Loc, subExpressions[0]);
        }

        public override IImSeq<Statements> SubStatementss { get { return Constants.EmptyStatementss; } }

        public override Expression CloneWithSubStatementss(IImSeq<Statements> subStatementss)
        {
            if (subStatementss.Count != 0)
                throw new InvalidOperationException("mismatched sub-statements arity");
            return this;
        }

        public override void CollectBoundVars(IMSet<Identifier> boundVars)
        {
            Constructor.CollectBoundVars(boundVars);
        }

        public override void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
            Constructor.CollectFreeVars(boundVars, freeVars);
        }

        public override Expression Simplify(SimplifierContext ctxt, EvalTimes evalTimes)
        {
            var simpConstructor = Constructor.SimplifyValue(ctxt, evalTimes);
            return new NewExpression(Loc, simpConstructor);
        }

        private void AppendNew(Writer writer)
        {
            writer.Append("new");
            writer.HardSpace();
        }

        public override void AppendMemberExpression(Writer writer)
        {
            AppendNew(writer);
            Constructor.AppendMemberExpressionAndArguments(writer);
        }

        public override void AppendNewExpression(Writer writer)
        {
            AppendNew(writer);
            Constructor.AppendNewExpression(writer);
        }

        public override void Append(Writer writer, Precedence required)
        {
            if (Precedence.LHS < required)
            {
                writer.Append('(');
                Append(writer, Precedence.LHS);
                writer.Append(')');
            }
            else if (required < Precedence.LHS)
                Append(writer, Precedence.LHS);
            else
                AppendNewExpression(writer);
        }

        public override int GetHashCode()
        {
            return (int)(0xb01e8a3eu ^ (uint)Constructor.GetHashCode());
        }

        protected override bool EqualBody(Expression other)
        {
            var ne = (NewExpression)other;
            return Constructor.Equals(ne.Constructor);
        }

        protected override int CompareBody(Expression other)
        {
            var ne = (NewExpression)other;
            return Constructor.CompareTo(ne.Constructor);
        }
    }

    // ----------------------------------------------------------------------
    // NullExpression
    // ----------------------------------------------------------------------

    public class NullExpression : Expression
    {
        public NullExpression(Location loc)
            : base(loc)
        {
        }

        public NullExpression()
        {
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.Null; } }

        public override bool NeedsDelimitingSpace { get { return false; } }
        public override bool IsDuplicatable { get { return true; } }
        public override bool IsValue { get { return true; } }

        public override int Size { get { return 1; } }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
        }

        public override IImSeq<Expression> SubExpressions
        {
            get { return Constants.EmptyExpressions; }
        }

        public override Expression CloneWithSubExpressions(IImSeq<Expression> subExpressions)
        {
            if (subExpressions.Count != 0)
                throw new InvalidOperationException("mismatched sub-expressions arity");
            return this;
        }

        public override IImSeq<Statements> SubStatementss { get { return Constants.EmptyStatementss; } }

        public override Expression CloneWithSubStatementss(IImSeq<Statements> subStatementss)
        {
            if (subStatementss.Count != 0)
                throw new InvalidOperationException("mismatched sub-statements arity");
            return this;
        }

        public override void CollectBoundVars(IMSet<Identifier> boundVars)
        {
        }

        public override void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
        }

        public override Expression Simplify(SimplifierContext ctxt, EvalTimes evalTimes)
        {
            return this;
        }

        public override void Append(Writer writer, Precedence required) { writer.Append("null"); }

        public override int GetHashCode()
        {
            var res = 0x78af2fdau;
            return (int)res;
        }

        protected override bool EqualBody(Expression other)
        {
            return true;
        }

        protected override int CompareBody(Expression node)
        {
            return 0;
        }
    }

    // ----------------------------------------------------------------------
    // NumericExpression
    // ----------------------------------------------------------------------

    public class NumericLiteral : Expression
    {
        public readonly double Value;

        public NumericLiteral(Location loc, double value) : base(loc)
        {
            Value = value;
        }

        public NumericLiteral(Location loc, int value) : base(loc)
        {
            Value = value;
        }

        public NumericLiteral(double value)
        {
            Value = value;
        }

        public NumericLiteral(int value)
        {
            Value = value;
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.Numeric; } }

        public override bool NeedsDelimitingSpace { get { return true; } }
        public override bool IsDuplicatable { get { return true; } }
        public override bool IsValue { get { return true; } }

        public override int Size { get { return 1; } }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
        }

        public override IImSeq<Expression> SubExpressions
        {
            get { return Constants.EmptyExpressions; }
        }

        public override Expression CloneWithSubExpressions(IImSeq<Expression> subExpressions)
        {
            if (subExpressions.Count != 0)
                throw new InvalidOperationException("mismatched sub-expressions arity");
            return this;
        }

        public override IImSeq<Statements> SubStatementss { get { return Constants.EmptyStatementss; } }

        public override Expression CloneWithSubStatementss(IImSeq<Statements> subStatementss)
        {
            if (subStatementss.Count != 0)
                throw new InvalidOperationException("mismatched sub-statements arity");
            return this;
        }

        public override void CollectBoundVars(IMSet<Identifier> boundVars)
        {
        }

        public override void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
        }

        public override Expression Simplify(SimplifierContext ctxt, EvalTimes evalTimes)
        {
            return this;
        }

        public override void Append(Writer writer, Precedence required)
        {
            writer.Append(Lexemes.NumberToJavaScript(Value));
        }

        public override int GetHashCode()
        {
            return (int)(0x55605c60u ^ (uint)Value.GetHashCode());
        }

        protected override bool EqualBody(Expression other)
        {
            var nl = (NumericLiteral)other;
            return Value.Equals(nl.Value);
        }

        protected override int CompareBody(Expression other)
        {
            var nl = (NumericLiteral)other;
            return Value.CompareTo(nl.Value);
        }

        public static NumericLiteral FromJavaScript(Location loc, string str)
        {
            return new NumericLiteral(loc, Lexemes.JavaScriptToNumber(str));
        }
    }

    // ----------------------------------------------------------------------
    // ObjectLiteral
    // ----------------------------------------------------------------------

    public class PropertyName : IEquatable<PropertyName>, IComparable<PropertyName>
    {
        [CanBeNull] // null => no location known
        public readonly Location Loc;
        [NotNull]
        public readonly string Value;
        public readonly bool IsNumber;

        public PropertyName(Location loc, string str)
        {
            Loc = loc;
            Value = str;
            IsNumber = false;
        }

        public PropertyName(Location loc, double num)
        {
            Loc = loc;
            Value = num.ToString();
            IsNumber = true;
        }

        public PropertyName(Location loc, int num)
        {
            Loc = loc;
            Value = num.ToString();
            IsNumber = true;
        }

        public PropertyName(string str)
        {
            Value = str;
            IsNumber = false;
        }

        public PropertyName(double num)
        {
            Value = num.ToString();
            IsNumber = true;
        }

        public PropertyName(int num)
        {
            Value = num.ToString();
            IsNumber = true;
        }

        public PropertyName(StringLiteral str)
        {
            Loc = str.Loc;
            Value = str.Value;
            IsNumber = false;
        }

        public PropertyName(NumericLiteral num)
        {
            Loc = num.Loc;
            Value = num.Value.ToString();
            IsNumber = true;
        }

        public PropertyName(Identifier id)
        {
            Loc = id.Loc;
            Value = id.Value;
            IsNumber = false;
        }

        public void Append(Writer writer)
        {
            if (IsNumber || Lexemes.IsIdentifier(Value))
                writer.Append(Value);
            else
            {
                writer.Append('"');
                writer.Append(Lexemes.StringToJavaScript(Value));
                writer.Append('"');
            }
        }

        public override bool Equals(object obj)
        {
            var other = obj as PropertyName;
            return other != null && Equals(other);
        }

        public override int GetHashCode()
        {
            var res = IsNumber ? 0xd396acc5u : 0x5664526cu;
            res ^= (uint)StringComparer.Ordinal.GetHashCode(Value);
            return (int)res;
        }

        public bool Equals(PropertyName other)
        {
            return IsNumber == other.IsNumber && Value.Equals(other.Value, StringComparison.Ordinal);
        }

        public int CompareTo(PropertyName other)
        {
            var i = IsNumber.CompareTo(other.IsNumber);
            if (i != 0)
                return i;
            return StringComparer.Ordinal.Compare(Value, other.Value);
        }

        public Identifier ToIdentifier()
        {
            if (IsNumber || !Lexemes.IsIdentifier(Value))
                return null;
            else
                return new Identifier(Loc, Value);
        }

        public Expression ToLiteral()
        {
            if (IsNumber)
                return new NumericLiteral(Loc, Lexemes.JavaScriptToNumber(Value));
            else
                return new StringLiteral(Loc, Value);
        }

        public static PropertyName FromJavaScriptNumber(Location loc, string str)
        {
            return new PropertyName(loc, Lexemes.JavaScriptToNumber(str));
        }

        public static PropertyName FromJavaScriptString(Location loc, string str, bool strict)
        {
            return new PropertyName(loc, Lexemes.JavaScriptToString(str, strict));
        }
    }

    public class ObjectLiteral : Expression
    {
        [NotNull]
        public readonly IImMap<PropertyName, Expression> Bindings;

        public ObjectLiteral(Location loc)
            : base(loc)
        {
            Bindings = Constants.EmptyBindings;
        }

        public ObjectLiteral(Location loc, IImMap<PropertyName, Expression> bindings)
            : base(loc)
        {
            Bindings = bindings ?? Constants.EmptyBindings;
        }

        public ObjectLiteral(Location loc, IEnumerable<KeyValuePair<string, Expression>> bindings)
            : base(loc)
        {
            if (bindings == null)
                Bindings = Constants.EmptyBindings;
            else
                Bindings = bindings.ToOrdMap(kv => new PropertyName(kv.Key), kv => kv.Value);
        }

        public ObjectLiteral(Location loc, IEnumerable<KeyValuePair<double, Expression>> bindings)
            : base(loc)
        {
            if (bindings == null)
                Bindings = Constants.EmptyBindings;
            else
                Bindings = bindings.ToOrdMap(kv => new PropertyName(kv.Key), kv => kv.Value);
        }

        public ObjectLiteral(Location loc, IEnumerable<KeyValuePair<int, Expression>> bindings)
            : base(loc)
        {
            if (bindings == null)
                Bindings = Constants.EmptyBindings;
            else
                Bindings = bindings.ToOrdMap(kv => new PropertyName(kv.Key), kv => kv.Value);
        }

        public ObjectLiteral(Location loc, IEnumerable<KeyValuePair<Identifier, Expression>> bindings)
            : base(loc)
        {
            if (bindings == null)
                Bindings = Constants.EmptyBindings;
            else
                Bindings = bindings.ToOrdMap(kv => new PropertyName(kv.Key), kv => kv.Value);
        }

        public ObjectLiteral(Location loc, IEnumerable<KeyValuePair<StringLiteral, Expression>> bindings)
            : base(loc)
        {
            if (bindings == null)
                Bindings = Constants.EmptyBindings;
            else
                Bindings = bindings.ToOrdMap(kv => new PropertyName(kv.Key), kv => kv.Value);
        }

        public ObjectLiteral(Location loc, IEnumerable<KeyValuePair<NumericLiteral, Expression>> bindings)
            : base(loc)
        {
            if (bindings == null)
                Bindings = Constants.EmptyBindings;
            else
                Bindings = bindings.ToOrdMap(kv => new PropertyName(kv.Key), kv => kv.Value);
        }

        public ObjectLiteral()
        {
            Bindings = Constants.EmptyBindings;
        }

        public ObjectLiteral(IImMap<PropertyName, Expression> bindings)
        {
            Bindings = bindings ?? Constants.EmptyBindings;
        }

        public ObjectLiteral(IEnumerable<KeyValuePair<string, Expression>> bindings)
        {
            if (bindings == null)
                Bindings = Constants.EmptyBindings;
            else
                Bindings = bindings.ToOrdMap(kv => new PropertyName(kv.Key), kv => kv.Value);
        }

        public ObjectLiteral(IEnumerable<KeyValuePair<double, Expression>> bindings)
        {
            if (bindings == null)
                Bindings = Constants.EmptyBindings;
            else
                Bindings = bindings.ToOrdMap(kv => new PropertyName(kv.Key), kv => kv.Value);
        }

        public ObjectLiteral(IEnumerable<KeyValuePair<int, Expression>> bindings)
        {
            if (bindings == null)
                Bindings = Constants.EmptyBindings;
            else
                Bindings = bindings.ToOrdMap(kv => new PropertyName(kv.Key), kv => kv.Value);
        }

        public ObjectLiteral(IEnumerable<KeyValuePair<Identifier, Expression>> bindings)
        {
            if (bindings == null)
                Bindings = Constants.EmptyBindings;
            else
                Bindings = bindings.ToOrdMap(kv => new PropertyName(kv.Key), kv => kv.Value);
        }

        public ObjectLiteral(IEnumerable<KeyValuePair<StringLiteral, Expression>> bindings)
        {
            if (bindings == null)
                Bindings = Constants.EmptyBindings;
            else
                Bindings = bindings.ToOrdMap(kv => new PropertyName(kv.Key), kv => kv.Value);
        }

        public ObjectLiteral(IEnumerable<KeyValuePair<NumericLiteral, Expression>> bindings)
        {
            if (bindings == null)
                Bindings = Constants.EmptyBindings;
            else
                Bindings = bindings.ToOrdMap(kv => new PropertyName(kv.Key), kv => kv.Value);
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.Object; } }

        public override bool NeedsDelimitingSpace { get { return false; } }
        public override bool IsDuplicatable { get { return false; } }

        public override bool IsValue { get { return Bindings.All(kv => kv.Value.IsValue); } }

        public override int Size { get { return 1 + Bindings.Sum(kv => kv.Value.Size); } }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            foreach (var kv in Bindings)
                kv.Value.AccumEffects(fxCtxt, callCtxt, evalTimes);
        }

        public override IImSeq<Expression> SubExpressions { get { return Bindings.Select(kv => kv.Value).ToSeq(); } }

        public override Expression CloneWithSubExpressions(IImSeq<Expression> subExpressions)
        {
            if (subExpressions.Count != Bindings.Count)
                throw new InvalidOperationException("mismatched sub-expressions arity");
            var bindings = new OrdMap<PropertyName, Expression>();
            var i = 0;
            foreach (var kv in Bindings)
                bindings.Add(kv.Key, subExpressions[i++]);
            return new ObjectLiteral(Loc, bindings);
        }

        public override IImSeq<Statements> SubStatementss { get { return Constants.EmptyStatementss; } }

        public override Expression CloneWithSubStatementss(IImSeq<Statements> subStatementss)
        {
            if (subStatementss.Count != 0)
                throw new InvalidOperationException("mismatched sub-statements arity");
            return this;
        }

        public override void CollectBoundVars(IMSet<Identifier> boundVars)
        {
            foreach (var kv in Bindings)
                kv.Value.CollectBoundVars(boundVars);
        }

        public override void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
            foreach (var kv in Bindings)
                kv.Value.CollectFreeVars(boundVars, freeVars);
        }

        public override Expression Simplify(SimplifierContext ctxt, EvalTimes evalTimes)
        {
            var simpBindings =
                Bindings.Select
                    (kv => new KeyValuePair<PropertyName, Expression>(kv.Key, kv.Value.Simplify(ctxt, evalTimes))).
                    ToOrdMap();
            return new ObjectLiteral(Loc, simpBindings);
        }

        public override void Append(Writer writer, Precedence required)
        {
            if (Bindings.Count == 0)
                writer.Append("{}");
            else if (Bindings.Count < 4)
            {
                writer.Append('{');
                writer.Space();
                var first = true;
                foreach (var kv in Bindings)
                {
                    if (first)
                        first = false;
                    else
                    {
                        writer.Append(',');
                        writer.Space();
                    }
                    kv.Key.Append(writer);
                    writer.Append(':');
                    writer.Space();
                    kv.Value.Append(writer, Precedence.Assignment);
                }
                writer.Space();
                writer.Append('}');
            }
            else
                writer.Indented
                    (w =>
                         {
                             w.Append('{');
                             w.EndLine();
                         },
                     w =>
                     {
                         var first = true;
                         foreach (var kv in Bindings)
                         {
                             if (first)
                                 first = false;
                             else
                             {
                                 w.Append(',');
                                 w.EndLine();
                             }
                             kv.Key.Append(w);
                             w.Append(':');
                             w.Space();
                             kv.Value.Append(w, Precedence.Assignment);
                         }
                     },
                     w =>
                     {
                         w.EndLine();
                         w.Append('}');
                     });
        }

        public override int GetHashCode()
        {
            return (int)Bindings.Aggregate(0x23893e81u, (h, kv) => Constants.Rot7(h) ^ Constants.Rot17((uint)kv.Key.GetHashCode()) ^ (uint)kv.Value.GetHashCode());
        }

        protected override bool EqualBody(Expression other)
        {
            var ol = (ObjectLiteral)other;
            if (Bindings.Count != ol.Bindings.Count)
                return false;
            if (Bindings.Where((t, j) => !Bindings.Keys[j].Equals(ol.Bindings.Keys[j])).Any())
                return false;
            return Bindings.All(kv => kv.Value.Equals(ol.Bindings[kv.Key]));
        }

        protected override int CompareBody(Expression other)
        {
            var ol = (ObjectLiteral)other;
            var i = Bindings.Count.CompareTo(ol.Bindings.Count);
            if (i != 0)
                return i;
            for (var j = 0; j < Bindings.Count; j++)
            {
                i = Bindings.Keys[j].CompareTo(ol.Bindings.Keys[j]);
                if (i != 0)
                    return i;
            }
            foreach (var kv in Bindings)
            {
                i = kv.Value.CompareTo(ol.Bindings[kv.Key]);
                if (i != 0)
                    return i;
            }
            return 0;
        }
    }

    // ----------------------------------------------------------------------
    // RegularExpressionLiteral
    // ----------------------------------------------------------------------

    public class RegularExpressionLiteral : Expression
    {
        [NotNull]
        public readonly string Pattern;
        [CanBeNull] // null => no attributes
        public readonly string Attributes;

        public RegularExpressionLiteral(Location loc, string pattern) : base(loc)
        {
            Pattern = pattern;
            Attributes = null;
        }

        public RegularExpressionLiteral(Location loc, string pattern, string attributes) : base(loc)
        {
            Pattern = pattern;
            Attributes = attributes;
        }

        public RegularExpressionLiteral(string pattern)
        {
            Pattern = pattern;
            Attributes = null;
        }

        public RegularExpressionLiteral(string pattern, string attributes)
        {
            Pattern = pattern;
            Attributes = attributes;
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.RegularExpression; } }

        public override bool NeedsDelimitingSpace { get { return false; } }
        public override bool IsDuplicatable { get { return true; } } 
        public override bool IsValue { get { return true; } }

        public override int Size { get { return 1; } }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
        }

        public override IImSeq<Expression> SubExpressions
        {
            get { return Constants.EmptyExpressions; }
        }

        public override Expression CloneWithSubExpressions(IImSeq<Expression> subExpressions)
        {
            if (subExpressions.Count != 0)
                throw new InvalidOperationException("mismatched sub-expressions arity");
            return this;
        }

        public override IImSeq<Statements> SubStatementss { get { return Constants.EmptyStatementss; } }

        public override Expression CloneWithSubStatementss(IImSeq<Statements> subStatementss)
        {
            if (subStatementss.Count != 0)
                throw new InvalidOperationException("mismatched sub-statements arity");
            return this;
        }

        public override void CollectBoundVars(IMSet<Identifier> boundVars)
        {
        }

        public override void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
        }

        public override Expression Simplify(SimplifierContext ctxt, EvalTimes evalTimes)
        {
            return this;
        }

        public override void Append(Writer writer, Precedence required)
        {
            Wrap(writer,
                 Precedence.LHS,
                 required,
                 (w, _) =>
                 {
                     w.Append("new");
                     w.HardSpace();
                     w.Append("RegExp(\"");
                     w.Append(Lexemes.StringToJavaScript(Pattern));
                     w.Append('"');
                     if (Attributes != null)
                     {
                         w.Append(',');
                         w.Space();
                         w.Append('"');
                         w.Append(Lexemes.StringToJavaScript(Attributes));
                         w.Append('"');
                     }
                     w.Append(")");
                 });
        }

        public override int GetHashCode()
        {
            var res = 0xb4cc5c34u;
            res ^= (uint)Pattern.GetHashCode();
            if (Attributes != null)
                res = Constants.Rot7(res) ^ (uint)Attributes.GetHashCode();
            return (int)res;
        }

        protected override bool EqualBody(Expression other)
        {
            var re = (RegularExpressionLiteral)other;
            if (!Pattern.Equals(re.Pattern, StringComparison.Ordinal))
                return false;
            if ((Attributes == null) != (re.Attributes == null))
                return false;
            return Attributes == null || Attributes.Equals(re.Attributes, StringComparison.Ordinal);
        }

        protected override int CompareBody(Expression other)
        {
            var re = (RegularExpressionLiteral)other;
            var i = StringComparer.Ordinal.Compare(Pattern, re.Pattern);
            if (i != 0)
                return i;
            if (Attributes == null && re.Attributes != null)
                return -1;
            if (Attributes != null && re.Attributes == null)
                return 1;
            return Attributes == null ? 0 : StringComparer.Ordinal.Compare(Attributes, re.Attributes);
        }

        public static RegularExpressionLiteral FromJavaScript(Location loc, string str)
        {
            var pattern = default(string);
            var attributes = default(string);
            Lexemes.JavaScriptToRegexp(str, out pattern, out attributes);
            return new RegularExpressionLiteral(loc, pattern, attributes);
        }
    }

    // ----------------------------------------------------------------------
    // StringLiteral
    // ----------------------------------------------------------------------

    public class StringLiteral : Expression
    {
        [NotNull]
        public readonly string Value;

        public StringLiteral(Location loc, string value) : base(loc)
        {
            Value = value;
        }

        public StringLiteral(string value)
        {
            Value = value;
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.String; } }

        public override bool NeedsDelimitingSpace { get { return false; } }
        public override bool IsDuplicatable { get { return true; } }
        public override bool IsValue { get { return true; } }

        public override int Size { get { return 1; } }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
        }

        public override IImSeq<Expression> SubExpressions
        {
            get { return Constants.EmptyExpressions; }
        }

        public override Expression CloneWithSubExpressions(IImSeq<Expression> subExpressions)
        {
            if (subExpressions.Count != 0)
                throw new InvalidOperationException("mismatched sub-expressions arity");
            return this;
        }

        public override IImSeq<Statements> SubStatementss { get { return Constants.EmptyStatementss; } }

        public override Expression CloneWithSubStatementss(IImSeq<Statements> subStatementss)
        {
            if (subStatementss.Count != 0)
                throw new InvalidOperationException("mismatched sub-statements arity");
            return this;
        }

        public override void CollectBoundVars(IMSet<Identifier> boundVars)
        {
        }

        public override void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
        }

        public override Expression Simplify(SimplifierContext ctxt, EvalTimes evalTimes)
        {
            return this;
        }

        public override void Append(Writer writer, Precedence required)
        {
            writer.Append('"');
            writer.Append(Lexemes.StringToJavaScript(Value));
            writer.Append('"');
        }

        public override int GetHashCode()
        {
            return (int)(0x741831f6u ^ (uint)StringComparer.Ordinal.GetHashCode(Value));
        }

        protected override bool EqualBody(Expression other)
        {
            var sl = (StringLiteral)other;
            return Value.Equals(sl.Value, StringComparison.Ordinal);
        }

        protected override int CompareBody(Expression other)
        {
            var sl = (StringLiteral)other;
            return StringComparer.Ordinal.Compare(Value, sl.Value);
        }

        public Identifier StringToIdentifier()
        {
            if (!Lexemes.IsIdentifier(Value))
                return null;
            else
                return new Identifier(Loc, Value);
        }

        public static StringLiteral FromJavaScript(string str, bool strict)
        {
            if (str == null || str.Length < 2 ||
                (str[0] != '"' || str[str.Length - 1] != '"') && (str[0] != '\'' || str[str.Length - 1] != '\''))
                throw new ArgumentException("unrecognized JavaScript string literal");
            else
                return new StringLiteral(Lexemes.JavaScriptToString(str.Substring(1, str.Length - 2), strict));
        }
    }

    // ----------------------------------------------------------------------
    // ThisExpression
    // ----------------------------------------------------------------------

    public class ThisExpression : Expression
    {
        public ThisExpression(Location loc)
            : base(loc)
        {
        }

        public ThisExpression()
        {
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.This; } }

        public override bool NeedsDelimitingSpace { get { return false; } }
        public override bool IsDuplicatable { get { return true; } }
        public override bool IsValue { get { return false; } }

        public override IImSeq<Expression> SubExpressions
        {
            get { return Constants.EmptyExpressions; }
        }

        public override Expression CloneWithSubExpressions(IImSeq<Expression> subExpressions)
        {
            if (subExpressions.Count != 0)
                throw new InvalidOperationException("mismatched sub-expressions arity");
            return this;
        }

        public override int Size { get { return 1; } }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            if (callCtxt != null)
                // Can't substitute arguments into function bodies which use 'this'
                callCtxt.Fail();
        }

        public override IImSeq<Statements> SubStatementss { get { return Constants.EmptyStatementss; } }

        public override Expression CloneWithSubStatementss(IImSeq<Statements> subStatementss)
        {
            if (subStatementss.Count != 0)
                throw new InvalidOperationException("mismatched sub-statements arity");
            return this;
        }

        public override void CollectBoundVars(IMSet<Identifier> boundVars)
        {
        }

        public override void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
        }

        public override Expression Simplify(SimplifierContext ctxt, EvalTimes evalTimes)
        {
            return this;
        }

        public override void Append(Writer writer, Precedence required) { writer.Append("this"); }

        public override int GetHashCode()
        {
            var res = 0x9b87931eu;
            return (int)res;
        }

        protected override bool EqualBody(Expression other)
        {
            return true;
        }

        protected override int CompareBody(Expression other)
        {
            return 0;
        }
    }

    // ----------------------------------------------------------------------
    // UnaryExpression
    // ----------------------------------------------------------------------

    public class UnaryExpression : Expression
    {
        [NotNull]
        public readonly Expression Operand;
        [NotNull]
        public readonly UnaryOperator Operator;

        public UnaryExpression(Location loc, Expression operand, UnaryOperator unaryOperator)
            : base(loc)
        {
            Operand = operand;
            Operator = unaryOperator;
        }

        public UnaryExpression(Location loc, Expression operand, UnaryOp op)
            : base(loc)
        {
            Operand = operand;
            Operator = new UnaryOperator(op);
        }

        public UnaryExpression(Location loc, UnaryOperator unaryOperator, Expression operand)
            : base(loc)
        {
            Operator = unaryOperator;
            Operand = operand;
        }

        public UnaryExpression(Location loc, UnaryOp op, Expression operand)
            : base(loc)
        {
            Operator = new UnaryOperator(op);
            Operand = operand;
        }


        public UnaryExpression(Expression operand, UnaryOperator unaryOperator)
        {
            Operand = operand;
            Operator = unaryOperator;
        }

        public UnaryExpression(Expression operand, UnaryOp op)
        {
            Operand = operand;
            Operator = new UnaryOperator(op);
        }

        public UnaryExpression(UnaryOperator unaryOperator, Expression operand)
        {
            Operator = unaryOperator;
            Operand = operand;
        }

        public UnaryExpression(UnaryOp op, Expression operand)
        {
            Operator = new UnaryOperator(op);
            Operand = operand;
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.Unary; } }

        public override bool NeedsDelimitingSpace
        {
            get { return Operator.NeedsDelimitingSpace || Operand.NeedsDelimitingSpace; }
        }

        public override bool IsDuplicatable { get { return false; } }
        public override bool IsValue { get { return false; } }

        public override int Size { get { return 1 + Operand.Size; } }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            Operand.AccumEffects(fxCtxt, callCtxt, evalTimes);
            if (Operator.IsMutating)
                Operand.AccumLvalueEffects(fxCtxt, callCtxt);
        }

        public override IImSeq<Expression> SubExpressions { get { return new Seq<Expression> { Operand }; } }

        public override Expression CloneWithSubExpressions(IImSeq<Expression> subExpressions)
        {
            if (subExpressions.Count != 1)
                throw new InvalidOperationException("mismatched sub-expressions arity");
            return new UnaryExpression(Loc, subExpressions[0], Operator);
        }

        public override IImSeq<Statements> SubStatementss { get { return Constants.EmptyStatementss; } }

        public override Expression CloneWithSubStatementss(IImSeq<Statements> subStatementss)
        {
            if (subStatementss.Count != 0)
                throw new InvalidOperationException("mismatched sub-statements arity");
            return this;
        }

        public override void CollectBoundVars(IMSet<Identifier> boundVars)
        {
            Operand.CollectBoundVars(boundVars);
        }

        public override void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
            Operand.CollectFreeVars(boundVars, freeVars);
        }

        public override Expression Simplify(SimplifierContext ctxt, EvalTimes evalTimes)
        {
            var simpOperand = Operand.SimplifyValue(ctxt, evalTimes);
            var b = simpOperand.IsBoolean;
            if (b.HasValue && Operator.Op == UnaryOp.LogicalNot)
                return new BooleanLiteral(!b.Value);
            else
            {
                if (Operator.IsMutating)
                    simpOperand.AccumLvalueEffects(ctxt);
                return new UnaryExpression(Loc, simpOperand, Operator);
            }
        }

        public override void Append(Writer writer, Precedence required)
        {
            if (Operator.IsPostfix)
            {
                Wrap(writer,
                     Precedence.Postfix,
                     required,
                     (w, _) =>
                     {
                         Operand.Append(w, Precedence.LHS);
                         if (Operator.NeedsDelimitingSpace)
                             w.HardSpace();
                         Operator.Append(w);
                     });
            }
            else
            {
                Wrap(writer,
                     Precedence.Unary,
                     required,
                     (w, _) =>
                     {
                         Operator.Append(w);
                         if (Operator.NeedsDelimitingSpace)
                             w.HardSpace();
                         Operand.Append(w, Precedence.Unary);
                     });
            }
        }

        public override int GetHashCode()
        {
            var res = 0x3b8f4898u;
            res ^= (uint)Operator.GetHashCode();
            res = Constants.Rot5(res) ^ (uint)Operand.GetHashCode();
            return (int)res;
        }

        protected override bool EqualBody(Expression other)
        {
            var ue = (UnaryExpression)other;
            return Operator.Equals(ue.Operator) && Operand.Equals(ue.Operand);
        }

        protected override int CompareBody(Expression other)
        {
            var ue = (UnaryExpression)other;
            var i = Operator.CompareTo(ue.Operator);
            if (i != 0)
                return i;
            return Operand.CompareTo(ue.Operand);
        }
    }

    // ----------------------------------------------------------------------
    // StatementsPseudoExpression
    // ----------------------------------------------------------------------

    // Allows embedding of statements into expression context without need to encode as
    // (function () { ... })().
    // NOTE: Does not introduce a fresh scope

    public class StatementsPseudoExpression : Expression
    {
        [NotNull]
        public readonly Statements Body;
        [CanBeNull] // null => result is undefined
        public readonly Expression Value;

        public StatementsPseudoExpression(Location loc, Statements body, Expression value)
            : base(loc)
        {
            Body = body;
            Value = value;
        }

        public StatementsPseudoExpression(Statements body, Expression value)
        {
            Body = body;
            Value = value;
        }

        public override ExpressionFlavor Flavor { get { return ExpressionFlavor.StatementsPseudo; } }

        public override bool NeedsDelimitingSpace { get { return false; } }

        public override bool IsDuplicatable { get { return false; } }

        public override bool IsValue { get { return false; } }

        public override int Size { get { return Body.Size + (Value == null ? 0 : Value.Size); } }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            // Same scope
            Body.AccumEffects(fxCtxt, callCtxt, evalTimes);
            if (Value != null)
                Value.AccumEffects(fxCtxt, callCtxt, evalTimes);
        }

        public override IImSeq<Expression> SubExpressions
        {
            get { return Value == null ? Constants.EmptyExpressions : new Seq<Expression> { Value }; }
        }

        public override Expression CloneWithSubExpressions(IImSeq<Expression> subExpressions)
        {
            if (Value == null && subExpressions.Count != 0 || Value != null && subExpressions.Count != 1)
                throw new InvalidOperationException("mismatched sub-expressions arity");
            if (Value == null)
                return this;
            else
                return new StatementsPseudoExpression(Loc, Body, subExpressions[0]);
        }

        public override IImSeq<Statements> SubStatementss { get { return new Seq<Statements> { Body }; } }

        public override Expression CloneWithSubStatementss(IImSeq<Statements> subStatementss)
        {
            if (subStatementss.Count != 1)
                throw new InvalidOperationException("mismatched sub-statements arity");
            return new StatementsPseudoExpression(Loc, subStatementss[0], Value);
        }

        public override void CollectBoundVars(IMSet<Identifier> boundVars)
        {
            // No fresh scope
            Body.CollectBoundVars(boundVars);
            if (Value != null)
                Value.CollectBoundVars(boundVars);
        }

        public override void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
            // No fresh scope
            Body.CollectFreeVars(boundVars, freeVars);
            if (Value != null)
                Value.CollectFreeVars(boundVars, freeVars);
        }

        public override Expression Simplify(SimplifierContext ctxt, EvalTimes evalTimes)
        {
            var subCtxt = ctxt.InFreshStatements();
            Body.Simplify(subCtxt, evalTimes, false);
            var simpValue = Value == null ? null : Value.Simplify(subCtxt, evalTimes);

            if (ctxt.Statements != null)
            {
                var subFxCtxt = new EffectsContext(ctxt.isValue);
                foreach (var s in subCtxt.Statements)
                    s.AccumEffects(subFxCtxt, null, null);

                if (subFxCtxt.AccumEffects.CommutableWith(ctxt.ContextEffects))
                {
                    foreach (var s in subCtxt.Statements)
                        ctxt.Add(s);
                    return simpValue;
                }
                // else: fall-through
            }
            // else: fall-through

            return new StatementsPseudoExpression(Loc, new Statements(subCtxt.Statements), simpValue);
        }

        public override void Append(Writer writer, Precedence required)
        {
            if (Precedence.LHS < required)
            {
                writer.Append('(');
                Append(writer, Precedence.LHS);
                writer.Append(')');
            }
            else if (required < Precedence.LHS)
                Append(writer, Precedence.LHS);
            else
            {
                writer.Append("(function(){");
                writer.Indented
                    (w =>
                         {
                             Body.Append(w);
                             if (Value != null)
                             {
                                 w.Append("return ");
                                 Value.Append(w);
                                 w.Append(';');
                             }
                             writer.EndLine();
                         });
                writer.Append("})()");
            }
        }

        public override int GetHashCode()
        {
            var res = 0x207d5ba2u;
            res ^= (uint)Body.GetHashCode();
            if (Value != null)
                res = Constants.Rot5(res) ^ (uint)Value.GetHashCode();
            return (int)res;
        }

        protected override bool EqualBody(Expression other)
        {
            var spe = (StatementsPseudoExpression)other;
            if ((Value == null) != (spe.Value == null))
                return false;
            return Body.Equals(spe.Body) && (Value == null || Value.Equals(spe.Value));
        }

        protected override int CompareBody(Expression other)
        {
            var spe = (StatementsPseudoExpression)other;
            var i = Body.CompareTo(spe.Body);
            if (i != 0)
                return i;
            if (Value == null && spe.Value != null)
                return -1;
            if (Value != null && spe.Value == null)
                return 1;
            return Value == null ? 0 : Value.CompareTo(spe.Value);
        }
    }
}
