using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.JavaScript.JST
{
    public enum ReturnStatus
    {
        None,
        One,
        Fail
    }

    public class ReturnResult
    {
        public ReturnStatus Status;
        [CanBeNull]
        public Expression Value;

        public static ReturnResult None;
        public static ReturnResult Fail;

        public ReturnResult(Expression value)
        {
            Status = ReturnStatus.One;
            Value = value;
        }

        private ReturnResult(ReturnStatus status)
        {
            Status = status;
        }

        static ReturnResult()
        {
            None = new ReturnResult(ReturnStatus.None);
            Fail = new ReturnResult(ReturnStatus.Fail);
        }

        public bool NotNone { get { return Status != ReturnStatus.None; } }
    }

    public enum StatementFlavor
    {
        Break,
        Comment,
        Continue,
        Do,
        Expression,
        For,
        Function,
        If,
        Labelled,
        Return,
        Switch,
        Throw,
        Try,
        Variable,
        While,
        With
    }

    // ----------------------------------------------------------------------
    // Statement
    // ----------------------------------------------------------------------

    public abstract class Statement : IEquatable<Statement>, IComparable<Statement>
    {
        [CanBeNull] // null => no location known
        public readonly Location Loc;

        protected Statement(Location loc)
        {
            Loc = loc;
        }

        protected Statement()
        {
        }

        public abstract StatementFlavor Flavor { get; }

        public override bool Equals(object obj)
        {
            var other = obj as Statement;
            return other != null && Equals(other);
        }

        public override int GetHashCode()
        {
            throw new InvalidOperationException("must be overridden");
        }

        public bool Equals(Statement other)
        {
            return Flavor == other.Flavor && EqualBody(other);
        }

        protected abstract bool EqualBody(Statement other);

        public int CompareTo(Statement other)
        {
            var i = Flavor.CompareTo(other.Flavor);
            if (i != 0)
                return i;
            return CompareBody(other);
        }

        protected abstract int CompareBody(Statement other);

        public abstract bool NeedsBraces { get; }
        public abstract bool NeedsSpace { get; }
        public abstract int Size { get; }

        // See Expression::AccumEffects
        public abstract ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes);

        public abstract IImSeq<Expression> SubExpressions { get; }
        public abstract Statement CloneWithSubExpressions(IImSeq<Expression> subExpressions);

        public abstract IImSeq<Statements> SubStatementss { get; }
        public abstract Statement CloneWithSubStatementss(IImSeq<Statements> subStatementss);

        public abstract void CollectBoundVars(IMSet<Identifier> boundVars);
        public abstract void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars);

        // Simplify statement into simplifier context's statements (which can't be null)
        public abstract ControlFlow Simplify(SimplifierContext ctxt, EvalTimes evalTimes, bool fallOffIsReturn);

        // Try to represent statements as a sequence of return-free statements and an optional return,
        // itself with an optional value (watch out for double lifting).
        public abstract ReturnResult ToReturnResult(ISeq<Statement> acc);

        public abstract void Append(Writer writer);

        public static Statement Assignment(Expression left, Expression right)
        {
            return new ExpressionStatement(new BinaryExpression(left, new BinaryOperator(BinaryOp.Assignment), right));
        }

        public static Statement IdAssignment(Identifier left, Expression right)
        {
            return Assignment(left.ToE(), right);
        }

        public static Statement IndexAssignment(Expression left, Expression index, Expression right)
        {
            return Assignment(new IndexExpression(left, index), right);
        }

        public static Statement IndexAssignment(Expression left, double index, Expression right)
        {
            return Assignment(new IndexExpression(left, index), right);
        }

        public static Statement IndexAssignment(Expression left, string name, Expression right)
        {
            return Assignment(new IndexExpression(left, name), right);
        }

        public static Statement DotAssignment(Expression left, Identifier field, Expression right)
        {
            return Assignment(Expression.Dot(left, field), right);
        }

        public static Statement Call(Expression applicand, IImSeq<Expression> arguments)
        {
            return new ExpressionStatement(new CallExpression(applicand, arguments));
        }

        public static Statement Call(Expression applicand, params Expression[] arguments)
        {
            return new ExpressionStatement(new CallExpression(applicand, arguments));
        }

        public static Statement OptReturn(bool hasReturnType, Expression value)
        {
            if (hasReturnType)
                return new ReturnStatement(value);
            else
                return new ExpressionStatement(value);
        }

        public static Statement OptReturnCall(bool hasReturnType, Expression applicand, IImSeq<Expression> arguments)
        {
            if (hasReturnType)
                return new ReturnStatement(new CallExpression(applicand, arguments));
            else
                return new ExpressionStatement(new CallExpression(applicand, arguments));
        }

        public static Statement OptReturnCall(bool hasReturnType, Expression applicand, params Expression[] arguments)
        {
            if (hasReturnType)
                return new ReturnStatement(new CallExpression(applicand, arguments));
            else
                return new ExpressionStatement(new CallExpression(applicand, arguments));
        }

        public static Statement DotCall(Expression left, Identifier field, IImSeq<Expression> arguments)
        {
            return new ExpressionStatement(Expression.DotCall(left, field, arguments));
        }

        public static Statement DotCall(Expression left, Identifier field, params Expression[] arguments)
        {
            return new ExpressionStatement(Expression.DotCall(left, field, arguments));
        }

        public static Statement Var(Identifier identifier)
        {
            return new VariableStatement(new VariableDeclaration(identifier));
        }

        public static Statement Var(Identifier identifier, Expression initializer)
        {
            return new VariableStatement(new VariableDeclaration(identifier, initializer));
        }

        public static Statement Var(Identifier identifier1, Identifier identifier2)
        {
            return new VariableStatement(new VariableDeclaration(identifier1), new VariableDeclaration(identifier2));
        }

        public static Statement Var(Identifier identifier1, Expression initializer1, Identifier identifier2, Expression initializer2)
        {
            return new VariableStatement
                (new VariableDeclaration(identifier1, initializer1),
                 new VariableDeclaration(identifier2, initializer2));
        }
    }

    public class Statements : IEquatable<Statements>, IComparable<Statements>, IEnumerable<Statement>
    {
        public readonly IImSeq<Statement> Body;

        public Statements()
        {
            Body = Constants.EmptyStatements;
        }

        public Statements(IImSeq<Statement> body)
        {
            Body = body ?? Constants.EmptyStatements;
        }

        public Statements(params Statement[] body)
        {
            Body = body.ToSeq();
        }

        public int Size { get { return Body.Sum(s => s.Size); } }

        public bool NeedsSpace { get { return Body.Count == 1 && Body[0].NeedsSpace && !Body[0].NeedsBraces; } }

        public bool NeedsBraces { get { return Body.Count > 1 || Body.Count == 1 && Body[0].NeedsBraces; } }

        public void CollectBoundVars(IMSet<Identifier> boundVars)
        {
            foreach (var s in Body)
                s.CollectBoundVars(boundVars);
        }

        public void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
            foreach (var s in Body)
                s.CollectFreeVars(boundVars, freeVars);
        }

        public ControlFlow Simplify(SimplifierContext ctxt, EvalTimes evalTimes, bool fallOffIsReturn)
        {
            var neverReturn = false;
            var finalCf = ControlFlow.AlwaysReturn;
            for (var i = 0; i < Body.Count; i++)
            {
                var cf = Body[i].Simplify(ctxt, evalTimes, fallOffIsReturn && i == Body.Count - 1);
                if (cf.Value == ControlFlowEnum.NeverReturn)
                    neverReturn = true;
                if (cf.IsTop)
                    // Give up trying to track evaluation times
                    evalTimes = EvalTimes.Top;
                else if (cf.Value != ControlFlowEnum.AlwaysReturn)
                    // Assume previous statement may not return, thus following execution is optional
                    evalTimes = evalTimes.Lub(EvalTimes.Opt);
                finalCf = finalCf.Lub(cf);
            }
            return neverReturn ? ControlFlow.NeverReturn : finalCf;
        }

        public void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            foreach (var s in Body)
                s.AccumEffects(fxCtxt, callCtxt, evalTimes);
        }

        public ControlFlow AccumBlockEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            var neverReturn = false;
            var finalCf = ControlFlow.AlwaysReturn;
            foreach (var s in Body)
            {
                var cf = s.AccumEffects(fxCtxt, callCtxt, evalTimes);
                if (cf.Value == ControlFlowEnum.NeverReturn)
                    // Keep accumulating effects so we can check for un-evaled parameters, however remember
                    // the block as a whole never returns
                    neverReturn = true;
                if (callCtxt != null && cf.IsTop)
                    // Give up trying to track evaluation times
                    evalTimes = EvalTimes.Top;
                else if (callCtxt != null && cf.Value != ControlFlowEnum.AlwaysReturn)
                    // Following statements may not be evaluated
                    evalTimes = evalTimes.Lub(EvalTimes.Opt);
                finalCf = finalCf.Lub(cf);
            }
            return neverReturn ? ControlFlow.NeverReturn : finalCf;
        }

        public ReturnResult ToReturnResult(ISeq<Statement> acc)
        {
            if (Body.Count > 0)
            {
                for (var i = 0; i < Body.Count - 1; i++)
                {
                    if (Body[i].ToReturnResult(acc).NotNone)
                        return ReturnResult.Fail;
                }
                return Body[Body.Count - 1].ToReturnResult(acc);
            }
            else
                return ReturnResult.None;
        }

        public override bool Equals(object obj)
        {
            var other = obj as Statements;
            return other != null && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)Body.Aggregate(0x699a17ffu, (h, s) => Constants.Rot7(h) ^ (uint)s.GetHashCode());
        }

        public bool Equals(Statements other)
        {
            return Body.Count == other.Body.Count && !Body.Select((s, i) => !s.Equals(other.Body[i])).Any();
        }

        public int CompareTo(Statements other)
        {
            var i = Body.Count.CompareTo(other.Body.Count);
            if (i != 0)
                return i;
            for (var j = 0; j < Body.Count; j++)
            {
                i = Body[j].CompareTo(other.Body[j]);
                if (i != 0)
                    return i;
            }
            return 0;
        }

        public void Append(Writer writer)
        {
            foreach (var s in Body)
                s.Append(writer);
        }

        public void AppendBlock(Writer writer)
        {
            writer.Indented
                (w =>
                     {
                         w.Append('{');
                         w.EndLine();
                     },
                 w =>
                 {
                     foreach (var s in Body)
                         s.Append(w);
                 },
                 w =>
                 {
                     w.Append('}');
                     w.EndLine();
                 });
        }

        public void AppendBlockNoEndLine(Writer writer)
        {
            writer.Indented
                (w =>
                     {
                         w.Append('{');
                         w.EndLine();
                     },
                 w =>
                 {
                     foreach (var s in Body)
                         s.Append(w);
                 },
                 w => w.Append('}'));
        }

        public void AppendStatementOrBlock(Writer writer)
        {
            if (Body.Count == 0)
                writer.Indented
                    (null,
                     w =>
                     {
                         w.Append(';');
                         w.EndLine();
                     },
                     null);
            else if (Body.Count == 1 && !Body[0].NeedsBraces)
                writer.Indented(null, w => Body[0].Append(w), null);
            else
            {
                writer.Append('{');
                writer.EndLine();
                writer.Indented(w => { foreach (var s in Body) s.Append(w); });
                writer.Append('}');
                writer.EndLine();
            }
        }

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

        public static Statements FromString(string str, string label, bool strict)
        {
            using (var reader = new StringReader(str))
            {
                var lexer = new Lexer(reader, label, strict);
                var parser = new Parser(lexer);
                return parser.TopLevelStatements();
            }
        }

        public int Count { get { return Body.Count; } }

        public Statement this[int i] { get { return Body[i]; } }

        public IEnumerator<Statement> GetEnumerator()
        {
            return Body.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Body.GetEnumerator();
        }
    }

    // ----------------------------------------------------------------------
    // BreakStatement
    // ----------------------------------------------------------------------

    public class BreakStatement : Statement
    {
        [CanBeNull] // null => no label
        public readonly Identifier Label;

        public BreakStatement(Location loc)
            : base(loc)
        {
            Label = null;
        }

        public BreakStatement(Location loc, Identifier label)
            : base(loc)
        {
            Label = label;
        }

        public BreakStatement()
        {
            Label = null;
        }

        public BreakStatement(Identifier label)
        {
            Label = label;
        }

        public override StatementFlavor Flavor { get { return StatementFlavor.Break; } }

        public override bool NeedsBraces { get { return false; } }

        public override bool NeedsSpace { get { return true; } }

        public override int Size { get { return 1; } }

        public override ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            // Give up trying to track control flow
            return ControlFlow.Top;
        }

        public override IImSeq<Expression> SubExpressions { get { return Constants.EmptyExpressions; } }

        public override Statement CloneWithSubExpressions(IImSeq<Expression> subExpressions)
        {
            if (subExpressions.Count != 0)
                throw new InvalidOperationException("mismatched sub-expressions arity");
            return this;
        }

        public override IImSeq<Statements> SubStatementss { get { return Constants.EmptyStatementss; } }

        public override Statement CloneWithSubStatementss(IImSeq<Statements> subStatementss)
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

        public override ControlFlow Simplify(SimplifierContext ctxt, EvalTimes evalTimes, bool fallOffIsReturn)
        {
            ctxt.Add(this);
            return ControlFlow.Top;
        }

        public override ReturnResult ToReturnResult(ISeq<Statement> acc)
        {
            acc.Add(this);
            return ReturnResult.None;
        }

        public override void Append(Writer writer)
        {
            writer.Append("break");
            if (Label != null)
            {
                writer.HardSpace();
                Label.Append(writer);
            }
            writer.Append(';');
            writer.EndLine();
        }

        public override int GetHashCode()
        {
            var res = 0x38d01377u;
            if (Label != null)
                res = Constants.Rot5(res) ^ (uint)Label.GetHashCode();
            return (int)res;
        }

        protected override bool EqualBody(Statement other)
        {
            var bs = (BreakStatement)other;
            if ((Label == null) != (bs.Label == null))
                return false;
            return Label == null || Label.Equals(bs.Label);
        }

        protected override int CompareBody(Statement other)
        {
            var bs = (BreakStatement)other;
            if (Label == null && bs.Label != null)
                return -1;
            if (Label != null && bs.Label == null)
                return 1;
            return Label == null ? 0 : Label.CompareTo(bs.Label);
        }
    }

    // ----------------------------------------------------------------------
    // CommentStatement
    // ----------------------------------------------------------------------

    public class CommentStatement : Statement
    {
        [CanBeNull] // null => no comment
        public readonly string Comment;

        public CommentStatement(Location loc, string comment)
            : base(loc)
        {
            Comment = comment;
        }

        public CommentStatement(string comment)
        {
            Comment = comment;
        }

        public override StatementFlavor Flavor { get { return StatementFlavor.Comment; } }

        public override bool NeedsBraces { get { return true; } }

        public override bool NeedsSpace { get { return false; } }

        public override int Size { get { return 0; } }

        public override ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            return ControlFlow.AlwaysReturn;
        }

        public override IImSeq<Expression> SubExpressions { get { return Constants.EmptyExpressions; } }

        public override Statement CloneWithSubExpressions(IImSeq<Expression> subExpressions)
        {
            if (subExpressions.Count != 0)
                throw new InvalidOperationException("mismatched sub-expressions arity");
            return this;
        }

        public override IImSeq<Statements> SubStatementss { get { return Constants.EmptyStatementss; } }

        public override Statement CloneWithSubStatementss(IImSeq<Statements> subStatementss)
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

        public override ControlFlow Simplify(SimplifierContext ctxt, EvalTimes evalTimes, bool fallOffIsReturn)
        {
            if (Comment != null)
                ctxt.Add(this);
            return ControlFlow.AlwaysReturn;
        }

        public override ReturnResult ToReturnResult(ISeq<Statement> acc)
        {
            acc.Add(this);
            return ReturnResult.None;
        }

        public override void Append(Writer writer)
        {
            if (writer.PrettyPrint && Comment != null)
            {
                writer.Append("// ");
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
                        if (i < Comment.Length)
                            writer.Append("// ");
                    }
                    else if (Comment[i] == '\n' || Comment[i] == '\r')
                    {
                        writer.Append(Comment.Substring(s, i - s));
                        writer.EndLine();
                        i++;
                        s = i;
                        if (i < Comment.Length)
                            writer.Append("// ");
                    }
                    else
                        i++;
                }
                writer.Append(Comment.Substring(s, i - s));
                writer.EndLine();
            }
        }

        public override int GetHashCode()
        {
            var res = 0x2ba9c55du;
            return (int)res;
        }

        protected override bool EqualBody(Statement other)
        {
            return true;
        }

        protected override int CompareBody(Statement other)
        {
            return 0;
        }
    }

    // ----------------------------------------------------------------------
    // ContinueStatement
    // ----------------------------------------------------------------------

    public class ContinueStatement : Statement
    {
        [CanBeNull] // null => no label
        public readonly Identifier Label;

        public ContinueStatement(Location loc)
            : base(loc)
        {
            Label = null;
        }

        public ContinueStatement(Location loc, Identifier label)
            : base(loc)
        {
            Label = label;
        }

        public ContinueStatement()
        {
            Label = null;
        }

        public ContinueStatement(Identifier label)
        {
            Label = label;
        }

        public override StatementFlavor Flavor { get { return StatementFlavor.Continue; } }

        public override bool NeedsBraces { get { return false; } }

        public override bool NeedsSpace { get { return true; } }

        public override int Size { get { return 1; } }

        public override ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            // Give up trying to track control flow
            return ControlFlow.Top;
        }

        public override IImSeq<Expression> SubExpressions { get { return Constants.EmptyExpressions; } }

        public override Statement CloneWithSubExpressions(IImSeq<Expression> subExpressions)
        {
            if (subExpressions.Count != 0)
                throw new InvalidOperationException("mismatched sub-expressions arity");
            return this;
        }

        public override IImSeq<Statements> SubStatementss { get { return Constants.EmptyStatementss; } }

        public override Statement CloneWithSubStatementss(IImSeq<Statements> subStatements)
        {
            if (subStatements.Count != 0)
                throw new InvalidOperationException("mismatched sub-statements arity");
            return this;
        }

        public override void CollectBoundVars(IMSet<Identifier> boundVars)
        {
        }

        public override void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
        }

        public override ControlFlow Simplify(SimplifierContext ctxt, EvalTimes evalTimes, bool fallOffIsReturn)
        {
            ctxt.Add(this);
            return ControlFlow.Top;
        }

        public override ReturnResult ToReturnResult(ISeq<Statement> acc)
        {
            acc.Add(this);
            return ReturnResult.None;
        }

        public override void Append(Writer writer)
        {
            writer.Append("continue");
            if (Label != null)
            {
                writer.HardSpace();
                Label.Append(writer);
            }
            writer.Append(';');
            writer.EndLine();
        }

        public override int GetHashCode()
        {
            var res = 0x9216d5d9u;
            if (Label != null)
                res = Constants.Rot5(res) ^ (uint)Label.GetHashCode();
            return (int)res;
        }

        protected override bool EqualBody(Statement other)
        {
            var cs = (ContinueStatement)other;
            if ((Label == null) != (cs.Label == null))
                return false;
            return Label == null || Label.Equals(cs.Label);
        }

        protected override int CompareBody(Statement other)
        {
            var cs = (ContinueStatement)other;
            if (Label == null && cs.Label != null)
                return -1;
            if (Label != null && cs.Label == null)
                return 1;
            return Label == null ? 0 : Label.CompareTo(cs.Label);
        }
    }

    // ----------------------------------------------------------------------
    // DoStatement
    // ----------------------------------------------------------------------

    public class DoStatement : Statement
    {
        [NotNull]
        public readonly Statements Body;
        [NotNull]
        public readonly Expression Condition;

        public DoStatement(Location loc, Statements body, Expression condition)
            : base(loc)
        {
            Body = body;
            Condition = condition;
        }

        public DoStatement(Statements body, Expression condition)
        {
            Body = body;
            Condition = condition;
        }

        public override StatementFlavor Flavor { get { return StatementFlavor.Do; } }

        public override bool NeedsBraces { get { return false; } }

        public override bool NeedsSpace { get { return true; } }

        public override int Size { get { return 1 + Body.Size + Condition.Size; } }

        public override ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            if (callCtxt != null)
                evalTimes = evalTimes.Lub(EvalTimes.AtLeastOnce);
            // Given up trying to track effects
            fxCtxt.IncludeEffects(Effects.Top);
            var bodyCf = Body.AccumBlockEffects(fxCtxt, callCtxt, evalTimes);
            Condition.AccumEffects(fxCtxt, callCtxt, EvalTimes.Top);
            return bodyCf;
        }

        public override IImSeq<Expression> SubExpressions { get { return new Seq<Expression> { Condition }; } }

        public override Statement CloneWithSubExpressions(IImSeq<Expression> subExpressions)
        {
            if (subExpressions.Count != 1)
                throw new InvalidOperationException("mismatched sub-expressions arity");
            return new DoStatement(Loc, Body, subExpressions[0]);
        }

        public override IImSeq<Statements> SubStatementss { get { return new Seq<Statements> { Body }; } }

        public override Statement CloneWithSubStatementss(IImSeq<Statements> subStatementss)
        {
            if (subStatementss.Count != 1)
                throw new InvalidOperationException("mismatched sub-statements arity");
            return new DoStatement(Loc, subStatementss[0], Condition);
        }

        public override void CollectBoundVars(IMSet<Identifier> boundVars)
        {
            Body.CollectBoundVars(boundVars);
            Condition.CollectBoundVars(boundVars);
        }

        public override void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
            Body.CollectFreeVars(boundVars, freeVars);
            Condition.CollectFreeVars(boundVars, freeVars);
        }

        public override ControlFlow Simplify(SimplifierContext ctxt, EvalTimes evalTimes, bool fallOffIsReturn)
        {
            evalTimes = evalTimes.Lub(EvalTimes.AtLeastOnce);
            var bodyCtxt = ctxt.InFreshStatements();
            var bodyCf = Body.Simplify(bodyCtxt, evalTimes, false);
            // NOTE: Not safe to hoist parts of condition into tail of body's statements since a
            //       'continue' in the body may skip directly to condition.
            var condCtxt = ctxt.InNoStatements();
            var simpCondition = Condition.SimplifyValue(condCtxt, EvalTimes.Top);
            ctxt.Add(new DoStatement(Loc, new Statements(bodyCtxt.Statements), simpCondition));
            return bodyCf;
        }

        public override ReturnResult ToReturnResult(ISeq<Statement> acc)
        {
            var body = new Seq<Statement>();
            if (Body.ToReturnResult(body).NotNone)
                return ReturnResult.Fail;
            acc.Add(new DoStatement(Loc, new Statements(body), Condition));
            return ReturnResult.None;
        }

        public override void Append(Writer writer)
        {
            writer.Append("do");
            writer.EndLine();
            var needsSpace = Body.NeedsSpace;
            if (!writer.PrettyPrint && needsSpace)
                writer.HardSpace();
            Body.AppendStatementOrBlock(writer);
            if (!writer.PrettyPrint && needsSpace)
                writer.HardSpace();
            writer.Append("while");
            writer.Space();
            writer.Append('(');
            Condition.Append(writer);
            writer.Append(')');
            writer.Append(';');
            writer.EndLine();
        }

        public override int GetHashCode()
        {
            var res = 0xd01adfb7u;
            res ^= (uint)Body.GetHashCode();
            res = Constants.Rot5(res) ^ (uint)Condition.GetHashCode();
            return (int)res;
        }

        protected override bool EqualBody(Statement other)
        {
            var ds = (DoStatement)other;
            return Body.Equals(ds.Body) && Condition.Equals(ds.Condition);
        }

        protected override int CompareBody(Statement other)
        {
            var ds = (DoStatement)other;
            var i = Body.CompareTo(ds.Body);
            if (i != 0)
                return i;
            return Condition.CompareTo(ds.Condition);
        }
    }

    // ----------------------------------------------------------------------
    // ExpressionStatement
    // ----------------------------------------------------------------------

    public class ExpressionStatement : Statement
    {
        [NotNull]
        public readonly Expression Expression;

        public ExpressionStatement(Location loc, Expression expression)
            : base(loc)
        {
            Expression = expression;
        }

        public ExpressionStatement(Expression expression)
        {
            Expression = expression;
        }

        public override StatementFlavor Flavor { get { return StatementFlavor.Expression; } }

        public override bool NeedsBraces { get { return false; } }

        public override bool NeedsSpace { get { return true; } }

        public override int Size { get { return Expression.Size; } }

        public override ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            Expression.AccumEffects(fxCtxt, callCtxt, evalTimes);
            return ControlFlow.AlwaysReturn;
        }

        public override IImSeq<Expression> SubExpressions { get { return new Seq<Expression> { Expression }; } }

        public override Statement CloneWithSubExpressions(IImSeq<Expression> subExpressions)
        {
            if (subExpressions.Count != 1)
                throw new InvalidOperationException("mismatched sub-expressions arity");
            return new ExpressionStatement(Loc, subExpressions[0]);
        }

        public override IImSeq<Statements> SubStatementss { get { return Constants.EmptyStatementss; } }

        public override Statement CloneWithSubStatementss(IImSeq<Statements> subStatements)
        {
            if (subStatements.Count != 0)
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

        public override ControlFlow Simplify(SimplifierContext ctxt, EvalTimes evalTimes, bool fallOffIsReturn)
        {
            var locCtxt = ctxt.InLocalEffects();
            var simpExpression = Expression.Simplify(locCtxt, evalTimes);
            if (simpExpression != null)
                ctxt.Add(new ExpressionStatement(Loc, simpExpression));
            return ControlFlow.AlwaysReturn;
        }

        public override ReturnResult ToReturnResult(ISeq<Statement> acc)
        {
            // Expression may itself be a StatementsPseduoExpression, however if it has a Value then
            // it represents an expression to be evaluated for its side effects, not a return. Thus
            // don't need to do anything special.
            acc.Add(this);
            return ReturnResult.None;
        }

        public override void Append(Writer writer)
        {
            Expression.Append(writer);
            writer.Append(';');
            writer.EndLine();
        }

        public override int GetHashCode()
        {
            return (int)(0xba7c9045u ^ (uint)Expression.GetHashCode());
        }

        protected override bool EqualBody(Statement other)
        {
            var es = (ExpressionStatement)other;
            return Expression.Equals(es.Expression);
        }

        protected override int CompareBody(Statement other)
        {
            var es = (ExpressionStatement)other;
            return Expression.CompareTo(es.Expression);
        }
    }

    // ----------------------------------------------------------------------
    // ForStatement
    // ----------------------------------------------------------------------

    public enum LoopFlavor
    {
        ForEach,
        ForEachVar,
        For,
        ForVar
    }

    public abstract class LoopClause : IEquatable<LoopClause>, IComparable<LoopClause>
    {
        [CanBeNull] // null => no location known
        public readonly Location Loc;

        protected LoopClause(Location loc)
        {
            Loc = loc;
        }

        protected LoopClause()
        {
        }

        public override bool Equals(object obj)
        {
            var other = obj as LoopClause;
            return other != null && Equals(other);
        }

        public override int GetHashCode()
        {
            throw new InvalidOperationException("must be overridden");
        }

        public bool Equals(LoopClause other)
        {
            return Flavor == other.Flavor && EqualBody(other);
        }

        protected abstract bool EqualBody(LoopClause other);

        public int CompareTo(LoopClause other)
        {
            var i = Flavor.CompareTo(other.Flavor);
            if (i != 0)
                return i;
            return CompareBody(other);
        }

        protected abstract int CompareBody(LoopClause other);

        public abstract LoopFlavor Flavor { get; }

        public abstract int Size { get; }

        public abstract void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes);

        public abstract IImSeq<Expression> SubExpressions { get; }

        public abstract LoopClause CloneWithSubExpressions(IImSeq<Expression> subExpressions);

        public abstract void CollectBoundVars(IMSet<Identifier> boundVars);

        public abstract void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars);

        public abstract LoopClause Simplify(SimplifierContext ctxt, EvalTimes evalTimes);

        public abstract void Append(Writer writer);
    }

    public class ForEachLoopClause : LoopClause
    {
        [NotNull]
        public readonly Expression IterationVariable;
        [NotNull]
        public readonly Expression Collection;

        public ForEachLoopClause(Location loc, Expression iterationVariable, Expression collection)
            : base(loc)
        {
            IterationVariable = iterationVariable;
            Collection = collection;
        }

        public ForEachLoopClause(Expression iterationVariable, Expression collection)
        {
            IterationVariable = iterationVariable;
            Collection = collection;
        }

        public override LoopFlavor Flavor { get { return LoopFlavor.ForEach; } }

        public override int Size { get { return IterationVariable.Size + Collection.Size; } }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            IterationVariable.AccumEffects(fxCtxt, callCtxt, evalTimes);
            Collection.AccumEffects(fxCtxt, callCtxt, evalTimes);
        }

        public override IImSeq<Expression> SubExpressions { get { return new Seq<Expression> { IterationVariable, Collection }; } }

        public override LoopClause CloneWithSubExpressions(IImSeq<Expression> subExpressions)
        {
            if (subExpressions.Count != 2)
                throw new InvalidOperationException("mismatched sub-expressions arity");
            return new ForEachLoopClause(Loc, subExpressions[0], subExpressions[1]);
        }

        public override void CollectBoundVars(IMSet<Identifier> boundVars)
        {
            IterationVariable.CollectBoundVars(boundVars);
            Collection.CollectBoundVars(boundVars);
        }

        public override void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
            IterationVariable.CollectFreeVars(boundVars, freeVars);
            Collection.CollectFreeVars(boundVars, freeVars);
        }

        public override LoopClause Simplify(SimplifierContext ctxt, EvalTimes evalTimes)
        {
            var locCtxt = ctxt.InLocalEffects();
            var simpIterVar = IterationVariable.Simplify(locCtxt, evalTimes);
            var simpCollection = Collection.Simplify(locCtxt, evalTimes);
            return new ForEachLoopClause(Loc, simpIterVar, simpCollection);
        }

        public override void Append(Writer writer)
        {
            IterationVariable.Append(writer, Precedence.LHS);
            writer.HardSpace();
            writer.Append("in");
            writer.HardSpace();
            Collection.Append(writer);
        }

        public override int GetHashCode()
        {
            var res = 0x24a19947u;
            res ^= (uint)IterationVariable.GetHashCode();
            res = Constants.Rot7(res) ^ (uint)Collection.GetHashCode();
            return (int)res;
        }

        protected override bool EqualBody(LoopClause other)
        {
            var felp = (ForEachLoopClause)other;
            return IterationVariable.Equals(felp.IterationVariable) && Collection.Equals(felp.Collection);
        }

        protected override int CompareBody(LoopClause other)
        {
            var felp = (ForEachLoopClause)other;
            var i = IterationVariable.CompareTo(felp.IterationVariable);
            if (i != 0)
                return i;
            return Collection.CompareTo(felp.Collection);
        }
    }

    public class ForEachVarLoopClause : LoopClause
    {
        [NotNull]
        public readonly VariableDeclaration IterationVariable;
        [NotNull]
        public readonly Expression Collection;

        public ForEachVarLoopClause(Location loc, VariableDeclaration iterationVariable, Expression collection)
            : base(loc)
        {
            IterationVariable = iterationVariable;
            Collection = collection;
        }

        public ForEachVarLoopClause(VariableDeclaration iterationVariable, Expression collection)
        {
            IterationVariable = iterationVariable;
            Collection = collection;
        }

        public override LoopFlavor Flavor { get { return LoopFlavor.ForEachVar; } }

        public override int Size { get { return IterationVariable.Size + Collection.Size; } }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            IterationVariable.AccumEffects(fxCtxt, callCtxt, evalTimes);
            Collection.AccumEffects(fxCtxt, callCtxt, evalTimes);
        }

        public override IImSeq<Expression> SubExpressions
        {
            get
            {
                var res = new Seq<Expression>();
                IterationVariable.AppendSubExpressions(res);
                res.Add(Collection);
                return res;
            }
        }

        public override LoopClause CloneWithSubExpressions(IImSeq<Expression> subExpressions)
        {
            var i = 0;
            var itervar = IterationVariable.CloneWithSubExpressions(subExpressions, ref i);
            if (i >= subExpressions.Count)
                throw new InvalidOperationException("mismatched sub-expressions arity");
            var col = subExpressions[i++];
            if (i != subExpressions.Count)
                throw new InvalidOperationException("mismatched sub-expressions arity");
            return new ForEachVarLoopClause(Loc, itervar, col);
        }

        public override void CollectBoundVars(IMSet<Identifier> boundVars)
        {
            IterationVariable.CollectBoundVars(boundVars);
            Collection.CollectBoundVars(boundVars);
        }

        public override void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
            IterationVariable.CollectFreeVars(boundVars, freeVars);
            Collection.CollectFreeVars(boundVars, freeVars);
        }

        public override LoopClause Simplify(SimplifierContext ctxt, EvalTimes evalTimes)
        {
            var locCtxt = ctxt.InLocalEffects();
            var simpIterVar = IterationVariable.Simplify(locCtxt, evalTimes);
            var simpCollection = Collection.Simplify(locCtxt, evalTimes);
            return new ForEachVarLoopClause(Loc, simpIterVar, simpCollection);
        }

        public override void Append(Writer writer)
        {
            writer.Append("var");
            writer.HardSpace();
            IterationVariable.Append(writer);
            writer.HardSpace();
            writer.Append("in");
            writer.HardSpace();
            Collection.Append(writer);
        }

        public override int GetHashCode()
        {
            var res = 0xb3916cf7u;
            res ^= (uint)IterationVariable.GetHashCode();
            res = Constants.Rot7(res) ^ (uint)Collection.GetHashCode();
            return (int)res;
        }

        protected override bool EqualBody(LoopClause other)
        {
            var fevlp = (ForEachVarLoopClause)other;
            return IterationVariable.Equals(fevlp.IterationVariable) && Collection.Equals(fevlp.Collection);
        }

        protected override int CompareBody(LoopClause other)
        {
            var fevlp = (ForEachVarLoopClause)other;
            var i = IterationVariable.CompareTo(fevlp.IterationVariable);
            if (i != 0)
                return i;
            return Collection.CompareTo(fevlp.Collection);
        }
    }

    public class ForLoopClause : LoopClause
    {
        [CanBeNull] // null => no initializer clause
        public readonly Expression Initializer;
        [CanBeNull] // null => no condition clause
        public readonly Expression Condition;
        [CanBeNull] // null => no increment clause
        public readonly Expression Increment;

        public ForLoopClause(Location loc, Expression initializer, Expression condition, Expression increment)
            : base(loc)
        {
            Initializer = initializer;
            Condition = condition;
            Increment = increment;
        }

        public ForLoopClause(Expression initializer, Expression condition, Expression increment)
        {
            Initializer = initializer;
            Condition = condition;
            Increment = increment;
        }

        public override LoopFlavor Flavor { get { return LoopFlavor.For; } }

        public override int Size
        {
            get
            {
                return (Initializer == null ? 0 : Initializer.Size) + (Condition == null ? 0 : Condition.Size) +
                       (Increment == null ? 0 : Increment.Size);
            }
        }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            if (Initializer != null)
                Initializer.AccumEffects(fxCtxt, callCtxt, evalTimes);
            if (Condition != null)
                Condition.AccumEffects(fxCtxt, callCtxt, EvalTimes.Top);
            if (Increment != null)
                Increment.AccumEffects(fxCtxt, callCtxt, EvalTimes.Top);
        }

        public override IImSeq<Expression> SubExpressions
        {
            get
            {
                var res = new Seq<Expression>();
                if (Initializer != null)
                    res.Add(Initializer);
                if (Condition != null)
                    res.Add(Condition);
                if (Increment != null)
                    res.Add(Increment);
                return res;
            }
        }

        public override LoopClause CloneWithSubExpressions(IImSeq<Expression> subExpressions)
        {
            var i = 0;
            var init = default(Expression);
            if (Initializer != null)
            {
                if (i >= subExpressions.Count)
                    throw new InvalidOperationException("mismatched sub-expression arity");
                init = subExpressions[i++];
            }
            var cond = default(Expression);
            if (Condition != null)
            {
                if (i >= subExpressions.Count)
                    throw new InvalidOperationException("mismatched sub-expression arity");
                cond = subExpressions[i++];
            }
            var inc = default(Expression);
            if (Increment != null)
            {
                if (i >= subExpressions.Count)
                    throw new InvalidOperationException("mismatched sub-expression arity");
                inc = subExpressions[i++];
            }
            if (i != subExpressions.Count)
                throw new InvalidOperationException("mismatched sub-expression arity");
            return new ForLoopClause(Loc, init, cond, inc);
        }

        public override void CollectBoundVars(IMSet<Identifier> boundVars)
        {
            if (Initializer != null)
                Initializer.CollectBoundVars(boundVars);
            if (Condition != null)
                Condition.CollectBoundVars(boundVars);
            if (Increment != null)
                Increment.CollectBoundVars(boundVars);
        }

        public override void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
            if (Initializer != null)
                Initializer.CollectFreeVars(boundVars, freeVars);
            if (Condition != null)
                Condition.CollectFreeVars(boundVars, freeVars);
            if (Increment != null)
                Increment.CollectFreeVars(boundVars, freeVars);
        }

        public override LoopClause Simplify(SimplifierContext ctxt, EvalTimes evalTimes)
        {
            var locCtxt = ctxt.InLocalEffects();
            var simpInitializer = Initializer == null ? null : Initializer.Simplify(locCtxt, evalTimes);
            var condIncrCtxt = ctxt.InNoStatements();
            var simpCondition = Condition == null ? null : Condition.Simplify(condIncrCtxt, EvalTimes.Top);
            var simpIncrement = Increment == null ? null : Increment.Simplify(condIncrCtxt, EvalTimes.Top);
            return new ForLoopClause(Loc, simpInitializer, simpCondition, simpIncrement);
        }

        public override void Append(Writer writer)
        {
            if (Initializer != null)
                Initializer.Append(writer);
            writer.Append(';');
            writer.Space();
            if (Condition != null)
                Condition.Append(writer);
            writer.Append(';');
            writer.Space();
            if (Increment != null)
                Increment.Append(writer);
        }

        public override int GetHashCode()
        {
            var res = 0x0801f2e2u;
            if (Initializer != null)
                res = Constants.Rot7(res) ^ (uint)Initializer.GetHashCode();
            if (Condition != null)
                res = Constants.Rot7(res) ^ (uint)Condition.GetHashCode();
            if (Increment != null)
                res = Constants.Rot7(res) ^ (uint)Increment.GetHashCode();
            return (int)res;
        }

        protected override bool EqualBody(LoopClause other)
        {
            var flc = (ForLoopClause)other;
            if ((Initializer == null) != (flc.Initializer == null))
                return false;
            if (Initializer != null && !Initializer.Equals(flc.Initializer))
                return false;
            if ((Condition == null) != (flc.Condition == null))
                return false;
            if (Condition != null && !Condition.Equals(flc.Condition))
                return false;
            if ((Increment == null) != (flc.Increment == null))
                return false;
            if (Increment != null && !Increment.Equals(flc.Increment))
                return false;
            return true;
        }

        protected override int CompareBody(LoopClause other)
        {
            var flc = (ForLoopClause)other;
            if (Initializer == null && flc.Initializer != null)
                return -1;
            if (Initializer != null && flc.Initializer == null)
                return 1;
            var i = Initializer == null ? 0 : Initializer.CompareTo(flc.Initializer);
            if (i != 0)
                return i;
            if (Condition == null && flc.Condition != null)
                return -1;
            if (Condition != null && flc.Condition == null)
                return 1;
            i = Condition == null ? 0 : Condition.CompareTo(flc.Condition);
            if (i != 0)
                return i;
            if (Increment == null && flc.Increment != null)
                return -1;
            if (Increment != null && flc.Increment == null)
                return 1;
            return Increment == null ? 0 : Increment.CompareTo(flc.Increment);
        }
    }

    public class ForVarLoopClause : LoopClause
    {
        [NotNull]
        public readonly IImSeq<VariableDeclaration> IterationVariables;
        [CanBeNull] // null => no condition clause
        public readonly Expression Condition;
        [CanBeNull] // null => no increment clause
        public readonly Expression Increment;

        public ForVarLoopClause(Location loc, IImSeq<VariableDeclaration> iterationVariables, Expression condition, Expression increment)
            : base(loc)
        {
            IterationVariables = iterationVariables ?? Constants.EmptyVariableDeclarations;
            Condition = condition;
            Increment = increment;
        }

        public ForVarLoopClause(Location loc, Identifier id, Expression condition, Expression increment)
            : base(loc)
        {
            IterationVariables = new Seq<VariableDeclaration> { new VariableDeclaration(loc, id) };
            Condition = condition;
            Increment = increment;
        }

        public ForVarLoopClause(Location loc, Identifier id, Expression initializer, Expression condition, Expression increment)
            : base(loc)
        {
            IterationVariables = new Seq<VariableDeclaration> { new VariableDeclaration(loc, id, initializer) };
            Condition = condition;
            Increment = increment;
        }

        public ForVarLoopClause(IImSeq<VariableDeclaration> iterationVariables, Expression condition, Expression increment)
        {
            IterationVariables = iterationVariables ?? Constants.EmptyVariableDeclarations;
            Condition = condition;
            Increment = increment;
        }

        public ForVarLoopClause(Identifier id, Expression condition, Expression increment)
        {
            IterationVariables = new Seq<VariableDeclaration> { new VariableDeclaration(id) };
            Condition = condition;
            Increment = increment;
        }

        public ForVarLoopClause(Identifier id, Expression initializer, Expression condition, Expression increment)
        {
            IterationVariables = new Seq<VariableDeclaration> { new VariableDeclaration(id, initializer) };
            Condition = condition;
            Increment = increment;
        }

        public override LoopFlavor Flavor { get { return LoopFlavor.ForVar; } }

        public override int Size
        {
            get
            {
                return IterationVariables.Sum(v => v.Size) + (Condition == null ? 0 : Condition.Size) +
                       (Increment == null ? 0 : Increment.Size);
            }
        }

        public override void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            foreach (var v in IterationVariables)
                v.AccumEffects(fxCtxt, callCtxt, evalTimes);
            if (Condition != null)
                Condition.AccumEffects(fxCtxt, callCtxt, EvalTimes.Top);
            if (Increment != null)
                Increment.AccumEffects(fxCtxt, callCtxt, EvalTimes.Top);
        }

        public override IImSeq<Expression> SubExpressions
        {
            get
            {
                var res = new Seq<Expression>();
                foreach (var v in IterationVariables)
                    v.AppendSubExpressions(res);
                if (Condition != null)
                    res.Add(Condition);
                if (Increment != null)
                    res.Add(Increment);
                return res;
            }
        }

        public override LoopClause CloneWithSubExpressions(IImSeq<Expression> subExpressions)
        {
            var i = 0;
            var itervars = IterationVariables.Select(d => d.CloneWithSubExpressions(subExpressions, ref i)).ToSeq();
            var cond = default(Expression);
            if (Condition != null)
            {
                if (i >= subExpressions.Count)
                    throw new InvalidOperationException("mismatched sub-expression arity");
                cond = subExpressions[i++];
            }
            var inc = default(Expression);
            if (Increment != null)
            {
                if (i >= subExpressions.Count)
                    throw new InvalidOperationException("mismatched sub-expression arity");
                inc = subExpressions[i++];
            }
            if (i != subExpressions.Count)
                throw new InvalidOperationException("mismatched sub-expression arity");
            return new ForVarLoopClause(Loc, itervars, cond, inc);
        }

        public override void CollectBoundVars(IMSet<Identifier> boundVars)
        {
            foreach (var v in IterationVariables)
                v.CollectBoundVars(boundVars);
            if (Condition != null)
                Condition.CollectBoundVars(boundVars);
            if (Increment != null)
                Increment.CollectBoundVars(boundVars);
        }

        public override void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
            foreach (var v in IterationVariables)
                v.CollectFreeVars(boundVars, freeVars);
            if (Condition != null)
                Condition.CollectFreeVars(boundVars, freeVars);
            if (Increment != null)
                Increment.CollectFreeVars(boundVars, freeVars);
        }

        public override LoopClause Simplify(SimplifierContext ctxt, EvalTimes evalTimes)
        {
            var locCtxt = ctxt.InLocalEffects();
            var simpIterVars = IterationVariables.Select(v => v.Simplify(locCtxt, evalTimes)).ToSeq();
            var condIncrCtxt = ctxt.InNoStatements();
            var simpCondition = Condition == null ? null : Condition.Simplify(condIncrCtxt, EvalTimes.Top);
            var simpIncrement = Increment == null ? null : Increment.Simplify(condIncrCtxt, EvalTimes.Top);
            return new ForVarLoopClause(Loc, simpIterVars, simpCondition, simpIncrement);
        }

        public override void Append(Writer writer)
        {
            writer.Append("var");
            writer.HardSpace();
            writer.List
                (IterationVariables,
                 null,
                 (w, d) => d.Append(w),
                 w =>
                 {
                     w.Append(',');
                     w.Space();
                 },
                 null);
            writer.Append(';');
            writer.Space();
            if (Condition != null)
                Condition.Append(writer);
            writer.Append(';');
            writer.Space();
            if (Increment != null)
                Increment.Append(writer);
        }

        public override int GetHashCode()
        {
            var res = IterationVariables.Aggregate(0x636920d8u, (h, v) => Constants.Rot5(h) ^ (uint)v.GetHashCode());
            if (Condition != null)
                res = Constants.Rot5(res) ^ (uint)Condition.GetHashCode();
            if (Increment != null)
                res = Constants.Rot5(res) ^ (uint)Increment.GetHashCode();
            return (int)res;
        }

        protected override bool EqualBody(LoopClause other)
        {
            var fvlc = (ForVarLoopClause)other;
            if (IterationVariables.Count != fvlc.IterationVariables.Count)
                return false;
            if (IterationVariables.Select((v, i) => !v.Equals(fvlc.IterationVariables[i])).Any())
                return false;
            if ((Condition == null) != (fvlc.Condition == null))
                return false;
            if (Condition != null && !Condition.Equals(fvlc.Condition))
                return false;
            if ((Increment == null) != (fvlc.Increment == null))
                return false;
            if (Increment != null && !Increment.Equals(fvlc.Increment))
                return false;
            return true;
        }

        protected override int CompareBody(LoopClause other)
        {
            var fvlc = (ForVarLoopClause)other;
            var i = IterationVariables.Count.CompareTo(fvlc.IterationVariables);
            if (i != 0)
                return i;
            for (var j = 0; j < IterationVariables.Count; j++)
            {
                i = IterationVariables[j].CompareTo(fvlc.IterationVariables[j]);
                if (i != 0)
                    return i;
            }
            if (Condition == null && fvlc.Condition != null)
                return -1;
            if (Condition != null && fvlc.Condition == null)
                return 1;
            i = Condition == null ? 0 : Condition.CompareTo(fvlc.Condition);
            if (i != 0)
                return i;
            if (Increment == null && fvlc.Increment != null)
                return -1;
            if (Increment != null && fvlc.Increment == null)
                return 1;
            return Increment == null ? 0 : Increment.CompareTo(fvlc.Increment);
        }
    }

    public class ForStatement : Statement
    {
        [NotNull]
        public readonly LoopClause LoopClause;
        [NotNull]
        public readonly Statements Body;

        public ForStatement(Location loc, LoopClause loopClause, Statements body)
            : base(loc)
        {
            LoopClause = loopClause;
            Body = body;
        }

        public ForStatement(LoopClause loopClause, Statements body)
        {
            LoopClause = loopClause;
            Body = body;
        }

        public override StatementFlavor Flavor { get { return StatementFlavor.For; } }

        public override bool NeedsBraces { get { return false; } }

        public override bool NeedsSpace { get { return true; } }

        public override int Size { get { return 1 + LoopClause.Size + Body.Size; } }

        public override ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            LoopClause.AccumEffects(fxCtxt, callCtxt, evalTimes);
            // Give up trying to track effects
            fxCtxt.IncludeEffects(Effects.Top);
            return Body.AccumBlockEffects(fxCtxt, callCtxt, EvalTimes.Top);
        }

        public override IImSeq<Expression> SubExpressions { get { return LoopClause.SubExpressions; } }

        public override Statement CloneWithSubExpressions(IImSeq<Expression> subExpressions)
        {
            return new ForStatement(Loc, LoopClause.CloneWithSubExpressions(subExpressions), Body);
        }

        public override IImSeq<Statements> SubStatementss { get { return new Seq<Statements> { Body }; } }

        public override Statement CloneWithSubStatementss(IImSeq<Statements> subStatementss)
        {
            if (subStatementss.Count != 1)
                throw new InvalidOperationException("mismatched sub-statements arity");
            return new ForStatement(Loc, LoopClause, subStatementss[0]);
        }

        public override void CollectBoundVars(IMSet<Identifier> boundVars)
        {
            LoopClause.CollectBoundVars(boundVars);
            Body.CollectBoundVars(boundVars);
        }

        public override void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
            LoopClause.CollectFreeVars(boundVars, freeVars);
            Body.CollectFreeVars(boundVars, freeVars);
        }

        public override ControlFlow Simplify(SimplifierContext ctxt, EvalTimes evalTimes, bool fallOffIsReturn)
        {
            var simpLoopClause = LoopClause.Simplify(ctxt, evalTimes);
            var bodyCtxt = ctxt.InFreshStatements();
            var bodyCf = Body.Simplify(bodyCtxt, EvalTimes.Top, false);
            ctxt.Add(new ForStatement(Loc, simpLoopClause, new Statements(bodyCtxt.Statements)));
            return bodyCf;
        }

        public override ReturnResult ToReturnResult(ISeq<Statement> acc)
        {
            var body = new Seq<Statement>();
            if (Body.ToReturnResult(body).NotNone)
                return ReturnResult.Fail;
            acc.Add(new ForStatement(Loc, LoopClause, new Statements(body)));
            return ReturnResult.None;
        }

        public override void Append(Writer writer)
        {
            writer.Append("for");
            writer.Space();
            writer.Append('(');
            LoopClause.Append(writer);
            writer.Append(')');
            writer.EndLine();
            if (!writer.PrettyPrint && Body.NeedsSpace)
                writer.HardSpace();
            Body.AppendStatementOrBlock(writer);
        }

        public override int GetHashCode()
        {
            var res = 0x858efc16u;
            res ^= (uint)LoopClause.GetHashCode();
            res = Constants.Rot7(res) ^ (uint)Body.GetHashCode();
            return (int)res;
        }

        protected override bool EqualBody(Statement other)
        {
            var fs = (ForStatement)other;
            return LoopClause.Equals(fs.LoopClause) && Body.Equals(fs.Body);
        }

        protected override int CompareBody(Statement other)
        {
            var fs = (ForStatement)other;
            var i = LoopClause.CompareTo(fs.LoopClause);
            if (i != 0)
                return i;
            return Body.CompareTo(fs.Body);
        }
    }

    // ----------------------------------------------------------------------
    // FunctionDeclaration
    // ----------------------------------------------------------------------

    public class FunctionDeclaration : Statement
    {
        [NotNull]
        public readonly Identifier Name;
        [NotNull]
        public readonly IImSeq<Identifier> Parameters;
        [NotNull]
        public readonly Statements Body;

        public FunctionDeclaration(Location loc, Identifier name, IImSeq<Identifier> parameters, Statements body)
            : base(loc)
        {
            Name = name;
            Parameters = parameters ?? Constants.EmptyIdentifiers;
            Body = body;
        }

        public FunctionDeclaration(Identifier name, IImSeq<Identifier> parameters, Statements body)
        {
            Name = name;
            Parameters = parameters ?? Constants.EmptyIdentifiers;
            Body = body;
        }

        public override StatementFlavor Flavor { get { return StatementFlavor.Function; } }

        public override bool NeedsBraces { get { return false; } }

        public override bool NeedsSpace { get { return true; } }

        public override int Size { get { return 1 + Parameters.Count + Body.Size; } }

        public override ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            if (callCtxt != null && callCtxt.IsOk)
            {
                var funcFreeVars = new Set<Identifier>();
                CollectFreeVars(new Set<Identifier>(), funcFreeVars);
                foreach (var kv in callCtxt.Parameters)
                {
                    if (funcFreeVars.Contains(kv.Key))
                        // Don't know how many times method body will be evaluated
                        callCtxt.Fail();
                }
            }
            fxCtxt.IncludeEffects(Effects.Write(Name));
            return ControlFlow.AlwaysReturn;
        }

        public override IImSeq<Expression> SubExpressions { get { return Constants.EmptyExpressions; } }

        public override Statement CloneWithSubExpressions(IImSeq<Expression> subExpressions)
        {
            if (subExpressions.Count != 0)
                throw new InvalidOperationException("mismatched sub-expressions arity");
            return this;
        }

        public override IImSeq<Statements> SubStatementss { get { return new Seq<Statements> { Body }; } }

        public override Statement CloneWithSubStatementss(IImSeq<Statements> subStatementss)
        {
            if (subStatementss.Count != 1)
                throw new InvalidOperationException("mismatched sub-statements arity");
            return new FunctionDeclaration(Loc, Name, Parameters, subStatementss[0]);
        }

        public override void CollectBoundVars(IMSet<Identifier> boundVars)
        {
            boundVars.Add(Name);
        }

        public override void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
            var subBoundVars = new Set<Identifier>();
            foreach (var id in boundVars)
                subBoundVars.Add(id);
            subBoundVars.Add(Name);
            foreach (var id in Parameters)
                subBoundVars.Add(id);

            Body.CollectBoundVars(subBoundVars);
            Body.CollectFreeVars(subBoundVars, freeVars);
        }

        public override ControlFlow Simplify(SimplifierContext ctxt, EvalTimes evalTimes, bool fallOffIsReturn)
        {
            var subCtxt = ctxt.InFreshScope();
            var simpName = ctxt.InGlobalScope ? Name : subCtxt.Freshen(Name);
            var simpParameters = Parameters.Select(subCtxt.Freshen).ToSeq();
            var boundVars = new Set<Identifier>();
            Body.CollectBoundVars(boundVars);
            foreach (var id in boundVars)
                subCtxt.Freshen(id);
            Body.Simplify(subCtxt, EvalTimes.Top, false);
            ctxt.Add(new FunctionDeclaration(Loc, simpName, simpParameters, new Statements(subCtxt.Statements)));
            return ControlFlow.AlwaysReturn;
        }

        public override ReturnResult ToReturnResult(ISeq<Statement> acc)
        {
            acc.Add(this);
            return ReturnResult.None;
        }

        public override void Append(Writer writer)
        {
            writer.Append("function");
            writer.HardSpace();
            Name.Append(writer);
            writer.List
                (Parameters,
                 w => w.Append('('),
                 (w, p) => p.Append(w),
                 w =>
                 {
                     w.Append(',');
                     w.Space();
                 },
                 w => w.Append(')'));
            writer.Space();
            Body.AppendBlock(writer);
        }

        public override int GetHashCode()
        {
            var res = 0x71574e69u;
            res ^= (uint)Name.GetHashCode();
            res = Parameters.Aggregate(res, (h, p) => Constants.Rot7(h) ^ (uint)p.GetHashCode());
            res = Constants.Rot7(res) ^ (uint)Body.GetHashCode();
            return (int)res;
        }

        protected override bool EqualBody(Statement other)
        {
            var fd = (FunctionDeclaration)other;
            if (!Name.Equals(fd.Name))
                return false;
            if (Parameters.Count != fd.Parameters.Count)
                return false;
            if (Parameters.Select((p, i) => !p.Equals(fd.Parameters[i])).Any())
                return false;
            return Body.Equals(fd.Body);
        }

        protected override int CompareBody(Statement other)
        {
            var fd = (FunctionDeclaration)other;
            var i = Name.CompareTo(fd.Name);
            if (i != 0)
                return i;
            i = Parameters.Count.CompareTo(fd.Parameters.Count);
            if (i != 0)
                return i;
            for (var j = 0; j < Parameters.Count; j++)
            {
                i = Parameters[j].CompareTo(fd.Parameters[j]);
                if (i != 0)
                    return i;
            }
            return Body.CompareTo(fd.Body);
        }
    }

    // ----------------------------------------------------------------------
    // IfStatement
    // ----------------------------------------------------------------------

    public class IfStatement : Statement
    {
        [NotNull]
        public readonly Expression Condition;
        [NotNull]
        public readonly Statements Then;
        [CanBeNull] // null => if-then only
        public readonly Statements Else;

        public IfStatement(Location loc, Expression condition, Statements thenStatements)
            : base(loc)
        {
            Condition = condition;
            Then = thenStatements;
        }

        public IfStatement(Location loc, Expression condition, Statements thenStatements, Statements elseStatements)
            : base(loc)
        {
            Condition = condition;
            Then = thenStatements;
            Else = elseStatements;
        }

        public IfStatement(Expression condition, Statements thenStatements)
        {
            Condition = condition;
            Then = thenStatements;
        }

        public IfStatement(Expression condition, Statements thenStatements, Statements elseStatements)
        {
            Condition = condition;
            Then = thenStatements;
            Else = elseStatements;
        }

        public override StatementFlavor Flavor { get { return StatementFlavor.If; } }

        public override bool NeedsBraces { get { return Else == null; } }

        public override bool NeedsSpace { get { return true; } }

        public override int Size
        {
            get
            {
                return 1 + Condition.Size + Then.Size + (Else == null ? 0 : Else.Size);
            }
        }

        public override ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            Condition.AccumEffects(fxCtxt, callCtxt, evalTimes);
            if (callCtxt != null)
                evalTimes = evalTimes.Lub(EvalTimes.Opt);
            if (Else == null)
            {
                var thenCf = Then.AccumBlockEffects(fxCtxt, callCtxt, evalTimes);
                return thenCf.Lub(ControlFlow.AlwaysReturn);
            }
            else
            {
                var thenCtxt = fxCtxt.Fork();
                var thenCf = Then.AccumBlockEffects(thenCtxt, callCtxt, evalTimes);
                var elseCtxt = fxCtxt.Fork();
                var elseCf = Else.AccumBlockEffects(elseCtxt, callCtxt, evalTimes);
                fxCtxt.IncludeEffects(thenCtxt.AccumEffects);
                fxCtxt.IncludeEffects(elseCtxt.AccumEffects);
                return thenCf.Lub(elseCf);
            }
        }

        public override IImSeq<Expression> SubExpressions { get { return new Seq<Expression> { Condition }; } }

        public override Statement CloneWithSubExpressions(IImSeq<Expression> subExpressions)
        {
            if (subExpressions.Count != 1)
                throw new InvalidOperationException("mismatched sub-expressions arity");
            return new IfStatement(Loc, subExpressions[0], Then, Else);
        }

        public override IImSeq<Statements> SubStatementss
        {
            get
            {
                if (Else == null)
                    return new Seq<Statements> { Then };
                else
                    return new Seq<Statements> { Then, Else };
            }
        }

        public override Statement CloneWithSubStatementss(IImSeq<Statements> subStatementss)
        {
            if (subStatementss.Count < 1 || subStatementss.Count > 2)
                throw new InvalidOperationException("mismatched sub-statements arity");
            return new IfStatement(Loc, Condition, subStatementss[0], subStatementss.Count == 2 ? subStatementss[1] : null);
        }

        public override void CollectBoundVars(IMSet<Identifier> boundVars)
        {
            Condition.CollectBoundVars(boundVars);
            Then.CollectBoundVars(boundVars);
            if (Else != null)
                Else.CollectBoundVars(boundVars);
        }

        public override void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
            Condition.CollectFreeVars(boundVars, freeVars);
            Then.CollectFreeVars(boundVars, freeVars);
            if (Else != null)
                Else.CollectFreeVars(boundVars, freeVars);
        }

        public override ControlFlow Simplify(SimplifierContext ctxt, EvalTimes evalTimes, bool fallOffIsReturn)
        {
            var locCtxt = ctxt.InLocalEffects();
            var simpCondition = Condition.SimplifyValue(locCtxt, evalTimes);
            var b = simpCondition.IsBoolean;
            if (b.HasValue)
            {
                if (b.Value)
                    return Then.Simplify(ctxt, evalTimes, fallOffIsReturn);
                else if (Else != null)
                    return Else.Simplify(ctxt, evalTimes, fallOffIsReturn);
                else
                    return ControlFlow.AlwaysReturn;
            }
            else
            {
                var thenCtxt = ctxt.InFreshStatements();
                evalTimes = evalTimes.Lub(EvalTimes.Opt);
                var thenCf = Then.Simplify(thenCtxt, evalTimes, fallOffIsReturn);
                if (Else == null)
                {
                    ctxt.Add(new IfStatement(Loc, simpCondition, new Statements(thenCtxt.Statements)));
                    return thenCf.Lub(ControlFlow.AlwaysReturn);
                }
                else
                {
                    var elseCtxt = ctxt.InFreshStatements();
                    var elseCf = Else.Simplify(elseCtxt, evalTimes, fallOffIsReturn);
                    ctxt.Add
                        (new IfStatement
                             (Loc,
                              simpCondition,
                              new Statements(thenCtxt.Statements),
                              new Statements(elseCtxt.Statements)));
                    return thenCf.Lub(elseCf);
                }
            }
        }

        public override ReturnResult ToReturnResult(ISeq<Statement> acc)
        {
            var thenAcc = new Seq<Statement>();
            if (Then.ToReturnResult(thenAcc).NotNone)
                return ReturnResult.Fail;
            var elseAcc = default(Seq<Statement>);
            if (Else != null)
            {
                elseAcc = new Seq<Statement>();
                if (Else.ToReturnResult(elseAcc).NotNone)
                    return ReturnResult.Fail;
            }
            acc.Add
                (new IfStatement
                     (Loc, Condition, new Statements(thenAcc), elseAcc == null ? null : new Statements(elseAcc)));
            return ReturnResult.None;
        }

        public override void Append(Writer writer)
        {
            writer.Append("if");
            writer.Space();
            writer.Append('(');
            Condition.Append(writer);
            writer.Append(')');
            writer.EndLine();
            Then.AppendStatementOrBlock(writer);
            if (Else != null)
            {
                if (!writer.PrettyPrint && Then.NeedsSpace)
                    writer.HardSpace();
                writer.Append("else");
                writer.EndLine();
                if (!writer.PrettyPrint && Else.NeedsSpace)
                    writer.HardSpace();
                Else.AppendStatementOrBlock(writer);
            }
        }

        public override int GetHashCode()
        {
            var res = 0x82154aeeu;
            res ^= (uint)Condition.GetHashCode();
            res = Constants.Rot7(res) ^ (uint)Then.GetHashCode();
            if (Else != null)
                res = Constants.Rot7(res) ^ (uint)Else.GetHashCode();
            return (int)res;
        }

        protected override bool EqualBody(Statement other)
        {
            var ifs = (IfStatement)other;
            if ((Else == null) != (ifs.Else == null))
                return false;
            return Condition.Equals(ifs.Condition) && Then.Equals(ifs.Then) && (Else == null || Else.Equals(ifs.Else));
        }

        protected override int CompareBody(Statement other)
        {
            var ifs = (IfStatement)other;
            var i = Condition.CompareTo(ifs.Condition);
            if (i != 0)
                return i;
            i = Then.CompareTo(ifs.Then);
            if (i != 0)
                return i;
            if (Else == null && ifs.Else != null)
                return -1;
            if (Else != null && ifs.Else == null)
                return 1;
            return Else == null ? 0 : Else.CompareTo(ifs.Else);
        }
    }

    // ----------------------------------------------------------------------
    // LabelledStatement
    // ----------------------------------------------------------------------

    public class LabelledStatement : Statement
    {
        [NotNull]
        public readonly Identifier Label;
        [NotNull]
        public readonly Statements Body;

        public LabelledStatement(Location loc, Identifier label, Statements body)
            : base(loc)
        {
            Label = label;
            Body = body;
        }

        public LabelledStatement(Identifier label, Statements body)
        {
            Label = label;
            Body = body;
        }

        public override StatementFlavor Flavor { get { return StatementFlavor.Labelled; } }

        public override bool NeedsBraces { get { return false; } }

        public override bool NeedsSpace { get { return true; } }

        public override int Size { get { return 1 + Body.Size; } }

        public override ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            // Give up trying to track control flow and effects
            fxCtxt.IncludeEffects(Effects.Top);
            Body.AccumBlockEffects(fxCtxt, callCtxt, EvalTimes.Top);
            return ControlFlow.Top;
        }

        public override IImSeq<Expression> SubExpressions { get { return Constants.EmptyExpressions; } }

        public override Statement CloneWithSubExpressions(IImSeq<Expression> subExpressions)
        {
            if (subExpressions.Count != 0)
                throw new InvalidOperationException("mismatched sub-expressions arity");
            return this;
        }

        public override IImSeq<Statements> SubStatementss { get { return new Seq<Statements> { Body }; } }

        public override Statement CloneWithSubStatementss(IImSeq<Statements> subStatementss)
        {
            if (subStatementss.Count != 1)
                throw new InvalidOperationException("mismatched sub-statements arity");
            return new LabelledStatement(Loc, Label, subStatementss[0]);
        }

        public override void CollectBoundVars(IMSet<Identifier> boundVars)
        {
            Body.CollectBoundVars(boundVars);
        }

        public override void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
            Body.CollectFreeVars(boundVars, freeVars);
        }

        public override ControlFlow Simplify(SimplifierContext ctxt, EvalTimes evalTimes, bool fallOffIsReturn)
        {
            var labCtxt = ctxt.InFreshStatements();
            Body.Simplify(labCtxt, EvalTimes.Top, false);
            ctxt.Add(new LabelledStatement(Loc, Label, new Statements(labCtxt.Statements)));
            return ControlFlow.Top;
        }

        public override ReturnResult ToReturnResult(ISeq<Statement> acc)
        {
            var body = new Seq<Statement>();
            if (Body.ToReturnResult(body).NotNone)
                return ReturnResult.Fail;
            acc.Add(new LabelledStatement(Loc, Label, new Statements(body)));
            return ReturnResult.None;
        }

        public override void Append(Writer writer)
        {
            Label.Append(writer);
            writer.Append(':');
            writer.Space();
            Body.AppendStatementOrBlock(writer);
        }

        public override int GetHashCode()
        {
            var res = 0x9c30d539u;
            res ^= (uint)Label.GetHashCode();
            res = Constants.Rot5(res) ^ (uint)Body.GetHashCode();
            return (int)res;
        }

        protected override bool EqualBody(Statement other)
        {
            var ls = (LabelledStatement)other;
            return Label.Equals(ls.Label) && Body.Equals(ls.Body);
        }

        protected override int CompareBody(Statement other)
        {
            var ls = (LabelledStatement)other;
            var i = Label.CompareTo(ls.Label);
            if (i != 0)
                return i;
            return Body.CompareTo(ls.Body);
        }
    }

    // ----------------------------------------------------------------------
    // ReturnStatement
    // ----------------------------------------------------------------------

    public class ReturnStatement : Statement
    {
        [CanBeNull] // null => no return value
        public readonly Expression Value;

        public ReturnStatement(Location loc)
            : base(loc)
        {
            Value = null;
        }

        public ReturnStatement(Location loc, Expression value)
            : base(loc)
        {
            Value = value;
        }

        public ReturnStatement()
        {
            Value = null;
        }

        public ReturnStatement(Expression value)
        {
            Value = value;
        }

        public override StatementFlavor Flavor { get { return StatementFlavor.Return; } }

        public override bool NeedsBraces { get { return false; } }

        public override bool NeedsSpace { get { return true; } }

        public override int Size { get { return 1 + (Value == null ? 0 : Value.Size); } }

        public override ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            if (Value != null)
                Value.AccumEffects(fxCtxt, callCtxt, evalTimes);
            // Return never returns...
            return ControlFlow.NeverReturn;
        }

        public override IImSeq<Expression> SubExpressions
        {
            get
            {
                if (Value == null)
                    return Constants.EmptyExpressions;
                else
                    return new Seq<Expression> { Value };
            }
        }

        public override Statement CloneWithSubExpressions(IImSeq<Expression> subExpressions)
        {
            if (Value == null)
            {
                if (subExpressions.Count != 0)
                    throw new InvalidOperationException("mismatched sub-expressions arity");
                return this;
            }
            else
            {
                if (subExpressions.Count != 1)
                    throw new InvalidOperationException("mismatched sub-expressions arity");
                return new ReturnStatement(Loc, subExpressions[0]);
            }
        }

        public override IImSeq<Statements> SubStatementss { get { return Constants.EmptyStatementss; } }

        public override Statement CloneWithSubStatementss(IImSeq<Statements> subStatementss)
        {
            if (subStatementss.Count != 0)
                throw new InvalidOperationException("mismatched sub-statements arity");
            return this;
        }

        public override void CollectBoundVars(IMSet<Identifier> boundVars)
        {
            if (Value != null)
                Value.CollectBoundVars(boundVars);
        }

        public override void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
            if (Value != null)
                Value.CollectFreeVars(boundVars, freeVars);
        }

        public override ControlFlow Simplify(SimplifierContext ctxt, EvalTimes evalTimes, bool fallOffIsReturn)
        {
            if (Value == null)
            {
                if (!fallOffIsReturn)
                    ctxt.Add(this);
            }
            else
            {
                var locCtxt = ctxt.InLocalEffects();
                var simpValue = Value.Simplify(locCtxt, evalTimes);
                ctxt.Add(new ReturnStatement(Loc, simpValue));
            }
            return ControlFlow.NeverReturn;
        }

        public override ReturnResult ToReturnResult(ISeq<Statement> acc)
        {
            return new ReturnResult(Value);
        }

        public override void Append(Writer writer)
        {
            writer.Append("return");
            if (Value != null)
            {
                writer.HardSpace();
                Value.Append(writer);
            }
            writer.Append(';');
            writer.EndLine();
        }

        public override int GetHashCode()
        {
            return (int)(0x1141e8ceu ^ (uint)Value.GetHashCode());
        }

        protected override bool EqualBody(Statement other)
        {
            var rs = (ReturnStatement)other;
            return Value.Equals(rs.Value);
        }

        protected override int CompareBody(Statement other)
        {
            var rs = (ReturnStatement)other;
            return Value.CompareTo(rs.Value);
        }
    }

    // ----------------------------------------------------------------------
    // SwitchStatement
    // ----------------------------------------------------------------------

    public class CaseClause : IEquatable<CaseClause>, IComparable<CaseClause>
    {
        [CanBeNull] // null => no location known
        public readonly Location Loc;
        [NotNull]
        public readonly Expression Value;
        [NotNull]
        public readonly Statements Body;

        public CaseClause(Location loc, Expression value, Statements body)
        {
            Loc = loc;
            Value = value;
            Body = body;
        }

        public CaseClause(Expression value, Statements body)
        {
            Value = value;
            Body = body;
        }

        public int Size { get { return 1 + Value.Size + Body.Size; } }

        public void AccumValueEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            Value.AccumEffects(fxCtxt, callCtxt, evalTimes);
        }

        public ControlFlow AccumBodyEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            if (callCtxt != null)
                evalTimes = evalTimes.Lub(EvalTimes.Opt);
            return Body.AccumBlockEffects(fxCtxt, callCtxt, evalTimes);
        }

        public void AppendSubExpressions(ISeq<Expression> subExpression)
        {
            subExpression.Add(Value);
        }

        public void AppendSubStatementss(ISeq<Statements> subStatementss)
        {
            subStatementss.Add(Body);
        }

        public CaseClause CloneWithSubExpressions(IImSeq<Expression> subExpressions, ref int i)
        {
            if (i >= subExpressions.Count)
                throw new InvalidOperationException("mismatched sub-statements arity");
            return new CaseClause(Loc, subExpressions[i++], Body);
        }

        public CaseClause CloneWithSubStatementss(IImSeq<Statements> subStatementss, ref int i)
        {
            if (i >= subStatementss.Count)
                throw new InvalidOperationException("mismatched sub-statements arity");
            return new CaseClause(Loc, Value, subStatementss[i++]);
        }

        public void CollectBoundVars(IMSet<Identifier> boundVars)
        {
            Value.CollectBoundVars(boundVars);
            Body.CollectBoundVars(boundVars);
        }

        public void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
            Value.CollectFreeVars(boundVars, freeVars);
            Body.CollectFreeVars(boundVars, freeVars);
        }

        public CaseClause Simplify(SimplifierContext ctxt, EvalTimes evalTimes, bool fallOffIsReturn, ref ControlFlow cf)
        {
            var locCtxt = ctxt.InLocalEffects();
            var simpValue = Value.Simplify(locCtxt, evalTimes);
            var caseCtxt = ctxt.InFreshStatements();
            evalTimes = evalTimes.Lub(EvalTimes.Opt);
            // Can't remove trailing return since still need to break out of case
            cf = cf.Lub(Body.Simplify(caseCtxt, evalTimes, false));
            return new CaseClause(Loc, simpValue, new Statements(caseCtxt.Statements));
        }

        public CaseClause ToReturnResultCaseClause()
        {
            var body = new Seq<Statement>();
            if (Body.ToReturnResult(body).NotNone)
                return null;
            return new CaseClause(Loc, Value, new Statements(body));
        }

        public void Append(Writer writer)
        {
            writer.Append("case");
            writer.HardSpace();
            Value.Append(writer);
            writer.Append(':');
            writer.EndLine();
            writer.Indented(null, w => Body.Append(w), null);
        }

        public override bool Equals(object obj)
        {
            var other = obj as CaseClause;
            return other != null && Equals(other);
        }

        public override int GetHashCode()
        {
            var res = 0x34e90c6cu;
            res ^= (uint)Value.GetHashCode();
            res = Constants.Rot5(res) ^ (uint)Body.GetHashCode();
            return (int)res;
        }

        public bool Equals(CaseClause other)
        {
            return Value.Equals(other.Value) && Body.Equals(other.Body);
        }

        public int CompareTo(CaseClause other)
        {
            var i = Value.CompareTo(other.Value);
            if (i != 0)
                return i;
            return Body.CompareTo(other.Body);
        }
    }

    public class DefaultClause : IEquatable<DefaultClause>, IComparable<DefaultClause>
    {
        [CanBeNull] // null => no location known
        public readonly Location Loc;
        [NotNull]
        public readonly Statements Body;
        public readonly int Index; // index of this clause in containing switch's cases 

        public DefaultClause(Location loc, Statements body, int index)
        {
            Loc = loc;
            Body = body;
            Index = index;
        }

        public DefaultClause(Statements body, int index)
        {
            Body = body;
            Index = index;
        }

        public int Size { get { return 1 + Body.Size; } }

        public ControlFlow AccumBodyEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            if (callCtxt != null)
                evalTimes = evalTimes.Lub(EvalTimes.Opt);
            return Body.AccumBlockEffects(fxCtxt, callCtxt, evalTimes);
        }

        public void AppendSubStatementss(ISeq<Statements> subStatementss)
        {
            subStatementss.Add(Body);
        }

        public DefaultClause CloneWithSubStatementss(IImSeq<Statements> subStatementss, ref int i)
        {
            if (i >= subStatementss.Count)
                throw new InvalidOperationException("mismatched sub-statements arity");
            return new DefaultClause(Loc, subStatementss[i++], Index);
        }

        public void CollectBoundVars(IMSet<Identifier> boundVars)
        {
            Body.CollectBoundVars(boundVars);
        }

        public void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
            Body.CollectFreeVars(boundVars, freeVars);
        }

        public DefaultClause Simplify(SimplifierContext ctxt, EvalTimes evalTimes, bool fallOffIsReturn, ref ControlFlow cf)
        {
            var defCtxt = ctxt.InFreshStatements();
            evalTimes = evalTimes.Lub(EvalTimes.Opt);
            // Can't remove trailing return since still need to break out of default clause
            cf = cf.Lub(Body.Simplify(defCtxt, evalTimes, false));
            return new DefaultClause(Loc, new Statements(defCtxt.Statements), Index);
        }

        public DefaultClause ToReturnResultDefaultClause()
        {
            var body = new Seq<Statement>();
            if (Body.ToReturnResult(body).NotNone)
                return null;
            return new DefaultClause(Loc, new Statements(body), Index);
        }

        public void Append(Writer writer)
        {
            writer.Append("default:");
            writer.EndLine();
            writer.Indented(null, w => Body.Append(w), null);
        }

        public override bool Equals(object obj)
        {
            var other = obj as DefaultClause;
            return other != null && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)(0xd1310ba6u ^ (uint)Body.GetHashCode());
        }

        public bool Equals(DefaultClause other)
        {
            return Body.Equals(other.Body);
        }

        public int CompareTo(DefaultClause other)
        {
            return Body.CompareTo(other.Body);
        }
    }

    public class SwitchStatement : Statement
    {
        [NotNull]
        public readonly Expression Value;
        [NotNull]
        public readonly IImSeq<CaseClause> Cases;
        [CanBeNull] // null => no default clause
        public readonly DefaultClause Default;

        public SwitchStatement(Location loc, Expression value, IImSeq<CaseClause> cases)
            : base(loc)
        {
            Value = value;
            Cases = cases ?? Constants.EmptyCaseClauses;
            Default = null;
        }

        public SwitchStatement(Location loc, Expression value, IImSeq<CaseClause> cases, DefaultClause defaultClause)
            : base(loc)
        {
            Value = value;
            Cases = cases ?? Constants.EmptyCaseClauses;
            Default = defaultClause;
        }

        public SwitchStatement(Expression value, IImSeq<CaseClause> cases)
        {
            Value = value;
            Cases = cases ?? Constants.EmptyCaseClauses;
            Default = null;
        }

        public SwitchStatement(Expression value, IImSeq<CaseClause> cases, DefaultClause defaultClause)
        {
            Value = value;
            Cases = cases ?? Constants.EmptyCaseClauses;
            Default = defaultClause;
        }

        public override StatementFlavor Flavor { get { return StatementFlavor.Switch; } }

        public override bool NeedsBraces { get { return false; } }

        public override bool NeedsSpace { get { return true; } }

        public override int Size
        {
            get { return 1 + Value.Size + Cases.Sum(c => c.Size) + (Default == null ? 0 : Default.Size); }
        }

        public override ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            Value.AccumEffects(fxCtxt, callCtxt, evalTimes);
            foreach (var c in Cases)
                c.AccumValueEffects(fxCtxt, callCtxt, evalTimes);
            var effects = new Seq<Effects>(Cases.Count + (Default == null ? 0 : 1));
            var resCf = ControlFlow.AlwaysReturn;
            foreach (var c in Cases)
            {
                var caseCtxt = fxCtxt.Fork();
                resCf = resCf.Lub(c.AccumBodyEffects(caseCtxt, callCtxt, evalTimes));
                effects.Add(caseCtxt.AccumEffects);
            }
            if (Default != null)
            {
                var defCtxt = fxCtxt.Fork();
                resCf = resCf.Lub(Default.AccumBodyEffects(defCtxt, callCtxt, evalTimes));
                effects.Add(defCtxt.AccumEffects);
            }
            foreach (var e in effects)
                fxCtxt.IncludeEffects(e);
            return resCf;
        }

        public override IImSeq<Expression> SubExpressions
        {
            get
            {
                var res = new Seq<Expression>();
                res.Add(Value);
                foreach (var c in Cases)
                    c.AppendSubExpressions(res);
                return res;
            }
        }

        public override Statement CloneWithSubExpressions(IImSeq<Expression> subExpressions)
        {
            var i = 0;
            if (i >= subExpressions.Count)
                throw new InvalidOperationException("mismatched sub-expressions arity");
            var val = subExpressions[i++];
            var cases = Cases.Select(c => c.CloneWithSubExpressions(subExpressions, ref i)).ToSeq();
            return new SwitchStatement(Loc, val, cases, Default);
        }

        public override IImSeq<Statements> SubStatementss
        {
            get
            {
                var res = new Seq<Statements>();
                foreach (var c in Cases)
                    c.AppendSubStatementss(res);
                if (Default != null)
                    Default.AppendSubStatementss(res);
                return res;
            }
        }

        public override Statement CloneWithSubStatementss(IImSeq<Statements> subStatementss)
        {
            var i = 0;
            var cases = Cases.Select(c => c.CloneWithSubStatementss(subStatementss, ref i)).ToSeq();
            var def = default(DefaultClause);
            if (Default != null)
                def = Default.CloneWithSubStatementss(subStatementss, ref i);
            if (i != subStatementss.Count)
                throw new InvalidOperationException("mismatched sub-statements arity");
            return new SwitchStatement(Loc, Value, cases, def);
        }

        public override void CollectBoundVars(IMSet<Identifier> boundVars)
        {
            Value.CollectBoundVars(boundVars);
            foreach (var c in Cases)
                c.CollectBoundVars(boundVars);
            if (Default != null)
                Default.CollectBoundVars(boundVars);
        }

        public override void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
            Value.CollectFreeVars(boundVars, freeVars);
            foreach (var c in Cases)
                c.CollectFreeVars(boundVars, freeVars);
            if (Default != null)
                Default.CollectFreeVars(boundVars, freeVars);
        }

        public override ControlFlow Simplify(SimplifierContext ctxt, EvalTimes evalTimes, bool fallOffIsReturn)
        {
            var locCtxt = ctxt.InLocalEffects();
            var simpValue = Value.SimplifyValue(locCtxt, evalTimes);
            var resCf = ControlFlow.AlwaysReturn;
            var simpCases = Cases.Select(c => c.Simplify(ctxt, evalTimes, fallOffIsReturn, ref resCf)).ToSeq();
            var simpDefault = Default == null ? null : Default.Simplify(ctxt, evalTimes, fallOffIsReturn, ref resCf);
            ctxt.Add(new SwitchStatement(Loc, simpValue, simpCases, simpDefault));
            return resCf;
        }

        public override ReturnResult ToReturnResult(ISeq<Statement> acc)
        {
            var cases = new Seq<CaseClause>(Cases.Count);
            foreach (var c in Cases.Select(c => c.ToReturnResultCaseClause()))
            {
                if (c == null)
                    return ReturnResult.Fail;
                cases.Add(c);
            }
            var def = default(DefaultClause);
            if (Default != null)
            {
                def = Default.ToReturnResultDefaultClause();
                if (def == null)
                    return ReturnResult.Fail;
            }
            acc.Add(new SwitchStatement(Loc, Value, cases, def));
            return ReturnResult.None;
        }

        public override void Append(Writer writer)
        {
            writer.Append("switch");
            writer.Space();
            writer.Append('(');
            Value.Append(writer);
            writer.Append(')');
            writer.Space();
            writer.Append('{');
            writer.EndLine();
            for (var i = 0; i < Cases.Count; i++)
            {
                if (Default != null && i == Default.Index)
                    Default.Append(writer);
                Cases[i].Append(writer);
            }
            if (Default != null && (Default.Index == -1 || Default.Index == Cases.Count))
                Default.Append(writer);
            writer.Append('}');
            writer.EndLine();
        }

        public override int GetHashCode()
        {
            var res = 0xce5c3e16u;
            res ^= (uint)Value.GetHashCode();
            res = Cases.Aggregate(res, (h, c) => Constants.Rot5(h) ^ (uint)c.GetHashCode());
            if (Default != null)
                res = Constants.Rot5(res) ^ (uint)Default.GetHashCode();
            return (int)res;
        }

        protected override bool EqualBody(Statement other)
        {
            var ss = (SwitchStatement)other;
            if (Cases.Count != ss.Cases.Count)
                return false;
            if (Cases.Where((c, i) => !c.Equals(ss.Cases[i])).Any())
                return false;
            if ((Default == null) != (ss.Default == null))
                return false;
            return Default == null || Default.Equals(ss.Default);
        }

        protected override int CompareBody(Statement other)
        {
            var ss = (SwitchStatement)other;
            var i = Value.CompareTo(ss.Value);
            if (i != 0)
                return i;
            i = Cases.Count.CompareTo(ss.Cases.Count);
            if (i != 0)
                return i;
            for (var j = 0; j < Cases.Count; j++)
            {
                i = Cases[j].CompareTo(ss.Cases[j]);
                if (i != 0)
                    return i;
            }
            if (Default == null && ss.Default != null)
                return -1;
            if (Default != null && ss.Default == null)
                return 1;
            return Default == null ? 0 : Default.CompareTo(ss.Default);
        }
    }

    // ----------------------------------------------------------------------
    // ThrowStatement
    // ----------------------------------------------------------------------

    public class ThrowStatement : Statement
    {
        [NotNull]
        public readonly Expression Value;

        public ThrowStatement(Location loc, Expression value)
            : base(loc)
        {
            Value = value;
        }

        public ThrowStatement(Expression value)
        {
            Value = value;
        }

        public override StatementFlavor Flavor { get { return StatementFlavor.Throw; } }

        public override bool NeedsBraces { get { return false; } }

        public override bool NeedsSpace { get { return true; } }

        public override int Size { get { return 1 + Value.Size; } }

        public override ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            Value.AccumEffects(fxCtxt, callCtxt, evalTimes);
            fxCtxt.IncludeEffects(Effects.Throws);
            return ControlFlow.Top;
        }

        public override IImSeq<Expression> SubExpressions { get { return new Seq<Expression> { Value }; } }

        public override Statement CloneWithSubExpressions(IImSeq<Expression> subExpressions)
        {
            if (subExpressions.Count != 1)
                throw new InvalidOperationException("mismatched sub-expressions arity");
            return new ThrowStatement(Loc, subExpressions[0]);
        }

        public override IImSeq<Statements> SubStatementss { get { return Constants.EmptyStatementss; } }

        public override Statement CloneWithSubStatementss(IImSeq<Statements> subStatementss)
        {
            if (subStatementss.Count != 0)
                throw new InvalidOperationException("mismatched sub-statements arity");
            return this;
        }

        public override void CollectBoundVars(IMSet<Identifier> boundVars)
        {
            Value.CollectBoundVars(boundVars);
        }

        public override void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
            Value.CollectFreeVars(boundVars, freeVars);
        }

        public override ControlFlow Simplify(SimplifierContext ctxt, EvalTimes evalTimes, bool fallOffIsReturn)
        {
            var locCtxt = ctxt.InLocalEffects();
            var simpValue = Value.SimplifyValue(locCtxt, evalTimes);
            ctxt.Add(new ThrowStatement(Loc, simpValue));
            return ControlFlow.Top;
        }

        public override ReturnResult ToReturnResult(ISeq<Statement> acc)
        {
            acc.Add(this);
            return ReturnResult.None;
        }

        public override void Append(Writer writer)
        {
            writer.Append("throw");
            writer.HardSpace();
            Value.Append(writer);
            writer.Append(';');
            writer.EndLine();
        }

        public override int GetHashCode()
        {
            return (int)(0xafd6ba33u ^ (uint)Value.GetHashCode());
        }

        protected override bool EqualBody(Statement other)
        {
            var ts = (ThrowStatement)other;
            return Value.Equals(ts.Value);
        }

        protected override int CompareBody(Statement other)
        {
            var ts = (ThrowStatement)other;
            return Value.CompareTo(ts.Value);
        }
    }

    // ----------------------------------------------------------------------
    // TryStatement
    // ----------------------------------------------------------------------

    public class CatchClause : IEquatable<CatchClause>, IComparable<CatchClause>
    {
        [CanBeNull] // null => no location known
        public readonly Location Loc;
        [NotNull]
        public readonly Identifier Name;
        [NotNull]
        public readonly Statements Body;

        public CatchClause(Location loc, Identifier name, Statements body)
        {
            Loc = loc;
            Name = name;
            Body = body;
        }

        public CatchClause(Identifier name, Statements body)
        {
            Name = name;
            Body = body;
        }

        public int Size { get { return 1 + Body.Size; } }

        public void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            evalTimes = evalTimes.Lub(EvalTimes.Opt);
#if JSCRIPT_IS_CORRECT
            // In a nested scope
            var boundVars = new Set<Identifier>();
            boundVars.Add(Name);
            Body.CollectBoundVars(boundVars);

            var subCtxt = fxCtxt.Fork();
            foreach (var id in boundVars)
                subCtxt.Bind(id);
            if (callCtxt != null)
                evalTimes = evalTimes.Lub(EvalTimes.Opt);
            Body.AccumBlockEffects(subCtxt, callCtxt, evalTimes);
            fxCtxt.IncludeEffects(subCtxt.AccumEffects);
#else
            Body.AccumBlockEffects(fxCtxt, callCtxt, evalTimes);
#endif
        }

        public void AppendSubStatementss(ISeq<Statements> subStatementss)
        {
            subStatementss.Add(Body);
        }

        public CatchClause CloneWithSubStatementss(IImSeq<Statements> subStatementss, ref int i)
        {
            if (i >= subStatementss.Count)
                throw new InvalidOperationException("mismatched sub-statements arity");
            return new CatchClause(Loc, Name, subStatementss[i++]);
        }

        public void CollectBoundVars(IMSet<Identifier> boundVars)
        {
            boundVars.Add(Name);
            Body.CollectBoundVars(boundVars);
        }

        public void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
            Body.CollectFreeVars(boundVars, freeVars);
        }

        public CatchClause Simplify(SimplifierContext ctxt, EvalTimes evalTimes, bool fallOffIsReturn)
        {
            evalTimes = evalTimes.Lub(EvalTimes.Opt);
#if JSCRIPT_IS_CORRECT
            // In a nested scope
            var subCtxt = ctxt.InFreshScope();
            var simpName = subCtxt.Freshen(Name);
            var boundVars = new Set<Identifier>();
            Body.CollectBoundVars(boundVars);
            foreach (var id in boundVars)
                subCtxt.Freshen(id);
            Body.Simplify(subCtxt, fallOffIsReturn);
            return new CatchClause(Loc, simpName, new Statements(subCtxt.Statements));
#else
            var bodyCtxt = ctxt.InFreshStatements();
            var simpName = ctxt.Rename(Name);
            Body.Simplify(bodyCtxt, evalTimes, fallOffIsReturn);
            return new CatchClause(Loc, simpName, new Statements(bodyCtxt.Statements));
#endif
        }

        public CatchClause ToReturnResultCatchClause()
        {
            var body = new Seq<Statement>();
            if (Body.ToReturnResult(body).NotNone)
                return null;
            return new CatchClause(Loc, Name, new Statements(body));
        }

        public void Append(Writer writer)
        {
            writer.Append("catch");
            writer.Space();
            writer.Append('(');
            Name.Append(writer);
            writer.Append(')');
            writer.EndLine();
            Body.AppendBlock(writer);
        }

        public override bool Equals(object obj)
        {
            var other = obj as CatchClause;
            return other != null && Equals(other);
        }

        public override int GetHashCode()
        {
            var res = 0xc0ac29b7u;
            res ^= (uint)Name.GetHashCode();
            res = Constants.Rot5(res) ^ (uint)Body.GetHashCode();
            return (int)res;
        }

        public bool Equals(CatchClause other)
        {
            return Name.Equals(other.Name) && Body.Equals(other.Body);
        }

        public int CompareTo(CatchClause other)
        {
            var i = Name.CompareTo(other.Name);
            if (i != 0)
                return i;
            return Body.CompareTo(other.Body);
        }
    }

    public class FinallyClause : IEquatable<FinallyClause>, IComparable<FinallyClause>
    {
        [CanBeNull] // null => no location known
        public readonly Location Loc;
        [NotNull]
        public readonly Statements Body;

        public FinallyClause(Location loc, Statements body)
        {
            Loc = loc;
            Body = body;
        }

        public FinallyClause(Statements body)
        {
            Body = body;
        }

        public int Size { get { return 1 + Body.Size; } }

        public void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            Body.AccumBlockEffects(fxCtxt, callCtxt, evalTimes);
        }

        public void AppendSubStatementss(ISeq<Statements> subStatementss)
        {
            subStatementss.Add(Body);
        }

        public FinallyClause CloneWithSubStatementss(IImSeq<Statements> subStatementss, ref int i)
        {
            if (i >= subStatementss.Count)
                throw new InvalidOperationException("mismatched sub-statements arity");
            return new FinallyClause(Loc, subStatementss[i++]);
        }

        public void CollectBoundVars(IMSet<Identifier> boundVars)
        {
            Body.CollectBoundVars(boundVars);
        }

        public void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
            Body.CollectFreeVars(boundVars, freeVars);
        }

        public FinallyClause Simplify(SimplifierContext ctxt, EvalTimes evalTimes, bool fallOffIsReturn)
        {
            var defCtxt = ctxt.InFreshStatements();
            Body.Simplify(defCtxt, evalTimes, fallOffIsReturn);
            return new FinallyClause(Loc, new Statements(defCtxt.Statements));
        }

        public FinallyClause ToReturnResultFinallyClause()
        {
            var body = new Seq<Statement>();
            if (Body.ToReturnResult(body).NotNone)
                return null;
            return new FinallyClause(Loc, new Statements(body));
        }

        public void Append(Writer writer)
        {
            writer.Append("finally");
            writer.EndLine();
            Body.AppendBlock(writer);
        }

        public override bool Equals(object obj)
        {
            var other = obj as FinallyClause;
            return other != null && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)(0xf12c7f99u ^ (uint)Body.GetHashCode());
        }

        public bool Equals(FinallyClause other)
        {
            return Body.Equals(other.Body);
        }

        public int CompareTo(FinallyClause other)
        {
            return Body.CompareTo(other.Body);
        }
    }

    public class TryStatement : Statement
    {
        [NotNull]
        public readonly Statements Body;
        [CanBeNull] // null => no catch, but must have finally
        public readonly CatchClause Catch;
        [CanBeNull] // null => no finally, but must have catch
        public readonly FinallyClause Finally;

        public TryStatement(Location loc, Statements body, CatchClause catchClause)
            : base(loc)
        {
            Body = body;
            Catch = catchClause;
        }

        public TryStatement(Location loc, Statements body, FinallyClause finallyClause)
            : base(loc)
        {
            Body = body;
            Finally = finallyClause;
        }

        public TryStatement(Location loc, Statements body, CatchClause catchClause, FinallyClause finallyClause)
            : base(loc)
        {
            Body = body;
            Catch = catchClause;
            Finally = finallyClause;
        }

        public TryStatement(Statements body, CatchClause catchClause)
        {
            Body = body;
            Catch = catchClause;
        }

        public TryStatement(Statements body, FinallyClause finallyClause)
        {
            Body = body;
            Finally = finallyClause;
        }

        public TryStatement(Statements body, CatchClause catchClause, FinallyClause finallyClause)
        {
            Body = body;
            Catch = catchClause;
            Finally = finallyClause;
        }

        public override StatementFlavor Flavor { get { return StatementFlavor.Try; } }

        public override bool NeedsBraces { get { return false; } }

        public override bool NeedsSpace { get { return true; } }

        public override int Size
        {
            get
            {
                return 1 + Body.Size + (Catch == null ? 0 : Catch.Size) + (Finally == null ? 0 : Finally.Size);
            }
        }

        public override ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            // Give up trying to track effects and control flow
            fxCtxt.IncludeEffects(Effects.Top);
            Body.AccumBlockEffects(fxCtxt, callCtxt, evalTimes);
            if (Catch != null)
                Catch.AccumEffects(fxCtxt, callCtxt, evalTimes);
            if (Finally != null)
                Finally.AccumEffects(fxCtxt, callCtxt, evalTimes);
            return ControlFlow.Top;
        }

        public override IImSeq<Expression> SubExpressions { get { return Constants.EmptyExpressions; } }

        public override Statement CloneWithSubExpressions(IImSeq<Expression> subExpressions)
        {
            if (subExpressions.Count != 0)
                throw new InvalidOperationException("mismatched sub-expressions arity");
            return this;
        }

        public override IImSeq<Statements> SubStatementss
        {
            get
            {
                var res = new Seq<Statements>();
                res.Add(Body);
                if (Catch != null)
                    Catch.AppendSubStatementss(res);
                if (Finally != null)
                    Finally.AppendSubStatementss(res);
                return res;
            }
        }

        public override Statement CloneWithSubStatementss(IImSeq<Statements> subStatementss)
        {
            var i = 0;
            if (i >= subStatementss.Count)
                throw new InvalidOperationException("mismatched sub-statements arity");
            var trys = subStatementss[i++];
            var ctch = default(CatchClause);
            if (Catch != null)
                ctch = Catch.CloneWithSubStatementss(subStatementss, ref i);
            var fin = default(FinallyClause);
            if (Finally != null)
                fin = Finally.CloneWithSubStatementss(subStatementss, ref i);
            if (i != subStatementss.Count)
                throw new InvalidOperationException("mismatched sub-statements arity");
            return new TryStatement(Loc, trys, ctch, fin);
        }

        public override void CollectBoundVars(IMSet<Identifier> boundVars)
        {
            Body.CollectBoundVars(boundVars);
            // BUG: IE treats catch clause as being in same scope as try
#if !JSCRIPT_IS_CORRECT
            if (Catch != null)
                Catch.CollectBoundVars(boundVars);
#endif
            // Finally clause is in same scope as try body
            if (Finally != null)
                Finally.CollectBoundVars(boundVars);
        }

        public override void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
            Body.CollectFreeVars(boundVars, freeVars);
            if (Catch != null)
            {
#if JSCRIPT_IS_CORRECT
                var subBoundVars = new Set<Identifier>();
                foreach (var id in boundVars)
                    subBoundVars.Add(id);
                Catch.CollectBoundVars(subBoundVars);
                Catch.CollectFreeVars(subBoundVars, freeVars);
#else
                Catch.CollectFreeVars(boundVars, freeVars);
#endif
            }
            if (Finally != null)
                Finally.CollectFreeVars(boundVars, freeVars);
        }

        public override ControlFlow Simplify(SimplifierContext ctxt, EvalTimes evalTimes, bool fallOffIsReturn)
        {
            var tryCtxt = ctxt.InFreshStatements();
            Body.Simplify(tryCtxt, evalTimes, fallOffIsReturn);
            var simpCatch = Catch == null ? null : Catch.Simplify(ctxt, evalTimes, fallOffIsReturn);
            var simpFinally = Finally == null ? null : Finally.Simplify(ctxt, evalTimes, fallOffIsReturn);
            ctxt.Add(new TryStatement(Loc, new Statements(tryCtxt.Statements), simpCatch, simpFinally));
            return ControlFlow.Top;
        }

        public override ReturnResult ToReturnResult(ISeq<Statement> acc)
        {
            var body = new Seq<Statement>();
            if (Body.ToReturnResult(body).NotNone)
                return ReturnResult.Fail;
            var ctch = default(CatchClause);
            if (Catch != null)
            {
                ctch = Catch.ToReturnResultCatchClause();
                if (ctch == null)
                    return ReturnResult.Fail;
            }
            var fin = default(FinallyClause);
            if (Finally != null)
            {
                fin = Finally.ToReturnResultFinallyClause();
                if (fin == null)
                    return ReturnResult.Fail;
            }
            acc.Add(new TryStatement(Loc, new Statements(acc), ctch, fin));
            return ReturnResult.None;
        }

        public override void Append(Writer writer)
        {
            if (Catch == null && Finally == null)
                throw new NullReferenceException("try statement missing both catch and finally clauses");
            writer.Append("try");
            writer.EndLine();
            Body.AppendBlock(writer);
            if (Catch != null)
                Catch.Append(writer);
            if (Finally != null)
                Finally.Append(writer);
        }

        public override int GetHashCode()
        {
            var res = 0x7a325381u;
            res ^= (uint)Body.GetHashCode();
            if (Catch != null)
                res = Constants.Rot7(res) ^ (uint)Catch.GetHashCode();
            if (Finally != null)
                res = Constants.Rot7(res) ^ (uint)Finally.GetHashCode();
            return (int)res;
        }

        protected override bool EqualBody(Statement other)
        {
            var ts = (TryStatement)other;
            if ((Catch == null) != (ts.Catch == null) || (Finally == null) != (ts.Finally == null))
                return false;
            if (!Body.Equals(ts.Body))
                return false;
            if (Catch != null && !Catch.Equals(ts.Catch))
                return false;
            return Finally == null || Finally.Equals(ts.Finally);
        }

        protected override int CompareBody(Statement other)
        {
            var ts = (TryStatement)other;
            var i = Body.CompareTo(ts.Body);
            if (i != 0)
                return i;
            if (Catch == null && ts.Catch != null)
                return -1;
            if (Catch != null && ts.Catch == null)
                return 1;
            i = Catch == null ? 0 : Catch.CompareTo(ts.Catch);
            if (i != 0)
                return i;
            if (Finally == null && ts.Finally != null)
                return -1;
            if (Finally != null && ts.Finally == null)
                return 1;
            return Finally == null ? 0 : Finally.CompareTo(ts.Finally);
        }
    }

    // ----------------------------------------------------------------------
    // VariableStatement
    // ----------------------------------------------------------------------

    public class VariableDeclaration : IEquatable<VariableDeclaration>, IComparable<VariableDeclaration>
    {
        [CanBeNull] // null => no location known
        public readonly Location Loc;
        [NotNull]
        public readonly Identifier Name;
        [CanBeNull] // null => no initializer
        public readonly Expression Initializer;

        public VariableDeclaration(Location loc, Identifier name)
        {
            Loc = loc;
            Name = name;
        }

        public VariableDeclaration(Location loc, Identifier name, Expression initializer)
        {
            Loc = loc;
            Name = name;
            Initializer = initializer;
        }

        public VariableDeclaration(Identifier name)
        {
            Name = name;
        }

        public VariableDeclaration(Identifier name, Expression initializer)
        {
            Name = name;
            Initializer = initializer;
        }

        public int Size { get { return 1 + (Initializer == null ? 0 : Initializer.Size); } }

        public void AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            if (Initializer != null)
            {
                Initializer.AccumEffects(fxCtxt, callCtxt, evalTimes);
                fxCtxt.IncludeEffects(Effects.Write(Name));
            }
        }

        public void AppendSubExpressions(ISeq<Expression> subExpressions)
        {
            if (Initializer != null)
                subExpressions.Add(Initializer);
        }

        public VariableDeclaration CloneWithSubExpressions(IImSeq<Expression> subExpressions, ref int i)
        {
            if (Initializer != null)
            {
                if (i >= subExpressions.Count)
                    throw new InvalidOperationException("mismatched sub-expressions arity");
                return new VariableDeclaration(Loc, Name, subExpressions[i++]);
            }
            else
                return this;
        }

        public void CollectBoundVars(IMSet<Identifier> boundVars)
        {
            if (Initializer != null)
                Initializer.CollectBoundVars(boundVars);
            boundVars.Add(Name);
        }

        public void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
            if (Initializer != null)
                Initializer.CollectFreeVars(boundVars, freeVars);
        }

        public VariableDeclaration Simplify(SimplifierContext ctxt, EvalTimes evalTimes)
        {
            var locCtxt = ctxt.InLocalEffects();
            var simpInitializer = Initializer == null ? null : Initializer.Simplify(locCtxt, evalTimes);
            var simpName = ctxt.Rename(Name);
            return new VariableDeclaration(Loc, simpName, simpInitializer);
        }

        public void Append(Writer writer)
        {
            Name.Append(writer);
            if (Initializer != null)
            {
                writer.Space();
                writer.Append('=');
                if (Initializer.NeedsDelimitingSpace)
                    writer.HardSpace();
                else
                    writer.Space();
                Initializer.Append(writer, Precedence.Assignment);
            }
        }

        public override bool Equals(object obj)
        {
            var other = obj as VariableDeclaration;
            return other != null && Equals(other);
        }

        public override int GetHashCode()
        {
            var res = 0x61d809ccu;
            res ^= (uint)Name.GetHashCode();
            if (Initializer != null)
                res = Constants.Rot7(res) ^ (uint)Initializer.GetHashCode();
            return (int)res;
        }

        public bool Equals(VariableDeclaration other)
        {
            if (!Name.Equals(other.Name))
                return false;
            if ((Initializer == null) != (other.Initializer == null))
                return false;
            return Initializer == null || Initializer.Equals(other.Initializer);
        }

        public int CompareTo(VariableDeclaration other)
        {
            var i = Name.CompareTo(other.Name);
            if (i != 0)
                return i;
            if (Initializer == null && other.Initializer != null)
                return -1;
            if (Initializer != null && other.Initializer == null)
                return 1;
            return Initializer == null ? 0 : Initializer.CompareTo(other.Initializer);
        }
    }

    public class VariableStatement : Statement
    {
        [NotNull]
        public readonly IImSeq<VariableDeclaration> Variables;

        public VariableStatement(Location loc, IImSeq<VariableDeclaration> variables)
            : base(loc)
        {
            Variables = variables ?? Constants.EmptyVariableDeclarations;
        }

        public VariableStatement(Location loc, params VariableDeclaration[] variables)
            : base(loc)
        {
            Variables = new Seq<VariableDeclaration>(variables);
        }

        public VariableStatement(IImSeq<VariableDeclaration> variables)
        {
            Variables = variables ?? Constants.EmptyVariableDeclarations;
        }

        public VariableStatement(params VariableDeclaration[] variables)
        {
            Variables = new Seq<VariableDeclaration>(variables);
        }

        public override StatementFlavor Flavor { get { return StatementFlavor.Variable; } }

        public override bool NeedsBraces { get { return Variables.Count == 0; } }

        public override bool NeedsSpace { get { return Variables.Count > 0; } }

        public override int Size { get { return 1 + Variables.Sum(v => v.Size); } }

        public override ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            foreach (var v in Variables)
                v.AccumEffects(fxCtxt, callCtxt, evalTimes);
            return ControlFlow.AlwaysReturn;
        }

        public override IImSeq<Expression> SubExpressions
        {
            get
            {
                var res = new Seq<Expression>();
                foreach (var v in Variables)
                    v.AppendSubExpressions(res);
                return res;
            }
        }

        public override Statement CloneWithSubExpressions(IImSeq<Expression> subExpressions)
        {
            var i = 0;
            return new VariableStatement(Loc, Variables.Select(d => d.CloneWithSubExpressions(subExpressions, ref i)).ToSeq());
        }

        public override IImSeq<Statements> SubStatementss { get { return Constants.EmptyStatementss; } }

        public override Statement CloneWithSubStatementss(IImSeq<Statements> subStatementss)
        {
            if (subStatementss.Count != 0)
                throw new InvalidOperationException("mismatched sub-statements arity");
            return this;
        }

        public override void CollectBoundVars(IMSet<Identifier> boundVars)
        {
            foreach (var v in Variables)
                v.CollectBoundVars(boundVars);
        }

        public override void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
            foreach (var v in Variables)
                v.CollectFreeVars(boundVars, freeVars);
        }

        public override ControlFlow Simplify(SimplifierContext ctxt, EvalTimes evalTimes, bool fallOffIsReturn)
        {
            var simpVariables = Variables.Select(v => v.Simplify(ctxt, evalTimes)).ToSeq();
            ctxt.Add(new VariableStatement(Loc, simpVariables));
            return ControlFlow.AlwaysReturn;
        }

        public override ReturnResult ToReturnResult(ISeq<Statement> acc)
        {
            acc.Add(this);
            return ReturnResult.None;
        }

        public override void Append(Writer writer)
        {
            if (Variables.Count > 0)
            {
                writer.Append("var");
                writer.HardSpace();
                writer.List
                    (Variables,
                     null,
                     (w, d) => d.Append(w),
                     w =>
                     {
                         w.Append(',');
                         w.Space();
                     },
                     null);
                writer.Append(';');
                writer.EndLine();
            }
        }

        public override int GetHashCode()
        {
            return (int)Variables.Aggregate(0xfb21a991u, (h, v) => Constants.Rot5(h) ^ (uint)v.GetHashCode());
        }

        protected override bool EqualBody(Statement other)
        {
            var vs = (VariableStatement)other;
            return !Variables.Select((v, i) => !v.Equals(vs.Variables[i])).Any();
        }

        protected override int CompareBody(Statement other)
        {
            var vs = (VariableStatement)other;
            var i = Variables.Count.CompareTo(vs.Variables.Count);
            if (i != 0)
                return i;
            for (var j = 0; j < Variables.Count; j++)
            {
                i = Variables[j].CompareTo(vs.Variables[j]);
                if (i != 0)
                    return i;
            }
            return 0;
        }
    }

    // ----------------------------------------------------------------------
    // WhileStatement
    // ----------------------------------------------------------------------

    public class WhileStatement : Statement
    {
        [NotNull]
        public readonly Expression Condition;
        [NotNull]
        public readonly Statements Body;

        public WhileStatement(Location loc, Expression condition, Statements body)
            : base(loc)
        {
            Condition = condition;
            Body = body;
        }

        public WhileStatement(Expression condition, Statements body)
        {
            Condition = condition;
            Body = body;
        }

        public override StatementFlavor Flavor { get { return StatementFlavor.While; } }

        public override bool NeedsBraces { get { return false; } }

        public override bool NeedsSpace { get { return true; } }

        public override int Size { get { return 1 + Condition.Size + Body.Size; } }

        public override ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            // Give up trying to track effects
            fxCtxt.IncludeEffects(Effects.Top);
            if (callCtxt != null)
                evalTimes = evalTimes.Lub(EvalTimes.AtLeastOnce);
            Condition.AccumEffects(fxCtxt, callCtxt, evalTimes);
            return Body.AccumBlockEffects(fxCtxt, callCtxt, EvalTimes.Top);
        }

        public override IImSeq<Expression> SubExpressions { get { return new Seq<Expression> { Condition }; } }

        public override Statement CloneWithSubExpressions(IImSeq<Expression> subExpressions)
        {
            if (subExpressions.Count != 1)
                throw new InvalidOperationException("mismatched sub-expressions arity");
            return new WhileStatement(Loc, subExpressions[0], Body);
        }

        public override IImSeq<Statements> SubStatementss { get { return new Seq<Statements> { Body }; } }

        public override Statement CloneWithSubStatementss(IImSeq<Statements> subStatementss)
        {
            if (subStatementss.Count != 1)
                throw new InvalidOperationException("mismatched sub-statements arity");
            return new WhileStatement(Loc, Condition, subStatementss[0]);
        }

        public override void CollectBoundVars(IMSet<Identifier> boundVars)
        {
            Condition.CollectBoundVars(boundVars);
            Body.CollectBoundVars(boundVars);
        }

        public override void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
            Condition.CollectFreeVars(boundVars, freeVars);
            Body.CollectFreeVars(boundVars, freeVars);
        }

        public override ControlFlow Simplify(SimplifierContext ctxt, EvalTimes evalTimes, bool fallOffIsReturn)
        {
            evalTimes = evalTimes.Lub(EvalTimes.AtLeastOnce);
            var condCtxt = ctxt.InNoStatements();
            var simpCondition = Condition.SimplifyValue(condCtxt, evalTimes);
            var bodyCtxt = ctxt.InFreshStatements();

            var bodyCf = Body.Simplify(bodyCtxt, EvalTimes.Top, false);
            ctxt.Add(new WhileStatement(Loc, simpCondition, new Statements(bodyCtxt.Statements)));
            return bodyCf;
        }

        public override ReturnResult ToReturnResult(ISeq<Statement> acc)
        {
            var body = new Seq<Statement>();
            if (Body.ToReturnResult(acc).NotNone)
                return ReturnResult.Fail;
            acc.Add(new WhileStatement(Loc, Condition, new Statements(body)));
            return ReturnResult.None;
        }

        public override void Append(Writer writer)
        {
            writer.Append("while");
            writer.Space();
            writer.Append('(');
            Condition.Append(writer);
            writer.Append(')');
            writer.EndLine();
            Body.AppendStatementOrBlock(writer);
        }

        public override int GetHashCode()
        {
            var res = 0x5dec8032u;
            res ^= (uint)Condition.GetHashCode();
            res = Constants.Rot7(res) ^ (uint)Body.GetHashCode();
            return (int)res;
        }

        protected override bool EqualBody(Statement other)
        {
            var ws = (WhileStatement)other;
            return Condition.Equals(ws.Condition) && Body.Equals(ws.Body);
        }

        protected override int CompareBody(Statement other)
        {
            var ws = (WhileStatement)other;
            var i = Condition.CompareTo(ws.Condition);
            if (i != 0)
                return i;
            return Body.CompareTo(ws.Body);
        }
    }

    // ----------------------------------------------------------------------
    // WithStatement
    // ----------------------------------------------------------------------

    public class WithStatement : Statement
    {
        [NotNull]
        public readonly Expression Environment;
        [NotNull]
        public readonly Statements Body;

        public WithStatement(Location loc, Expression environment, Statements body)
            : base(loc)
        {
            Environment = environment;
            Body = body;
        }

        public WithStatement(Expression environment, Statements body)
        {
            Environment = environment;
            Body = body;
        }

        public override StatementFlavor Flavor { get { return StatementFlavor.With; } }

        public override bool NeedsBraces { get { return false; } }

        public override bool NeedsSpace { get { return true; } }

        public override int Size { get { return 1 + Environment.Size + Body.Size; } }

        public override ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            Environment.AccumEffects(fxCtxt, callCtxt, evalTimes);
            // We can no longer track scoping of variables in body
            fxCtxt.IncludeEffects(Effects.WriteAll);
            return Body.AccumBlockEffects(fxCtxt, callCtxt, evalTimes);
        }

        public override IImSeq<Expression> SubExpressions { get { return new Seq<Expression> { Environment }; } }

        public override Statement CloneWithSubExpressions(IImSeq<Expression> subExpressions)
        {
            if (subExpressions.Count != 1)
                throw new InvalidOperationException("mismatched sub-expressions arity");
            return new WithStatement(Loc, subExpressions[0], Body);
        }

        public override IImSeq<Statements> SubStatementss { get { return new Seq<Statements> { Body }; } }

        public override Statement CloneWithSubStatementss(IImSeq<Statements> subStatementss)
        {
            if (subStatementss.Count != 1)
                throw new InvalidOperationException("mismatched sub-statements arity");
            return new WithStatement(Loc, Environment, subStatementss[0]);
        }

        public override void CollectBoundVars(IMSet<Identifier> boundVars)
        {
            Environment.CollectBoundVars(boundVars);
            Body.CollectBoundVars(boundVars);
        }

        public override void CollectFreeVars(IImSet<Identifier> boundVars, IMSet<Identifier> freeVars)
        {
            Environment.CollectFreeVars(boundVars, freeVars);
            Body.CollectFreeVars(boundVars, freeVars);
        }

        public override ControlFlow Simplify(SimplifierContext ctxt, EvalTimes evalTimes, bool fallOffIsReturn)
        {
            var locCtxt = ctxt.InLocalEffects();
            var simpEnvironment = Environment.SimplifyValue(locCtxt, evalTimes);
            var bodyCtxt = ctxt.InNoStatements();
            var bodyCf = Body.Simplify(bodyCtxt, evalTimes, fallOffIsReturn);
            ctxt.Add(new WithStatement(Loc, simpEnvironment, new Statements(bodyCtxt.Statements)));
            return bodyCf;
        }

        public override ReturnResult ToReturnResult(ISeq<Statement> acc)
        {
            var body = new Seq<Statement>();
            if (Body.ToReturnResult(body).NotNone)
                return ReturnResult.Fail;
            acc.Add(new WithStatement(Loc, Environment, new Statements(body)));
            return ReturnResult.None;
        }

        public override void Append(Writer writer)
        {
            writer.Append("with");
            writer.Space();
            writer.Append('(');
            Environment.Append(writer);
            writer.Append(')');
            writer.EndLine();
            Body.AppendStatementOrBlock(writer);
        }

        public override int GetHashCode()
        {
            var res = 0xef845d5du;
            res ^= (uint)Environment.GetHashCode();
            res = Constants.Rot7(res) ^ (uint)Body.GetHashCode();
            return (int)res;
        }

        protected override bool EqualBody(Statement other)
        {
            var ws = (WithStatement)other;
            return Environment.Equals(ws.Environment) && Body.Equals(ws.Body);
        }

        protected override int CompareBody(Statement other)
        {
            var ws = (WithStatement)other;
            var i = Environment.CompareTo(ws.Environment);
            if (i != 0)
                return i;
            return Body.CompareTo(ws.Body);
        }
    }
}