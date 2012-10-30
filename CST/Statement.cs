//
// Statements in intermediate language
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.LiveLabs.Extras;
using Microsoft.LiveLabs.JavaScript.JST;
using JST = Microsoft.LiveLabs.JavaScript.JST;

namespace Microsoft.LiveLabs.CST
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
        Expression,
        Break,
        Continue,
        Throw,
        Rethrow,
        Return,
        IfThenElse,
        Switch,
        DoWhile,
        WhileDo,
        InitializeObject,
        Try,
        HandlePseudo,
        PushTryPseudo,
        LeavePseudo,
        EndPseudo,
        GotoPseudo
    }

    // ----------------------------------------------------------------------
    // Statement
    // ----------------------------------------------------------------------

    public abstract class Statement : IEquatable<Statement>
    {
        [CanBeNull] // currently always null
        public readonly Location Loc;

        public abstract StatementFlavor Flavor { get; }

        public abstract bool NeverReturns { get; }

        public override bool Equals(object obj)
        {
            var stmnt = obj as Statement;
            return stmnt != null && Equals(stmnt);
        }

        public bool Equals(Statement other)
        {
            return Flavor == other.Flavor && EqualBody(other);
        }

        protected abstract bool EqualBody(Statement other);

        public override int GetHashCode()
        {
            throw new InvalidOperationException("should have been overridden");
        }

        public abstract void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage);

        public abstract void Simplify(SimplifierContext ctxt);

        public abstract ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes);

        public abstract ReturnResult ToReturnResult(ISeq<Statement> acc);

        public abstract void Append(CSTWriter w);

        public override string ToString()
        {
            return CSTWriter.WithAppendDebug(Append);
        }
    }

    public class Statements : IEquatable<Statements>, IEnumerable<Statement>
    {
        [NotNull]
        public readonly IImSeq<Statement> Body;
        [CanBeNull] // null => usage not yet collected
        protected Usage usage;

        public static readonly Statements Empty;

        static Statements()
        {
            Empty = new Statements(Constants.EmptyStatements);
        }

        public Statements(IImSeq<Statement> body)
        {
            Body = body ?? Constants.EmptyStatements;
        }

        public Statements(params Statement[] body)
        {
            Body = new Seq<Statement>(body);
        }

        public bool NeverReturns
        {
            get { return Body.Any(s => s.NeverReturns); }
        }

        public Usage Usage(CompilationEnvironment compEnv)
        {
            if (usage == null)
            {
                usage = new Usage();
                foreach (var s in Body)
                    s.AccumUsage(compEnv, true, usage);
            }
            return usage;
        }

        public void SimplifyInto(SimplifierContext ctxt)
        {
            foreach (var s in Body)
                s.Simplify(ctxt);
        }

        public Statements Simplify(SimplifierContext ctxt)
        {
            var subCtxt = ctxt.InFreshStatements();
            foreach (var s in Body)
                s.Simplify(subCtxt);
            return new Statements(subCtxt.Statements);
        }

        public ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            var neverReturn = false;
            var finalCf = ControlFlow.AlwaysReturn;
            foreach (var s in Body)
            {
                var cf = s.AccumEffects(fxCtxt, callCtxt, evalTimes);
                if (cf.Value == ControlFlowEnum.NeverReturn)
                    neverReturn = true;
                if (callCtxt != null && cf.Value != ControlFlowEnum.AlwaysReturn)
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

        public Statements CheckNoReturn()
        {
            var acc = new Seq<Statement>();
            if (Body.Any(s => s.ToReturnResult(acc).NotNone))
                return null;
            return new Statements(acc);
        }

        public override bool Equals(object obj)
        {
            var stmnts = obj as Statements;
            return stmnts != null && Equals(stmnts);
        }

        public override int GetHashCode()
        {
            var res = 0x78c14389u;
            foreach (var s in Body)
                res = Constants.Rot3(res) ^ (uint)s.GetHashCode();
            return (int)res;
        }

        public bool Equals(Statements other)
        {
            if (Body.Count != other.Body.Count)
                return false;
            for (var i = 0; i < Body.Count; i++)
            {
                if (!Body[i].Equals(other.Body[i]))
                    return false;
            }
            return true;
        }

        public void Append(CSTWriter w)
        {
            foreach (var s in Body)
            {
                s.Append(w);
                w.EndLine();
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
    // ExpressionStatement
    // ----------------------------------------------------------------------

    public class ExpressionStatement : Statement
    {
        [NotNull]
        public readonly Expression Value;

        public ExpressionStatement(Expression value)
        {
            Value = value;
        }

        public override StatementFlavor Flavor { get { return StatementFlavor.Expression; } }

        public override bool NeverReturns { get { return false; } }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage)
        {
            Value.AccumUsage(compEnv, isAlwaysUsed, usage, false);
        }

        public override ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            Value.AccumEffects(fxCtxt, callCtxt, evalTimes);
            return ControlFlow.AlwaysReturn;
        }

        public override void Simplify(SimplifierContext ctxt)
        {
            var locCtxt = ctxt.InLocalEffects();
            var simpValue = Value.Simplify(locCtxt);
            if (simpValue != null)
                ctxt.Add(new ExpressionStatement(simpValue));
        }

        public override ReturnResult ToReturnResult(ISeq<Statement> acc)
        {
            acc.Add(this);
            return ReturnResult.None;
        }

        public override int GetHashCode()
        {
            var res = 0xb6f84565u;
            res ^= (uint)Value.GetHashCode();
            return (int)res;
        }

        protected override bool EqualBody(Statement other)
        {
            var exp = (ExpressionStatement)other;
            return Value.Equals(exp.Value);
        }

        public override void Append(CSTWriter w)
        {
            Value.Append(w, 0);
            w.Append(';');
        }
    }

    // ----------------------------------------------------------------------
    // BreakStatement
    // ----------------------------------------------------------------------

    // TODO: Labels on loops

    public class BreakStatement : Statement
    {
        [CanBeNull] // null => no label
        public readonly JST.Identifier Label;

        public BreakStatement(JST.Identifier label)
        {
            Label = label;
        }

        public BreakStatement()
            : this(null)
        {
        }

        public override StatementFlavor Flavor { get { return StatementFlavor.Break; } }

        public override bool NeverReturns { get { return true; } }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage)
        {
        }

        public override ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            // Give up tracking control flow
            return ControlFlow.Top;
        }

        public override void Simplify(SimplifierContext ctxt)
        {
            ctxt.Add(this);
        }

        public override ReturnResult ToReturnResult(ISeq<Statement> acc)
        {
            acc.Add(this);
            return ReturnResult.None;
        }

        public override int GetHashCode()
        {
            var res = 0xe1ddf2dau;
            if (Label != null)
                res ^= (uint)Label.GetHashCode();
            return (int)res;
        }

        protected override bool EqualBody(Statement other)
        {
            var brk = (BreakStatement)other;
            if ((Label == null) != (brk.Label == null))
                return false;
            return Label == null || Label.Equals(brk.Label);
        }

        public override void Append(CSTWriter w)
        {
            w.Append("break");
            if (Label != null)
            {
                w.Append(' ');
                w.Append(Label.Value);
            }
            w.Append(';');
        }
    }

    // ----------------------------------------------------------------------
    // ContiuneStatement
    // ----------------------------------------------------------------------

    public class ContinueStatement : Statement
    {
        [CanBeNull] // null => no label
        public readonly JST.Identifier Label;

        public ContinueStatement(JST.Identifier label)
        {
            Label = label;
        }

        public ContinueStatement()
            : this(null)
        {
        }

        public override StatementFlavor Flavor { get { return StatementFlavor.Continue; } }

        public override bool NeverReturns { get { return true; } }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage)
        {
        }

        public override ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            // Give up tracking control flow
            return ControlFlow.Top;
        }

        public override void Simplify(SimplifierContext ctxt)
        {
            ctxt.Add(this);
        }

        public override ReturnResult ToReturnResult(ISeq<Statement> acc)
        {
            acc.Add(this);
            return ReturnResult.None;
        }

        public override int GetHashCode()
        {
            var res = 0xa4cb7e33u;
            if (Label != null)
                res ^= (uint)Label.GetHashCode();
            return (int)res;
        }

        protected override bool EqualBody(Statement other)
        {
            var cont = (ContinueStatement)other;
            if ((Label == null) != (cont.Label == null))
                return false;
            return Label == null || Label.Equals(cont.Label);
        }

        public override void Append(CSTWriter w)
        {
            w.Append("continue");
            if (Label != null)
            {
                w.Append(' ');
                w.Append(Label.Value);
            }
            w.Append(';');
        }
    }

    // ----------------------------------------------------------------------
    // ThrowStatement
    // ----------------------------------------------------------------------

    public class ThrowStatement : Statement
    {
        [NotNull]
        public readonly Expression Exception;

        public ThrowStatement(Expression exception)
        {
            Exception = exception;
        }

        public override StatementFlavor Flavor { get { return StatementFlavor.Throw; } }

        public override bool NeverReturns { get { return true; } }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage)
        {
            Exception.AccumUsage(compEnv, isAlwaysUsed, usage, false);
        }

        public override ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            Exception.AccumEffects(fxCtxt, callCtxt, evalTimes);
            fxCtxt.IncludeEffects(JST.Effects.Throws);
            return ControlFlow.NeverReturn;
        }

        public override void Simplify(SimplifierContext ctxt)
        {
            var locCtxt = ctxt.InLocalEffects();
            var simpException = Exception.Simplify(locCtxt);
            ctxt.Add(new ThrowStatement(simpException));
        }

        public override ReturnResult ToReturnResult(ISeq<Statement> acc)
        {
            acc.Add(this);
            return ReturnResult.None;
        }

        public override int GetHashCode()
        {
            var res = 0x8e3c5b2fu;
            res ^= (uint)Exception.GetHashCode();
            return (int)res;
        }

        protected override bool EqualBody(Statement other)
        {
            var thrw = (ThrowStatement)other;
            return Exception.Equals(thrw.Exception);
        }

        public override void Append(CSTWriter w)
        {
            w.Append("throw ");
            Exception.Append(w, 0);
            w.Append(';');
        }
    }

    // ----------------------------------------------------------------------
    // RethrowStatement
    // ----------------------------------------------------------------------

    public class RethrowStatement : Statement
    {
        [NotNull]
        public readonly Expression Exception;

        public RethrowStatement(Expression exception)
        {
            Exception = exception;
        }

        public override StatementFlavor Flavor { get { return StatementFlavor.Rethrow; } }

        public override bool NeverReturns { get { return true; } }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage)
        {
            Exception.AccumUsage(compEnv, isAlwaysUsed, usage, false);
        }

        public override ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            Exception.AccumEffects(fxCtxt, callCtxt, evalTimes);
            fxCtxt.IncludeEffects(JST.Effects.Throws);
            return ControlFlow.NeverReturn;
        }

        public override void Simplify(SimplifierContext ctxt)
        {
            var locCtxt = ctxt.InLocalEffects();
            var simpException = Exception.Simplify(locCtxt);
            ctxt.Add(new RethrowStatement(simpException));
        }

        public override ReturnResult ToReturnResult(ISeq<Statement> acc)
        {
            acc.Add(this);
            return ReturnResult.None;
        }

        public override int GetHashCode()
        {
            var res = 0xd1cff191u;
            res ^= (uint)Exception.GetHashCode();
            return (int)res;
        }

        protected override bool EqualBody(Statement other)
        {
            var rethrw = (RethrowStatement)other;
            return Exception.Equals(rethrw.Exception);
        }

        public override void Append(CSTWriter w)
        {
            w.Append("rethrow ");
            Exception.Append(w, 0);
            w.Append(';');
        }
    }

    // ----------------------------------------------------------------------
    // ReturnStatement
    // ----------------------------------------------------------------------

    public class ReturnStatement : Statement
    {
        [CanBeNull] // null => no return value
        public readonly Expression Value;

        public ReturnStatement(Expression value)
        {
            Value = value;
        }

        public override StatementFlavor Flavor { get { return StatementFlavor.Return; } }

        public override bool NeverReturns { get { return true; } }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage)
        {
            if (Value != null)
                Value.AccumUsage(compEnv, isAlwaysUsed, usage, false);
        }

        public override ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            if (Value != null)
                Value.AccumEffects(fxCtxt, callCtxt, evalTimes);
            return ControlFlow.AlwaysReturn;
        }

        public override void Simplify(SimplifierContext ctxt)
        {
            if (Value == null)
                ctxt.Add(this);
            else
            {
                var locCtxt = ctxt.InLocalEffects();
                var simpValue = Value.Simplify(locCtxt);
                ctxt.Add(new ReturnStatement(simpValue));
            }
        }

        public override ReturnResult ToReturnResult(ISeq<Statement> acc)
        {
            return new ReturnResult(Value);
        }

        public override int GetHashCode()
        {
            var res = 0xb3a8c1adu;
            if (Value != null)
                res ^= (uint)Value.GetHashCode();
            return (int)res;
        }

        protected override bool EqualBody(Statement other)
        {
            var ret = (ReturnStatement)other;
            if ((Value == null) != (ret.Value == null))
                return false;
            return Value != null && Value.Equals(ret.Value);
        }

        public override void Append(CSTWriter w)
        {
            w.Append("return");
            if (Value != null)
            {
                w.Append(' ');
                Value.Append(w, 0);
            }
            w.Append(';');
        }
    }

    // ----------------------------------------------------------------------
    // IfThenElseStatement
    // ----------------------------------------------------------------------

    public class IfThenElseStatement : Statement
    {
        [NotNull]
        public readonly Expression Condition;
        [NotNull]
        public readonly Statements Then;
        [CanBeNull]  // null => if-then statement
        public readonly Statements Else;

        public IfThenElseStatement(Expression condition, Statements then, Statements els)
        {
            Condition = condition;
            Then = then;
            Else = els;
        }

        public IfThenElseStatement(Expression condition, Statements then)
            : this(condition, then, null)
        {
        }

        public override StatementFlavor Flavor { get { return StatementFlavor.IfThenElse; } }

        public override bool NeverReturns { get { return Then.NeverReturns && Else != null && Else.NeverReturns; } }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage)
        {
            Condition.AccumUsage(compEnv, false, usage, false);
            if (Else == null)
                usage.Merge(Then.Usage(compEnv), false);
            else
                usage.Merge(new Seq<Usage> { Then.Usage(compEnv), Else.Usage(compEnv) });
        }

        public override ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            Condition.AccumEffects(fxCtxt, callCtxt, evalTimes);
            if (callCtxt != null)
                evalTimes = evalTimes.Lub(EvalTimes.Opt);
            if (Else == null)
            {
                var thenCf = Then.AccumEffects(fxCtxt, callCtxt, evalTimes);
                return thenCf.Lub(ControlFlow.AlwaysReturn);
            }
            else
            {
                var thenCtxt = fxCtxt.Fork();
                var thenCf = Then.AccumEffects(thenCtxt, callCtxt, evalTimes);
                var elseCtxt = fxCtxt.Fork();
                var elseCf = Else.AccumEffects(elseCtxt, callCtxt, evalTimes);
                fxCtxt.IncludeEffects(thenCtxt.AccumEffects);
                fxCtxt.IncludeEffects(elseCtxt.AccumEffects);
                return thenCf.Lub(elseCf);
            }
        }

        public override void Simplify(SimplifierContext ctxt)
        {
            var locCtxt = ctxt.InLocalEffects();
            var simpCondition = Condition.Simplify(locCtxt);
            var simpThen = Then.Simplify(ctxt);
            var simpElse = Else == null ? null : Else.Simplify(ctxt);
            ctxt.Add(new IfThenElseStatement(simpCondition, simpThen, simpElse));
        }

        public override ReturnResult ToReturnResult(ISeq<Statement> acc)
        {
            var then = Then.CheckNoReturn();
            if (then == null)
                return ReturnResult.Fail;
            var els = default(Statements);
            if (Else != null)
            {
                els = Else.CheckNoReturn();
                if (els == null)
                    return ReturnResult.Fail;
            }
            acc.Add(new IfThenElseStatement(Condition, then, els));
            return ReturnResult.None;
        }

        public override int GetHashCode()
        {
            var res = 0xbe0e1777u;
            res ^= (uint)Condition.GetHashCode();
            res = Constants.Rot3(res) ^ (uint)Then.GetHashCode();
            if (Else != null)
                res = Constants.Rot7(res) ^ (uint)Else.GetHashCode();
            return (int)res;
        }

        protected override bool EqualBody(Statement other)
        {
            var ite = (IfThenElseStatement)other;
            if (!Condition.Equals(ite.Condition) || (Else == null) != (ite.Else == null) || !Then.Equals(ite.Then))
                return false;
            if (Else != null)
            {
                if (!Else.Equals(ite.Else))
                    return false;
            }
            return true;
        }

        public override void Append(CSTWriter w)
        {
            w.Append("if (");
            Condition.Append(w, 0);
            w.Append(") {");
            w.EndLine();
            w.Indented(Then.Append);
            w.Append('}');
            if (Else != null)
            {
                w.Append(" else {");
                w.EndLine();
                w.Indented(Else.Append);
                w.Append('}');
            }
        }
    }

    // ----------------------------------------------------------------------
    // SwitchStatement
    // ----------------------------------------------------------------------

    public class SwitchStatementCase
    {
        [NotNull]
        public readonly IImSet<int> Values; // -1 denotes includes default case
        [NotNull]
        public readonly Statements Body;

        public SwitchStatementCase(IImSet<int> values, Statements body)
        {
            Values = values;
            Body = body;
        }

        public override bool Equals(object obj)
        {
            var c = obj as SwitchStatementCase;
            if (c == null || Values.Count != c.Values.Count)
                return false;
            foreach (var v in Values)
            {
                if (!c.Values.Contains(v))
                    return false;
            }
            return Body.Equals(c.Body);
        }

        public override int GetHashCode()
        {
            var res = 0xea752dfeu;
            // Equal sets enumerate in the same order
            foreach (var v in Values)
                res = Constants.Rot3(res) ^ (uint)v;
            res = Constants.Rot7(res) ^ (uint)Body.GetHashCode();
            return (int)res;
        }

        public SwitchStatementCase Simplify(SimplifierContext ctxt)
        {
            var simpBody = Body.Simplify(ctxt);
            return new SwitchStatementCase(Values, simpBody);
        }

        public ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            if (callCtxt != null)
                evalTimes = evalTimes.Lub(EvalTimes.Opt);
            return Body.AccumEffects(fxCtxt, callCtxt, evalTimes);
        }

        public SwitchStatementCase ToReturnResultCase()
        {
            var body = Body.CheckNoReturn();
            if (body == null)
                return null;
            return new SwitchStatementCase(Values, body);
        }

        public void Append(CSTWriter w, bool isDefault)
        {
            if (!isDefault && Values.Count == 1 && Values[0] < 0)
                return;
            if (isDefault && Values.All(v => v >= 0))
                return;
            foreach (var v in Values)
            {
                if (isDefault && v < 0)
                {
                    w.Append("default:");
                    w.EndLine();
                }
                else if (!isDefault && v >= 0)
                {
                    w.Append("case ");
                    w.Append(v);
                    w.Append(':');
                    w.EndLine();
                }
            }
            w.Append('{');
            w.EndLine();
            w.Indented(Body.Append);
            w.Append('}');
            w.EndLine();
        }
    }

    public class SwitchStatement : Statement
    {
        [NotNull]
        public readonly Expression Value;
        [NotNull]
        public readonly IImSeq<SwitchStatementCase> Cases;

        public SwitchStatement(Expression value, IImSeq<SwitchStatementCase> cases)
        {
            Value = value;
            Cases = cases ?? Constants.EmptySwitchStatementCases;
        }

        public override StatementFlavor Flavor { get { return StatementFlavor.Switch; } }

        public override bool NeverReturns
        {
            get { return Cases.All(c => c.Body.NeverReturns); }
        }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage)
        {
            Value.AccumUsage(compEnv, isAlwaysUsed, usage, false);
            usage.Merge(Cases.Select(c => c.Body.Usage(compEnv)).ToSeq());
        }

        public override ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            Value.AccumEffects(fxCtxt, callCtxt, evalTimes);
            var effects = new Seq<JST.Effects>(Cases.Count);
            var resCf = ControlFlow.AlwaysReturn;
            foreach (var c in Cases)
            {
                var caseCtxt = fxCtxt.Fork();
                resCf = resCf.Lub(c.AccumEffects(caseCtxt, callCtxt, evalTimes));
                effects.Add(caseCtxt.AccumEffects);
            }
            foreach (var e in effects)
                fxCtxt.IncludeEffects(e);
            return resCf;
        }

        public override void Simplify(SimplifierContext ctxt)
        {
            var locCtxt = ctxt.InLocalEffects();
            var simpValue = Value.Simplify(locCtxt);
            var simpCases = Cases.Select(c => c.Simplify(ctxt)).ToSeq();
            ctxt.Add(new SwitchStatement(simpValue, simpCases));
        }

        public override ReturnResult ToReturnResult(ISeq<Statement> acc)
        {
            var cases = new Seq<SwitchStatementCase>(Cases.Count);
            foreach (var c in Cases.Select(c => c.ToReturnResultCase()))
            {
                if (c == null)
                    return ReturnResult.Fail;
                cases.Add(c);
            }
            acc.Add(new SwitchStatement(Value, cases));
            return ReturnResult.None;
        }

        public override int GetHashCode()
        {
            var res = 0xb56f74e8u;
            res ^= (uint)Value.GetHashCode();
            foreach (var c in Cases)
                res = Constants.Rot7(res) ^ (uint)c.GetHashCode();
            return (int)res;
        }

        protected override bool EqualBody(Statement other)
        {
            var swtch = (SwitchStatement)other;
            if (!Value.Equals(swtch.Value) || Cases.Count != swtch.Cases.Count)
                return false;
            for (var i = 0; i < Cases.Count; i++)
            {
                if (!Cases[i].Equals(swtch.Cases[i]))
                    return false;
            }
            return true;
        }

        public override void Append(CSTWriter w)
        {
            w.Append("switch (");
            Value.Append(w, 0);
            w.Append(") {");
            w.EndLine();
            w.Indented
                (w2 =>
                 {
                     foreach (var c in Cases)
                         c.Append(w2, false);
                     foreach (var c in Cases)
                         c.Append(w2, true);
                 });
            w.Append('}');
        }
    }

    // ----------------------------------------------------------------------
    // DoWhileStatement
    // ----------------------------------------------------------------------

    public class DoWhileStatement : Statement
    {
        [NotNull]
        public readonly Statements Body;
        [NotNull]
        public readonly Expression Condition;

        public DoWhileStatement(Statements body, Expression condition)
        {
            Body = body;
            Condition = condition;
        }

        public override StatementFlavor Flavor { get { return StatementFlavor.DoWhile; } }

        // Assume the loop terminates
        public override bool NeverReturns { get { return false; } }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage)
        {
            usage.Merge(Body.Usage(compEnv), true);
            Condition.AccumUsage(compEnv, true, usage, false);
        }

        public override ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            // Give up trying to track effects
            if (callCtxt != null)
                evalTimes = evalTimes.Lub(EvalTimes.AtLeastOnce);
            fxCtxt.IncludeEffects(JST.Effects.Top);
            var bodyCf = Body.AccumEffects(fxCtxt, callCtxt, evalTimes);
            Condition.AccumEffects(fxCtxt, callCtxt, EvalTimes.Top);
            return bodyCf;
        }

        public override void Simplify(SimplifierContext ctxt)
        {
            var simpBody = Body.Simplify(ctxt);
            var condCtxt = ctxt.InNoStatements();
            var simpCondition = Condition.Simplify(condCtxt);
            ctxt.Add(new DoWhileStatement(simpBody, simpCondition));
        }

        public override ReturnResult ToReturnResult(ISeq<Statement> acc)
        {
            var body = Body.CheckNoReturn();
            if (body == null)
                return ReturnResult.Fail;
            acc.Add(new DoWhileStatement(body, Condition));
            return ReturnResult.None;
        }

        public override int GetHashCode()
        {
            var res = 0x18acf3d6u;
            res = Constants.Rot3(res) ^ (uint)Body.GetHashCode();
            res ^= (uint)Condition.GetHashCode();
            return (int)res;
        }

        protected override bool EqualBody(Statement other)
        {
            var dw = (DoWhileStatement)other;
            return Body.Equals(dw.Body) && Condition.Equals(dw.Condition);
        }

        public override void Append(CSTWriter w)
        {
            w.Append("do {");
            w.EndLine();
            w.Indented(Body.Append);
            w.Append("} while (");
            Condition.Append(w, 0);
            w.Append(')');
        }
    }

    // ----------------------------------------------------------------------
    // WhileDoStatement
    // ----------------------------------------------------------------------

    public class WhileDoStatement : Statement
    {
        [NotNull]
        public readonly Expression Condition;
        [NotNull]
        public readonly Statements Body;

        public WhileDoStatement(Expression condition, Statements body)
        {
            Condition = condition;
            Body = body;
        }

        public override StatementFlavor Flavor { get { return StatementFlavor.WhileDo; } }

        // Assume the loop terminates
        public override bool NeverReturns { get { return false; } }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage)
        {
            Condition.AccumUsage(compEnv, true, usage, false);
            usage.Merge(Body.Usage(compEnv), true);
        }

        public override ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            // Give up trying to track effects
            if (callCtxt != null)
                evalTimes = evalTimes.Lub(EvalTimes.AtLeastOnce);
            fxCtxt.IncludeEffects(JST.Effects.Top);
            Condition.AccumEffects(fxCtxt, callCtxt, evalTimes);
            return Body.AccumEffects(fxCtxt, callCtxt, EvalTimes.Top);
        }

        public override void Simplify(SimplifierContext ctxt)
        {
            var condCtxt = ctxt.InNoStatements();
            var simpCondition = Condition.Simplify(condCtxt);
            var simpBody = Body.Simplify(ctxt);
            ctxt.Add(new WhileDoStatement(simpCondition, simpBody));
        }

        public override ReturnResult ToReturnResult(ISeq<Statement> acc)
        {
            var body = Body.CheckNoReturn();
            if (body == null)
                return ReturnResult.Fail;
            acc.Add(new WhileDoStatement(Condition, body));
            return ReturnResult.None;
        }

        public override int GetHashCode()
        {
            var res = 0xce89e299u;
            res ^= (uint)Condition.GetHashCode();
            res = Constants.Rot3(res) ^ (uint)Body.GetHashCode();
            return (int)res;
        }

        protected override bool EqualBody(Statement other)
        {
            var wd = (WhileDoStatement)other;
            return Condition.Equals(wd.Condition) && Body.Equals(wd.Condition);
        }

        public override void Append(CSTWriter w)
        {
            w.Append("while (");
            Condition.Append(w, 0);
            w.Append(") do {");
            w.EndLine();
            w.Indented(Body.Append);
            w.Append('}');
        }
    }

    // ----------------------------------------------------------------------
    // InitializeObjectStatement
    // ----------------------------------------------------------------------

    public class InitializeObjectStatement : Statement
    {
        [NotNull]
        public readonly Expression Address;

        public InitializeObjectStatement(Expression address)
        {
            Address = address;
        }

        public override StatementFlavor Flavor { get { return StatementFlavor.InitializeObject; } }

        public override bool NeverReturns { get { return false; } }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage)
        {
            Address.AccumUsage(compEnv, isAlwaysUsed, usage, false);
        }

        public override ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            Address.AccumEffects(fxCtxt, callCtxt, evalTimes);
            return ControlFlow.AlwaysReturn;
        }

        public override void Simplify(SimplifierContext ctxt)
        {
            var locCtxt = ctxt.InLocalEffects();
            var simpAddress = Address.Simplify(locCtxt);
            ctxt.Add(new InitializeObjectStatement(simpAddress));
        }

        public override ReturnResult ToReturnResult(ISeq<Statement> acc)
        {
            acc.Add(this);
            return ReturnResult.None;
        }

        public override int GetHashCode()
        {
            var res = 0xfd13e0b7u;
            res ^= (uint)Address.GetHashCode();
            return (int)res;
        }

        protected override bool EqualBody(Statement other)
        {
            var io = (InitializeObjectStatement)other;
            return Address.Equals(io.Address);
        }

        public override void Append(CSTWriter w)
        {
            w.Append("initialize(");
            Address.Append(w, 0);
            w.Append(");");
        }
    }

    // ----------------------------------------------------------------------
    // TryStatement
    // ----------------------------------------------------------------------

    public class TryStatement : Statement
    {
        [NotNull]
        public readonly Statements Body;
        [NotNull]
        public readonly IImSeq<TryStatementHandler> Handlers;

        public TryStatement(Statements body, IImSeq<TryStatementHandler> handlers)
        {
            Body = body;
            Handlers = handlers ?? Constants.EmptyTryStatementHandlers;
        }

        public override StatementFlavor Flavor { get { return StatementFlavor.Try; } }

        public override bool NeverReturns { get { return false; } }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage)
        {
            usage.Merge(Body.Usage(compEnv), isAlwaysUsed);
            foreach (var h in Handlers)
                h.AccumUsage(compEnv, isAlwaysUsed, usage);
        }

        public override ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            // Give up trying to track effects
            fxCtxt.IncludeEffects(JST.Effects.Top);
            Body.AccumEffects(fxCtxt, callCtxt, evalTimes);
            foreach (var h in Handlers)
                h.AccumEffects(fxCtxt, callCtxt, evalTimes);
            return ControlFlow.Top;
        }

        public override void Simplify(SimplifierContext ctxt)
        {
            var simpBody = Body.Simplify(ctxt);
            var simpHandlers = Handlers.Select(h => h.Simplify(ctxt)).ToSeq();
            ctxt.Add(new TryStatement(simpBody, simpHandlers));
        }

        public override ReturnResult ToReturnResult(ISeq<Statement> acc)
        {
            var body = Body.CheckNoReturn();
            if (body == null)
                return ReturnResult.Fail;
            var handlers = new Seq<TryStatementHandler>(Handlers.Count);
            foreach (var h in Handlers.Select(h => h.ToReturnResultHandler()))
            {
                if (h == null)
                    return ReturnResult.Fail;
                handlers.Add(h);
            }
            acc.Add(new TryStatement(body, handlers));
            return ReturnResult.None;
        }

        public override int GetHashCode()
        {
            var res = 0xd2ada8d9u;
            res ^= (uint)Body.GetHashCode();
            foreach (var h in Handlers)
                res = Constants.Rot7(res) ^ (uint)h.GetHashCode();
            return (int)res;
        }

        protected override bool EqualBody(Statement other)
        {
            var t = (TryStatement)other;
            if (!Body.Equals(t.Body) || Handlers.Count != t.Handlers.Count)
                return false;
            for (var i = 0; i < Handlers.Count; i++)
            {
                if (!Handlers[i].Equals(t.Handlers[i]))
                    return false;
            }
            return true;
        }

        public override void Append(CSTWriter w)
        {
            w.Append("try {");
            w.EndLine();
            w.Indented(Body.Append);
            w.Append('}');
            foreach (var h in Handlers)
            {
                w.EndLine();
                h.Append(w);
            }
        }
    }

    public abstract class TryStatementHandler : IEquatable<TryStatementHandler>
    {
        [NotNull]
        public readonly Statements Body;

        protected TryStatementHandler(Statements body)
        {
            Body = body;
        }

        public abstract HandlerFlavor Flavor { get; }

        public abstract TryStatementHandler ToReturnResultHandler();

        public abstract bool IsCatchAll(RootEnvironment rootEnv);

        public abstract void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage);

        public abstract ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes);

        public abstract TryStatementHandler Simplify(SimplifierContext ctxt);

        public override bool Equals(object obj)
        {
            var tsh = obj as TryStatementHandler;
            return tsh != null && Equals(tsh);
        }

        public override int GetHashCode()
        {
            return Body.GetHashCode();
        }

        public bool Equals(TryStatementHandler h)
        {
            return Flavor == h.Flavor && EqualBody(h);
        }

        protected virtual bool EqualBody(TryStatementHandler other)
        {
            return Body.Equals(other.Body);
        }

        public abstract void Append(CSTWriter w);
    }

    public class TryStatementCatchHandler : TryStatementHandler
    {
        [CanBeNull] // null => empty handler body
        public readonly JST.Identifier ExceptionId;
        [NotNull]
        public readonly TypeRef Type;

        public TryStatementCatchHandler(Statements body, JST.Identifier exceptionId, TypeRef type)
            : base(body)
        {
            ExceptionId = exceptionId;
            Type = type;
        }

        public override HandlerFlavor Flavor { get { return HandlerFlavor.Catch; } }

        public override bool IsCatchAll(RootEnvironment rootEnv)
        {
            return Type.IsEquivalentTo(rootEnv, rootEnv.Global.ExceptionRef);
        }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage)
        {
            if (ExceptionId != null)
                usage.SeenVariable(ExceptionId, false);
            compEnv.SubstituteType(Type).AccumUsage(usage, false);
            usage.Merge(Body.Usage(compEnv), false);
        }

        public override ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            if (callCtxt != null)
                evalTimes = evalTimes.Lub(EvalTimes.Opt);
            return Body.AccumEffects(fxCtxt, callCtxt, evalTimes);
        }

        public override TryStatementHandler Simplify(SimplifierContext ctxt)
        {
            var exId = ctxt.ApplyId(ExceptionId);
            var simpBody = Body.Simplify(ctxt);
            return new TryStatementCatchHandler(simpBody, exId, Type);
        }

        public override TryStatementHandler ToReturnResultHandler()
        {
            var body = Body.CheckNoReturn();
            if (body == null)
                return null;
            return new TryStatementCatchHandler(body, ExceptionId, Type);
        }

        public override int GetHashCode()
        {
            var res = 0x80957705u;
            res ^= (uint)base.GetHashCode();
            if (ExceptionId != null)
                res = Constants.Rot3(res) ^ (uint)ExceptionId.GetHashCode();
            res ^= (uint)Type.GetHashCode();
            return (int)res;
        }

        protected override bool EqualBody(TryStatementHandler other)
        {
            var ch = (TryStatementCatchHandler)other;
            if (!base.EqualBody(ch))
                return false;
            if ((ExceptionId == null) != (ch.ExceptionId == null))
                return false;
            if (ExceptionId != null && !ExceptionId.Equals(ch.ExceptionId))
                return false;
            return Type.Equals(ch.Type);
        }

        public override void Append(CSTWriter w)
        {
            w.Append("catch (");
            Type.Append(w);
            if (ExceptionId != null)
            {
                w.Append(' ');
                w.AppendName(ExceptionId.Value);
            }
            w.Append(") {");
            w.EndLine();
            w.Indented(Body.Append);
            w.Append('}');
        }
    }

    public class TryStatementFaultHandler : TryStatementHandler
    {
        public TryStatementFaultHandler(Statements body)
            : base(body)
        {
        }

        public override HandlerFlavor Flavor { get { return HandlerFlavor.Fault; } }

        public override bool IsCatchAll(RootEnvironment rootEnv)
        {
            return false;
        }

        public override TryStatementHandler Simplify(SimplifierContext ctxt)
        {
            var simpBody = Body.Simplify(ctxt);
            return new TryStatementFaultHandler(simpBody);
        }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage)
        {
            usage.Merge(Body.Usage(compEnv), false);
        }

        public override ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            if (callCtxt != null)
                evalTimes = evalTimes.Lub(EvalTimes.Opt);
            return Body.AccumEffects(fxCtxt, callCtxt, evalTimes);
        }

        public override TryStatementHandler ToReturnResultHandler()
        {
            var body = Body.CheckNoReturn();
            if (body == null)
                return null;
            return new TryStatementFaultHandler(body);
        }

        public override int GetHashCode()
        {
            var res = 0x211a1477u;
            res ^= (uint)base.GetHashCode();
            return (int)res;
        }

        public override void Append(CSTWriter w)
        {
            w.Append("fault {");
            w.EndLine();
            w.Indented(Body.Append);
            w.Append('}');
        }
    }

    public class TryStatementFinallyHandler : TryStatementHandler
    {
        public TryStatementFinallyHandler(Statements body)
            : base(body)
        {
        }

        public override HandlerFlavor Flavor { get { return HandlerFlavor.Finally; } }

        public override bool IsCatchAll(RootEnvironment rootEnv)
        {
            return false;
        }

        public override TryStatementHandler Simplify(SimplifierContext ctxt)
        {
            var simpBody = Body.Simplify(ctxt);
            return new TryStatementFinallyHandler(simpBody);
        }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage)
        {
            usage.Merge(Body.Usage(compEnv), isAlwaysUsed);
        }

        public override ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            return Body.AccumEffects(fxCtxt, callCtxt, evalTimes);
        }

        public override TryStatementHandler ToReturnResultHandler()
        {
            var body = Body.CheckNoReturn();
            if (body == null)
                return null;
            return new TryStatementFinallyHandler(body);
        }

        public override int GetHashCode()
        {
            var res = 0xe6ad2065u;
            res ^= (uint)base.GetHashCode();
            return (int)res;
        }

        public override void Append(CSTWriter w)
        {
            w.Append("finally {");
            w.EndLine();
            w.Indented(Body.Append);
            w.Append('}');
        }
    }

    // ----------------------------------------------------------------------
    // HandlePseudoStatement
    // ----------------------------------------------------------------------

    //
    // Pseudo-expressions and -statements are used for encoding the state machine of residual basic-block
    // control flow
    //

    // Represents entering generic exception handler
    public class HandlePseudoStatement : Statement
    {
        [NotNull]
        public readonly JST.Identifier StateId; // temporary holding machine state
        [NotNull]
        public readonly JST.Identifier ExceptionId; // temporary bound to exception object

        public HandlePseudoStatement(JST.Identifier stateId, JST.Identifier exceptionId)
        {
            StateId = stateId;
            ExceptionId = exceptionId;
        }

        public override StatementFlavor Flavor { get { return StatementFlavor.HandlePseudo; } }

        public override bool NeverReturns { get { return false; } }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage)
        {
            usage.SeenVariable(StateId, isAlwaysUsed);
            usage.SeenVariable(ExceptionId, isAlwaysUsed);
        }

        public override ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            fxCtxt.IncludeEffects(JST.Effects.Top);
            return ControlFlow.AlwaysReturn;
        }

        public override void Simplify(SimplifierContext ctxt)
        {
            var stId = ctxt.ApplyId(StateId);
            var exId = ctxt.ApplyId(ExceptionId);
            ctxt.Add(new HandlePseudoStatement(stId, exId));
        }

        public override ReturnResult ToReturnResult(ISeq<Statement> acc)
        {
            return ReturnResult.Fail;
        }

        public override int GetHashCode()
        {
            var res = 0xfb9d35cfu;
            res ^= (uint)StateId.GetHashCode();
            res = Constants.Rot7(res) ^ (uint)ExceptionId.GetHashCode();
            return (int)res;
        }

        protected override bool EqualBody(Statement other)
        {
            var h = (HandlePseudoStatement)other;
            return StateId.Equals(h.StateId) && ExceptionId.Equals(h.ExceptionId);
        }

        public override void Append(CSTWriter w)
        {
            w.Append("$Handle(");
            w.Append(StateId.Value);
            w.Append(", ");
            w.Append(ExceptionId.Value);
            w.Append(");");
        }

    }

    // ----------------------------------------------------------------------
    // PushTryPseudoStatement
    // ----------------------------------------------------------------------

    // Represents entering a try block with given handlers
    public class PushTryPseudoStatement : Statement
    {
        [NotNull]
        public readonly JST.Identifier StateId; // temporary holding machine state
        [NotNull]
        public readonly IImSeq<TryPseudoStatementHandler> Handlers;

        public PushTryPseudoStatement(JST.Identifier stateId, IImSeq<TryPseudoStatementHandler> handlers)
        {
            StateId = stateId;
            Handlers = handlers ?? Constants.EmptyTryPsuedoStatementHandlers;
        }

        public override StatementFlavor Flavor { get { return StatementFlavor.PushTryPseudo; } }

        public override bool NeverReturns { get { return false; } }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage)
        {
            usage.SeenVariable(StateId, true);
            foreach (var h in Handlers)
                h.AccumUsage(compEnv, isAlwaysUsed, usage);
        }

        public override ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            fxCtxt.IncludeEffects(JST.Effects.Top);
            return ControlFlow.AlwaysReturn;
        }

        public override void Simplify(SimplifierContext ctxt)
        {
            var stId = ctxt.ApplyId(StateId);
            ctxt.Add(new PushTryPseudoStatement(stId, Handlers.Select(h => h.Simplify(ctxt)).ToSeq()));
        }

        public override ReturnResult ToReturnResult(ISeq<Statement> acc)
        {
            return ReturnResult.Fail;
        }

        public override int GetHashCode()
        {
            var res = 0xebcdaf0cu;
            res ^= (uint)StateId.GetHashCode();
            foreach (var h in Handlers)
                res = Constants.Rot3(res) ^ (uint)h.GetHashCode();
            return (int)res;
        }

        protected override bool EqualBody(Statement other)
        {
            var pt = (PushTryPseudoStatement)other;
            if (!StateId.Equals(pt.StateId) || Handlers.Count != pt.Handlers.Count)
                return false;
            for (var i = 0; i < Handlers.Count; i++)
            {
                if (!Handlers[i].Equals(pt.Handlers[i]))
                    return false;
            }
            return true;
        }

        public override void Append(CSTWriter w)
        {
            w.Append("$PushTry(");
            for (var i = 0; i < Handlers.Count; i++)
            {
                if (i > 0)
                    w.Append(", ");
                Handlers[i].Append(w);
            }
            w.Append(");");
        }
    }

    public abstract class TryPseudoStatementHandler : IEquatable<TryPseudoStatementHandler>
    {
        public readonly int HandlerId; // state number for entering handler

        protected TryPseudoStatementHandler(int handlerId)
        {
            HandlerId = handlerId;
        }

        public abstract HandlerFlavor Flavor { get; }

        public abstract void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage);

        public abstract TryPseudoStatementHandler Simplify(SimplifierContext ctxt);

        public override bool Equals(object obj)
        {
            var th = obj as TryPseudoStatementHandler;
            return th != null && Equals(th);
        }

        public override int GetHashCode()
        {
            throw new InvalidOperationException("should have been overridden");
        }

        public bool Equals(TryPseudoStatementHandler other)
        {
            return Flavor == other.Flavor && EqualBody(other);
        }

        protected abstract bool EqualBody(TryPseudoStatementHandler other);

        public abstract void Append(CSTWriter w);
    }

    public class CatchTryPseudoStatementHandler : TryPseudoStatementHandler
    {
        [NotNull]
        public readonly TypeRef ExceptionType; // type of exception to match
        [NotNull]
        public readonly JST.Identifier ExceptionId; // temporary which handler expects to be bound to exception

        public CatchTryPseudoStatementHandler(int handlerId, TypeRef exceptionType, JST.Identifier exceptionId)
            : base(handlerId)
        {
            ExceptionType = exceptionType;
            ExceptionId = exceptionId;
        }

        public override HandlerFlavor Flavor { get { return HandlerFlavor.Catch; } }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage)
        {
            compEnv.SubstituteType(ExceptionType).AccumUsage(usage, isAlwaysUsed);
            usage.SeenVariable(ExceptionId, isAlwaysUsed);
        }

        public override TryPseudoStatementHandler Simplify(SimplifierContext ctxt)
        {
            var exId = ctxt.ApplyId(ExceptionId);
            return new CatchTryPseudoStatementHandler(HandlerId, ExceptionType, exId);
        }

        public override int GetHashCode()
        {
            var res = 0x00250e2du;
            res ^= (uint)HandlerId;
            res = Constants.Rot3(res) ^ (uint)ExceptionType.GetHashCode();
            res = Constants.Rot3(res) ^ (uint)ExceptionId.GetHashCode();
            return (int)res;
        }

        protected override bool EqualBody(TryPseudoStatementHandler other)
        {
            var ch = (CatchTryPseudoStatementHandler)other;
            return HandlerId == ch.HandlerId && ExceptionType.Equals(ch.ExceptionType) && ExceptionId.Equals(ch.ExceptionId);
        }

        public override void Append(CSTWriter w)
        {
            w.Append("$Catch(");
            ExceptionType.Append(w);
            w.Append(", ");
            w.Append(ExceptionId.Value);
            w.Append(", ");
            w.Append(HandlerId);
            w.Append(")");
        }
    }

    public class FaultTryPseudoStatementHandler : TryPseudoStatementHandler
    {
        public FaultTryPseudoStatementHandler(int handlerId)
            : base(handlerId)
        {
        }

        public override HandlerFlavor Flavor { get { return HandlerFlavor.Fault; } }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage)
        {
        }

        public override TryPseudoStatementHandler Simplify(SimplifierContext ctxt)
        {
            return this;
        }

        public override int GetHashCode()
        {
            var res = 0x226800bbu;
            res ^= (uint)HandlerId;
            return (int)res;
        }

        protected override bool EqualBody(TryPseudoStatementHandler other)
        {
            var fh = (FaultTryPseudoStatementHandler)other;
            return HandlerId == fh.HandlerId;
        }

        public override void Append(CSTWriter w)
        {
            w.Append("$Fault(");
            w.Append(HandlerId);
            w.Append(")");
        }
    }

    public class FinallyTryPseudoStatementHandler : TryPseudoStatementHandler
    {
        public FinallyTryPseudoStatementHandler(int handlerId)
            : base(handlerId)
        {
        }

        public override HandlerFlavor Flavor { get { return HandlerFlavor.Finally; } }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage)
        {
        }

        public override TryPseudoStatementHandler Simplify(SimplifierContext ctxt)
        {
            return this;
        }

        public override int GetHashCode()
        {
            var res = 0x57b8e0afu;
            res ^= (uint)HandlerId;
            return (int)res;
        }

        protected override bool EqualBody(TryPseudoStatementHandler other)
        {
            var fh = (FinallyTryPseudoStatementHandler)other;
            return HandlerId == fh.HandlerId;
        }

        public override void Append(CSTWriter w)
        {
            w.Append("$Finally(");
            w.Append(HandlerId);
            w.Append(")");
        }
    }

    // ----------------------------------------------------------------------
    // LeavePseudoStatement
    // ----------------------------------------------------------------------

    // Represents leaving one or more try or catch blocks
    public class LeavePseudoStatement : Statement
    {
        [NotNull]
        public readonly JST.Identifier StateId; // temporary holding machine state
        public readonly int PopCount; // number of try contexts to pop
        public readonly int TargetId; // state number of target basic block

        public LeavePseudoStatement(JST.Identifier stateId, int popCount, int targetIdId)
        {
            StateId = stateId;
            PopCount = popCount;
            TargetId = targetIdId;
        }

        public override StatementFlavor Flavor { get { return StatementFlavor.LeavePseudo; } }

        public override bool NeverReturns { get { return false; } }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage)
        {
            usage.SeenVariable(StateId, isAlwaysUsed);
        }

        public override ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            fxCtxt.IncludeEffects(JST.Effects.Top);
            return ControlFlow.AlwaysReturn;
        }

        public override void Simplify(SimplifierContext ctxt)
        {
            var stId = ctxt.ApplyId(StateId);
            ctxt.Add(new LeavePseudoStatement(stId, PopCount, TargetId));
        }

        public override ReturnResult ToReturnResult(ISeq<Statement> acc)
        {
            return ReturnResult.Fail;
        }

        public override int GetHashCode()
        {
            var res = 0x2464369bu;
            res ^= (uint)StateId.GetHashCode();
            res = Constants.Rot7(res) ^ (uint)PopCount;
            res = Constants.Rot7(res) ^ (uint)TargetId;
            return (int)res;
        }

        protected override bool EqualBody(Statement other)
        {
            var l = (LeavePseudoStatement)other;
            return StateId.Equals(l.StateId) && PopCount == l.PopCount && TargetId == l.TargetId;
        }

        public override void Append(CSTWriter w)
        {
            w.Append("$Leave(");
            w.Append(StateId.Value);
            w.Append(", ");
            w.Append(PopCount);
            w.Append(", ");
            w.Append(TargetId);
            w.Append(");");
        }
    }

    // ----------------------------------------------------------------------
    // EndPseudoStatement
    // ----------------------------------------------------------------------

    // Represents ending a fault or finally block
    public class EndPseudoStatement : Statement
    {
        [NotNull]
        public readonly JST.Identifier StateId; // temporary holding machine state

        public EndPseudoStatement(JST.Identifier stateId)
        {
            StateId = stateId;
        }

        public override StatementFlavor Flavor { get { return StatementFlavor.EndPseudo; } }

        public override bool NeverReturns { get { return false; } }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage)
        {
            usage.SeenVariable(StateId, isAlwaysUsed);
        }

        public override ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            fxCtxt.IncludeEffects(JST.Effects.Top);
            return ControlFlow.AlwaysReturn;
        }

        public override void Simplify(SimplifierContext ctxt)
        {
            var stId = ctxt.ApplyId(StateId);
            ctxt.Add(new EndPseudoStatement(stId));
        }

        public override ReturnResult ToReturnResult(ISeq<Statement> acc)
        {
            return ReturnResult.Fail;
        }

        public override int GetHashCode()
        {
            var res = 0xf009b91eu;
            res ^= (uint)StateId.GetHashCode();
            return (int)res;
        }

        protected override bool EqualBody(Statement other)
        {
            var e = (EndPseudoStatement)other;
            return StateId.Equals(e.StateId);
        }

        public override void Append(CSTWriter w)
        {
            w.Append("$End(");
            w.Append(StateId.Value);
            w.Append(");");
        }
    }

    // ----------------------------------------------------------------------
    // GotoPseudoStatement
    // ----------------------------------------------------------------------

    // Represents a transition to a new basic block
    public class GotoPseudoStatement : Statement
    {
        [NotNull]
        public readonly JST.Identifier StateId; // temporary holding machine state
        public readonly int TargetId;  // state number of target basic block

        public GotoPseudoStatement(JST.Identifier stateId, int targetId)
        {
            StateId = stateId;
            TargetId = targetId;
        }

        public override StatementFlavor Flavor { get { return StatementFlavor.GotoPseudo; } }

        public override bool NeverReturns { get { return false; } }

        public override void AccumUsage(CompilationEnvironment compEnv, bool isAlwaysUsed, Usage usage)
        {
            usage.SeenVariable(StateId, isAlwaysUsed);
        }

        public override ControlFlow AccumEffects(EffectsContext fxCtxt, CallContext callCtxt, EvalTimes evalTimes)
        {
            fxCtxt.IncludeEffects(JST.Effects.Top);
            return ControlFlow.AlwaysReturn;
        }

        public override void Simplify(SimplifierContext ctxt)
        {
            var stId = ctxt.ApplyId(StateId);
            ctxt.Add(new GotoPseudoStatement(stId, TargetId));
        }

        public override ReturnResult ToReturnResult(ISeq<Statement> acc)
        {
            return ReturnResult.Fail;
        }

        public override int GetHashCode()
        {
            var res = 0x5563911du;
            res ^= (uint)StateId.GetHashCode();
            res = Constants.Rot7(res) ^ (uint)TargetId;
            return (int)res;
        }

        protected override bool EqualBody(Statement other)
        {
            var g = (GotoPseudoStatement)other;
            return StateId.Equals(g.StateId) && TargetId == g.TargetId;
        }

        public override void Append(CSTWriter w)
        {
            w.Append("$Goto(");
            w.Append(StateId.Value);
            w.Append(", ");
            w.Append(TargetId);
            w.Append(");");
        }
    }
}