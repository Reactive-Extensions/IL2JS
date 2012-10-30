using System;
using Microsoft.LiveLabs.Extras;
using JST = Microsoft.LiveLabs.JavaScript.JST;

namespace Microsoft.LiveLabs.CST
{
    public interface ISimplifierDatabase
    {
        // Is method imported and small enough to inline?
        bool IsInlinableImported(MethodRef methodRef);
        // Is method not imported and small enough to inline?
        bool IsInlinable(MethodRef methodRef);
        // Is method an imported constructor which should be re-interpreted as a factory method?
        // (Ie no 'this' is passed as first argument, and the constructed instance is returned)
        bool IsFactory(MethodRef methodRef);
        // Should the idx'th argument of method have its interop suppressed?
        bool IsNoInteropParameter(MethodRef methodRef, int idx);
        // Should the return result of method have its interop suppressed?
        bool IsNoInteropResult(MethodRef methodRef);
    }

    public class SimplifierContext
    {
        // Environment for outermost method being simplified, which includes all variables used as
        // parameters and locals
        [NotNull]
        public readonly CompilationEnvironment CompEnv;
        // Name supply for alpha-conversion
        [NotNull]
        public readonly JST.NameSupply NameSupply;
        // Map variable names to their replacement values
        [NotNull]
        protected readonly Map<JST.Identifier, Expression> subst;
        // Statements accumulated during simplification
        [CanBeNull] // null => in expression context without surrounding statements
        protected readonly Seq<Statement> statements;
        // Effects of expressions between stataments and current context
        [NotNull]
        protected JST.Effects contextEffects;
        [NotNull]
        public readonly ISimplifierDatabase Database;

        [CanBeNull] // null => no tracing 
        public readonly CSTWriter Trace;

        protected SimplifierContext
            (CompilationEnvironment compEnv,
             JST.NameSupply nameSupply,
             Map<JST.Identifier, Expression> subst,
             Seq<Statement> statements,
             JST.Effects contextEffects,
             ISimplifierDatabase database,
             CSTWriter trace)
        {
            CompEnv = compEnv;
            NameSupply = nameSupply;
            this.subst = subst;
            this.statements = statements;
            this.contextEffects = contextEffects;
            Database = database;
            Trace = trace;
        }

        public SimplifierContext
            (CompilationEnvironment compEnv,
             JST.NameSupply nameSupply,
             ISimplifierDatabase database,
             CSTWriter trace)
        {
            CompEnv = compEnv;
            NameSupply = nameSupply;
            subst = new Map<JST.Identifier, Expression>();
            statements = null;
            contextEffects = JST.Effects.Bottom;
            Database = database;
            Trace = trace;
        }

        public SimplifierContext InLocalEffects()
        {
            return new SimplifierContext
                (CompEnv,
                 NameSupply,
                 subst,
                 statements,
                 JST.Effects.Bottom,
                 Database,
                 Trace);
        }

        public SimplifierContext InSubMethod()
        {
            return new SimplifierContext
                (CompEnv,
                 NameSupply,
                 subst.ToMap(),
                 new Seq<Statement>(),
                 JST.Effects.Bottom,
                 Database,
                 Trace);
        }

        public SimplifierContext InFreshStatements()
        {
            return new SimplifierContext
                (CompEnv,
                 NameSupply,
                 subst,
                 new Seq<Statement>(),
                 JST.Effects.Bottom,
                 Database,
                 Trace);
        }

        public SimplifierContext InNoStatements()
        {
            return new SimplifierContext
                (CompEnv,
                 NameSupply,
                 subst,
                 null,
                 JST.Effects.Bottom,
                 Database,
                 Trace);
        }

        public ISeq<Statement> Statements { get { return statements; } }

        public void Add(Statement statement)
        {
            if (statements == null)
                throw new InvalidOperationException("no statements in context");
            statements.Add(statement);
        }

        public JST.Effects ContextEffects { get { return contextEffects; } }

        public void IncludeEffects(JST.Effects effects)
        {
            contextEffects = contextEffects.Lub(effects);
        }

        public JST.Identifier FreshenArgument(JST.Identifier id, TypeRef type)
        {
            var newid = NameSupply.GenSym();
            var cell = new VariableCell(newid);
            Bind(id, cell.Read());
            CompEnv.AddVariable(newid, ArgLocal.Local, true, true, type);
            return newid;
        }

        // Rename every non-parameter in given compilation environment and include new parameter in 
        // this compilation environment
        public void FreshenLocals(CompilationEnvironment inlinedCompEnv)
        {
            foreach (var kv in inlinedCompEnv.Variables)
            {
                if (kv.Value.ArgLocal == ArgLocal.Local)
                {
                    var newid = NameSupply.GenSym();
                    var cell = new VariableCell(newid);
                    Bind(kv.Value.Id, cell.Read());
                    CompEnv.AddVariable(newid, kv.Value.ArgLocal, kv.Value.IsInit, kv.Value.IsReadOnly, kv.Value.Type);
                }
            }
        }

        public void BindArgument(JST.Identifier id, Expression argument)
        {
            Bind(id, argument);
        }

        protected void Bind(JST.Identifier id, Expression e)
        {
            if (subst.ContainsKey(id))
                subst[id] = e;
            else
                subst.Add(id, e);
        }

        public Expression ApplyReadFrom(JST.Identifier id)
        {
            var e = default(Expression);
            if (subst.TryGetValue(id, out e))
                return e;
            else
                return new VariableCell(id).Read();
        }

        public Cell ApplyCell(JST.Identifier id)
        {
            var e = default(Expression);
            if (subst.TryGetValue(id, out e))
            {
                var r = e as ReadExpression;
                if (r != null)
                {
                    var cell = r.Address.IsAddressOfCell;
                    if (cell != null)
                        return cell;
                    // else: fall-through
                }
                // else: fall-through
            }
            // else: fall-through

            return new VariableCell(id);
        }

        public JST.Identifier ApplyId(JST.Identifier id)
        {
            var e = default(Expression);
            if (subst.TryGetValue(id, out e))
            {
                var r = e as ReadExpression;
                if (r != null)
                {
                    var cell = r.Address.IsAddressOfCell;
                    if (cell != null)
                    {
                        var id2 = cell.IsVariable;
                        if (id2 != null)
                            return id2;
                        // else: fall-through
                    }
                    // else: fall-through
                }
                // else: fall-through

            }
            // else: fall-through

            return id;
        }
    }
}